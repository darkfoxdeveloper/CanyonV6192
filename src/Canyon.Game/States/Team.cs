using System.Collections.Concurrent;
using System.Drawing;
using Canyon.Database.Entities;
using Canyon.Game.Services.Managers;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States.Events.Interfaces;
using Canyon.Game.States.User;
using Canyon.Game.States.World;
using Canyon.Network.Packets;

namespace Canyon.Game.States;

public sealed class Team
{
    public const int MAX_MEMBERS = 5;

    private DbVipTransPoint vipTransPoint;
    private Character leader;
    private readonly ConcurrentDictionary<uint, Character> players = new();
    private readonly TimeOut teamTeleportConfirmTimer = new();

    public Team(Character leader)
    {
        this.leader = leader;
        JoinEnable = true;
        MoneyEnable = true;
    }

    public Character Leader { get => leader; }

    public bool JoinEnable { get; set; }
    public bool MoneyEnable { get; set; }
    public bool ItemEnable { get; set; }
    public bool JewelEnable { get; set; }

    public ICollection<Character> Members => players.Values;

    public uint TeamId { get; private set; }
    public int MemberCount => players.Count;

    public ArenaStatus QualifierStatus { get; set; }

    ~Team()
    {
        TeamManager.Disband(TeamId);
    }

    public bool Create(uint teamId)
    {
        if (Leader.Team != null) return false;

        TeamId = teamId;
        players.TryAdd(Leader.Identity, Leader);
        Leader.Team = this;
        return true;
    }

    /// <summary>
    ///     Erase the team.
    /// </summary>
    public async Task<bool> DismissAsync(Character request, bool disconnect = false)
    {
        if (request.Identity != Leader.Identity)
        {
            await request.SendAsync(StrTeamDismissNoLeader);
            return false;
        }

        if (players.Count == 1) // only leader? disband
        {
            await SendAsync(new MsgTeam
            {
                Action = MsgTeam.TeamAction.Dismiss,
                Identity = Leader.Identity
            }, disconnect ? request.Identity : 0);

            var teamEvent = EventManager.QueryEvents<ITeamEvent>();
            foreach (Character member in players.Values)
            {
                foreach (ITeamEvent te in teamEvent)
                {
                    await te.OnLeaveTeamAsync(member);
                }
                member.Team = null;
            }

            TeamManager.Disband(TeamId);
        }
        else // has more members? pass leadership
        {
            await DismissMemberAsync(request);
            leader = players.Values.FirstOrDefault();
            await leader.AttachStatusAsync(leader, StatusSet.TEAM_LEADER, 0, int.MaxValue, 0);

            foreach (var member in players.Values)
            {
                await SendShowAsync(member);
            }
        }
        return true;
    }

    public async Task<bool> DismissMemberAsync(Character user)
    {
        if (!players.TryRemove(user.Identity, out _)) return false;

        var teamEvent = EventManager.QueryEvents<ITeamEvent>();
        foreach (ITeamEvent te in teamEvent)
        {
            await te.OnLeaveTeamAsync(user);
        }

        await SendAsync(new MsgTeam
        {
            Identity = user.Identity,
            Action = MsgTeam.TeamAction.LeaveTeam
        });
        user.Team = null;

        await user.OnLeaveTeamAsync();
        await SyncFamilyBattlePowerAsync();
        return true;
    }

    public async Task<bool> KickMemberAsync(Character leader, uint idTarget)
    {
        if (!IsLeader(leader.Identity) || !players.TryGetValue(idTarget, out Character target))
        {
            return false;
        }

        var teamEvent = EventManager.QueryEvents<ITeamEvent>();
        foreach (ITeamEvent te in teamEvent)
        {
            await te.OnLeaveTeamAsync(target);
        }

        await SendAsync(new MsgTeam
        {
            Identity = idTarget,
            Action = MsgTeam.TeamAction.Kick
        });

        players.TryRemove(idTarget, out _);
        target.Team = null;

        await target.OnLeaveTeamAsync();
        await SyncFamilyBattlePowerAsync();
        return true;
    }

    public async Task<bool> EnterTeamAsync(Character target)
    {
        if (!players.TryAdd(target.Identity, target)) return false;

        target.Team = this;
        await SendShowAsync(target);

        await target.SendAsync(string.Format(StrTeamSilver, MoneyEnable ? StrOpen : StrClose),
            TalkChannel.Talk);
        await target.SendAsync(string.Format(StrTeamItems, ItemEnable ? StrOpen : StrClose),
            TalkChannel.Talk);
        await target.SendAsync(string.Format(StrTeamGems, JewelEnable ? StrOpen : StrClose),
            TalkChannel.Talk);

        await SyncFamilyBattlePowerAsync();

        var teamEvent = EventManager.QueryEvents<ITeamEvent>();
        foreach (ITeamEvent te in teamEvent)
        {
            await te.OnJoinTeamAsync(Leader, target);
        }

        await ProcessAuraAsync();
        return true;
    }

    public bool IsLeader(uint id)
    {
        return Leader.Identity == id;
    }

    public bool IsMember(uint id)
    {
        return players.ContainsKey(id);
    }

    public async Task SendShowAsync(Character target)
    {
        MsgTeamMember msg = new()
        {
            Action = MsgTeamMember.ADD_MEMBER_B,
            Unknown1 = 1
        };
        foreach (Character member in players.Values.OrderBy(x => IsLeader(x.Identity) ? 0 : 1))
        {
            msg.Members.Add(new MsgTeamMember.TeamMember
            {
                Identity = member.Identity,
                Name = member.Name,
                Life = member.MaxLife,
                Lookface = member.Mesh,
                MaxLife = member.MaxLife
            });
        }

        MsgAuraGroup group = new()
        {
            Mode = MsgAuraGroup.AuraGroupMode.Leader,
            LeaderIdentity = Leader.Identity,
            Count = (uint)players.Count
        };
        foreach (Character member in players.Values)
        {
            group.Identity = member.Identity;
            await member.SendAsync(msg);
            await member.SendAsync(group);
        }
    }

    public Task SendAsync(string message, Color? color = null)
    {
        return SendAsync(new MsgTalk(0, TalkChannel.Team, color ?? Color.White, message));
    }

    public async Task SendAsync(IPacket msg, uint exclude = 0)
    {
        foreach (Character player in players.Values)
        {
            if (exclude == player.Identity) continue;

            await player.SendAsync(msg);
        }
    }

    public async Task BroadcastMemberLifeAsync(Character user, bool maxLife = false)
    {
        if (user == null || !IsMember(user.Identity)) return;

        MsgUserAttrib msg = new(user.Identity, ClientUpdateType.Hitpoints, user.Life);
        if (maxLife) msg.Append(ClientUpdateType.MaxHitpoints, user.MaxLife);

        foreach (Character member in players.Values)
            if (member.Identity != user.Identity)
                await member.SendAsync(msg);
    }

    public async Task AwardMemberExpAsync(uint idKiller, Role target, long exp)
    {
        if (target == null || exp == 0) return;

        if (!players.TryGetValue(idKiller, out Character killer)) return;

        foreach (Character user in players.Values)
        {
            if (user.Identity == idKiller) continue;

            if (!user.IsAlive) continue;

            if (user.MapIdentity != killer.MapIdentity) continue;

            if (user.GetDistance(killer) > Screen.VIEW_SIZE * 2) continue;

            DbLevelExperience dbExp = ExperienceManager.GetLevelExperience(user.Level);
            if (dbExp == null) continue;

            var addExp = user.AdjustExperience(target, exp, false);
            addExp = (long)Math.Min(dbExp.Exp, (ulong)addExp);
            addExp = Math.Max(1, Math.Min(user.Level * 360, addExp));

            addExp = (int)Math.Min(addExp, user.Level * 360);

            if (user.IsMate(killer) || user.IsApprentice(idKiller)) addExp *= 2;

            await user.AwardBattleExpAsync(addExp, true);
            await user.SendAsync(string.Format(StrTeamExperience, addExp));
        }
    }

    public int FamilyBattlePower(Character user, out uint idProvider)
    {
        idProvider = 0;
        if (!players.ContainsKey(user.Identity)) return 0;

        if (user.FamilyIdentity == 0) return 0;

        Character clanMember = players.Values
            .OrderByDescending(x => x.PureBattlePower)
            .FirstOrDefault(
                x => x.Identity != user.Identity &&
                     x.MapIdentity == user.MapIdentity &&
                     x.FamilyIdentity == user.FamilyIdentity);

        if (clanMember == null || clanMember.PureBattlePower <= user.PureBattlePower) return 0;

        if (user.IsArenicWitness() && !clanMember.IsArenicWitness())
        {
            return 0;
        }

        DbFamilyBattleEffectShareLimit limit = FamilyManager.GetSharedBattlePowerLimit(user.PureBattlePower);
        if (limit == null) return 0;

        idProvider = clanMember.Identity;
        var value = (int)((clanMember.PureBattlePower - user.PureBattlePower) *
                          (user.Family.SharedBattlePowerFactor / 100d));
        value = Math.Min(Math.Max(0, value), limit.ShareLimit);
        return value;
    }

    public async Task SyncFamilyBattlePowerAsync()
    {
        foreach (Character member in players.Values)
        {
            await member.SynchroFamilyBattlePowerAsync();
        }
    }

    private readonly int[] auraSkills =
    {
        StatusSet.TYRANT_AURA,
        StatusSet.FEND_AURA,
        StatusSet.METAL_AURA,
        StatusSet.WOOD_AURA,
        StatusSet.WATER_AURA,
        StatusSet.FIRE_AURA,
        StatusSet.EARTH_AURA,
        StatusSet.MAGIC_DEFENDER
    };

    public async Task ProcessAuraAsync()
    {
        foreach (var member in players.Values)
        {
            foreach (var i in auraSkills)
            {
                await ValidateAuraAsync(member, i);
            }

            foreach (var i in auraSkills)
            {
                IStatus aura = member.QueryStatus(i);
                if (aura == null || aura.CasterId != member.Identity)
                {
                    continue;
                }

                foreach (var target in players.Values.Where(x => x.Identity != member.Identity))
                {
                    IStatus targetAura = target.QueryStatus(i);
                    if (targetAura != null && targetAura.Level >= aura.Level)
                    {
                        continue;
                    }

                    if (member.MapIdentity != target.MapIdentity || member.GetDistance(target) > (aura.Magic?.Range ?? 30))
                    {
                        continue;
                    }

                    if (member.Map.IsArenicMapInGeneral())
                    {
                        if (member.IsArenicWitness())
                        {
                            continue;
                        }
                    }

                    await target.AttachStatusAsync(member, i, aura.Power, aura.RemainingTime, aura.RemainingTimes, aura.Magic);
                }
            }
        }
    }

    /// <summary>
    /// Validates if user can keep an aura.
    /// </summary>
    private async Task ValidateAuraAsync(Character user, int auraStatus)
    {
        IStatus aura = user.QueryStatus(auraStatus);
        if (aura == null)
        {
            return;
        }

        // it's my own aura, wont remove it
        if (aura.IsUserCast)
        {
            if (!user.IsAlive)
            {
                await user.DetachStatusAsync(auraStatus);
            }
            return;
        }

        // the owner has left the team
        if (!players.TryGetValue(aura.CasterId, out var owner))
        {
            await user.DetachStatusAsync(auraStatus);
            return;
        }

        if (owner.QueryStatus(auraStatus) == null)
        {
            await user.DetachStatusAsync(auraStatus);
            return;
        }

        if (!owner.IsAlive)
        {
                await user.DetachStatusAsync(auraStatus);
            return;
        }

        if (user.MapIdentity != owner.MapIdentity || user.GetDistance(owner) > (aura.Magic?.Range ?? 30))
        {
            await user.DetachStatusAsync(auraStatus);
            return;
        }

        if (user.Map.IsArenicMapInGeneral())
        {
            if (owner.IsArenicWitness())
            {
                await user.DetachStatusAsync(auraStatus);
                return;
            }
        }
    }

    public bool AllowTeamVipTeleport => !teamTeleportConfirmTimer.IsActive() || teamTeleportConfirmTimer.IsTimeOut();

    public void SetVipTeleportLocation(int seconds, DbVipTransPoint vipTransPoint)
    {
        if (!AllowTeamVipTeleport)
        {
            return;
        }

        teamTeleportConfirmTimer.Startup(seconds);
        this.vipTransPoint = vipTransPoint;
    }

    public DbVipTransPoint GetTransPoint()
    {
        if (!teamTeleportConfirmTimer.IsActive() || teamTeleportConfirmTimer.IsTimeOut())
        {
            return null;
        }
        return this.vipTransPoint;
    }
}