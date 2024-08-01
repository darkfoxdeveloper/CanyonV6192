using Canyon.Game.Services.Managers;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States.Events;
using Canyon.Game.States.Families;
using Canyon.Game.States.World;
using System.Drawing;

namespace Canyon.Game.States.User
{
    public partial class Character
    {
        public Family Family { get; set; }
        public FamilyMember FamilyMember => Family?.GetMember(Identity);

        public uint FamilyIdentity => Family?.Identity ?? 0;
        public string FamilyName => Family?.Name ?? StrNone;

        public Family.FamilyRank FamilyPosition => FamilyMember?.Rank ?? Family.FamilyRank.None;

        public async Task LoadFamilyAsync()
        {
            Family = FamilyManager.FindByUser(Identity);
            if (Family == null)
            {
                if (MateIdentity != 0)
                {
                    Family family = FamilyManager.FindByUser(MateIdentity);
                    FamilyMember mateFamily = family?.GetMember(MateIdentity);
                    if (mateFamily == null || mateFamily.Rank == Family.FamilyRank.Spouse)
                    {
                        return;
                    }

                    if (!await family.AppendMemberAsync(null, this, Family.FamilyRank.Spouse))
                    {
                        return;
                    }
                }
            }
            else
            {
                await SendFamilyAsync();
                await Family.SendRelationsAsync(this);
            }

            if (Family == null)
            {
                return;
            }

            var war = EventManager.GetEvent<FamilyWar>();
            if (war == null)
            {
                return;
            }

            if (Family.Challenge != 0)
            {
                GameMap map = MapManager.GetMap(Family.Challenge);
                if (map == null)
                {
                    return;
                }

                await SendAsync(string.Format(StrPrepareToChallengeFamilyLogin, map.Name), TalkChannel.Talk, Color.White);
            }

            if (Family.Occupy != 0)
            {
                GameMap map = MapManager.GetMap(Family.Occupy);
                if (map == null)
                {
                    return;
                }

                if (war.GetChallengersByNpc(Family.Occupy).Count == 0)
                {
                    return;
                }

                await SendAsync(string.Format(StrPrepareToDefendFamilyLogin, map.Name), TalkChannel.Talk, Color.White);
            }
        }

        private string FamilyOccupyString
        {
            get
            {
                var war = EventManager.GetEvent<FamilyWar>();
                if (war == null || Family == null)
                {
                    return "0 0 0 0 0 0 0 0";
                }

                uint idNpc = war.GetDominatingNpc(Family)?.Identity ?? 0;
                return "0 " +
                       $"{war.GetFamilyOccupyDays(Family.Identity)} " +
                       $"{war.GetNextReward(idNpc)} " +
                       $"{war.GetNextWeekReward(idNpc)} " +
                       $"{(war.IsNpcChallenged(Family.Occupy) ? 1 : 0)} " +
                       $"{(war.HasRewardToClaim(this) ? 1 : 0)} " +
                       $"{(Level < ExperienceManager.GetLevelLimit() && war.HasExpToClaim(this) ? 1 : 0)}";
            }
        }

        public string FamilyDominatedMap => Family != null ? EventManager.GetEvent<FamilyWar>()?.GetMapByNpc(Family.Occupy)?.Name ?? "" : "";

        public string FamilyChallengedMap => Family != null ? EventManager.GetEvent<FamilyWar>()?.GetMapByNpc(Family.Challenge) ?.Name ?? "" : "";

        public Task SendFamilyAsync()
        {
            if (Family == null)
            {
                return Task.CompletedTask;
            }

            var msg = new MsgFamily
            {
                Identity = FamilyIdentity,
                Action = MsgFamily.FamilyAction.Query
            };
            msg.Strings.Add(
                $"{Family.Identity} {Family.MembersCount} {Family.MembersCount} {Family.Money} {Family.Rank} {(int)FamilyPosition} 0 {Family.BattlePowerTower} 0 0 1 {FamilyMember.Proffer}");
            msg.Strings.Add(FamilyName);
            msg.Strings.Add(Name);
            msg.Strings.Add(FamilyOccupyString);
            msg.Strings.Add(FamilyDominatedMap);
            msg.Strings.Add(FamilyChallengedMap);
            return SendAsync(msg);
        }

        public Task SendFamilyOccupyAsync()
        {
            if (Family == null)
            {
                return Task.CompletedTask;
            }

            var msg = new MsgFamily
            {
                Identity = FamilyIdentity,
                Action = MsgFamily.FamilyAction.QueryOccupy
            };
            // uid occupydays reward nextreward challenged rewardtoclaim exptoclaim
            msg.Strings.Add(FamilyOccupyString);
            return SendAsync(msg);
        }

        public async Task SendNoFamilyAsync()
        {
            var msg = new MsgFamily
            {
                Identity = FamilyIdentity,
                Action = MsgFamily.FamilyAction.Query
            };
            msg.Strings.Add(FamilyOccupyString);
            msg.Strings.Add("");
            msg.Strings.Add(Name);
            await SendAsync(msg);

            msg.Action = MsgFamily.FamilyAction.Quit;
            await SendAsync(msg);
        }

        public async Task<bool> CreateFamilyAsync(string name, uint proffer)
        {
            if (Family != null)
            {
                return false;
            }

            if (!RoleManager.IsValidName(name))
            {
                return false;
            }

            if (name.Length > 15)
            {
                return false;
            }

            if (FamilyManager.GetFamily(name) != null)
            {
                return false;
            }

            if (!await SpendMoneyAsync((int)proffer, true))
            {
                return false;
            }

            Family = await Family.CreateAsync(this, name, proffer / 2);
            if (Family == null)
            {
                return false;
            }

            await SendFamilyAsync();
            await Family.SendRelationsAsync(this);
            return true;
        }

        public async Task<bool> ChangeFamilyNameAsync(string newName)
        {
            if (Family == null)
            {
                return false;
            }

            if (newName.Length > 15)
            {
                return false;
            }

            if (!RoleManager.IsValidName(newName))
            {
                return false;
            }

            if (FamilyManager.GetFamily(newName) != null)
            {
                return false;
            }

            await Family.ChangeNameAsync(newName);
            return true;
        }

        public async Task<bool> DisbandFamilyAsync()
        {
            if (Family == null)
            {
                return false;
            }

            if (FamilyPosition != Family.FamilyRank.ClanLeader)
            {
                return false;
            }

            if (Family.MembersCount > 1)
            {
                return false;
            }

            await FamilyMember.DeleteAsync();
            await Family.SoftDeleteAsync();

            Family = null;

            await SendNoFamilyAsync();
            return true;
        }

        public Task SynchroFamilyBattlePowerAsync()
        {
            if (Team == null || Family == null)
            {
                return Task.CompletedTask;
            }

            int bp = Team.FamilyBattlePower(this, out uint provider);
            var msg = new MsgUserAttrib(Identity, ClientUpdateType.FamilySharedBattlePower, provider);
            msg.Append(ClientUpdateType.FamilySharedBattlePower, (ulong)bp);
            return SendAsync(msg);
        }

        public int FamilyBattlePower => Team?.FamilyBattlePower(this, out _) ?? 0;
    }
}
