using Canyon.Database.Entities;
using Canyon.Game.Database;
using Canyon.Game.Database.Repositories;
using Canyon.Game.Services.Managers;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States.Relationship;
using Canyon.Network.Packets;
using Canyon.Shared;
using System.Collections.Concurrent;
using System.Drawing;
using static Canyon.Game.Sockets.Game.Packets.MsgInteract;

namespace Canyon.Game.States.User
{
    public partial class Character
    {
        #region Marriage

        public bool IsMate(Character user)
        {
            return user.Identity == MateIdentity;
        }

        public bool IsMate(uint idMate)
        {
            return idMate == MateIdentity;
        }

        #endregion

        #region Team

        private readonly TimeOut teamLeaderPosTimer = new(3);

        public Team Team { get; set; }

        public override Team GetTeam()
        {
            return Team;
        }

        public async Task BroadcastTeamLifeAsync(bool maxLife = false)
        {
            if (Team != null)
            {
                await Team.BroadcastMemberLifeAsync(this, maxLife);
            }
        }

        public async Task OnLeaveTeamAsync()
        {

            for (int i = StatusSet.TYRANT_AURA; i <= StatusSet.EARTH_AURA; i++)
            {
                IStatus aura = QueryStatus(i);
                if (aura == null || aura.IsUserCast)
                    continue;

                await DetachStatusAsync(i);
            }

        }

        #endregion

        #region Friends

        private readonly ConcurrentDictionary<uint, Friend> friends = new();

        public int FriendAmount => friends.Count;

        public int MaxFriendAmount => 50;

        public async Task LoadRelationshipAsync()
        {
            foreach (DbFriend dbFriend in await FriendRepository.GetAsync(Identity))
            {
                var friend = new Friend(this);
                await friend.CreateAsync(dbFriend);
                AddFriend(friend);
            }

            foreach (DbEnemy dbEnemy in await EnemyRepository.GetAsync(Identity))
            {
                if (dbEnemy.TargetIdentity == Identity)
                {
                    await ServerDbContext.DeleteAsync(dbEnemy);
                    continue;
                }

                var enemy = new Enemy(this);
                await enemy.CreateAsync(dbEnemy);
                AddEnemy(enemy);
            }


        }

        public bool AddFriend(Friend friend)
        {
            return friends.TryAdd(friend.Identity, friend);
        }

        public async Task<bool> CreateFriendAsync(Character target)
        {
            if (IsFriend(target.Identity))
            {
                return false;
            }

            var friend = new Friend(this);
            if (!friend.Create(target))
            {
                return false;
            }

            var targetFriend = new Friend(target);
            if (!targetFriend.Create(this))
            {
                return false;
            }

            await friend.SaveAsync();
            await targetFriend.SaveAsync();
            await friend.SendAsync();
            await targetFriend.SendAsync();

            AddFriend(friend);
            target.AddFriend(targetFriend);

            await BroadcastRoomMsgAsync(string.Format(StrMakeFriend, Name, target.Name));
            return true;
        }

        public bool IsFriend(uint idTarget)
        {
            return friends.ContainsKey(idTarget);
        }

        public Friend GetFriend(uint idTarget)
        {
            return friends.TryGetValue(idTarget, out Friend friend) ? friend : null;
        }

        public async Task<bool> DeleteFriendAsync(uint idTarget, bool notify = false)
        {
            if (!IsFriend(idTarget) || !friends.TryRemove(idTarget, out Friend target))
            {
                return false;
            }

            if (target.Online)
            {
                await target.User.DeleteFriendAsync(Identity);
            }
            else
            {
                DbFriend targetFriend = await FriendRepository.GetAsync(Identity, idTarget);
                await using var ctx = new ServerDbContext();
                ctx.Remove(targetFriend);
                await ctx.SaveChangesAsync();
            }

            await target.DeleteAsync();

            await SendAsync(new MsgFriend
            {
                Identity = target.Identity,
                Name = target.Name,
                Action = MsgFriend.MsgFriendAction.RemoveFriend,
                Online = target.Online
            });

            if (notify)
            {
                await BroadcastRoomMsgAsync(string.Format(StrBreakFriend, Name, target.Name));
            }

            return true;
        }

        public async Task SendAllFriendAsync()
        {
            foreach (Friend friend in friends.Values)
            {
                await friend.SendAsync();
                if (friend.Online)
                {
                    await friend.User.SendAsync(new MsgFriend
                    {
                        Identity = Identity,
                        Name = Name,
                        Action = MsgFriend.MsgFriendAction.SetOnlineFriend,
                        Online = true,
                        Gender = Gender,
                        Nobility = (int)NobilityRank

                    });
                }
            }
        }

        public async Task NotifyOfflineFriendAsync()
        {
            foreach (Friend friend in friends.Values)
            {
                if (friend.Online)
                {
                    await friend.User.SendAsync(new MsgFriend
                    {
                        Identity = Identity,
                        Name = Name,
                        Action = MsgFriend.MsgFriendAction.SetOfflineFriend,
                        Online = false,
                        Gender = Gender,
                        Nobility = (int)NobilityRank
                    });
                }
            }
        }

        public async Task SendToFriendsAsync(IPacket msg)
        {
            byte[] encoded = msg.Encode();
            foreach (Friend friend in friends.Values.Where(x => x.Online))
            {
                await friend.User.SendAsync(encoded);
            }
        }

        public Task BroadcastToFriendsAsync(string message, Color? color = null)
        {
            return SendToFriendsAsync(new MsgTalk(Identity, TalkChannel.Friend, color ?? Color.Red, message));
        }

        #endregion

        #region Enemies

        private readonly ConcurrentDictionary<uint, Enemy> enemies = new();

        public bool AddEnemy(Enemy friend)
        {
            return enemies.TryAdd(friend.Identity, friend);
        }

        public async Task<bool> CreateEnemyAsync(Character target)
        {
            await target.PkStatistic.KillAsync(this);

            if (IsEnemy(target.Identity))
            {
                return false;
            }

            var enemy = new Enemy(this);
            if (!await enemy.CreateAsync(target))
            {
                return false;
            }

            await enemy.SaveAsync();
            await enemy.SendAsync();
            AddEnemy(enemy);
            return true;
        }

        public bool IsEnemy(uint idTarget)
        {
            return enemies.ContainsKey(idTarget);
        }

        public Enemy GetEnemy(uint idTarget)
        {
            return enemies.TryGetValue(idTarget, out Enemy friend) ? friend : null;
        }

        public async Task<bool> DeleteEnemyAsync(uint idTarget)
        {
            if (!IsFriend(idTarget) || !enemies.TryRemove(idTarget, out Enemy target))
            {
                return false;
            }

            await target.DeleteAsync();

            await SendAsync(new MsgFriend
            {
                Identity = target.Identity,
                Name = target.Name,
                Action = MsgFriend.MsgFriendAction.RemoveEnemy,
                Online = true
            });
            return true;
        }

        public async Task SendAllEnemiesAsync()
        {
            foreach (Enemy enemy in enemies.Values)
            {
                await enemy.SendAsync();
            }

            foreach (DbEnemy enemy in await EnemyRepository.GetOwnEnemyAsync(Identity))
            {
                Character user = RoleManager.GetUser(enemy.UserIdentity);
                if (user != null)
                {
                    await user.SendAsync(new MsgFriend
                    {
                        Identity = Identity,
                        Name = Name,
                        Action = MsgFriend.MsgFriendAction.SetOnlineEnemy,
                        Online = true,
                        Gender = Gender,
                        Nobility = (int)NobilityRank

                    });
                }
            }
        }

        #endregion

        #region Trade

        public Trade Trade { get; set; }

        #endregion

        #region Trade Partner

        private readonly ConcurrentDictionary<uint, TradePartner> tradePartners = new();

        public int TradePartnerAmount => tradePartners.Count;

        public void AddTradePartner(TradePartner partner)
        {
            tradePartners.TryAdd(partner.Identity, partner);
        }

        public void RemoveTradePartner(uint idTarget)
        {
            tradePartners.TryRemove(idTarget, out _);
        }

        public async Task<bool> CreateTradePartnerAsync(Character target)
        {
            if (IsTradePartner(target.Identity) || target.IsTradePartner(Identity))
            {
                await SendAsync(StrTradeBuddyAlreadyAdded);
                return false;
            }

            var business = new DbBusiness
            {
                User = character,
                Business = target.character,
                Date = (uint)DateTime.Now.AddDays(3).ToUnixTimestamp()
            };

            if (!await ServerDbContext.SaveAsync(business))
            {
                await SendAsync(StrTradeBuddySomethingWrong);
                return false;
            }

            TradePartner me;
            TradePartner targetTp;
            AddTradePartner(me = new TradePartner(this, business));
            target.AddTradePartner(targetTp = new TradePartner(target, business));

            await me.SendAsync();
            await targetTp.SendAsync();

            await BroadcastRoomMsgAsync(string.Format(StrTradeBuddyAnnouncePartnership, Name, target.Name));
            return true;
        }

        public async Task<bool> DeleteTradePartnerAsync(uint idTarget)
        {
            if (!IsTradePartner(idTarget))
            {
                return false;
            }

            TradePartner partner = GetTradePartner(idTarget);
            if (partner == null)
            {
                return false;
            }

            await partner.SendRemoveAsync();
            RemoveTradePartner(idTarget);
            await SendAsync(string.Format(StrTradeBuddyBrokePartnership1, partner.Name));

            Task<bool> delete = partner.DeleteAsync();
            Character target = RoleManager.GetUser(idTarget);
            if (target != null)
            {
                partner = target.GetTradePartner(Identity);
                if (partner != null)
                {
                    await partner.SendRemoveAsync();
                    target.RemoveTradePartner(Identity);
                }

                await target.SendAsync(string.Format(StrTradeBuddyBrokePartnership0, Name));
            }

            await delete;
            return true;
        }

        public async Task LoadTradePartnerAsync()
        {
            List<DbBusiness> tps = await BusinessRepository.GetAsync(Identity);
            foreach (DbBusiness tp in tps)
            {
                var db = new TradePartner(this, tp);
                AddTradePartner(db);
            }

            bool notify = tradePartners.Values.Any(x => !x.IsValid());
            if (notify)
            {
                await SendAsync(new MsgAction
                {
                    Action = MsgAction.ActionType.ClientCommand,
                    Identity = Identity,
                    Command = 1207,
                    ArgumentX = X,
                    ArgumentY = Y
                });
            }

            foreach (var tp in tradePartners.Values) 
            {
                await tp.SendAsync();
                if (!tp.IsValid())
                {
                    await tp.NotifyAsync();
                }
            }
        }

        public TradePartner GetTradePartner(uint target)
        {
            return tradePartners.TryGetValue(target, out TradePartner result) ? result : null;
        }

        public bool IsTradePartner(uint target)
        {
            return tradePartners.ContainsKey(target);
        }

        public bool IsValidTradePartner(uint target)
        {
            return tradePartners.ContainsKey(target) && tradePartners[target].IsValid();
        }

        #endregion

        #region Guide

        private DbTutorAccess tutorAccess;

        public ulong MentorExpTime
        {
            get => tutorAccess?.Experience ?? 0;
            set
            {
                tutorAccess ??= new DbTutorAccess
                {
                    GuideIdentity = Identity
                };
                tutorAccess.Experience = value;
            }
        }

        public ushort MentorAddLevexp
        {
            get => tutorAccess?.Composition ?? 0;
            set
            {
                tutorAccess ??= new DbTutorAccess
                {
                    GuideIdentity = Identity
                };
                tutorAccess.Composition = value;
            }
        }

        public ushort MentorGodTime
        {
            get => tutorAccess?.Blessing ?? 0;
            set
            {
                tutorAccess ??= new DbTutorAccess
                {
                    GuideIdentity = Identity
                };
                tutorAccess.Blessing = value;
            }
        }

        public Tutor Guide;

        private readonly ConcurrentDictionary<uint, Tutor> apprentices = new();

        public Tutor GetStudent(uint idStudent)
        {
            return apprentices.TryGetValue(idStudent, out Tutor value) ? value : null;
        }

        public int ApprenticeCount => apprentices.Count;

        public async Task LoadGuideAsync()
        {
            DbTutor tutor = await TutorRepository.GetAsync(Identity);
            if (tutor != null)
            {
                Guide = await Tutor.CreateAsync(tutor);
                if (Guide != null)
                {
                    await Guide.SendTutorAsync();
                    await Guide.SendStudentAsync();

                    Character guide = Guide.Guide;
                    if (guide != null)
                    {
                        await SynchroAttributesAsync(ClientUpdateType.ExtraBattlePower, (uint)Guide.SharedBattlePower,
                                                     (uint)guide.BattlePower);
                        await guide.SendAsync(string.Format(StrGuideStudentLogin, Name));
                    }
                }
            }

            List<DbTutor> apprentices = await TutorRepository.GetStudentsAsync(Identity);
            foreach (DbTutor dbApprentice in apprentices)
            {
                var apprentice = await Tutor.CreateAsync(dbApprentice);
                if (apprentice != null)
                {
                    this.apprentices.TryAdd(dbApprentice.StudentId, apprentice);
                    await apprentice.SendTutorAsync();
                    await apprentice.SendStudentAsync();

                    Character student = apprentice.Student;
                    if (student != null)
                    {
                        await student.SynchroAttributesAsync(ClientUpdateType.ExtraBattlePower,
                                                             (uint)apprentice.SharedBattlePower, (uint)BattlePower);
                        await student.SendAsync(string.Format(StrGuideTutorLogin, Name));
                    }
                }
            }

            tutorAccess = await TutorAccessRepository.GetAsync(Identity);
        }

        public static async Task<bool> CreateTutorRelationAsync(Character guide, Character apprentice)
        {
            if (guide.Level < apprentice.Level || guide.Metempsychosis < apprentice.Metempsychosis)
            {
                return false;
            }

            int deltaLevel = guide.Level - apprentice.Level;
            if (apprentice.Metempsychosis == 0)
            {
                if (deltaLevel < 30)
                {
                    return false;
                }
            }
            else if (apprentice.Metempsychosis == 1)
            {
                if (deltaLevel < 20)
                {
                    return false;
                }
            }
            else
            {
                if (deltaLevel < 10)
                {
                    return false;
                }
            }

            DbTutorType type = TutorManager.GetTutorType(guide.Level);
            if (type == null || guide.ApprenticeCount >= type.StudentNum)
            {
                return false;
            }

            if (apprentice.Guide != null)
            {
                return false;
            }

            if (guide.apprentices.ContainsKey(apprentice.Identity))
            {
                return false;
            }

            var dbTutor = new DbTutor
            {
                GuideId = guide.Identity,
                StudentId = apprentice.Identity,
                Date = (uint)UnixTimestamp.Now
            };
            if (!await ServerDbContext.SaveAsync(dbTutor))
            {
                return false;
            }

            var tutor = await Tutor.CreateAsync(dbTutor);

            apprentice.Guide = tutor;
            await tutor.SendTutorAsync();
            guide.apprentices.TryAdd(apprentice.Identity, tutor);
            await tutor.SendStudentAsync();
            await apprentice.SynchroAttributesAsync(ClientUpdateType.ExtraBattlePower, (uint)tutor.SharedBattlePower,
                                                    (uint)guide.BattlePower);
            return true;
        }

        public async Task SynchroStudentsAsync()
        {
            foreach (var apprentice in apprentices.Values.Where(x => x.Student != null))
            {
                await apprentice.SendTutorAsync();
                await apprentice.SendStudentAsync();
            }
        }

        public async Task SynchroApprenticesSharedBattlePowerAsync()
        {
            foreach (Tutor apprentice in apprentices.Values.Where(x => x.Student != null))
            {
                await apprentice.Student.SynchroAttributesAsync(ClientUpdateType.ExtraBattlePower,
                                                                (uint)apprentice.SharedBattlePower,
                                                                (uint)(apprentice.Guide?.BattlePower ?? 0));
            }
        }

        /// <summary>
        ///     Returns true if the current user is the tutor of the target ID.
        /// </summary>
        public bool IsTutor(uint idApprentice)
        {
            return apprentices.ContainsKey(idApprentice);
        }

        public bool IsApprentice(uint idGuide)
        {
            return Guide?.GuideIdentity == idGuide;
        }

        public void RemoveApprentice(uint idApprentice)
        {
            apprentices.TryRemove(idApprentice, out _);
        }

        public Task<bool> SaveTutorAccessAsync()
        {
            if (tutorAccess != null)
            {
                return ServerDbContext.SaveAsync(tutorAccess);
            }

            return Task.FromResult(true);
        }

        #endregion

        #region Merchant

        public int Merchant => character.Business == 0 ? 0 : IsMerchant() ? 255 : 1;

        public int BusinessManDays => (int)(character.Business == 0 ? 0 : Math.Ceiling((UnixTimestamp.ToDateTime(character.Business) - DateTime.Now).TotalDays));


        public bool IsMerchant()
        {
            return character.Business != 0 && UnixTimestamp.ToDateTime(character.Business) < DateTime.Now;
        }

        public bool IsAwaitingMerchantStatus()
        {
            return character.Business != 0 && UnixTimestamp.ToDateTime(character.Business) > DateTime.Now;
        }

        public async Task<bool> SetMerchantAsync()
        {
            if (IsMerchant())
            {
                return false;
            }

            if (Level <= 30 && Metempsychosis == 0)
            {
                character.Business = (uint)DateTime.Now.ToUnixTimestamp();
                await SynchroAttributesAsync(ClientUpdateType.Merchant, 255);
            }
            else
            {
                character.Business = (uint)DateTime.Now.AddDays(5).ToUnixTimestamp();
            }

            return await SaveAsync();
        }

        public async Task RemoveMerchantAsync()
        {
            character.Business = 0;
            await SynchroAttributesAsync(ClientUpdateType.Merchant, 0);
            await SaveAsync();
        }

        public async Task SendMerchantAsync()
        {
            if (IsMerchant())
            {
                await SynchroAttributesAsync(ClientUpdateType.Merchant, 255);
                return;
            }

            if (IsAwaitingMerchantStatus())
            {
                await SynchroAttributesAsync(ClientUpdateType.Merchant, 1);
                await SendAsync(new MsgInteract
                {
                    Action = MsgInteractType.MerchantProgress,
                    Command = BusinessManDays
                });
                return;
            }

            if (Level <= 30 && Metempsychosis == 0)
            {
                await SendAsync(new MsgInteract
                {
                    Action = MsgInteractType.InitialMerchant
                });
                return;
            }

            await SynchroAttributesAsync(ClientUpdateType.Merchant, 0);
        }

        #endregion

        #region Player Pose

        private uint coupleInteractionTarget;
        private bool coupleInteractionStarted;

        public bool HasCoupleInteraction()
        {
            return coupleInteractionTarget != 0;
        }

        public Character GetCoupleInteractionTarget()
        {
            return Map.GetUser(coupleInteractionTarget);
        }

        public EntityAction CoupleAction { get; private set; }

        public async Task<bool> SetActionAsync(EntityAction action, uint target)
        {
            // hum
            CoupleAction = action;
            coupleInteractionTarget = target;
            return true;
        }

        public void CancelCoupleInteraction()
        {
            CoupleAction = EntityAction.None;
            coupleInteractionTarget = 0;
            PopRequest(RequestType.CoupleInteraction);
            coupleInteractionStarted = false;
        }

        public void StartCoupleInteraction()
        {
            coupleInteractionStarted = true;
        }

        public bool HasCoupleInteractionStarted() => coupleInteractionStarted;

        #endregion

        #region Enlightment

        private readonly TimeOut enlightenTimeExp = new(ENLIGHTENMENT_EXP_PART_TIME);

        public const int ENLIGHTENMENT_MAX_TIMES = 5;
        public const int ENLIGHTENMENT_UPLEV_MAX_EXP = 600;
        public const int ENLIGHTENMENT_EXP_PART_TIME = 60 * 20;
        public const int ENLIGHTENMENT_MIN_LEVEL = 90;

        private const int EnlightenmentUserStc = 1127;

        public uint EnlightenPoints
        {
            get => character.MentorOpportunity;
            set => character.MentorOpportunity = value;
        }

        public uint EnlightenedTimes
        {
            get => character.MentorAchieve;
            set => character.MentorAchieve = value;
        }

        public uint EnlightenExperience
        {
            get => character.MentorUplevTime;
            set => character.MentorUplevTime = value;
        }

        public uint EnlightmentLastUpdate
        {
            get => character.MentorDay;
            set => character.MentorDay = value;
        }

        public void SetEnlightenLastUpdate()
        {
            EnlightmentLastUpdate = uint.Parse(DateTime.Now.ToString("yyyyMMdd"));
        }

        public bool CanBeEnlightened(Character mentor)
        {
            if (mentor == null)
            {
                return false;
            }

            if (EnlightenedTimes >= ENLIGHTENMENT_MAX_TIMES)
            {
                return false;
            }

            if (EnlightenExperience >= ENLIGHTENMENT_UPLEV_MAX_EXP / 2 * ENLIGHTENMENT_MAX_TIMES)
            {
                return false;
            }

            if (mentor.Level - Level < 20)
            {
                return false;
            }

            DbStatistic stc = Statistic.GetStc(EnlightenmentUserStc, mentor.Identity);
            if (stc?.Timestamp != null)
            {
                int day = (int)stc.Timestamp;
                int now = int.Parse(DateTime.Now.ToString("yyyyMMdd"));
                return day != now;
            }
            return true;
        }

        public async Task<bool> EnlightenPlayerAsync(Character target)
        {
            if (Map != null && Map.IsNoExpMap())
            {
                return false;
            }

            var enlightTimes = (int)(EnlightenPoints / 100);
            if (enlightTimes <= 0)
            {
                return false;
            }

            if (target.Level > Level - 20)
            {
                // todo send message
                return false;
            }

            if (!target.CanBeEnlightened(this))
            {
                // todo send message
                return false;
            }

            EnlightenPoints = Math.Max(EnlightenPoints - 100, 0);
            if (target.EnlightenedTimes == 0 || !enlightenTimeExp.IsActive())
            {
                enlightenTimeExp.Startup(ENLIGHTENMENT_EXP_PART_TIME); // 20 minutes
            }

            target.EnlightenedTimes += 1;
            target.EnlightenExperience += ENLIGHTENMENT_UPLEV_MAX_EXP / 2;

            await target.Statistic.AddOrUpdateAsync(EnlightenmentUserStc, Identity, 1, true);

            // we will send instand 300 uplev exp and 300 will be awarded for 5 minutes later
            await target.AwardExperienceAsync(CalculateExpBall(ENLIGHTENMENT_UPLEV_MAX_EXP / 2), true);
            await target.SendAsync(new MsgUserAttrib(Identity, ClientUpdateType.EnlightenPoints, 0));
            //await SynchroAttributesAsync(ClientUpdateType.EnlightenPoints, EnlightenPoints, true);

            await SaveAsync();
            await target.SaveAsync();
            return true;
        }

        public async Task ResetEnlightenmentAsync()
        {
            if (EnlightmentLastUpdate >= uint.Parse(DateTime.Now.ToString("yyyyMMdd")))
            {
                return;
            }

            EnlightmentLastUpdate = uint.Parse(DateTime.Now.ToString("yyyyMMdd"));

            EnlightenedTimes = 0;

            EnlightenPoints = 0;
            if (Level >= 90)
            {
                EnlightenPoints += 100;
            }

            switch (NobilityRank)
            {
                case MsgPeerage.NobilityRank.Knight:
                case MsgPeerage.NobilityRank.Baron:
                    EnlightenPoints += 100;
                    break;
                case MsgPeerage.NobilityRank.Earl:
                case MsgPeerage.NobilityRank.Duke:
                    EnlightenPoints += 200;
                    break;
                case MsgPeerage.NobilityRank.Prince:
                    EnlightenPoints += 300;
                    break;
                case MsgPeerage.NobilityRank.King:
                    EnlightenPoints += 400;
                    break;
            }

            switch (VipLevel)
            {
                case 1:
                case 2:
                case 3:
                    EnlightenPoints += 100;
                    break;
                case 4:
                case 5:
                    EnlightenPoints += 200;
                    break;
                case 6:
                    EnlightenPoints += 300;
                    break;
            }

            await SynchroAttributesAsync(ClientUpdateType.EnlightenPoints, EnlightenPoints, true);
        }

        #endregion
    }
}
