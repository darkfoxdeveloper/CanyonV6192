using Canyon.Database.Entities;
using Canyon.Game.Database;
using Canyon.Game.Database.Repositories;
using Canyon.Game.Services.Managers;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States.User;
using Canyon.Network.Packets;
using System.Collections.Concurrent;
using System.Drawing;
using System.Globalization;

namespace Canyon.Game.States.Families
{
    public sealed class Family
    {
        public const int MAX_MEMBERS = 6;
        public const int MAX_RELATION = 5;

        private string name;
        private DbFamily mFamily;
        private readonly ConcurrentDictionary<uint, FamilyMember> mMembers = new();
        private readonly ConcurrentDictionary<uint, Family> mAllies = new();
        private readonly ConcurrentDictionary<uint, Family> mEnemies = new();

        private Family()
        {
        }

        #region Static Creation

        public static async Task<Family> CreateAsync(Character leader, string name, uint money)
        {
            var dbFamily = new DbFamily
            {
                Announcement = StrFamilyDefaultAnnounce,
                Amount = 1,
                CreationDate = (uint)DateTime.Now.ToUnixTimestamp(),
                LeaderIdentity = leader.Identity,
                Money = money,
                Name = name,
                Rank = 0,
                AllyFamily0 = 0,
                AllyFamily1 = 0,
                AllyFamily2 = 0,
                AllyFamily3 = 0,
                AllyFamily4 = 0,
                EnemyFamily0 = 0,
                EnemyFamily1 = 0,
                EnemyFamily2 = 0,
                EnemyFamily3 = 0,
                EnemyFamily4 = 0,
                Challenge = 0,
                ChallengeMap = 0,
                CreateName = "",
                FamilyMap = 0,
                Occupy = 0,
                Repute = 0,
                StarTower = 0
            };

            if (!await ServerDbContext.SaveAsync(dbFamily))
            {
                return null;
            }

            var family = new Family
            {
                mFamily = dbFamily,
                name = name,
            };

            var fmLeader = await FamilyMember.CreateAsync(leader, family, FamilyRank.ClanLeader, money);
            if (fmLeader == null)
            {
                await ServerDbContext.DeleteAsync(dbFamily);
                return null;
            }

            family.mMembers.TryAdd(fmLeader.Identity, fmLeader);
            FamilyManager.AddFamily(family);

            await leader.SendFamilyAsync();
            await family.SendRelationsAsync(leader);

            await BroadcastWorldMsgAsync(string.Format(StrFamilyCreate, leader.Name, family.Name));
            return family;
        }

        public static async Task<Family> CreateAsync(DbFamily dbFamily)
        {
            var family = new Family { mFamily = dbFamily, name = dbFamily.Name };

            List<DbFamilyAttr> members = await FamilyAttrRepository.GetAsync(family.Identity);
            if (members == null)
            {
                return null;
            }

            foreach (DbFamilyAttr dbMember in members.OrderByDescending(x => x.Rank))
            {
                var member = await FamilyMember.CreateAsync(dbMember, family);
                if (member == null)
                {
                    await ServerDbContext.DeleteAsync(dbMember);
                    continue;
                }

                family.mMembers.TryAdd(member.Identity, member);
            }

            // validate our members
            foreach (FamilyMember member in family.mMembers.Values.Where(x => x.Rank == FamilyRank.Spouse))
            {
                FamilyMember mate = family.GetMember(member.MateIdentity);
                if (mate == null || mate.Rank == FamilyRank.Spouse)
                {
                    family.mMembers.TryRemove(member.Identity, out _);
                    await member.DeleteAsync();
                }
            }

            return family;
        }

        #endregion

        #region Properties

        public uint Identity => mFamily.Identity;
        public string Name => name;
        public int MembersCount => mMembers.Count;
        public int PureMembersCount => mMembers.Count(x => x.Value.Rank != FamilyRank.Spouse);
        public bool IsDeleted => mFamily.DeleteDate != 0;

        public uint LeaderIdentity => mFamily.LeaderIdentity;
        public FamilyMember Leader => mMembers.TryGetValue(LeaderIdentity, out FamilyMember value) ? value : null;

        public ulong Money
        {
            get => mFamily.Money;
            set => mFamily.Money = value;
        }

        public byte Rank
        {
            get => mFamily.Rank;
            set => mFamily.Rank = value;
        }

        public uint Reputation
        {
            get => mFamily.Repute;
            set => mFamily.Repute = value;
        }

        public string Announcement
        {
            get => mFamily.Announcement;
            set => mFamily.Announcement = value;
        }

        public byte BattlePowerTower
        {
            get => mFamily.StarTower;
            set => mFamily.StarTower = value;
        }

        public DateTime CreationDate => UnixTimestamp.ToDateTime(mFamily.CreationDate);

        #endregion

        #region Clan War

        public uint Challenge
        {
            get => mFamily.Challenge;
            set => mFamily.Challenge = value;
        }

        public uint Occupy
        {
            get => mFamily.Occupy;
            set => mFamily.Occupy = value;
        }

        #endregion

        #region Members

        public int SharedBattlePowerFactor
        {
            get
            {
                switch (BattlePowerTower)
                {
                    case 1: return 40;
                    case 2: return 50;
                    case 3: return 60;
                    case 4: return 70;
                    default:
                        return 30;
                }
            }
        }

        public FamilyMember GetMember(uint idMember)
        {
            return mMembers.TryGetValue(idMember, out FamilyMember value) ? value : null;
        }

        public FamilyMember GetMember(string name)
        {
            return mMembers.Values.FirstOrDefault(x => x.Name.Equals(name));
        }

        public async Task<bool> AppendMemberAsync(Character caller, Character target,
                                                  FamilyRank rank = FamilyRank.Member)
        {
            if (target.Family != null)
            {
                return false;
            }

            if (target.Level < 50 && rank != FamilyRank.Spouse)
            {
                return false;
            }

            if (mMembers.Values.Count(x => x.Rank != FamilyRank.Spouse) > MAX_MEMBERS && rank != FamilyRank.Spouse)
            {
                return false;
            }

            var member = await FamilyMember.CreateAsync(target, this, rank);
            if (member == null)
            {
                return false;
            }

            mMembers.TryAdd(member.Identity, member);

            target.Family = this;

            if (rank != FamilyRank.Spouse)
            {
                Character mateCharacter = RoleManager.GetUser(target.MateIdentity);
                if (mateCharacter != null)
                {
                    await AppendMemberAsync(caller, mateCharacter, FamilyRank.Spouse);
                }
            }

            await target.SendFamilyAsync();
            await SendRelationsAsync(target);
            await target.Screen.SynchroScreenAsync();
            return true;
        }

        public async Task<bool> LeaveAsync(Character user)
        {
            if (user.Family == null)
            {
                return false;
            }

            if (user.FamilyPosition == FamilyRank.ClanLeader)
            {
                return false;
            }

            if (user.FamilyPosition == FamilyRank.Spouse)
            {
                return false;
            }

            if (mMembers.TryRemove(user.Identity, out var member))
            {
                await member.DeleteAsync();
            }

            user.Family = null;
            await user.SendNoFamilyAsync();
            await user.Screen.SynchroScreenAsync();

            if (user.MateIdentity != 0)
            {
                FamilyMember mate = GetMember(user.MateIdentity);
                if (mate != null)
                {
                    await KickOutAsync(user, user.MateIdentity);
                }
            }

            return true;
        }

        public async Task<bool> KickOutAsync(Character caller, uint idTarget)
        {
            FamilyMember target = GetMember(idTarget);
            if (target == null)
            {
                return false;
            }

            if (target.Rank == FamilyRank.ClanLeader)
            {
                return false;
            }

            if (caller.FamilyPosition != FamilyRank.ClanLeader)
            {
                if (target.Rank != FamilyRank.Spouse || target.MateIdentity != caller.Identity)
                {
                    return false;
                }
            }

            mMembers.TryRemove(idTarget, out _);

            if (target.User != null)
            {
                target.User.Family = null;
                await target.User.SendNoFamilyAsync();
                await target.User.Screen.SynchroScreenAsync();
            }

            await target.DeleteAsync();

            FamilyMember mate = GetMember(target.MateIdentity);
            if (mate != null && mate.Rank == FamilyRank.Spouse)
            {
                await KickOutAsync(caller, mate.Identity);
            }

            return true;
        }

        public async Task<bool> AbdicateAsync(Character caller, string targetName)
        {
            if (caller.FamilyPosition != FamilyRank.ClanLeader)
            {
                return false;
            }

            FamilyMember target = GetMember(targetName);
            if (target == null)
            {
                return false;
            }

            if (caller.Identity == target.Identity)
            {
                return false;
            }

            if (target.FamilyIdentity != Identity)
            {
                return false;
            }

            if (target.User == null)
            {
                return false; // not online
            }

            if (target.Rank == FamilyRank.Spouse)
            {
                return false; // cannot abdicate for a spouse
            }

            target.Rank = FamilyRank.ClanLeader;
            caller.FamilyMember.Rank = FamilyRank.Member;

            await target.SaveAsync();
            await caller.FamilyMember.SaveAsync();

            await target.User.SendFamilyAsync();
            await caller.SendFamilyAsync();

            await target.User.Screen.SynchroScreenAsync();
            await caller.Screen.SynchroScreenAsync();

            await BroadcastWorldMsgAsync(string.Format(StrFamilyAbdicate, caller.Name, target.Name),
                                                TalkChannel.Family);
            return true;
        }

        #endregion

        #region Change Name

        public async Task<bool> ChangeNameAsync(string name)
        {
            this.mFamily.Name = name;
            return await SaveAsync();
        }

        #endregion

        #region Relations

        public void LoadRelations()
        {
            // Ally
            Family family = FamilyManager.GetFamily(mFamily.AllyFamily0);
            if (family != null)
            {
                mAllies.TryAdd(family.Identity, family);
            }
            else
            {
                mFamily.AllyFamily0 = 0;
            }

            family = FamilyManager.GetFamily(mFamily.AllyFamily1);
            if (family != null)
            {
                mAllies.TryAdd(family.Identity, family);
            }
            else
            {
                mFamily.AllyFamily1 = 0;
            }

            family = FamilyManager.GetFamily(mFamily.AllyFamily2);
            if (family != null)
            {
                mAllies.TryAdd(family.Identity, family);
            }
            else
            {
                mFamily.AllyFamily2 = 0;
            }

            family = FamilyManager.GetFamily(mFamily.AllyFamily3);
            if (family != null)
            {
                mAllies.TryAdd(family.Identity, family);
            }
            else
            {
                mFamily.AllyFamily3 = 0;
            }

            family = FamilyManager.GetFamily(mFamily.AllyFamily4);
            if (family != null)
            {
                mAllies.TryAdd(family.Identity, family);
            }
            else
            {
                mFamily.AllyFamily4 = 0;
            }

            // Enemies
            family = FamilyManager.GetFamily(mFamily.EnemyFamily0);
            if (family != null)
            {
                mAllies.TryAdd(family.Identity, family);
            }
            else
            {
                mFamily.EnemyFamily0 = 0;
            }

            family = FamilyManager.GetFamily(mFamily.EnemyFamily1);
            if (family != null)
            {
                mAllies.TryAdd(family.Identity, family);
            }
            else
            {
                mFamily.EnemyFamily1 = 0;
            }

            family = FamilyManager.GetFamily(mFamily.EnemyFamily2);
            if (family != null)
            {
                mAllies.TryAdd(family.Identity, family);
            }
            else
            {
                mFamily.EnemyFamily2 = 0;
            }

            family = FamilyManager.GetFamily(mFamily.EnemyFamily3);
            if (family != null)
            {
                mAllies.TryAdd(family.Identity, family);
            }
            else
            {
                mFamily.EnemyFamily3 = 0;
            }

            family = FamilyManager.GetFamily(mFamily.EnemyFamily4);
            if (family != null)
            {
                mAllies.TryAdd(family.Identity, family);
            }
            else
            {
                mFamily.EnemyFamily4 = 0;
            }
        }

        #endregion

        #region Allies

        public int AllyCount => mAllies.Count;

        public bool IsAlly(uint idAlly)
        {
            return mAllies.ContainsKey(idAlly);
        }

        public void SetAlly(Family ally)
        {
            uint idAlly = ally.Identity;

            if (mFamily.AllyFamily0 == 0)
            {
                mFamily.AllyFamily0 = idAlly;
            }
            else if (mFamily.AllyFamily1 == 0)
            {
                mFamily.AllyFamily1 = idAlly;
            }
            else if (mFamily.AllyFamily2 == 0)
            {
                mFamily.AllyFamily2 = idAlly;
            }
            else if (mFamily.AllyFamily3 == 0)
            {
                mFamily.AllyFamily3 = idAlly;
            }
            else if (mFamily.AllyFamily4 == 0)
            {
                mFamily.AllyFamily4 = idAlly;
            }
            else
            {
                return;
            }

            mAllies.TryAdd(idAlly, ally);
        }

        public void UnsetAlly(uint idAlly)
        {
            if (mFamily.AllyFamily0 == idAlly)
            {
                mFamily.AllyFamily0 = 0;
            }

            if (mFamily.AllyFamily1 == idAlly)
            {
                mFamily.AllyFamily1 = 0;
            }

            if (mFamily.AllyFamily2 == idAlly)
            {
                mFamily.AllyFamily2 = 0;
            }

            if (mFamily.AllyFamily3 == idAlly)
            {
                mFamily.AllyFamily3 = 0;
            }

            if (mFamily.AllyFamily4 == idAlly)
            {
                mFamily.AllyFamily4 = 0;
            }

            mAllies.TryRemove(idAlly, out _);
        }

        #endregion

        #region Enemies

        public int EnemyCount => mEnemies.Count;

        public bool IsEnemy(uint idEnemy)
        {
            return mEnemies.ContainsKey(idEnemy);
        }

        public void SetEnemy(Family enemy)
        {
            uint idEnemy = enemy.Identity;
            if (mFamily.EnemyFamily0 == 0)
            {
                mFamily.EnemyFamily0 = idEnemy;
            }
            else if (mFamily.EnemyFamily1 == 0)
            {
                mFamily.EnemyFamily1 = idEnemy;
            }
            else if (mFamily.EnemyFamily2 == 0)
            {
                mFamily.EnemyFamily2 = idEnemy;
            }
            else if (mFamily.EnemyFamily3 == 0)
            {
                mFamily.EnemyFamily3 = idEnemy;
            }
            else if (mFamily.EnemyFamily4 == 0)
            {
                mFamily.EnemyFamily4 = idEnemy;
            }
            else
            {
                return;
            }

            mEnemies.TryAdd(idEnemy, enemy);
        }

        public void UnsetEnemy(uint idEnemy)
        {
            if (mFamily.EnemyFamily0 == idEnemy)
            {
                mFamily.EnemyFamily0 = 0;
            }

            if (mFamily.EnemyFamily1 == idEnemy)
            {
                mFamily.EnemyFamily1 = 0;
            }

            if (mFamily.EnemyFamily2 == idEnemy)
            {
                mFamily.EnemyFamily2 = 0;
            }

            if (mFamily.EnemyFamily3 == idEnemy)
            {
                mFamily.EnemyFamily3 = 0;
            }

            if (mFamily.EnemyFamily4 == idEnemy)
            {
                mFamily.EnemyFamily4 = 0;
            }

            mEnemies.TryRemove(idEnemy, out _);
        }

        #endregion

        #region Socket

        public Task SendMembersAsync(int idx, Character target)
        {
            if (target.FamilyIdentity != Identity)
            {
                return Task.CompletedTask;
            }

            var msg = new MsgFamily
            {
                Identity = Identity,
                Action = MsgFamily.FamilyAction.QueryMemberList
            };

            foreach (FamilyMember member in mMembers.Values.OrderByDescending(x => x.IsOnline)
                                                     .ThenByDescending(x => x.Rank))
            {
                msg.Objects.Add(new MsgFamily.MemberListStruct
                {
                    Profession = member.Profession,
                    Donation = member.Proffer,
                    Name = member.Name,
                    Rank = (ushort)member.Rank,
                    Level = member.Level,
                    Online = member.IsOnline
                });
            }

            return target.SendAsync(msg);
        }

        public async Task SendRelationsAsync()
        {
            foreach (FamilyMember member in mMembers.Values.Where(x => x.IsOnline))
            {
                await SendRelationsAsync(member.User);
            }
        }

        public async Task SendRelationsAsync(Character target)
        {
            var msg = new MsgFamily
            {
                Identity = Identity,
                Action = MsgFamily.FamilyAction.SendAlly
            };
            foreach (Family ally in mAllies.Values)
            {
                msg.Objects.Add(new MsgFamily.RelationListStruct
                {
                    Name = ally.Name,
                    LeaderName = ally.Leader.Name
                });
            }

            await target.SendAsync(msg);

            msg = new MsgFamily
            {
                Identity = Identity,
                Action = MsgFamily.FamilyAction.SendEnemy
            };
            foreach (Family enemy in mEnemies.Values)
            {
                msg.Objects.Add(new MsgFamily.RelationListStruct
                {
                    Name = enemy.Name,
                    LeaderName = enemy.Leader.Name
                });
            }

            await target.SendAsync(msg);
        }

        public async Task SendAsync(string message, uint idIgnore = 0u, Color? color = null)
        {
            await SendAsync(new MsgTalk(0, TalkChannel.Family, color ?? Color.White, message), idIgnore);
        }

        public async Task SendAsync(IPacket msg, uint exclude = 0u)
        {
            foreach (FamilyMember player in mMembers.Values)
            {
                if (exclude == player.Identity || player.User == null)
                {
                    continue;
                }

                await player.User.SendAsync(msg);
            }
        }

        #endregion

        #region Database

        public Task<bool> SaveAsync()
        {
            return ServerDbContext.SaveAsync(mFamily);
        }

        public Task<bool> SoftDeleteAsync()
        {
            mFamily.DeleteDate = 1;
            return SaveAsync();
        }

        public Task<bool> DeleteAsync()
        {
            return ServerDbContext.DeleteAsync(mFamily);
        }

        #endregion

        public enum FamilyRank : ushort
        {
            ClanLeader = 100,
            Spouse = 11,
            Member = 10,
            None = 0
        }
    }
}
