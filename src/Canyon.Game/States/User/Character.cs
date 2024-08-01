using Canyon.Database.Entities;
using Canyon.Game.Database;
using Canyon.Game.Database.Repositories;
using Canyon.Game.Services.Managers;
using Canyon.Game.Sockets.Ai.Packets;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.Sockets.Piglet;
using Canyon.Game.Sockets.Piglet.Packets;
using Canyon.Game.States.Events.Mount;
using Canyon.Game.States.Events.Qualifier.TeamQualifier;
using Canyon.Game.States.Events.Qualifier.UserQualifier;
using Canyon.Game.States.Items;
using Canyon.Game.States.Magics;
using Canyon.Game.States.Mails;
using Canyon.Game.States.NeiGong;
using Canyon.Game.States.Relationship;
using Canyon.Game.States.World;
using Canyon.Network.Packets;
using Canyon.Network.Packets.Ai;
using Canyon.Network.Packets.Piglet;
using Canyon.Shared.Mathematics;
using Canyon.World.Enums;
using Canyon.World.Map;
using System.Collections.Concurrent;
using System.Drawing;
using System.Globalization;
using static Canyon.Game.Services.Managers.ActivityManager;
using static Canyon.Game.Sockets.Game.Packets.MsgAction;
using static Canyon.Game.Sockets.Game.Packets.MsgGodExp;
using static Canyon.Game.Sockets.Game.Packets.MsgHangUp;
using static Canyon.Game.Sockets.Game.Packets.MsgPeerage;
using static Canyon.Game.States.Items.Item;

namespace Canyon.Game.States.User
{
    public partial class Character : Role
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<Character>();

        private readonly TimeOutMS vigorTimer = new(1500);
        private readonly TimeOut worldChatTimer = new();
        private readonly TimeOutMS energyTimer = new(ADD_ENERGY_STAND_MS);
        private readonly TimeOut autoHealTimer = new(AUTOHEALLIFE_TIME);
        private readonly TimeOut pkDecreaseTimer = new(PK_DEC_TIME);
        private readonly TimeOut heavenBlessingTimer = new(60);
        private readonly TimeOut luckyAbsorbStartTimer = new(2);
        private readonly TimeOut luckyStepTimer = new(1);
        private readonly TimeOut xpPointsTimer = new(3);
        private readonly TimeOut miningTimer = new();
        private readonly TimeOut gameMasterInfoTimer = new();
        private readonly TimeOut dateSyncTimer = new();
        private readonly TimeOut deadMarkTimer = new(1);
        private readonly TimeOut coolSyncTimer = new(5);

        private int blessPoints = 0;
        private uint idLuckyTarget = 0;
        private int luckyTimeCount = 0;

        private readonly DbCharacter character;


        /// <summary>
        ///     Instantiates a new instance of <see cref="Character" /> using a database fetched
        ///     <see cref="DbCharacter" />. Copies attributes over to the base class of this
        ///     class, which will then be used to save the character from the game world.
        /// </summary>
        /// <param name="character">Database character information</param>
        /// <param name="socket"></param>
        public Character(DbCharacter character, Client socket)
        {
            /*
             * Removed the base class because we'll be inheriting role stuff.
             */
            this.character = character;

            Screen = new Screen(this);
            WeaponSkill = new WeaponSkill(this);
            UserPackage = new UserPackage(this);
            Statistic = new UserStatistic(this);
            TaskDetail = new TaskDetail(this);
            MailBox = new MailBox(this);
            AstProf = new SubClass(this);
            Fate = new Fate(this);
            JiangHu = new JiangHu(this);
            Achievements = new Achievements(this);
            PkStatistic = new PkStatistic(this);
            InnerStrength = new InnerStrength(this);

            mesh = character.Mesh;
            currentX = character.X;
            currentY = character.Y;
            idMap = character.MapID;

            if (socket == null)
            {
                return; // ?
            }

            Client = socket;

            if (character.LuckyTime != 0)
            {
                luckyTimeCount = (int)Math.Max(0, (UnixTimestamp.ToDateTime(character.LuckyTime) - DateTime.Now).TotalSeconds);
            }

            if (EnlightenExperience > 0)
            {
                enlightenTimeExp.Startup(ENLIGHTENMENT_EXP_PART_TIME);
            }

            energyTimer.Update();
            autoHealTimer.Update();
            pkDecreaseTimer.Update();
            xpPointsTimer.Update();
            gameMasterInfoTimer.Startup(1);
            dateSyncTimer.Startup(30);
            deadMarkTimer.Startup(1);
        }

        public Client Client { get; init; }

        public UserPackage UserPackage { get; init; }

        public PrivilegeFlag Flag 
        {
            get => (PrivilegeFlag)character.Flag;
            set => character.Flag = (uint) value;
        }

        public bool IsDeleted => isDeleted;

        #region Identity

        public override uint Identity
        {
            get => character.Identity;
        }

        public override string Name
        {
            get => character.Name;
        }

        public void ChangeName(string newName)
        {
            character.Name = newName;
        }

        public string MateName { get; set; }

        public uint MateIdentity
        {
            get => character.Mate;
            set => character.Mate = value;
        }

        #endregion

        #region Profession

        public byte ProfessionSort => (byte)(Profession / 10);

        public byte ProfessionLevel => (byte)(Profession % 10);

        public byte Profession
        {
            get => character?.Profession ?? 0;
            set => character.Profession = value;
        }

        public byte PreviousProfession
        {
            get => character?.PreviousProfession ?? 0;
            set => character.PreviousProfession = value;
        }

        public byte FirstProfession
        {
            get => character?.FirstProfession ?? 0;
            set => character.FirstProfession = value;
        }

        #endregion

        #region Appearence

        private uint mesh;
        private ushort transformationMesh;

        public int Gender => Body == BodyType.AgileMale || Body == BodyType.MuscularMale ? 1 : 2;

        public ushort TransformationMesh
        {
            get => transformationMesh;
            set
            {
                transformationMesh = value;
                Mesh = (uint)((uint)value * 10000000 + Avatar * 10000 + (uint)Body);
            }
        }

        public override uint Mesh
        {
            get => mesh;
            set
            {
                mesh = value;
                character.Mesh = value % 10000000;
            }
        }

        public BodyType Body
        {
            get => (BodyType)(Mesh % 10000);
            set => Mesh = (uint)value + Avatar * 10000u;
        }

        public ushort Avatar
        {
            get => (ushort)(Mesh % 10000000 / 10000);
            set => Mesh = (uint)(value * 10000 + (int)Body);
        }

        public ushort Hairstyle
        {
            get => (ushort)(ProfessionSort == 6 && Gender == 1 ? 0 : character.Hairstyle);
            set => character.Hairstyle = value;
        }

        #endregion

        #region Level and Experience

        public bool AutoAllot
        {
            get => character.AutoAllot != 0;
            set => character.AutoAllot = (byte)(value ? 1 : 0);
        }

        public override byte Level
        {
            get => character?.Level ?? 0;
            set => character.Level = Math.Min(MAX_UPLEV, Math.Max((byte)1, value));
        }

        public ulong Experience
        {
            get => character?.Experience ?? 0;
            set
            {
                if (Level >= MAX_UPLEV)
                {
                    return;
                }

                character.Experience = value;
            }
        }

        public ulong AutoHangUpExperience
        {
            get;
            set;
        }

        public byte Metempsychosis
        {
            get => character?.Rebirths ?? 0;
            set => character.Rebirths = value;
        }

        public bool IsAutoHangUp { get; set; }

        public bool IsNewbie()
        {
            return Level < 70;
        }

        public async Task<bool> AwardLevelAsync(ushort amount)
        {
            if (Level >= MAX_UPLEV)
            {
                return false;
            }

            if (Level + amount <= 0)
            {
                return false;
            }

            int addLev = amount;
            if (addLev + Level > MAX_UPLEV)
            {
                addLev = MAX_UPLEV - Level;
            }

            if (addLev <= 0)
            {
                return false;
            }

            await AddAttributesAsync(ClientUpdateType.Atributes, (ushort)(addLev * 3));
            await AddAttributesAsync(ClientUpdateType.Level, addLev);
            await BroadcastRoomMsgAsync(new MsgAction
            {
                Identity = Identity,
                Action = ActionType.CharacterLevelUp,
                X = Level
            }, true);

            await UpLevelEventAsync();
            return true;
        }

        public async Task AwardBattleExpAsync(long experience, bool bGemEffect)
        {
            if (experience == 0 || QueryStatus(StatusSet.CURSED) != null)
            {
                return;
            }

            if (Level >= MAX_UPLEV)
            {
                return;
            }

            if (experience < 0)
            {
                await AddAttributesAsync(ClientUpdateType.Experience, experience);
                return;
            }

            const int battleExpTax = 5;
            if (Level < 130)
            {
                experience *= battleExpTax;
            }

            if (Level >= 120)
            {
                experience /= 2;
            }

            double multiplier = 1;
            if (HasMultipleExp)
            {
                multiplier += ExperienceMultiplier - 1;
            }

            if (!IsNewbie() && ProfessionSort == 13 && ProfessionLevel >= 3)
            {
                multiplier += 1;
            }

            DbLevelExperience levExp = ExperienceManager.GetLevelExperience(Level);
            if (IsBlessed)
            {
                if (levExp != null)
                {
                    OnlineTrainingExp += (uint)(levExp.UpLevTime * (experience / (float)levExp.Exp));
                }
            }

            if (bGemEffect)
            {
                multiplier += RainbowGemBonus / 100d;
            }

            if (IsLucky && await ChanceCalcAsync(10, 10000))
            {
                await SendEffectAsync("LuckyGuy", true);
                experience *= 5;
                await SendAsync(StrLuckyGuyQuintuple);
            }

            multiplier += 1 + BattlePower / 100d;

            experience = (long)(experience * Math.Max(0.01d, multiplier));

            if (Metempsychosis >= 2)
            {
                experience /= 3;
            }

            if (QueryStatus(StatusSet.OBLIVION) != null)
            {
                oblivionExperience += experience;
                return;
            }

            if (Map.IsAutoHungUpMap() && IsAutoHangUp)
            {
                AutoHangUpExperience += (ulong)experience;

                DbLevelExperience dbExp = ExperienceManager.GetLevelExperience(Level);
                if (dbExp != null && dbExp.Exp < (Experience + AutoHangUpExperience))
                {
                    await AwardExperienceAsync((long)AutoHangUpExperience, true);
                    AutoHangUpExperience = 0;
                }
                return;
            }

            await AwardExperienceAsync(experience);
        }

        public long AdjustExperience(Role pTarget, long nRawExp, bool bNewbieBonusMsg)
        {
            if (pTarget == null)
            {
                return 0;
            }

            long nExp = nRawExp;
            nExp = BattleSystem.AdjustExp(nExp, Level, pTarget.Level);
            return nExp;
        }

        public async Task<bool> AwardExperienceAsync(long amount, bool noContribute = false)
        {
            if (Level > ExperienceManager.GetLevelLimit())
            {
                return true;
            }

            if (Map != null && Map.IsNoExpMap())
            {
                return false;
            }

            amount += (long)Experience;
            var leveled = false;
            uint pointAmount = 0;
            byte newLevel = Level;
            ushort virtue = 0;
            double mentorUpLevTime = 0;
            while (newLevel < MAX_UPLEV && amount >= (long)ExperienceManager.GetLevelExperience(newLevel).Exp)
            {
                DbLevelExperience dbExp = ExperienceManager.GetLevelExperience(newLevel);
                amount -= (long)dbExp.Exp;
                leveled = true;
                newLevel++;

                if (newLevel <= 70)
                {
                    virtue += (ushort)dbExp.UpLevTime;
                }

                if (!AutoAllot || newLevel >= 120)
                {
                    pointAmount += 3;
                }

                mentorUpLevTime += dbExp.MentorUpLevTime;

                if (newLevel < ExperienceManager.GetLevelLimit())
                {
                    continue;
                }

                amount = 0;
                break;
            }

            uint metLev = 0;
            DbLevelExperience leveXp = ExperienceManager.GetLevelExperience(newLevel);
            if (leveXp != null)
            {
                float fExp = amount / (float)leveXp.Exp;
                metLev = (uint)(newLevel * 10000 + fExp * 1000);
            }

            int metLevel = character.MeteLevel2 != 0 ? 110 : 130;
            uint metExp = character.MeteLevel2 != 0 ? character.MeteLevel2 : character.MeteLevel;
            if (newLevel >= metLevel && Metempsychosis > 0 && metExp > metLev)
            {
                byte extra = 0;
                if (metExp / 10000 > newLevel)
                {
                    uint mete = metExp / 10000;
                    extra += (byte)(mete - newLevel);
                    pointAmount += (uint)(extra * 3);
                    leveled = true;
                    amount = 0;
                }

                newLevel += extra;

                if (newLevel >= ExperienceManager.GetLevelLimit())
                {
                    newLevel = (byte)ExperienceManager.GetLevelLimit();
                    amount = 0;
                }
                else if (metExp >= newLevel * 10000)
                {
                    amount = (long)(ExperienceManager.GetLevelExperience(newLevel).Exp *
                                     (metExp % 10000 / 1000d));
                }
            }

            if (leveled)
            {
                byte job;
                if (Profession > 100)
                {
                    job = 10;
                }
                else
                {
                    job = (byte)((Profession - Profession % 10) / 10);
                }

                Level = newLevel;

                if (AutoAllot && newLevel <= 120)
                {
                    DbPointAllot allot = ExperienceManager.GetPointAllot(job, Math.Min((byte)120, newLevel));
                    if (allot != null)
                    {
                        await SetAttributesAsync(ClientUpdateType.Strength, allot.Strength);
                        await SetAttributesAsync(ClientUpdateType.Agility, allot.Agility);
                        await SetAttributesAsync(ClientUpdateType.Vitality, allot.Vitality);
                        await SetAttributesAsync(ClientUpdateType.Spirit, allot.Spirit);
                    }
                }

                if (pointAmount > 0)
                {
                    await AddAttributesAsync(ClientUpdateType.Atributes, (int)pointAmount);
                }

                await SetAttributesAsync(ClientUpdateType.Level, Level);
                await SetAttributesAsync(ClientUpdateType.Hitpoints, MaxLife);
                await SetAttributesAsync(ClientUpdateType.Mana, MaxMana);
                await Screen.BroadcastRoomMsgAsync(new MsgAction
                {
                    Action = ActionType.CharacterLevelUp,
                    Identity = Identity,
                    X = Level
                });

                await UpLevelEventAsync();

                if (!noContribute && Guide != null && mentorUpLevTime > 0)
                {
                    mentorUpLevTime /= 5;
                    await Guide.AwardTutorExperienceAsync((uint)mentorUpLevTime).ConfigureAwait(false);
#if DEBUG
                    if (Guide.Guide?.IsPm() == true)
                    {
                        await Guide.Guide.SendAsync($"Mentor uplev time add: +{mentorUpLevTime}", TalkChannel.Talk);
                    }
#endif
                }
            }

            if (Team != null && !Team.IsLeader(Identity) && virtue > 0 
                && Team.Leader.MapIdentity == MapIdentity && Team.Leader.GetDistance(this) < 30)
            {
                Team.Leader.VirtuePoints += virtue;
                await Team.SendAsync(new MsgTalk(Identity, TalkChannel.Team, Color.White,
                                                 string.Format(StrAwardVirtue, Team.Leader.Name, virtue)));

                if (Team.Leader.SyndicateIdentity != 0)
                {
                    Team.Leader.SyndicateMember.GuideDonation += 1;
                    Team.Leader.SyndicateMember.GuideTotalDonation += 1;
                    await Team.Leader.SyndicateMember.SaveAsync();
                }
            }

            Experience = (ulong)amount;
            await SetAttributesAsync(ClientUpdateType.Experience, Experience);
            return true;
        }
            
        public async Task UpLevelEventAsync()
        {
            await GameAction.ExecuteActionAsync(USER_UPLEV_ACTION, this, this, null, string.Empty);

            if (Team != null)
            {
                await Team.BroadcastMemberLifeAsync(this, true);
                await Team.SyncFamilyBattlePowerAsync();
            }

            if (Metempsychosis >= 2 && Level >= 30 && !JiangHu.HasJiangHu)
            {
                await SendAsync(new MsgOwnKongfuBase
                {
                    Mode = MsgOwnKongfuBase.KongfuBaseMode.IconBar
                });
            }

            if (ApprenticeCount > 0)
            {
                await SynchroApprenticesSharedBattlePowerAsync();
            }

            if (await CheckForActivityTaskUpdatesAsync())
            {
                await SubmitActivityListAsync();
            }
        }

        public long CalculateExpBall(int amount = EXPBALL_AMOUNT)
        {
            long exp = 0;

            if (Level >= ExperienceManager.GetLevelLimit())
            {
                return 0;
            }

            byte level = Level;
            if (Experience > 0)
            {
                double pct = 1.00 - Experience / (double)ExperienceManager.GetLevelExperience(Level).Exp;
                if (amount > pct * ExperienceManager.GetLevelExperience(Level).UpLevTime)
                {
                    amount -= (int)(pct * ExperienceManager.GetLevelExperience(Level).UpLevTime);
                    exp += (long)(ExperienceManager.GetLevelExperience(Level).Exp - Experience);
                    level++;
                }
            }

            while (level < MAX_UPLEV && amount > ExperienceManager.GetLevelExperience(level).UpLevTime)
            {
                amount -= ExperienceManager.GetLevelExperience(level).UpLevTime;
                exp += (long)ExperienceManager.GetLevelExperience(level).Exp;

                if (level >= MAX_UPLEV)
                {
                    return exp;
                }

                level++;
            }

            exp += (long)(amount / (double)ExperienceManager.GetLevelExperience(Level).UpLevTime *
                           ExperienceManager.GetLevelExperience(Level).Exp);
            return exp;
        }

        public ExperiencePreview PreviewExpBallUsage(int amount = EXPBALL_AMOUNT)
        {
            long expBallExp = (long)Experience + CalculateExpBall(amount);
            byte newLevel = Level;
            DbLevelExperience dbExp = null;
            while (newLevel < MAX_UPLEV && expBallExp >= (long)ExperienceManager.GetLevelExperience(newLevel).Exp)
            {
                dbExp = ExperienceManager.GetLevelExperience(newLevel);
                expBallExp -= (long)dbExp.Exp;
                newLevel++;
                if (newLevel < MAX_UPLEV)
                {
                    continue;
                }

                dbExp = null;
                expBallExp = 0;
                break;
            }

            double percent = 0;
            if (expBallExp > 0 && dbExp != null)
            {
                percent = dbExp.Exp / (double)expBallExp * 100;
            }

            return new ExperiencePreview
            {
                Level = newLevel,
                Experience = (ulong)expBallExp,
                Percent = percent
            };
        }

        public ExperiencePreview PreviewExperienceIncrement(ulong experience)
        {
            if (Level >= MAX_UPLEV)
            {
                return new ExperiencePreview
                {
                    Level = MAX_UPLEV
                };
            }

            byte newLevel = Level;
            ulong newExperience = Experience + experience;
            double percent = 0;
            DbLevelExperience dbExp = ExperienceManager.GetLevelExperience(newLevel);
            do
            {
                if (newExperience < dbExp.Exp)
                {
                    break;
                }

                newLevel++;
                newExperience -= dbExp.Exp;
                dbExp = ExperienceManager.GetLevelExperience(newLevel);
            }
            while (newLevel < MAX_UPLEV && dbExp != null);

            if (newExperience != 0 && dbExp != null)
            {
                percent = (double)newExperience / dbExp.Exp * 100;
            }

            return new ExperiencePreview
            {
                Level = newLevel,
                Experience = newExperience,
                Percent = percent
            };
        }

        public async Task IncrementExpBallAsync()
        {
            await Statistic.IncrementValueAsync(EXP_BALL_USAGE_STC_EVENT, EXP_BALL_USAGE_STC_DATA);
            character.ExpBallUsage += EXPBALL_AMOUNT;
            character.MentorOpportunity += 10;
            await SynchroAttributesAsync(ClientUpdateType.EnlightenPoints, EnlightenPoints);
            await SaveAsync();
        }

        /*
         * Reference from LUA
         * tExpProps_Stc[723700] = {}
         * tExpProps_Stc[723700]["EventType"] = 114
         * tExpProps_Stc[723700]["DataType"] = 44
         * tExpProps_Stc[723700]["MaxData"] = 10
         */
        private uint EXP_BALL_USAGE_STC_EVENT = 114;
        private uint EXP_BALL_USAGE_STC_DATA = 44;

        public bool CanUseExpBall()
        {
            if (Level >= ExperienceManager.GetLevelLimit())
            {
                return false;
            }

            var expUsageStc = Statistic.GetStc(EXP_BALL_USAGE_STC_EVENT, EXP_BALL_USAGE_STC_DATA);
            if (expUsageStc != null && UnixTimestamp.ToDateTime(expUsageStc.Timestamp).Date < DateTime.Now.Date)
            {
                character.ExpBallUsage = 0;
                Statistic.AddOrUpdateAsync(EXP_BALL_USAGE_STC_EVENT, EXP_BALL_USAGE_STC_DATA, 0, true).GetAwaiter().GetResult();
                return true;
            }

            if (expUsageStc != null && expUsageStc.Data >= 10)
            {
                return false;
            }

            return true;
        }

        #endregion

        #region Rebirth

        public async Task<bool> ReincarnateAsync(ushort prof, ushort look)
        {
            DbRebirth data = ExperienceManager.GetRebirth(Profession, prof, 3);
            if (data == null)
            {
                if (IsGm())
                {
                    await SendAsync($"No rebirth set for {Profession} -> {prof}");
                }
                return false;
            }

            int requiredLevel = data.NeedLevel;
            if (Level < requiredLevel)
            {
                await SendAsync(StrNotEnoughLevel);
                return false;
            }

            if (Level >= data.NeedLevel)
            {
                DbLevelExperience levExp = ExperienceManager.GetLevelExperience(Level);
                if (levExp != null)
                {
                    float fExp = Experience / (float)levExp.Exp;
                    var metLev = (uint)(Level * 10000 + fExp * 1000);
                    if (metLev > character.MeteLevel2)
                        character.MeteLevel2 = metLev;
                }
                else if (Level >= MAX_UPLEV)
                {
                    character.MeteLevel2 = MAX_UPLEV * 10000;
                }
            }

            int forgetProfession = FirstProfession;
            int firstProfession = PreviousProfession;
            int previousProfession = Profession;
            await ResetUserAttributesAsync(Metempsychosis, prof, look, data.NewLevel);

            for (var pos = ItemPosition.EquipmentBegin; pos <= ItemPosition.EquipmentEnd; pos++)
            {
                if (UserPackage[pos] != null)
                {
                    await UserPackage[pos].DegradeItemAsync(false);
                }
            }

            if (UserPackage[ItemPosition.LeftHand]?.IsArrowSort() == false)
            {
                await UserPackage.UnEquipAsync(ItemPosition.LeftHand);
            }

            if (UserPackage[ItemPosition.RightHand]?.IsBow() == true && ProfessionSort != 4)
            {
                await UserPackage.UnEquipAsync(ItemPosition.RightHand);
            }

            /*
             * Let's think that if I'm reincarnating I will lose my first profession, this means that I need to unlearn all
             * skills that I am not supposed to have from that class.
             * This means that possibly I need to cross First class skills from the first class and the current one,
             * so I don't exclude skills that the current class also shares (like riding which is listed on magictypeop 4).
             */
            forgetProfession = forgetProfession / 10 * 10 + 1;
            if (forgetProfession >= 100)
            {
                forgetProfession++;
            }

            firstProfession = firstProfession / 10 * 10 + 1;
            if (firstProfession >= 100)
            {
                firstProfession++;
            }

            previousProfession = previousProfession / 10 * 10 + 1;
            if (previousProfession >= 100)
            {
                previousProfession++;
            }

            bool isPureProfessional = Profession == PreviousProfession && Profession == FirstProfession;
            if (!isPureProfessional)
            {
                List<ushort> pureSkills = ExperienceManager.GetMagictypeOp(MagicTypeOperation.MagicOperation.PureSkills, forgetProfession, forgetProfession, 0);
                foreach (var magicType in pureSkills)
                {
                    await MagicData.UnlearnMagicAsync(magicType, true);
                }
            }

            // in this scenario, I need to remove all skills, because I have no memories of that profession
            if (forgetProfession != firstProfession 
                && forgetProfession != previousProfession 
                && forgetProfession != Profession)
            {
                List<ushort> removeFromFirstClass = ExperienceManager.GetMagictypeOp(MagicTypeOperation.MagicOperation.FirstLifeSkills, 0, forgetProfession, 0);
                List<ushort> keepFromCurrentClass = ExperienceManager.GetMagictypeOp(MagicTypeOperation.MagicOperation.FirstLifeSkills, 0, Profession, 0);
                foreach (var magicType in removeFromFirstClass)
                {
                    // if this magic does not belong to my profession, I'll remove it.
                    if (keepFromCurrentClass.All(x => x != magicType))
                    {
                        await MagicData.UnlearnMagicAsync(magicType, true);
                    }
                }
            }
            else
            {
                List<ushort> epiphanyRemoveSkills = ExperienceManager.GetMagictypeOp(MagicTypeOperation.MagicOperation.RemoveOnRebirth, forgetProfession, Profession, 1);
                foreach (ushort magicType in epiphanyRemoveSkills)
                {
                    await MagicData.UnlearnMagicAsync(magicType, false);
                }

                List<ushort> epiphanyResetSkills = ExperienceManager.GetMagictypeOp(MagicTypeOperation.MagicOperation.ResetOnRebirth, forgetProfession, Profession, 1);
                foreach (ushort magicType in epiphanyResetSkills)
                {
                    await MagicData.ResetSkillAsync(magicType);
                }
            }

            previousProfession = previousProfession / 10 * 10 + 5;
            List<ushort> removeSkills = ExperienceManager.GetMagictypeOp(MagicTypeOperation.MagicOperation.RemoveOnRebirth, previousProfession, prof, 1);
            foreach (ushort magicType in removeSkills)
            {
                await MagicData.UnlearnMagicAsync(magicType, false);
            }

            List<ushort> resetSkills = ExperienceManager.GetMagictypeOp(MagicTypeOperation.MagicOperation.ResetOnRebirth, previousProfession, prof, 1);
            foreach (ushort magicType in resetSkills)
            {
                await MagicData.ResetSkillAsync(magicType);
            }

            List<ushort> learnSkills = ExperienceManager.GetMagictypeOp(MagicTypeOperation.MagicOperation.LearnAfterRebirth, previousProfession, prof, 1);
            foreach (ushort magicType in learnSkills)
            {
                await MagicData.CreateAsync(magicType, 0);
            }

            logger.LogInformation("User [{Id}:{Name}] reincarnated {Metem} times.", Identity, Name, Metempsychosis);
            return true;
        }

        public async Task<bool> RebirthAsync(ushort prof, ushort look)
        {
            DbRebirth data = ExperienceManager.GetRebirth(Profession, prof, Metempsychosis + 1);
            if (data == null)
            {
                if (IsGm())
                {
                    await SendAsync($"No rebirth set for {Profession} -> {prof}");
                }
                return false;
            }

            int requiredLevel = data.NeedLevel;
            if (Level < requiredLevel)
            {
                await SendAsync(StrNotEnoughLevel);
                return false;
            }

            if (Level >= 130)
            {
                DbLevelExperience levExp = ExperienceManager.GetLevelExperience(Level);
                if (levExp != null)
                {
                    float fExp = Experience / (float)levExp.Exp;
                    var metLev = (uint)(Level * 10000 + fExp * 1000);
                    if (metLev > character.MeteLevel)
                        character.MeteLevel = metLev;
                }
                else if (Level >= MAX_UPLEV)
                {
                    character.MeteLevel = MAX_UPLEV * 10000;
                }
            }

            int metempsychosis = Math.Min(Math.Max((byte)1, Metempsychosis), (byte)2);
            int oldProf = Profession;
            await ResetUserAttributesAsync(Metempsychosis, prof, look, data.NewLevel);

            for (var pos = ItemPosition.EquipmentBegin; pos <= ItemPosition.EquipmentEnd; pos++)
            {
                if (UserPackage[pos] != null)
                {
                    await UserPackage[pos].DegradeItemAsync(false);
                }
            }

            List<ushort> removeSkills = ExperienceManager.GetMagictypeOp(MagicTypeOperation.MagicOperation.RemoveOnRebirth, oldProf, prof, metempsychosis);
            List<ushort> resetSkills = ExperienceManager.GetMagictypeOp(MagicTypeOperation.MagicOperation.ResetOnRebirth, oldProf, prof, metempsychosis);
            List<ushort> learnSkills = ExperienceManager.GetMagictypeOp(MagicTypeOperation.MagicOperation.LearnAfterRebirth, oldProf, prof, metempsychosis);

            foreach (ushort skill in removeSkills)
            {
                await MagicData.UnlearnMagicAsync(skill, false);
            }

            foreach (ushort skill in resetSkills)
            {
                await MagicData.ResetSkillAsync(skill);
            }

            foreach (ushort skill in learnSkills)
            {
                await MagicData.CreateAsync(skill, 0);
            }

            if (UserPackage[ItemPosition.LeftHand]?.IsArrowSort() == false)
            {
                await UserPackage.UnEquipAsync(ItemPosition.LeftHand);
            }

            if (UserPackage[ItemPosition.RightHand]?.IsBow() == true && ProfessionSort != 4)
            {
                await UserPackage.UnEquipAsync(ItemPosition.RightHand);
            }

            logger.LogInformation("User [{Id}:{Name}] got {Metem} reborns.", Identity, Name, Metempsychosis);
            return true;
        }

        public async Task ResetUserAttributesAsync(byte mete, ushort newProf, ushort newLook, int newLev)
        {
            if (newProf == 0) newProf = (ushort)(Profession / 10 * 10 + 1);
            var prof = (byte)(newProf > 100 ? 10 : newProf / 10);

            int force = 5, speed = 2, health = 3, soul = 0;
            DbPointAllot pointAllot = ExperienceManager.GetPointAllot(prof, 1);
            if (pointAllot != null)
            {
                force = pointAllot.Strength;
                speed = pointAllot.Agility;
                health = pointAllot.Vitality;
                soul = pointAllot.Spirit;
            }
            else if (prof == 1)
            {
                force = 5;
                speed = 2;
                health = 3;
                soul = 0;
            }
            else if (prof == 2)
            {
                force = 5;
                speed = 2;
                health = 3;
                soul = 0;
            }
            else if (prof == 4)
            {
                force = 2;
                speed = 7;
                health = 1;
                soul = 0;
            }
            else if (prof == 10)
            {
                force = 0;
                speed = 2;
                health = 3;
                soul = 5;
            }

            AutoAllot = false;

            int newAttrib;
            if (mete < 2)
            {
                newAttrib = GetRebirthAddPoint(Profession, Level, mete) + newLev * 3;
            }
            else
            {
                // reincarnation, keep points no reset anymore or no point on reborning on 130
                newAttrib = Math.Min(MAX_USER_ATTRIB_POINTS, Strength + Speed + Vitality + Spirit + AttributePoints); // all the user current points
                newAttrib -= 10; // minus base class attribute points
                newAttrib -= ((Level - 15) * 3); // minus the points awarded by levels
            }

            await SetAttributesAsync(ClientUpdateType.Atributes, (ulong)newAttrib);
            await SetAttributesAsync(ClientUpdateType.Strength, (ulong)force);
            await SetAttributesAsync(ClientUpdateType.Agility, (ulong)speed);
            await SetAttributesAsync(ClientUpdateType.Vitality, (ulong)health);
            await SetAttributesAsync(ClientUpdateType.Spirit, (ulong)soul);
            await SetAttributesAsync(ClientUpdateType.Hitpoints, MaxLife);
            await SetAttributesAsync(ClientUpdateType.Mana, MaxMana);
            await SetAttributesAsync(ClientUpdateType.Stamina, DEFAULT_USER_ENERGY);
            await SetAttributesAsync(ClientUpdateType.XpCircle, 0);

            if (newLook > 0 && newLook != Mesh % 10)
            {
                await SetAttributesAsync(ClientUpdateType.Mesh, Mesh);
            }

            await SetAttributesAsync(ClientUpdateType.Level, (ulong)newLev);
            await SetAttributesAsync(ClientUpdateType.Experience, 0);

            if (mete == 0)
            {
                await SetAttributesAsync(ClientUpdateType.FirstProfession, Profession);
                FirstProfession = Profession;
            }
            else if (mete == 1)
            {
                await SetAttributesAsync(ClientUpdateType.PreviousProfession, Profession);
                PreviousProfession = Profession;
            }
            else
            {
                await SetAttributesAsync(ClientUpdateType.FirstProfession, PreviousProfession);
                await SetAttributesAsync(ClientUpdateType.PreviousProfession, Profession);
            }

            mete++;
            await SetAttributesAsync(ClientUpdateType.Class, newProf);
            await SetAttributesAsync(ClientUpdateType.Reborn, mete);
            await SaveAsync();
        }

        public int GetRebirthAddPoint(int oldProf, int oldLev, int metempsychosis)
        {
            var points = 0;

            if (metempsychosis == 0)
            {
                if (oldProf == HIGHEST_WATER_WIZARD_PROF)
                    points += Math.Min((1 + (oldLev - 110) / 2) * ((oldLev - 110) / 2) / 2, 55);
                else
                    points += Math.Min((1 + (oldLev - 120)) * (oldLev - 120) / 2, 55);
            }
            else
            {
                if (oldProf == HIGHEST_WATER_WIZARD_PROF)
                    points += 52 + Math.Min((1 + (oldLev - 110) / 2) * ((oldLev - 110) / 2) / 2, 55);
                else
                    points += 52 + Math.Min((1 + (oldLev - 120)) * (oldLev - 120) / 2, 55);
            }

            return points;
        }

        public async Task<bool> UnlearnAllSkillAsync()
        {
            return await WeaponSkill.UnearnAllAsync();
        }

        #endregion

        #region Online Training

        public uint GodTimeExp
        {
            get => character.OnlineGodExpTime;
            set => character.OnlineGodExpTime = Math.Max(0, Math.Min(value, 60000));
        }

        public uint OnlineTrainingExp
        {
            get => character.BattleGodExpTime;
            set => character.BattleGodExpTime = Math.Max(0, Math.Min(value, 60000));
        }

        #endregion

        #region Multiple Exp

        private ExperienceManager.ExperienceMultiplierData experienceMultiplierData;

        public void LoadExperienceData()
        {
            experienceMultiplierData = ExperienceManager.GetExperienceMultiplierData(Identity);
        }

        public bool HasMultipleExp => !experienceMultiplierData.Equals(default) && experienceMultiplierData.EndTime > DateTime.Now;
        public float ExperienceMultiplier => experienceMultiplierData.Equals(default) ? 1f : experienceMultiplierData.ExperienceMultiplier;

        public async Task SendMultipleExpAsync()
        {
            if (RemainingExperienceSeconds > 0)
            {
                await SynchroAttributesAsync(ClientUpdateType.MultipleExpTimer, RemainingExperienceSeconds, 0, (uint)(ExperienceMultiplier * 100), 0);
            }
        }

        public uint RemainingExperienceSeconds
        {
            get
            {
                if (!experienceMultiplierData.IsActive)
                {
                    return 0;
                }

                return (uint)experienceMultiplierData.RemainingSeconds;
            }
        }

        public async Task<bool> SetExperienceMultiplierAsync(uint seconds, float multiplier = 2f)
        {
            if (ExperienceManager.AddExperienceMultiplierData(Identity, multiplier, (int)seconds))
            {
                experienceMultiplierData = ExperienceManager.GetExperienceMultiplierData(Identity);
            }
            await SendMultipleExpAsync();
            await ProcessGoalManager.SetProgressAsync(this, ProcessGoalManager.GoalType.ExperienceMultiplier, 1);
            return true;
        }

        #endregion

        #region Attribute Points

        public ushort Strength
        {
            get => character?.Strength ?? 0;
            set => character.Strength = value;
        }

        public ushort Speed
        {
            get => character?.Agility ?? 0;
            set => character.Agility = value;
        }

        public ushort Vitality
        {
            get => character?.Vitality ?? 0;
            set => character.Vitality = value;
        }

        public ushort Spirit
        {
            get => character?.Spirit ?? 0;
            set => character.Spirit = value;
        }

        public ushort AttributePoints
        {
            get => character?.AttributePoints ?? 0;
            set => character.AttributePoints = value;
        }

        public int TotalAttributePoints => Strength + Speed + Vitality + Spirit + AttributePoints;

        #endregion

        #region XP and Stamina

        public int KoCount { get; set; }
        public byte Energy { get; private set; } = DEFAULT_USER_ENERGY;
        public byte MaxEnergy => (byte)(IsBlessed ? 150 : 100);

        public byte XpPoints;

        public async Task ProcXpValAsync()
        {
            if (!IsAlive)
            {
                await ClsXpValAsync();
                return;
            }

            IStatus pStatus = QueryStatus(StatusSet.START_XP);
            if (pStatus != null)
            {
                return;
            }

            if (XpPoints >= 100)
            {
                await BurstXpAsync();
                await SetXpAsync(0);
                xpPointsTimer.Update();
            }
            else
            {
                if (Map != null && Map.IsBoothEnable())
                {
                    return;
                }

                await AddXpAsync(1);
            }
        }

        public async Task<bool> BurstXpAsync()
        {
            if (XpPoints < 100)
            {
                return false;
            }

            IStatus pStatus = QueryStatus(StatusSet.START_XP);
            if (pStatus != null)
            {
                return true;
            }

            await AttachStatusAsync(this, StatusSet.START_XP, 0, 20, 0);
            return true;
        }

        public async Task SetXpAsync(byte nXp)
        {
            if (nXp > 100)
            {
                return;
            }

            await SetAttributesAsync(ClientUpdateType.XpCircle, nXp);
        }

        public async Task AddXpAsync(byte nXp)
        {
            if (nXp <= 0 
                || !IsAlive 
                || QueryStatus(StatusSet.START_XP) != null 
                || QueryStatus(StatusSet.SHURIKEN_VORTEX) != null
                || QueryStatus(StatusSet.CHAIN_BOLT_ACTIVE) != null
                || QueryStatus(StatusSet.BLADE_FLURRY) != null
                || QueryStatus(StatusSet.BLACK_BEARDS_RAGE) != null
                || QueryStatus(StatusSet.CANNON_BARRAGE) != null
                || QueryStatus(StatusSet.SUPER_CYCLONE) != null)
            {
                return;
            }

            await AddAttributesAsync(ClientUpdateType.XpCircle, nXp);
        }

        public async Task ClsXpValAsync()
        {
            XpPoints = 0;
            await StatusSet.DelObjAsync(StatusSet.START_XP);
        }

        public async Task FinishXpAsync()
        {
            int currentPoints = RoleManager.GetSupermanPoints(Identity);
            if (KoCount >= 25
                && currentPoints < KoCount)
            {
                await RoleManager.AddOrUpdateSupermanAsync(Identity, KoCount);
                int rank = RoleManager.GetSupermanRank(Identity);
                if (rank < 100)
                {
                    await BroadcastWorldMsgAsync(string.Format(StrSupermanBroadcast, Name, KoCount, rank), TalkChannel.Talk, Color.White);
                }
            }
            KoCount = 0;
        }

        #endregion

        #region Attributes Set and Add

        public override async Task<bool> AddAttributesAsync(ClientUpdateType type, long value)
        {
            var screen = false;
            bool save = false;
            switch (type)
            {
                case ClientUpdateType.Level:
                    {
                        if (value < 0)
                        {
                            return false;
                        }

                        screen = true;
                        value = Level = (byte)Math.Max(1, Math.Min(MAX_UPLEV, Level + value));

                        if (Syndicate != null)
                        {
                            SyndicateMember.Level = Level;
                        }

                        save = true;
                        await GameAction.ExecuteActionAsync(USER_UPLEV_ACTION, this, null, null, string.Empty);
                        break;
                    }

                case ClientUpdateType.Experience:
                    {
                        if (value < 0)
                        {
                            Experience = Math.Max(0, Experience - (ulong)(value * -1));
                        }
                        else
                        {
                            Experience += (ulong)value;
                        }

                        value = (long)Experience;
                        break;
                    }

                case ClientUpdateType.Strength:
                    {
                        if (value < 0)
                        {
                            return false;
                        }

                        int maxAddPoints = MAX_ATTRIBUTE_POINTS - TotalAttributePoints;
                        if (maxAddPoints < 0)
                        {
                            return false;
                        }

                        value = Math.Min(maxAddPoints, value);
                        value = Strength = (ushort)Math.Max(0, Math.Min(ushort.MaxValue, Strength + value));
                        save = true;
                        break;
                    }
                case ClientUpdateType.Agility:
                    {
                        if (value < 0)
                        {
                            return false;
                        }

                        int maxAddPoints = MAX_ATTRIBUTE_POINTS - TotalAttributePoints;
                        if (maxAddPoints < 0)
                        {
                            return false;
                        }
                        value = Math.Min(maxAddPoints, value);
                        value = Speed = (ushort)Math.Max(0, Math.Min(ushort.MaxValue, Speed + value));
                        save = true;
                        break;
                    }

                case ClientUpdateType.Vitality:
                    {
                        if (value < 0)
                        {
                            return false;
                        }

                        int maxAddPoints = MAX_ATTRIBUTE_POINTS - TotalAttributePoints;
                        if (maxAddPoints < 0)
                        {
                            return false;
                        }

                        value = Math.Min(maxAddPoints, value);
                        value = Vitality = (ushort)Math.Max(0, Math.Min(ushort.MaxValue, Vitality + value));
                        save = true;
                        break;
                    }

                case ClientUpdateType.Spirit:
                    {
                        if (value < 0)
                        {
                            return false;
                        }

                        int maxAddPoints = MAX_ATTRIBUTE_POINTS - TotalAttributePoints;
                        if (maxAddPoints < 0)
                        {
                            return false;
                        }
                        value = Math.Min(maxAddPoints, value);
                        value = Spirit = (ushort)Math.Max(0, Math.Min(ushort.MaxValue, Spirit + value));
                        save = true;
                        break;
                    }

                case ClientUpdateType.Atributes:
                    {
                        int maxAddPoints = MAX_ATTRIBUTE_POINTS - TotalAttributePoints;
                        if (maxAddPoints < 0)
                        {
                            return false;
                        }

                        value = Math.Min(maxAddPoints, value);
                        value = AttributePoints = (ushort)Math.Max(0, Math.Min(ushort.MaxValue, AttributePoints + value));
                        save = true;
                        break;
                    }

                case ClientUpdateType.XpCircle:
                    {
                        if (value < 0)
                        {
                            XpPoints = (byte)Math.Max(0, XpPoints - value * -1);
                        }
                        else
                        {
                            XpPoints = (byte)Math.Max(0, XpPoints + value);
                        }

                        value = XpPoints;
                        break;
                    }

                case ClientUpdateType.Stamina:
                    {
                        if (value < 0)
                        {
                            Energy = (byte)Math.Max(0, Energy - value * -1);
                        }
                        else
                        {
                            Energy = (byte)Math.Max(0, Math.Min(MaxEnergy, Energy + value));
                        }

                        value = Energy;
                        break;
                    }

                case ClientUpdateType.PkPoints:
                    {
                        value = PkPoints = (ushort)Math.Max(0, Math.Min(PkPoints + value, ushort.MaxValue));
                        await CheckPkStatusAsync();
                        save = true;
                        break;
                    }

                case ClientUpdateType.Vigor:
                    {
                        Vigor = Math.Max(0, Math.Min(MaxVigor, (int)value + Vigor));
                        await SendAsync(new MsgData
                        {
                            Action = MsgData.DataAction.SetMountMovePoint,
                            Year = Vigor
                        });
                        return true;
                    }

                case ClientUpdateType.Hitpoints:
                    {
                        value = Life = (uint)Math.Min(MaxLife, Math.Max(Life + value, 0));
                        await BroadcastTeamLifeAsync();
                        break;
                    }

                default:
                    {
                        bool result = await base.AddAttributesAsync(type, value);
                        return result && await SaveAsync();
                    }
            }

            if (save)
            {
                await SaveAsync();
            }
            await SynchroAttributesAsync(type, (ulong)value, screen);
            return true;
        }

        public override async Task<bool> SetAttributesAsync(ClientUpdateType type, ulong value)
        {
            var screen = false;
            switch (type)
            {
                case ClientUpdateType.Level:
                    {
                        screen = true;
                        Level = (byte)Math.Max(1, Math.Min(MAX_UPLEV, value));
                        break;
                    }

                case ClientUpdateType.Experience:
                    {
                        Experience = Math.Max(0, value);
                        break;
                    }

                case ClientUpdateType.XpCircle:
                    {
                        XpPoints = (byte)Math.Max(0, Math.Min(value, 100));
                        break;
                    }

                case ClientUpdateType.Stamina:
                    {
                        Energy = (byte)Math.Max(0, Math.Min(value, MaxEnergy));
                        break;
                    }

                case ClientUpdateType.Atributes:
                    {
                        AttributePoints = (ushort)Math.Max(0, Math.Min(ushort.MaxValue, value));
                        break;
                    }

                case ClientUpdateType.PkPoints:
                    {
                        PkPoints = (ushort)Math.Max(0, Math.Min(ushort.MaxValue, value));
                        await CheckPkStatusAsync();
                        break;
                    }

                case ClientUpdateType.Mesh:
                    {
                        screen = true;
                        Mesh = (uint)value;
                        break;
                    }

                case ClientUpdateType.HairStyle:
                    {
                        screen = true;
                        Hairstyle = (ushort)value;
                        break;
                    }

                case ClientUpdateType.Strength:
                    {
                        value = Strength = (ushort)Math.Min(ushort.MaxValue, value);
                        break;
                    }

                case ClientUpdateType.Agility:
                    {
                        value = Speed = (ushort)Math.Min(ushort.MaxValue, value);
                        break;
                    }

                case ClientUpdateType.Vitality:
                    {
                        value = Vitality = (ushort)Math.Min(ushort.MaxValue, value);
                        break;
                    }

                case ClientUpdateType.Spirit:
                    {
                        value = Spirit = (ushort)Math.Min(ushort.MaxValue, value);
                        break;
                    }

                case ClientUpdateType.Class:
                    {
                        screen = true;
                        Profession = (byte)value;

                        if (SyndicateMember != null)
                        {
                            SyndicateMember.Profession = (int)value;
                        }
                        break;
                    }

                case ClientUpdateType.FirstProfession:
                    {
                        FirstProfession = (byte)value;
                        break;
                    }

                case ClientUpdateType.PreviousProfession:
                    {
                        PreviousProfession = (byte)value;
                        break;
                    }

                case ClientUpdateType.Reborn:
                    {
                        Metempsychosis = (byte)Math.Min(2, Math.Max(0, value));
                        value = Math.Min(2, value);
                        break;
                    }

                case ClientUpdateType.VipLevel:
                    {
                        value = VipLevel = (uint)Math.Max(0, Math.Min(6, value));
                        await SendAsync(new MsgVipFunctionValidNotify() { Flags = (int)UserVipFlag });
                        break;
                    }

                case ClientUpdateType.Vigor:
                    {
                        Vigor = Math.Max(0, Math.Min(MaxVigor, (int)value));
                        await SendAsync(new MsgData
                        {
                            Action = MsgData.DataAction.SetMountMovePoint,
                            Year = Vigor
                        });
                        return true;
                    }

                case ClientUpdateType.Money:
                    {
                        Silvers = (uint)Math.Max(0, Math.Min(int.MaxValue, value));
                        return true;
                    }

                case ClientUpdateType.ConquerPoints:
                    {
                        ConquerPoints = (uint)Math.Max(0, Math.Min(int.MaxValue, value));
                        return true;
                    }

                case ClientUpdateType.BoundConquerPoints:
                    {
                        ConquerPointsBound = (uint)Math.Max(0, Math.Min(int.MaxValue, value));
                        return true;
                    }

                default:
                    bool result = await base.SetAttributesAsync(type, value);
                    return result && await SaveAsync();
            }

            await SaveAsync();
            await SynchroAttributesAsync(type, value, screen);
            return true;
        }

        public async Task CheckPkStatusAsync()
        {
            if (PkPoints > 99 && QueryStatus(StatusSet.BLACK_NAME) == null)
            {
                await DetachStatusAsync(StatusSet.RED_NAME);
                await AttachStatusAsync(this, StatusSet.BLACK_NAME, 0, int.MaxValue, 1);
            }
            else if (PkPoints > 29 && PkPoints < 100 && QueryStatus(StatusSet.RED_NAME) == null)
            {
                await DetachStatusAsync(StatusSet.BLACK_NAME);
                await AttachStatusAsync(this, StatusSet.RED_NAME, 0, int.MaxValue, 1);
            }
            else if (PkPoints < 30)
            {
                await DetachStatusAsync(StatusSet.BLACK_NAME);
                await DetachStatusAsync(StatusSet.RED_NAME);
            }
        }

        #endregion

        #region Heaven Blessing

        public async Task SendBlessAsync()
        {
            if (IsBlessed)
            {
                DateTime now = DateTime.Now;
                await SynchroAttributesAsync(ClientUpdateType.HeavensBlessing,
                                             (uint)(HeavenBlessingExpires - now).TotalSeconds);

                if (Map != null && !Map.IsTrainingMap())
                {
                    await SynchroAttributesAsync(ClientUpdateType.OnlineTraining, 0);
                }
                else
                {
                    await SynchroAttributesAsync(ClientUpdateType.OnlineTraining, 1);
                }

                await AttachStatusAsync(this, StatusSet.HEAVEN_BLESS, 0,
                                        (int)(HeavenBlessingExpires - now).TotalSeconds, 0);
            }
        }

        /// <summary>
        ///     This method will update the user blessing time.
        /// </summary>
        /// <param name="amount">The amount of minutes to be added.</param>
        /// <returns>If the heaven blessing has been added successfully.</returns>
        public async Task<bool> AddBlessingAsync(uint amount)
        {
            DateTime now = DateTime.Now;
            if (character.HeavenBlessing != 0 && UnixTimestamp.ToDateTime(character.HeavenBlessing) > now)
            {
                character.HeavenBlessing = (uint)UnixTimestamp.ToDateTime(character.HeavenBlessing).AddHours(amount).ToUnixTimestamp();
            }
            else
            {
                character.HeavenBlessing = (uint)now.AddHours(amount).ToUnixTimestamp();
            }

            await SendBlessAsync();
            return true;
        }

        public DateTime HeavenBlessingExpires => UnixTimestamp.ToDateTime(character.HeavenBlessing);

        public bool IsBlessed => UnixTimestamp.ToDateTime(character.HeavenBlessing) > DateTime.Now;

        #endregion

        #region Life and Mana

        public override uint Life
        {
            get => character.HealthPoints;
            set => character.HealthPoints = Math.Min(MaxLife, value);
        }

        public override uint MaxLife
        {
            get
            {
                if (Transformation != null)
                {
                    return (uint)Transformation.MaxLife;
                }

                var result = (uint)(Vitality * 24);
                result += (uint)((Strength + Speed + Spirit) * 3);
                switch (Profession)
                {
                    case 11:
                        result = (uint)(result * 1.05d);
                        break;
                    case 12:
                        result = (uint)(result * 1.08d);
                        break;
                    case 13:
                        result = (uint)(result * 1.10d);
                        break;
                    case 14:
                        result = (uint)(result * 1.12d);
                        break;
                    case 15:
                        result = (uint)(result * 1.15d);
                        break;
                }

                for (var pos = ItemPosition.EquipmentBegin;
                     pos <= ItemPosition.EquipmentEnd;
                     pos++)
                {
                    result += (uint)(UserPackage[pos]?.Life ?? 0);
                }

                result += (uint)AstProf.GetPower(AstProfType.Wrangler);
                result += (uint)Fate.HealthPoints;
                result += (uint)JiangHu.MaxLife;
                result += (uint)InnerStrength.MaxLife;
                return result;
            }
        }

        public override uint Mana
        {
            get => character.ManaPoints;
            set => character.ManaPoints = (ushort)Math.Min(MaxMana, value);
        }

        public override uint MaxMana
        {
            get
            {
                var result = (uint)(Spirit * 5);
                switch (Profession)
                {
                    case 132:
                    case 142:
                        result *= 3;
                        break;
                    case 133:
                    case 143:
                        result *= 4;
                        break;
                    case 134:
                    case 144:
                        result *= 5;
                        break;
                    case 135:
                    case 145:
                        result *= 6;
                        break;
                }

                for (var pos = ItemPosition.EquipmentBegin;
                     pos <= ItemPosition.EquipmentEnd;
                     pos++)
                {
                    result += (uint)(UserPackage[pos]?.Mana ?? 0);
                }
                result += (uint)JiangHu.MaxMana;
                return result;
            }
        }

        #endregion

        #region Currency

        public ulong Silvers
        {
            get => character?.Silver ?? 0;
            set => character.Silver = value;
        }

        public uint ConquerPoints
        {
            get => character?.ConquerPoints ?? 0;
            set => character.ConquerPoints = value;
        }

        public uint ConquerPointsBound
        {
            get => character?.ConquerPointsBound ?? 0;
            set => character.ConquerPointsBound = value;
        }

        public uint StorageMoney
        {
            get => character?.StorageMoney ?? 0;
            set => character.StorageMoney = value;
        }

        public uint StudyPoints
        {
            get => character?.Cultivation ?? 0;
            set => character.Cultivation = value;
        }

        public uint ChiPoints
        {
            get => character?.StrengthValue ?? 0;
            set => character.StrengthValue = value;
        }

        public uint HorseRacingPoints
        {
            get => character?.RidePetPoint ?? 0;
            set => character.RidePetPoint = value;
        }

        public async Task<bool> ChangeMoneyAsync(long amount, bool notify = false)
        {
            if (amount > 0)
            {
                await AwardMoneyAsync(amount);
                ServerStatisticManager.DropMoney(amount);
                return true;
            }
            if (amount < 0)
            {
                return await SpendMoneyAsync((amount * -1), notify);
            }
            return false;
        }

        public async Task AwardMoneyAsync(long amount)
        {
            Silvers = Math.Max(0, Math.Min(Silvers + (ulong)amount, 10_000_000_000));
            await SaveAsync();
            await SynchroAttributesAsync(ClientUpdateType.Money, Silvers);
        }

        public async Task<bool> SpendMoneyAsync(long amount, bool notify = false)
        {
            if (amount < 0)
            {
                logger.LogWarning("[Cheat] [{}] {} tried to submit negative amount of money to be discounted.", Identity, Name);
                return false;
            }

            if ((ulong)amount > Silvers)
            {
                if (notify)
                {
                    await SendAsync(StrNotEnoughMoney, TalkChannel.TopLeft, Color.Red);
                }

                return false;
            }

            Silvers = Math.Max(0, Math.Min(Silvers - (ulong)amount, 10_000_000_000));
            await SaveAsync();
            await SynchroAttributesAsync(ClientUpdateType.Money, Silvers);
            return true;
        }

        public async Task<bool> ChangeConquerPointsAsync(int amount, bool notify = false)
        {
            if (amount > 0)
            {
                await AwardConquerPointsAsync(amount);
                ServerStatisticManager.DropConquerPoints(amount);
                return true;
            }
            if (amount < 0)
            {
                return await SpendConquerPointsAsync(amount * -1, notify);
            }
            return false;
        }

        public async Task AwardConquerPointsAsync(int amount)
        {
            ConquerPoints = (uint)(ConquerPoints + amount);
            await SaveAsync();
            await SynchroAttributesAsync(ClientUpdateType.ConquerPoints, ConquerPoints);
        }

        public async Task<bool> SpendConquerPointsAsync(int amount, bool notify = false)
        {
            if (amount > ConquerPoints)
            {
                if (notify)
                {
                    await SendAsync(StrNotEnoughEmoney, TalkChannel.TopLeft, Color.Red);
                }

                return false;
            }

            ConquerPoints = (uint)(ConquerPoints - amount);
            await SaveAsync();
            await SynchroAttributesAsync(ClientUpdateType.ConquerPoints, ConquerPoints);
            return true;
        }

        public async Task<bool> SpendConquerPointsAsync(int amount, bool bound, bool notify)
        {
            if (!bound || ConquerPointsBound == 0)
            {
                return await SpendConquerPointsAsync(amount, notify);
            }

            if (amount > ConquerPoints + ConquerPointsBound)
            {
                if (notify)
                {
                    await SendAsync(StrNotEnoughEmoney, TalkChannel.TopLeft, Color.Red);
                }

                return false;
            }

            if (ConquerPointsBound > amount)
            {
                return await SpendBoundConquerPointsAsync(amount, notify);
            }

            int remain = (int)(amount - ConquerPointsBound);
            await SpendBoundConquerPointsAsync((int)ConquerPointsBound);
            await SpendConquerPointsAsync(remain);
            return true;
        }

        public async Task<bool> ChangeBoundConquerPointsAsync(int amount, bool notify = false)
        {
            if (amount > 0)
            {
                await AwardBoundConquerPointsAsync(amount);
                ServerStatisticManager.DropBoundConquerPoints(amount);
                return true;
            }
            if (amount < 0)
            {
                return await SpendBoundConquerPointsAsync(amount * -1, notify);
            }
            return false;
        }

        public async Task AwardBoundConquerPointsAsync(int amount)
        {
            ConquerPointsBound = (uint)(ConquerPointsBound + amount);
            await SaveAsync();
            await SynchroAttributesAsync(ClientUpdateType.BoundConquerPoints, ConquerPointsBound);
        }

        public async Task<bool> SpendBoundConquerPointsAsync(int amount, bool notify = false)
        {
            int emoney = 0;
            if (amount > ConquerPointsBound)
            {
                emoney = (int)(amount - ConquerPointsBound);
                if (ConquerPoints < emoney)
                {
                    if (notify)
                    {
                        await SendAsync(StrNotEnoughEmoney, TalkChannel.TopLeft, Color.Red);
                    }
                    return false;
                }
            }

            ConquerPointsBound = (uint)Math.Max(ConquerPointsBound - amount, 0);
            await SaveAsync();
            await SynchroAttributesAsync(ClientUpdateType.BoundConquerPoints, ConquerPointsBound);
            if (emoney > 0)
            {
                await SpendConquerPointsAsync(emoney);
            }
            return true;
        }

        public async Task<bool> ChangeCultivationAsync(int amount)
        {
            if (amount > 0)
            {
                await AwardCultivationAsync(amount);
                return true;
            }
            if (amount < 0)
            {
                return await SpendCultivationAsync(amount * -1);
            }
            return false;
        }

        public async Task AwardCultivationAsync(int amount)
        {
            StudyPoints = (uint)(StudyPoints + amount);
            await SaveAsync();

            await SendAsync(new MsgSubPro
            {
                Action = AstProfAction.UpdateStudy,
                Points = StudyPoints,
                Study = (ulong)amount
            });
        }

        public async Task<bool> SpendCultivationAsync(int amount)
        {
            if (amount > StudyPoints)
            {
                return false;
            }

            StudyPoints = (uint)(StudyPoints - amount);
            await SaveAsync();
            await SendAsync(new MsgSubPro
            {
                Action = AstProfAction.UpdateStudy,
                Points = StudyPoints
            });
            return true;
        }

        public async Task<bool> ChangeStrengthValueAsync(int amount)
        {
            if (amount > 0)
            {
                await AwardStrengthValueAsync(amount);
                return true;
            }
            if (amount < 0)
            {
                return await SpendStrengthValueAsync(amount * -1);
            }
            return false;
        }

        public async Task AwardStrengthValueAsync(int amount)
        {
            ChiPoints = (uint)(ChiPoints + amount);
            if (Fate != null)
            {
                await Fate.SendAsync(true);
            }
            await SaveAsync();
        }

        public async Task<bool> SpendStrengthValueAsync(int amount, bool sync = true)
        {
            if (amount > ChiPoints)
            {
                return false;
            }
            ChiPoints = (uint)(ChiPoints - amount);
            if (Fate != null && sync)
            {
                await Fate.SendAsync(true);
            }
            await SaveAsync();
            return true;
        }

        public async Task<bool> ChangeHorseRacePointsAsync(int amount)
        {
            if (amount > 0)
            {
                await AwardHorseRacePointsAsync(amount);
                return true;
            }
            if (amount < 0)
            {
                return await SpendHorseRacePointsAsync(amount * -1);
            }
            return false;
        }

        public async Task AwardHorseRacePointsAsync(int amount)
        {
            HorseRacingPoints = (uint)(HorseRacingPoints + amount);
            await SynchroAttributesAsync(ClientUpdateType.RidePetPoint, HorseRacingPoints);
            await SaveAsync();
        }

        public async Task<bool> SpendHorseRacePointsAsync(int amount)
        {
            if (amount > HorseRacingPoints)
            {
                return false;
            }
            HorseRacingPoints = (uint)(HorseRacingPoints - amount);
            await SynchroAttributesAsync(ClientUpdateType.RidePetPoint, HorseRacingPoints);
            await SaveAsync();
            return true;
        }

        #endregion

        #region Pk

        public PkModeType PkMode { get; set; }

        public JiangPkMode JiangPkImmunity 
        {
            get;// => (JiangPkMode)character.PkSettings;
            set;// => character.PkSettings = (uint)value;
        }

        public ushort PkPoints
        {
            get => character?.KillPoints ?? 0;
            set => character.KillPoints = value;
        }

        public uint PkSettings
        {
            get => character?.PkSettings ?? 0;
            set => character.PkSettings = value;
        }

        public async Task SetPkModeAsync(PkModeType mode)
        {
            if (PkMode == PkModeType.JiangHu)
            {
                await JiangHu.ExitJiangHuAsync();
            }

            PkMode = mode;

            if (PkMode == PkModeType.JiangHu)
            {
                await JiangHu.SendStatusAsync();
            }

            await SendAsync(new MsgAction
            {
                Identity = Identity,
                Action = ActionType.CharacterPkMode,
                Command = (uint)PkMode
            });
        }

        #endregion

        #region Movement

        public override async Task ProcessOnMoveAsync()
        {
            StopMining();

            if (AwaitingProgressBar != null && !AwaitingProgressBar.Completed)
            {
                await GameAction.ExecuteActionAsync(AwaitingProgressBar.IdNextFail, this, null, null, string.Empty);
                AwaitingProgressBar = null;

                await SendAsync(new MsgAction
                {
                    Action = ActionType.ProgressBar,
                    Identity = Identity,
                    Command = 0,
                    Direction = 1,
                    MapColor = 0,
                    Strings = new List<string>
                    {
                        "Error"
                    }
                });
            }

            if (QueryStatus(StatusSet.LUCKY_DIFFUSE) != null)
            {
                foreach (Character user in Screen.Roles.Values
                                                 .Where(x => x.IsPlayer() &&
                                                             x.QueryStatus(StatusSet.LUCKY_ABSORB)?.CasterId ==
                                                             Identity).Cast<Character>())
                {
                    await user.DetachStatusAsync(StatusSet.LUCKY_DIFFUSE);
                }
            }

            if (IsAway)
            {
                IsAway = false;

                await BroadcastRoomMsgAsync(new MsgAction
                {
                    Identity = Identity,
                    Action = ActionType.Away
                }, true);
            }

            idLuckyTarget = 0;

            protectionTimer.Clear();

            await base.ProcessOnMoveAsync();
        }

        public override async Task ProcessAfterMoveAsync()
        {
            if (Team != null)
            {
                await Team.ProcessAuraAsync();
            }

            if (Map.IsRaceTrack() && Map.QueryRegion(RegionType.RacingEndArea, X, Y))
            {
                HorseRacing horseRacing = EventManager.GetEvent<HorseRacing>();
                if (horseRacing != null)
                {
                    await horseRacing.CrossFinishLineAsync(this);
                }
            }

            await base.ProcessAfterMoveAsync();
        }

        public override async Task ProcessOnAttackAsync()
        {
            StopMining();

            energyTimer.Startup(ADD_ENERGY_STAND_MS);

            if (AwaitingProgressBar != null && !AwaitingProgressBar.Completed)
            {
                await GameAction.ExecuteActionAsync(AwaitingProgressBar.IdNextFail, this, null, null, string.Empty);
            }

            if (QueryStatus(StatusSet.LUCKY_DIFFUSE) != null)
            {
                foreach (Character user in Screen.Roles.Values
                                                 .Where(x => x.IsPlayer() &&
                                                             x.QueryStatus(StatusSet.LUCKY_ABSORB)?.CasterId ==
                                                             Identity).Cast<Character>())
                {
                    await user.DetachStatusAsync(StatusSet.LUCKY_DIFFUSE);
                }
            }

            if (IsAway)
            {
                IsAway = false;

                await BroadcastRoomMsgAsync(new MsgAction
                {
                    Identity = Identity,
                    Action = ActionType.Away
                }, true);
            }

            protectionTimer.Clear();

            await base.ProcessOnAttackAsync();
        }

        public async Task<bool> SynPositionAsync(ushort x, ushort y, int nMaxDislocation)
        {
            if (nMaxDislocation <= 0 || x == 0 && y == 0) // ignore in this condition
            {
                return true;
            }

            int nDislocation = GetDistance(x, y);
            if (nDislocation >= nMaxDislocation)
            {
                return false;
            }

            if (nDislocation <= 0)
            {
                return true;
            }

            if (IsGm())
            {
                await SendAsync($"syn move: ({X},{Y})->({x},{y})", TalkChannel.Talk, Color.Red);
            }

            if (!Map.IsValidPoint(x, y))
            {
                return false;
            }

            await ProcessOnMoveAsync();
            await JumpPosAsync(x, y);
            await Screen.BroadcastRoomMsgAsync(new MsgAction
            {
                Identity = Identity,
                Action = ActionType.Kickback,
                ArgumentX = x,
                ArgumentY = y,
                Command = (uint)((y << 16) | x),
                Direction = (ushort)Direction
            });

            return true;
        }

        public Task KickbackAsync()
        {
            return SendAsync(new MsgAction
            {
                Identity = Identity,
                Direction = (ushort)Direction,
                Map = MapIdentity,
                X = X,
                Y = Y,
                Action = ActionType.Kickback,
                Timestamp = (uint)Environment.TickCount
            });
        }

        #endregion

        #region Map

        public Screen Screen { get; init; }

        public override GameMap Map { get; protected set; }

        /// <summary>
        ///     The current map identity for the role.
        /// </summary>
        public override uint MapIdentity
        {
            get => idMap;
            set => idMap = value;
        }

        /// <summary>
        ///     Current X position of the user in the map.
        /// </summary>
        public override ushort X
        {
            get => currentX;
            set => currentX = value;
        }

        /// <summary>
        ///     Current X position of the user in the map.
        /// </summary>
        public override ushort Y
        {
            get => currentY;
            set => currentY = value;
        }

        public uint RecordMapIdentity
        {
            get => character.MapID;
            set => character.MapID = value;
        }

        public ushort RecordMapX
        {
            get => character.X;
            set => character.X = value;
        }

        public ushort RecordMapY
        {
            get => character.Y;
            set => character.Y = value;
        }

        /// <summary>
        /// </summary>
        public override async Task EnterMapAsync()
        {
            Map = MapManager.GetMap(idMap);
            if (Map != null)
            {
                await Map.AddAsync(this);
                await Map.SendMapInfoAsync(this);
                protectionTimer.Startup(CHGMAP_LOCK_SECS);
                await Screen.SynchroScreenAsync();

                var enteringEvent = EventManager.GetEvent(idMap);
                if (enteringEvent != null)
                {
                    if (await SignInEventAsync(enteringEvent))
                    {
                        await enteringEvent.OnEnterMapAsync(this);
                    }
                }

                // check qualifiers map
                var witnessEvents = EventManager.QueryWitnessEvents();
                foreach (var witnessEvent in witnessEvents)
                {
                    if (witnessEvent.IsWitness(this))
                    {
                        if (witnessEvent is ArenaQualifier arenaQualifier)
                        {
                            ArenaQualifierUserMatch match = arenaQualifier.FindMatchByMap(MapIdentity);
                            if (match != null)
                            {
                                await Task.WhenAll(SendAsync(new MsgArenicWitness()), match.SendBoardAsync());
                            }
                        }
                        else if (witnessEvent is TeamArenaQualifier teamQualifier)
                        {
                            TeamArenaQualifierMatch match = teamQualifier.FindMatchByMap(MapIdentity);
                            if (match != null)
                            {
                                await Task.WhenAll(SendAsync(new MsgArenicWitness()), match.SendBoardAsync());
                            }
                        }
                    }
                }

                if (Team != null)
                {
                    await Team.SyncFamilyBattlePowerAsync();
                    await Team.ProcessAuraAsync();
                }

                if (Map.IsPkField() && QueryStatus(StatusSet.RIDING) != null)
                {
                    await DetachStatusAsync(StatusSet.RIDING);
                }

                await ProcessAfterMoveAsync();

                await BroadcastNpcMsgAsync(new MsgAiAction
                {
                    Action = AiActionType.FlyMap,
                    Identity = Identity,
                    TargetIdentity = idMap,
                    X = X,
                    Y = Y
                });
            }
            else
            {
                logger.LogError($"Invalid map {idMap} for user {Identity} {Name}");
                Client?.Disconnect();
            }
        }

        /// <summary>
        /// </summary>
        public override async Task LeaveMapAsync()
        {
            BattleSystem.ResetBattle();
            await MagicData.AbortMagicAsync(false);
            StopMining();

            if (Map != null)
            {
                await ProcessOnMoveAsync();
                await Map.RemoveAsync(Identity);

                var currentEvent = GetCurrentEvent();
                if (currentEvent != null)
                {
                    await currentEvent.OnExitMapAsync(this, Map);
                }

                if (Map.IsRaceTrack())
                {
                    await ClearRaceItemsAsync();
                }

                if (IsAutoHangUp && !reviveLeaveMap)
                {
                    await FinishAutoHangUpAsync(HangUpMode.ChangedMap);
                }
                else
                {
                    reviveLeaveMap = false;
                }
            }

            if (Team != null)
            {
                await Team.SyncFamilyBattlePowerAsync();
                await Team.ProcessAuraAsync();
            }

            await BroadcastNpcMsgAsync(new MsgAiAction
            {
                Action = AiActionType.LeaveMap,
                Identity = Identity
            });

            await Screen.ClearAsync();
        }

        public async Task SavePositionAsync(uint idMap, ushort x, ushort y)
        {
            GameMap map = MapManager.GetMap(idMap);
            if (map?.IsRecordDisable() == false)
            {
                character.X = x;
                character.Y = y;
                character.MapID = idMap;
                await SaveAsync();
            }
        }

        public async Task<bool> FlyMapAsync(uint idMap, int x, int y)
        {
            if (Map == null)
            {
                logger.LogWarning("FlyMap user [{Identity}] not in map", Identity);
                return false;
            }

            if (idMap == 0)
            {
                idMap = MapIdentity;
            }

            GameMap newMap = MapManager.GetMap(idMap);
            if (newMap == null || !newMap.IsValidPoint(x, y))
            {
                logger.LogCritical("FlyMap user fly invalid position {idMap}[{x},{y}]", idMap, x, y);
                return false;
            }

            if (newMap.IsRaceTrack() && QueryStatus(StatusSet.RIDING) == null)
            {
                if (MagicData[7001] == null || !await ProcessMagicAttackAsync(7001, Identity, X, Y))
                {
                    logger.LogWarning($"Blocked flymap! User has no riding skill for map track");
                    return false;
                }
            }
            else if (newMap.IsFamilyMap() && QueryStatus(StatusSet.RIDING) == null)
            {
                await DetachStatusAsync(StatusSet.RIDING);
            }

            if (!newMap.IsStandEnable(x, y))
            {
                bool succ = false;
                for (int i = 0; i < 8; i++)
                {
                    int testX = x + GameMapData.WalkXCoords[i];
                    int testY = y + GameMapData.WalkYCoords[i];

                    if (newMap.IsStandEnable(testX, testY))
                    {
                        x = testX;
                        y = testY;
                        succ = true;
                        break;
                    }
                }

                if (!succ)
                {
                    newMap = MapManager.GetMap(1002);
                    x = 300;
                    y = 278;
                }
            }

            try
            {
                await LeaveMapAsync(); // leave map on current partition

                this.idMap = newMap.Identity;
                X = (ushort)x;
                Y = (ushort)y;

                await SendAsync(new MsgAction
                {
                    Identity = Identity,
                    Command = newMap.MapDoc,
                    X = X,
                    Y = Y,
                    Action = ActionType.MapTeleport,
                    Direction = (ushort)Direction
                });

                Task characterEnterMapTask()
                {
                    return EnterMapAsync();
                }

                if (newMap.Partition == Map.Partition)
                {
                    await characterEnterMapTask();
                }
                else
                {
                    QueueAction(characterEnterMapTask);
                }
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Fly map error", ex.Message);
            }
            return true;
        }

        public Role QueryRole(uint idRole)
        {
            return Map.QueryAroundRole(this, idRole);
        }

        #endregion

        #region Chat

        public bool CanUseWorldChat()
        {
            if (Level < 50)
            {
                return false;
            }

            if (Level < 70 && worldChatTimer.ToNextTime(60))
            {
                return false;
            }
            // todo get correct times
            return worldChatTimer.ToNextTime(15);
        }

        #endregion

        #region Vigor

        public int Vigor { get; set; }

        public int MaxVigor
        {
            get
            {
                int value = 0;
                if (QueryStatus(StatusSet.RIDING) != null)
                {
                    value += UserPackage[ItemPosition.Steed]?.Vigor ?? 0;
                    value += UserPackage[ItemPosition.Crop]?.Vigor ?? 0;
                }
                return value;
            }
        }

        public void UpdateVigorTimer()
        {
            vigorTimer.Update();
        }

        #endregion

        #region VIP

        private readonly TimeOut vipCmdTeleportTimer = new(120);

        public bool IsVipTeleportEnable()
        {
            return vipCmdTeleportTimer.ToNextTime();
        }

        public uint BaseVipLevel => Math.Min(6, Math.Max(0, VipLevel));

        public uint VipLevel { get; set; }

        public VipFlags UserVipFlag
        {
            get
            {
                return BaseVipLevel switch
                {
                    1 => VipFlags.VipOne,
                    2 => VipFlags.VipTwo,
                    3 => VipFlags.VipThree,
                    4 => VipFlags.VipFour,
                    5 => VipFlags.VipFive,
                    6 => VipFlags.VipSix,
                    _ => 0,
                };
            }
        }

        #endregion

        #region Lucky

        public const int LUCKY_TIME_SECS_LIMIT = 60 * 120;

        public Task ChangeLuckyTimerAsync(int value)
        {
            ulong ms = 0;

            luckyTimeCount += value;
            if (luckyTimeCount > 0 && value > 0)
            {
                character.LuckyTime = (uint)DateTime.Now.AddSeconds(Math.Min(LUCKY_TIME_SECS_LIMIT, luckyTimeCount)).ToUnixTimestamp();
            }

            if (IsLucky)
            {
                ms = (ulong)(UnixTimestamp.ToDateTime(character.LuckyTime) - DateTime.Now).TotalSeconds * 1000UL;
            }

            return SynchroAttributesAsync(ClientUpdateType.LuckyTimeTimer, ms);
        }

        public bool IsLucky => character.LuckyTime != 0 && UnixTimestamp.ToDateTime(character.LuckyTime) > DateTime.Now;

        public async Task SendLuckAsync()
        {
            if (IsLucky)
            {
                await SynchroAttributesAsync(ClientUpdateType.LuckyTimeTimer, (ulong)(UnixTimestamp.ToDateTime(character.LuckyTime) - DateTime.Now).TotalSeconds * 1000UL);
            }
        }

        #endregion

        #region Status

        public bool IsAway { get; set; }

        public async Task LoadStatusAsync()
        {
            List<DbStatus> statusList = await StatusRepository.GetAsync(Identity);
            await using var serverDbContext = new ServerDbContext();
            foreach (DbStatus status in statusList)
            {
                if (UnixTimestamp.ToDateTime(status.EndTime) < DateTime.Now)
                {
                    serverDbContext.Status.Remove(status);
                    continue;
                }

                await AttachStatusAsync(status);
            }
            await serverDbContext.SaveChangesAsync();
        }

        #endregion

        #region Authority

        public bool IsPm()
        {
            return Name.Contains("[PM]");
        }

        public bool IsGm()
        {
            return IsPm() || Name.Contains("[GM]");
        }

        public bool DisplayAction { get; set; } = false;

        #endregion

        #region Offline TG

        public ushort MaxTrainingMinutes => (ushort)Math.Min(1440 + 60 * VipLevel, (UnixTimestamp.ToDateTime(character.HeavenBlessing) - DateTime.Now).TotalMinutes);

        public ushort CurrentTrainingMinutes => (ushort)Math.Min((DateTime.Now - LastLogin).TotalMinutes * 10, MaxTrainingMinutes);

        public ushort CurrentOfflineTrainingTime
        {
            get
            {
                if (character.AutoExercise == 0 || character.LogoutTime2 == 0)
                {
                    return 0;
                }

                DateTime endTime = UnixTimestamp.ToDateTime(character.LogoutTime2).AddMinutes(character.AutoExercise);
                if (endTime < DateTime.Now)
                {
                    return CurrentTrainingTime;
                }

                var remainingTime = (int)Math.Min((DateTime.Now - UnixTimestamp.ToDateTime(character.LogoutTime2)).TotalMinutes, CurrentTrainingTime);
                return (ushort)remainingTime;
            }
        }

        public ushort CurrentTrainingTime => character.AutoExercise;

        public bool IsOfflineTraining => character.AutoExercise != 0;

        public async Task EnterAutoExerciseAsync()
        {
            if (!IsBlessed)
            {
                return;
            }

            character.AutoExercise = CurrentTrainingMinutes;
            character.LogoutTime2 = (uint)DateTime.Now.ToUnixTimestamp();
            await SaveAsync();
        }

        public async Task LeaveAutoExerciseAsync()
        {
            await AwardExperienceAsync(CalculateExpBall(GetAutoExerciseExpTimes()), true);

            await FlyMapAsync(RecordMapIdentity, RecordMapX, RecordMapY);

            character.AutoExercise = 0;
            character.LogoutTime2 = 0;
            await SaveAsync();
        }

        public int GetAutoExerciseExpTimes()
        {
            const int MAX_REWARD = 3000; // 5 Exp Balls every 8 hours
            const double REWARD_EVERY_N_MINUTES = 480;
            return (int)(Math.Min(CurrentOfflineTrainingTime, CurrentTrainingTime) / REWARD_EVERY_N_MINUTES *
                          MAX_REWARD);
        }

        public ExperiencePreview GetCurrentOnlineTGExp()
        {
            return PreviewExpBallUsage(GetAutoExerciseExpTimes());
        }

        #endregion

        #region Nationality

        public PlayerCountry Nationality
        {
            get => (PlayerCountry)character.Nationality;
            set => character.Nationality = (ushort)value;
        }

        #endregion

        #region Team

        public uint VirtuePoints
        {
            get => character.Virtue;
            set => character.Virtue = value;
        }

        #endregion

        #region Weapon Skill

        public WeaponSkill WeaponSkill { get; init; }

        public async Task AddWeaponSkillExpAsync(ushort type, int experience, bool byAction = false)
        {
            DbWeaponSkill skill = WeaponSkill[type];
            if (skill == null)
            {
                await WeaponSkill.CreateAsync(type, 0);
                if ((skill = WeaponSkill[type]) == null)
                {
                    return;
                }
            }

            if (skill.Level >= MAX_WEAPONSKILLLEVEL)
            {
                return;
            }

            if (skill.Unlearn != 0)
            {
                skill.Unlearn = 0;
            }

            experience = (int)(experience * (1 + VioletGemBonus / 100d));

            uint increaseLev = 0;
            if (skill.Level > MASTER_WEAPONSKILLLEVEL)
            {
                int ratio = 100 - (skill.Level - MASTER_WEAPONSKILLLEVEL) * 20;
                if (ratio < 10)
                {
                    ratio = 10;
                }

                experience = Calculations.MulDiv(experience, ratio, 100) / 2;
            }

            var nNewExp = (int)Math.Max(experience + skill.Experience, skill.Experience);

            int nLevel = skill.Level;
            var oldPercent = (uint)(skill.Experience / (double)WeaponSkill.RequiredExperience[nLevel] * 100);
            if (nLevel < MAX_WEAPONSKILLLEVEL)
            {
                if (nNewExp > WeaponSkill.RequiredExperience[nLevel] ||
                    nLevel >= skill.OldLevel / 2 && nLevel < skill.OldLevel)
                {
                    nNewExp = 0;
                    increaseLev = 1;
                }
            }

            if (byAction || skill.Level < Level / 10 + 1
                         || skill.Level >= MASTER_WEAPONSKILLLEVEL)
            {
                skill.Experience = (uint)nNewExp;

                if (increaseLev > 0)
                {
                    skill.Level += (byte)increaseLev;
                    await SendAsync(StrWeaponSkillUp);
                    await WeaponSkill.SaveAsync(skill);
                }
                else
                {
                    var newPercent =
                        (int)(skill.Experience / (double)WeaponSkill.RequiredExperience[nLevel] * 100);
                    if (oldPercent - oldPercent % 10 != newPercent - newPercent % 10)
                    {
                        await WeaponSkill.SaveAsync(skill);
                    }
                }

                await SendAsync(new MsgWeaponSkill(skill));
            }
        }

        #endregion

        #region Home

        public uint HomeIdentity
        {
            get => character?.HomeIdentity ?? 0u;
            set => character.HomeIdentity = value;
        }

        #endregion

        #region User Secondary Password

        public ulong SecondaryPassword
        {
            get => character.LockKey;
            set => character.LockKey = value;
        }

        public bool IsUnlocked()
        {
            return SecondaryPassword == 0 || VarData[0] != 0;
        }

        public void UnlockSecondaryPassword()
        {
            VarData[0] = 1;
        }

        public bool CanUnlock2ndPassword()
        {
            return VarData[1] <= 2;
        }

        public void Increment2ndPasswordAttempts()
        {
            VarData[1] += 1;
        }

        public async Task SendSecondaryPasswordInterfaceAsync()
        {
            await GameAction.ExecuteActionAsync(8003020, this, null, null, string.Empty);
        }

        #endregion

        #region Lottery

        public byte LotteryLastColor { get; set; } = 0;
        public byte LotteryLastRank { get; set; } = 0;
        public string LotteryLastItemName { get; set; } = "";
        public DbItem LotteryTemporaryItem { get; set; } = null;

        public int LotteryRetries { get; private set; }

        public async Task AcceptLotteryPrizeAsync()
        {
            if (LotteryTemporaryItem == null)
            {
                return;
            }

            Item item = new(this);
            if (!await item.CreateAsync(LotteryTemporaryItem))
            {
                return;
            }

            await UserPackage.AddItemAsync(item);

            if (LotteryLastRank <= 5)
            {
                await BroadcastWorldMsgAsync(string.Format(StrLotteryHigh, Name, LotteryLastItemName), TalkChannel.Talk, Color.White);
            }
            else
            {
                await SendAsync(string.Format(StrLotteryLow, LotteryLastItemName));
            }

            LotteryRetries = 0;
            LotteryLastRank = 0;
            LotteryLastItemName = "";
            LotteryTemporaryItem = null;
        }

        public async Task LotteryTryAgainAsync()
        {
            if (LotteryTemporaryItem == null)
            {
                return; // user has really tried lottery before? There's no prize pending.
            }

            if (LotteryRetries >= 2)
            {
                return; // user has already tried 3 times
            }

            if (!await UserPackage.MultiSpendItemAsync(SMALL_LOTTERY_TICKET, SMALL_LOTTERY_TICKET, 1))
            {
                await SendAsync(StrEmbedNoRequiredItem);
                return;
            }

            await LotteryManager.QueryPrizeAsync(this, LotteryLastColor, true);
            LotteryRetries++;
        }

        #endregion

        #region Peerage

        public NobilityRank NobilityRank => PeerageManager.GetRanking(Identity);

        public int NobilityPosition => PeerageManager.GetPosition(Identity);

        public long NobilityDonation
        {
            get => (long)character.Donation;
            set => character.Donation = (ulong)value;
        }

        public async Task SendNobilityInfoAsync(bool broadcast = false)
        {
            MsgPeerage msg = new()
            {
                Action = NobilityAction.Info,
                DataLow = Identity
            };
            msg.Strings.Add($"{Identity} {NobilityDonation} {(int)NobilityRank:d} {NobilityPosition}");
            await SendAsync(msg);

            if (broadcast)
            {
                await BroadcastRoomMsgAsync(msg, false);
            }
        }

        #endregion

        #region Requests

        private readonly ConcurrentDictionary<RequestType, uint> requests = new();
        private int requestData;

        public void SetRequest(RequestType type, uint target, int data = 0)
        {
            requests.TryRemove(type, out _);
            if (target == 0)
            {
                return;
            }

            requestData = data;
            requests.TryAdd(type, target);
        }

        public uint QueryRequest(RequestType type)
        {
            return requests.TryGetValue(type, out uint value) ? value : 0;
        }

        public int QueryRequestData(RequestType type)
        {
            if (requests.TryGetValue(type, out _))
            {
                return requestData;
            }

            return 0;
        }

        public uint PopRequest(RequestType type)
        {
            if (requests.TryRemove(type, out uint value))
            {
                requestData = 0;
                return value;
            }
            return 0;
        }

        #endregion

        #region Flower

        public uint FlowerCharm { get; set; }
        public uint FairyType { get; set; }

        public bool IsSendGiftEnable()
        {
            if (SendFlowerTime == 0)
            {
                return true;
            }

            DateTime today = DateTime.Now.Date;
            DateTime lastSendFlower = DateTime.ParseExact(SendFlowerTime.ToString(), "yyyyMMdd", CultureInfo.InvariantCulture).Date;
            return lastSendFlower < today;
        }

        public uint SendFlowerTime
        {
            get => character.SendFlowerDate;
            set => character.SendFlowerDate = value;
        }

        public uint FlowerRed
        {
            get => character.FlowerRed;
            set => character.FlowerRed = value;
        }

        public uint FlowerWhite
        {
            get => character.FlowerWhite;
            set => character.FlowerWhite = value;
        }

        public uint FlowerOrchid
        {
            get => character.FlowerOrchid;
            set => character.FlowerOrchid = value;
        }

        public uint FlowerTulip
        {
            get => character.FlowerTulip;
            set => character.FlowerTulip = value;
        }

        #endregion

        #region Mail Box

        public MailBox MailBox { get; init; }

        #endregion

        #region User Title

        public byte TitleSelect
        {
            get => character.TitleSelect;
            set => character.TitleSelect = value;
        }

        public uint Title
        {
            get => character.Title;
            set => character.Title = value;
        }

        public async Task LoadTitlesAsync()
        {
            const int GAME_STAFF_TITLE = 1;
            if (IsGm() && !HasTitle(GAME_STAFF_TITLE))
            {
                await AddTitleAsync(GAME_STAFF_TITLE);
                await BroadcastRoomMsgAsync(new MsgTitle
                {
                    Action = MsgTitle.TitleAction.Select,
                    Identity = Identity,
                    Title = GAME_STAFF_TITLE
                }, true);
            }
            else if (!IsGm() && HasTitle(GAME_STAFF_TITLE))
            {
                await RemoveTitleAsync(GAME_STAFF_TITLE);
                await BroadcastRoomMsgAsync(new MsgTitle
                {
                    Action = MsgTitle.TitleAction.Select,
                    Identity = Identity,
                    Title = 0
                }, true);
            }
        }

        private bool IsValidTitle(int title)
        {
            return title is > 0 and <= 32;
        }

        public bool HasTitle(int title)
        {
            uint titleFlag = 1u << (title - 1);
            return (character.Title & titleFlag) != 0;
        }

        public Task<bool> AddTitleAsync(int title)
        {
            if (HasTitle(title))
            {
                return Task.FromResult(true);
            }

            if (!IsValidTitle(title))
            {
                return Task.FromResult(false);
            }

            uint titleFlag = 1u << (title - 1);
            character.Title |= titleFlag;
            return SaveAsync();
        }

        public async Task<bool> RemoveTitleAsync(int title)
        {
            if (!HasTitle(title))
            {
                return false;
            }

            uint titleFlag = 1u << (title - 1);
            character.Title &= ~titleFlag;
            
            if (TitleSelect == title)
            {
                TitleSelect = 0;
                await BroadcastRoomMsgAsync(new MsgTitle
                {
                    Action = MsgTitle.TitleAction.Select,
                    Identity = Identity
                }, true);
            }
            return await SaveAsync();
        }

        public static Task AddTitleAsync(uint idUser, int title)
        {
            Character user = RoleManager.GetUser(idUser);
            if (user != null)
            {
                return user.AddTitleAsync(title);
            }
            else
            {
                uint titleFlag = 1u << (title - 1);
                return ServerDbContext.ScalarAsync($"UPDATE cq_user SET title = title | {titleFlag}, title_select = 0 WHERE id={idUser} LIMIT 1;");
            }
        }

        public static Task RemoveTitleAsync(uint idUser, int title)
        {
            Character user = RoleManager.GetUser(idUser);
            if (user != null)
            {
                return user.RemoveTitleAsync(title);
            }
            else
            {
                uint titleFlag = 1u << (title - 1);
                return ServerDbContext.ScalarAsync($"UPDATE cq_user SET title = title & ~{titleFlag}, title_select = 0 WHERE id={idUser} LIMIT 1;");
            }
        }

        public async Task SelectTitleAsync(int title)
        {
            if (TitleSelect != 0 && title == 0)
            {
                TitleSelect = 0;
                await BroadcastRoomMsgAsync(new MsgTitle
                {
                    Action = MsgTitle.TitleAction.Select,
                    Identity = Identity
                }, true);
                return;
            }

            if (!HasTitle(title))
            {
                return;
            }

            TitleSelect = (byte)title;
            await BroadcastRoomMsgAsync(new MsgTitle
            {
                Action = MsgTitle.TitleAction.Select,
                Identity = Identity,
                Title = (byte)title
            }, true);
            await SaveAsync();
        }

        public async Task SendTitlesAsync()
        {
            for (int title = 1; title <= 32; title++)
            {
                if (HasTitle(title))
                {
                    await SendAsync(new MsgTitle
                    {
                        Action = MsgTitle.TitleAction.Add,
                        Title = (byte)title,
                        Identity = Identity
                    });
                }
            }
        }

        #endregion

        #region Mining

        private static readonly ILogger mineLogger = LogFactory.CreateGmLogger("mining");
        private int mineCount;
        private int oreCount;
        private int itemCount;

        public void StartMining()
        {
            miningTimer.Startup(3);
            mineCount = 0;
            oreCount = 0;
            itemCount = 0;
        }

        public void StopMining()
        {
            miningTimer.Clear();
        }

        public async Task DoMineAsync()
        {
            if (!IsAlive)
            {
                await SendAsync(StrDead);
                StopMining();
                return;
            }

            if (!Map.IsMineField())
            {
                await SendAsync(StrNoMine);
                StopMining();
                return;
            }

            if (UserPackage[Item.ItemPosition.RightHand]?.GetItemSubType() != 562)
            {
                await SendAsync(StrMineWithPecker);
                StopMining();
                return;
            }

            try
            {
                if (UserPackage.IsPackFull())
                {
                    await SendAsync(StrYourBagIsFull);
                }
                else
                {
                    uint idItem = 0;
                    float nChance = 30f + (float)(WeaponSkill[562]?.Level ?? 0) / 2;
                    if (await ChanceCalcAsync(nChance))
                    {
                        const int euxiniteOre = 1072031;
                        const int ironOre = 1072010;
                        const int copperOre = 1072020;
                        const int silverOre = 1072040;
                        const int goldOre = 1072050;
                        int oreRate = await NextAsync(100);
                        int oreLevel = await NextAsync(10) % 10;
                        switch (Map.ResLev) // TODO gems
                        {
                            case 1:
                                {
                                    if (oreRate < 4) // 4% Euxinite
                                    {
                                        idItem = euxiniteOre;
                                    }
                                    else if (oreRate < 6) // 6% Gold Ore
                                    {
                                        idItem = (uint)(goldOre + oreLevel);
                                    }
                                    else if (oreRate < 50) // 40% Iron Ore
                                    {
                                        idItem = (uint)(ironOre + oreLevel);
                                    }

                                    break;
                                }
                            case 2:
                                {
                                    if (oreRate < 5) // 5% Gold Ore
                                    {
                                        idItem = (uint)(goldOre + oreLevel);
                                    }
                                    else if (oreRate < 15) // 10% Copper Ore
                                    {
                                        idItem = (uint)(copperOre + oreLevel);
                                    }
                                    else if (oreRate < 50) // 35% Iron Ore
                                    {
                                        idItem = (uint)(ironOre + oreLevel);
                                    }

                                    break;
                                }
                            case 3:
                                {
                                    if (oreRate < 5) // 5% Gold Ore
                                    {
                                        idItem = (uint)(goldOre + oreLevel);
                                    }
                                    else if (oreRate < 12) // 7% Silver Ore
                                    {
                                        idItem = (uint)(silverOre + oreLevel);
                                    }
                                    else if (oreRate < 25) // 13% Copper Ore
                                    {
                                        idItem = (uint)(copperOre + oreLevel);
                                    }
                                    else if (oreRate < 50) // 25% Iron Ore
                                    {
                                        idItem = (uint)(ironOre + oreLevel);
                                    }

                                    break;
                                }
                        }

                        oreCount++;
                    }
                    else
                    {
                        idItem = await MineManager.MineAsync(MapIdentity, this);
                        itemCount++;
                    }

                    DbItemtype itemtype = ItemManager.GetItemtype(idItem);
                    if (itemtype == null)
                    {
                        return;
                    }

                    if (await UserPackage.AwardItemAsync(idItem))
                    {
                        await SendAsync(string.Format(StrMineItemFound, itemtype.Name));
                        mineLogger.LogInformation($"{Identity},{Name},{idItem},{MapIdentity},{Map?.Name},{X},{Y}");
                    }

                    mineCount++;
                }
            }

            catch (Exception ex)
            {
                logger.LogCritical(ex, "Error on mining. {ex}", ex.Message);
            }
            finally
            {
                await BroadcastRoomMsgAsync(new MsgAction
                {
                    Identity = Identity,
                    Command = 0,
                    ArgumentX = X,
                    ArgumentY = Y,
                    Action = ActionType.MapMine
                }, true);
            }
        }

        #endregion

        #region Quiz

        public uint QuizPoints
        {
            get => character.QuizPoints;
            set => character.QuizPoints = value;
        }

        #endregion

        #region Vip Teleport

        private const int PERSONAL_VIP_TELEPORT_COOLDOWN = 180;
        private const int TEAM_VIP_TELEPORT_COOLDOWN = 300;

        private readonly TimeOut portalTeleportTimer = new();
        private readonly TimeOut cityTeleportTimer = new();
        private readonly TimeOut teamPortalTeleportTimer = new();
        private readonly TimeOut teamCityPortalTeleportTimer = new();

        public bool CanUseVipPortal() => BaseVipLevel >= 3 && (!portalTeleportTimer.IsActive() || portalTeleportTimer.IsTimeOut());
        public bool CanUseVipCityTeleport() => BaseVipLevel >= 2 && (!cityTeleportTimer.IsActive() || cityTeleportTimer.IsTimeOut());
        public bool CanUseVipTeamPortal() => BaseVipLevel >= 3 && (!teamPortalTeleportTimer.IsActive() || teamPortalTeleportTimer.IsTimeOut());
        public bool CanUseVipTeamCityTeleport() => BaseVipLevel >= 3 && (!teamCityPortalTeleportTimer.IsActive() || teamCityPortalTeleportTimer.IsTimeOut());

        public void UseVipPortal()
        {
            portalTeleportTimer.Startup(PERSONAL_VIP_TELEPORT_COOLDOWN);
        }

        public void UseVipCityPortal()
        {
            cityTeleportTimer.Startup(PERSONAL_VIP_TELEPORT_COOLDOWN);
        }

        public void UseVipTeamPortal()
        {
            teamPortalTeleportTimer.Startup(TEAM_VIP_TELEPORT_COOLDOWN);
        }

        public void UseVipTeamCityPortal()
        {
            teamCityPortalTeleportTimer.Startup(TEAM_VIP_TELEPORT_COOLDOWN);
        }

        #endregion

        #region Layout

        public byte CurrentLayout
        {
            get => character.ShowType;
            set => character.ShowType = value;
        }

        #endregion

        #region Call Pet

        private TimeOut callPetKeepSecs = new();
        private Monster callPet;

        public async Task<bool> CallPetAsync(uint type, ushort x, ushort y, int keepSecs = 0)
        {
            await KillCallPetAsync();

            Monster pet = await Monster.CreateCallPetAsync(this, type, x, y);
            if (pet == null)
                return false;

            callPet = pet;

            if (keepSecs > 0)
            {
                callPetKeepSecs.Startup(keepSecs);
            }
            else
            {
                callPetKeepSecs.Clear();
            }
            return true;
        }

        public async Task KillCallPetAsync(bool now = false)
        {
            if (callPet == null)
                return;

            if (!callPet.IsDeleted())
            {
                await callPet.DelMonsterAsync(now);
                callPet = null;
            }
        }

        public Role GetCallPet()
        {
            return callPet;
        }

        #endregion

        #region Jar

        public async Task AddJarKillsAsync(int stcType)
        {
            Item jar = UserPackage.GetItemByType(Item.TYPE_JAR);
            if (jar != null)
                if (jar.MaximumDurability == stcType)
                {
                    jar.Data += 1;
                    await jar.SaveAsync();

                    if (jar.Data % 50 == 0)
                    {
                        await jar.SendJarAsync();
                    }
                }
        }

        #endregion

        #region Hung Up

        public async Task FinishAutoHangUpAsync(HangUpMode mode) 
        {
            if (!IsAutoHangUp)
            {
                return;
            }

            if (mode == HangUpMode.KilledNoBlessing || mode == HangUpMode.ChangedMap)
            {
                await SendAsync(new MsgHangUp
                {
                    Action = mode,
                    Experience = AutoHangUpExperience
                });

                await SendAsync(new MsgHangUp
                {
                    Action = HangUpMode.End
                });
            }

            await AwardExperienceAsync((long)AutoHangUpExperience, true);
            AutoHangUpExperience = 0;

            IsAutoHangUp = false;
        }

        #endregion

        #region Cool Action

        public bool IsCoolEnable()
        {
            return coolSyncTimer.ToNextTime();
        }

        public bool IsFullSuper()
        {
            for (ItemPosition pos = ItemPosition.EquipmentBegin; pos <= ItemPosition.EquipmentEnd; pos++)
            {
                Item item = UserPackage[pos];
                if (item == null)
                {
                    switch (pos)
                    {
                        case ItemPosition.Steed:
                        case ItemPosition.Gourd:
                        case ItemPosition.Garment:
                        case ItemPosition.RightHandAccessory:
                        case ItemPosition.LeftHandAccessory:
                        case ItemPosition.SteedArmor:
                        case (ItemPosition)13:
                        case (ItemPosition)14:
                            continue;
                        default:
                            return false;
                    }
                }

                if (!item.IsEquipment())
                {
                    continue;
                }

                if (item.GetQuality() % 10 < 9)
                {
                    return false;
                }
            }
            return true;
        }

        public bool IsFullUnique()
        {
            for (ItemPosition pos = ItemPosition.EquipmentBegin; pos <= ItemPosition.EquipmentEnd; pos++)
            {
                Item item = UserPackage[pos];
                if (item == null)
                {
                    switch (pos)
                    {
                        case ItemPosition.Steed:
                        case ItemPosition.Gourd:
                        case ItemPosition.Garment:
                        case ItemPosition.RightHandAccessory:
                        case ItemPosition.LeftHandAccessory:
                        case ItemPosition.SteedArmor:
                        case (ItemPosition)13:
                        case (ItemPosition)14:
                            continue;
                        default:
                            return false;
                    }
                }

                if (!item.IsEquipment())
                {
                    continue;
                }

                if (item.GetQuality() % 10 < 7)
                {
                    return false;
                }
            }
            return true;
        }

        #endregion

        #region Change Name

        public int GetChangeNameRemainingAttempts()
        {
            uint periodInterval = (uint)(UnixTimestamp.Now - MsgChangeName.CHANGE_NAME_PERIOD);
            using var ctx = new ServerDbContext();
            int amount = ctx.ChangeNameBackups
                .Where(x => x.IdUser == Identity
                            && !x.OldName.Contains("[Z"))
                .Count(x => x.ChangeTime >= periodInterval);
            return Math.Max(0, Math.Min(MsgChangeName.MAX_CHANGES_PERIOD, MsgChangeName.MAX_CHANGES_PERIOD - amount));
        }

        public async Task BroadcastNewNameAsync()
        {
            if (SyndicateMember != null)
            {
                SyndicateMember.ChangeName(Name);
            }

            if (FamilyMember != null)
            {
                FamilyMember.ChangeName(Name);
            }
        }

        #endregion

        #region Leave word



        #endregion

        #region Purchases

        public async Task CheckFirstCreditAsync()
        {
            if (Flag.HasFlag(PrivilegeFlag.FirstCreditReady))
            {
                return;
            }

            if (PigletClient.Instance?.Actor != null)
            {
                await PigletClient.Instance.Actor.SendAsync(new MsgPigletUserCreditInfo(character.AccountIdentity));
            }
        }

        public async Task SetFirstCreditAsync()
        {
            if (SashSlots < MAXIMUM_SASH_SLOTS)
            {
                await SetSashSlotAmountAsync(MAXIMUM_SASH_SLOTS);
                await SendAsync(StrVipQkdSashUpgrade, TalkChannel.Talk, Color.White);
            }

            if (Flag.HasFlag(PrivilegeFlag.FirstCreditReady))
            {
                return;
            }

            Flag |= PrivilegeFlag.FirstCreditReady;
            await SynchroAttributesAsync(ClientUpdateType.PrivilegeFlag, (ulong)Flag);
        }

        public async Task ClaimFirstCreditGiftAsync()
        {
            if (!Flag.HasFlag(PrivilegeFlag.FirstCreditReady) || Flag.HasFlag(PrivilegeFlag.FirstCreditClaimed))
            {
                return;
            }

            if (!UserPackage.IsPackSpare(9))
            {
                await SendAsync(string.Format(StrNotEnoughSpaceN, 9));
                return;
            }

            var logger = LogFactory.CreateGmLogger("first_credit");
            var rewards = await AwardConfigRepository.GetFirstCreditRewardsAsync(ProfessionSort);
            foreach (var reward in rewards)
            {
                if (await UserPackage.AwardItemAsync((uint)reward.Data2, ItemPosition.Inventory, reward.Data3 != 0, reward.Data4 != 0))
                {
                    logger.LogInformation($"{Identity},{Profession},{Metempsychosis},{reward.Data2},{reward.Data3}");
                }
            }

            if (PigletClient.Instance?.Actor != null)
            {
                await PigletClient.Instance.Actor.SendAsync(new MsgPigletClaimFirstCredit(Client.AccountIdentity));
            }

            Flag |= PrivilegeFlag.FirstCreditClaimed | PrivilegeFlag.MapItemDisplay;
            await SynchroAttributesAsync(ClientUpdateType.PrivilegeFlag, (ulong)Flag);
            await SaveAsync();
        }

        #endregion

        #region Deletion

        private bool isDeleted;

        public async Task<bool> DeleteCharacterAsync()
        {
            if (Syndicate != null)
            {
                if (SyndicateRank != Syndicates.SyndicateMember.SyndicateRank.GuildLeader)
                {
                    if (!await Syndicate.QuitSyndicateAsync(this))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!await Syndicate.DisbandAsync(this))
                    {
                        return false;
                    }
                }
            }

            await ServerDbContext.ScalarAsync($"INSERT INTO `cq_deluser` SELECT * FROM `cq_user` WHERE `id`={Identity};");
            await ServerDbContext.DeleteAsync(character);

            LogFactory.CreateGmLogger("delete_user").LogInformation($"{Identity},{Name},{MapIdentity},{X},{Y},{Silvers},{ConquerPoints},{Level},{Profession},{FirstProfession},{PreviousProfession}");

            foreach (Friend friend in friends.Values)
            {
                await friend.DeleteAsync();
            }

            foreach (Enemy enemy in enemies.Values)
            {
                await enemy.DeleteAsync();
            }

            foreach (TradePartner tradePartner in tradePartners.Values)
            {
                await tradePartner.DeleteAsync();
            }

            if (Guide != null)
            {
                await Guide.DeleteAsync();
            }

            PeerageManager.NobilityObject peerage = PeerageManager.GetUser(Identity);
            if (peerage != null)
            {
                await peerage.DeleteAsync();
            }

            return isDeleted = true;
        }

        #endregion

        #region OnTimer

        public async Task OnBattleTimerAsync()
        {
            if (BattleSystem.IsActive()
                && BattleSystem.NextAttack(GetInterAtkRate()))
            {
                await BattleSystem.ProcessAttackAsync();
            }

            if (MagicData.State != MagicData.MagicState.None)
            {
                await MagicData.OnTimerAsync();
            }
        }

        public override async Task OnTimerAsync()
        {
            if (Map == null)
            {
                return;
            }

            await base.OnTimerAsync();

            _ = MailBox.OnTimerAsync();

            if (MessageBox != null)
            {
                QueueAction(MessageBox.OnTimerAsync);
            }

            if (MessageBox?.HasExpired == true)
            {
                MessageBox = null;
            }

            if (AwaitingProgressBar?.Completed == true)
            {
                QueueAction(async () =>
                {
                    if (AwaitingProgressBar != null)
                    {
                        Role role = RoleManager.GetRole(InteractingNpc);
                        Item item = UserPackage[InteractingItem];
                        await GameAction.ExecuteActionAsync(AwaitingProgressBar.IdNext, this, role, item, string.Empty);
                        AwaitingProgressBar = null;
                    }
                });
            }

            if (pkDecreaseTimer.ToNextTime(PK_DEC_TIME) && PkPoints > 0)
            {
                QueueAction(async () =>
                {
                    if (Map?.IsPrisionMap() == true)
                    {
                        await AddAttributesAsync(ClientUpdateType.PkPoints, PKVALUE_DEC_ONCE_IN_PRISON);
                    }
                    else
                    {
                        await AddAttributesAsync(ClientUpdateType.PkPoints, PKVALUE_DEC_ONCE);
                    }
                });
            }

            if (IsBlessed && heavenBlessingTimer.ToNextTime() && !Map.IsTrainingMap())
            {
                blessPoints++;
                if (blessPoints >= 10)
                {
                    GodTimeExp += 60;

                    if (GodTimeExp >= 60000 && Level < ExperienceManager.GetLevelLimit())
                    {
                        await SendAsync(new MsgGodExp
                        {
                            Action = MsgGodExpAction.MaximimBlessExpTimeAlert
                        });
                    }

                    await SynchroAttributesAsync(ClientUpdateType.OnlineTraining, 5);
                    await SynchroAttributesAsync(ClientUpdateType.OnlineTraining, 0);
                    blessPoints = 0;
                }
                else
                {
                    await SynchroAttributesAsync(ClientUpdateType.OnlineTraining, 4);
                    await SynchroAttributesAsync(ClientUpdateType.OnlineTraining, 3);
                }
            }

            if (idLuckyTarget == 0 && Metempsychosis < 2 && QueryStatus(StatusSet.LUCKY_DIFFUSE) == null)
            {
                QueueAction(() =>
                {
                    if (QueryStatus(StatusSet.LUCKY_ABSORB) == null)
                    {
                        foreach (Character user in Screen.Roles.Values.Where(x => x is Character).Cast<Character>())
                        {
                            if (user.QueryStatus(StatusSet.LUCKY_DIFFUSE) != null && GetDistance(user) <= 3)
                            {
                                idLuckyTarget = user.Identity;
                                luckyAbsorbStartTimer.Startup(3);
                                break;
                            }
                        }
                    }
                    return Task.CompletedTask;
                });
            }
            else if (QueryStatus(StatusSet.LUCKY_DIFFUSE) == null)
            {
                QueueAction(async () =>
                {
                    var role = QueryRole(idLuckyTarget) as Character;
                    if (luckyAbsorbStartTimer.IsActive() && luckyAbsorbStartTimer.IsTimeOut() && role != null)
                    {
                        await AttachStatusAsync(role, StatusSet.LUCKY_ABSORB, 0, 1000000, 0);
                        idLuckyTarget = 0;
                        luckyAbsorbStartTimer.Clear();
                    }
                });
            }

            if (luckyStepTimer.ToNextTime() && IsLucky)
            {
                QueueAction(() =>
                {
                    if (QueryStatus(StatusSet.LUCKY_DIFFUSE) == null && QueryStatus(StatusSet.LUCKY_ABSORB) == null)
                    {
                        return ChangeLuckyTimerAsync(-1);
                    }
                    return Task.CompletedTask;
                });
            }

            if (Map != null && !Map.IsNoExpMap() && enlightenTimeExp.IsActive() && enlightenTimeExp.IsTimeOut())
            {
                enlightenTimeExp.Update();

                QueueAction(async () =>
                {
                    var amount = (int)Math.Min(ENLIGHTENMENT_UPLEV_MAX_EXP / 2, EnlightenExperience);
                    if (amount != 0)
                    {
                        await AwardExperienceAsync(amount, true);
                        EnlightenExperience -= (uint)amount;
                    }

                    if (EnlightenExperience == 0)
                    {
                        enlightenTimeExp.Clear();
                    }
                });
            }

            if (Team != null && !Team.IsLeader(Identity) && Team.Leader.MapIdentity == MapIdentity &&
                teamLeaderPosTimer.ToNextTime())
            {
                await SendAsync(new MsgAction
                {
                    Action = ActionType.MapTeamLeaderStar,
                    Command = Team.Leader.Identity,
                    X = Team.Leader.X,
                    Y = Team.Leader.Y
                });
            }

            if (Guide != null && Guide.BetrayalCheck)
            {
                QueueAction(Guide.BetrayalTimerAsync);
            }

            foreach (Tutor apprentice in apprentices.Values.Where(x => x.BetrayalCheck))
            {
                QueueAction(apprentice.BetrayalTimerAsync);
            }

            QueueAction(UserPackage.OnTimerAsync);

            await JiangHu.OnTimerAsync();

            if (dateSyncTimer.ToNextTime())
            {
                await SendAsync(new MsgData(DateTime.Now));
            }

            if (activityLoginHalfHourTimer.IsActive() && activityLoginHalfHourTimer.ToNextTime())
            {
                QueueAction(() =>
                {
                    return UpdateTaskActivityAsync(ActivityType.HalfHourOnline);
                });
            }

            if (!IsAlive)
            {
                return;
            }

            if (Transformation != null && transformationTimer.IsActive() && transformationTimer.IsTimeOut())
            {
                await ClearTransformationAsync();
            }

            if (vigorTimer.ToNextTime() && QueryStatus(StatusSet.RIDING) != null && Vigor < MaxVigor)
            {
                await AddAttributesAsync(ClientUpdateType.Vigor, (long)Math.Max(10, Math.Min(200, MaxVigor * 0.005)));
            }

            if (energyTimer.ToNextTime(ADD_ENERGY_STAND_MS) && Energy < MaxEnergy)
            {
                byte energyAmount = ADD_ENERGY_STAND;
                if (IsWing)
                {
                    energyAmount = ADD_ENERGY_STAND / 2;
                }
                else
                {
                    if (Action == EntityAction.Sit)
                    {
                        energyAmount = ADD_ENERGY_SIT;
                    }
                    else if (Action == EntityAction.Lie)
                    {
                        energyAmount = ADD_ENERGY_LIE;
                    }
                }

                var ridingCrop = UserPackage[ItemPosition.Crop];
                if (ridingCrop != null)
                {
                    energyAmount += (byte) ridingCrop.RecoverEnergy;
                }

                QueueAction(() => AddAttributesAsync(ClientUpdateType.Stamina, energyAmount));
            }

            if (xpPointsTimer.ToNextTime())
            {
                await ProcXpValAsync();
            }

            if (autoHealTimer.ToNextTime() && IsAlive && Life < MaxLife)
            {
                QueueAction(async () =>
                {
                    if (IsAlive)
                    {
                        await AddAttributesAsync(ClientUpdateType.Hitpoints, AUTOHEALLIFE_EACHPERIOD);
                    }
                });
            }

            if (miningTimer.IsActive() && miningTimer.ToNextTime())
            {
                QueueAction(DoMineAsync);
            }
        }

        #endregion

        #region Session

        public DateTime LastLogin => UnixTimestamp.ToDateTime(character.LoginTime);
        public DateTime LastLogout => UnixTimestamp.ToDateTime(character.LogoutTime);
        public int TotalOnlineTime => character.OnlineSeconds;

        public DateTime PreviousLoginTime { get; private set; }

        public TimeSpan OnlineTime => TimeSpan.Zero
                                              .Add(new TimeSpan(0, 0, 0, character.OnlineSeconds))
                                              .Add(new TimeSpan(
                                                       0, 0, 0,
                                                       (int)(DateTime.Now - LastLogin).TotalSeconds));

        public TimeSpan SessionOnlineTime => TimeSpan.Zero
                                                     .Add(new TimeSpan(
                                                              0, 0, 0,
                                                              (int)(DateTime.Now - LastLogin)
                                                              .TotalSeconds));

        public async Task SetLoginAsync()
        {
            PreviousLoginTime = UnixTimestamp.ToDateTime(character.LoginTime);
            character.LoginTime = (uint)DateTime.Now.ToUnixTimestamp();
            await SaveAsync();
        }

        public async Task OnDisconnectAsync()
        {
            if (PigletClient.Instance?.Actor != null)
            {
                await PigletClient.Instance.Actor.SendAsync(new MsgPigletUserLogin()
                {
                    Data = new MsgPigletUserLogin<PigletActor>.UserLoginData
                    {
                        Users = new List<MsgPigletUserLogin<PigletActor>.UserData>
                            {
                                new MsgPigletUserLogin<PigletActor>.UserData
                                {
                                    AccountId = Client.AccountIdentity,
                                    UserId = Identity,
                                    IsLogin = false
                                }
                            }
                    }
                });
            }

            using var ctx = new ServerDbContext();
            if (Map?.IsRecordDisable() == false)
            {
                if (IsAlive)
                {
                    character.MapID = idMap;
                    character.X = currentX;
                    character.Y = currentY;
                }
            }

            character.LogoutTime = (uint)DateTime.Now.ToUnixTimestamp();
            character.OnlineSeconds += (int)(LastLogout - LastLogin).TotalSeconds;

            if (Booth != null)
            {
                await Booth.LeaveMapAsync();
            }

            if (Team != null && Team.IsLeader(Identity))
            {
                await Team.DismissAsync(this, true);
            }
            else if (Team != null)
            {
                await Team.DismissMemberAsync(this);
            }

            if (Trade != null)
            {
                await Trade.SendCloseAsync();
            }

            await EventManager.OnLogoutAsync(this);

            await NotifyOfflineFriendAsync();

            foreach (Tutor apprentice in apprentices.Values.Where(x => x.Student != null))
            {
                await apprentice.SendTutorAsync();
                await apprentice.Student.SynchroAttributesAsync(ClientUpdateType.ExtraBattlePower, 0, 0);
            }

            if (tutorAccess != null)
            {
                ctx.TutorAccess.Update(tutorAccess);
            }

            foreach (IStatus status in StatusSet.Status.Values.Where(x => x.Model != null))
            {
                if (status is StatusMore && status.RemainingTimes == 0)
                {
                    continue;
                }

                status.Model.LeaveTimes = (uint)status.RemainingTimes;
                status.Model.RemainTime = (uint)status.RemainingTime;

                if (status.Identity == 0)
                {
                    ctx.Status.Add(status.Model);
                }
                else
                {
                    ctx.Status.Update(status.Model);
                }
            }

            await WeaponSkill.SaveAllAsync(ctx);

            if (Syndicate != null && SyndicateMember != null)
            {
                SyndicateMember.LastLogout = DateTime.Now;
                await SyndicateMember.SaveAsync();
            }

            if (Map != null)
            {
                QueueAction(LeaveMapAsync);
            }

            await Fate.SaveAsync();
            await JiangHu.LogoutAsync(ctx);

            if (!isDeleted)
            {
                ctx.Characters.Update(character);
            }
            ctx.GameLoginRecords.Add(new DbGameLoginRecord
            {
                AccountIdentity = Client.AccountIdentity,
                UserIdentity = Identity,
                LoginTime = LastLogin,
                LogoutTime = LastLogout,
                ServerVersion = $"{Program.Version}",
                IpAddress = Client.IpAddress,
                MacAddress = Client.MacAddress,
                OnlineTime = (uint)(LastLogout - LastLogin).TotalSeconds
            });
            await ctx.SaveChangesAsync();
        }

        public async Task DoDailyResetAsync(bool login)
        {
            const uint chiPointsDaily = 4000;
            
            if (login && (PreviousLoginTime.Date >= DateTime.Now.Date || LastLogout.Date >= DateTime.Now.Date))
            {
                // already reseted
                return;
            }

            int offlineDays = (int)Math.Ceiling((DateTime.Now.Date - PreviousLoginTime.Date).TotalDays);
            uint chiPoints = 0;
            for (uint i = 0; i < offlineDays; i++)
            {
                chiPoints += chiPointsDaily;
            }

            if (ChiPoints < MAX_STRENGTH_POINTS_VALUE)
            {
                ChiPoints = Math.Min(Math.Max(0, ChiPoints + chiPoints), MAX_STRENGTH_POINTS_VALUE);
            }

            if (Level >= 40 && Client != null)
            {
                await SendAsync(new MsgFlower
                {
                    Mode = Gender == 1 ? MsgFlower.RequestMode.QueryFlower : MsgFlower.RequestMode.QueryGift,
                    RedRoses = 1
                });
            }

            await ResetEnlightenmentAsync();

            if (JiangHu != null && JiangHu.HasJiangHu)
            {
                await JiangHu.DailyClearAsync();
            }

            if (Client != null)
            {
                await ActivityTasksDailyResetAsync();
                await TaskDetail.DailyResetAsync();
                Statistic.ClearDailyStatistic();
            }
        }

        #endregion

        #region Socket

        public override Task SendAsync(IPacket msg)
        {
            return SendAsync(msg.Encode());
        }

        public override Task SendAsync(byte[] msg)
        {
            try
            {
                if (Client != null)
                {
                    return Client.SendAsync(msg);
                }
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Character.SendAsync: {Message}", ex.Message);
                return Task.CompletedTask;
            }
        }

        public override async Task SendSpawnToAsync(Character player)
        {
            await player.SendAsync(new MsgPlayer(this, player));

            if (Syndicate != null)
            {
                await Syndicate.SendAsync(player);
            }

            if (FairyType != 0)
            {
                await player.SendAsync(new MsgSuitStatus
                {
                    Action = 1,
                    Data = (int)FairyType,
                    Param = (int)Identity
                });
            }
        }

        public override async Task SendSpawnToAsync(Character player, int x, int y)
        {
            await player.SendAsync(new MsgPlayer(this, player, (ushort)x, (ushort)y));

            if (Syndicate != null)
            {
                await Syndicate.SendAsync(player);
            }

            if (FairyType != 0)
            {
                await player.SendAsync(new MsgSuitStatus
                {
                    Action = 1,
                    Data = (int)FairyType,
                    Param = (int)Identity
                });
            }
        }

        public async Task SendWindowToAsync(Character player)
        {
            await player.SendAsync(new MsgPlayer(this, player)
            {
                WindowSpawn = true
            });

            if (FairyType != 0)
            {
                await player.SendAsync(new MsgSuitStatus
                {
                    Action = 1,
                    Data = (int)FairyType,
                    Param = (int)Identity
                });
            }
        }

        public Task SendRelationAsync(Character target)
        {
            return SendAsync(new MsgRelation
            {
                SenderIdentity = target.Identity,
                Level = target.Level,
                BattlePower = target.BattlePower,
                IsSpouse = target.Identity == MateIdentity,
                IsTradePartner = IsTradePartner(target.Identity),
                IsTutor = IsTutor(target.Identity),
                TargetIdentity = Identity
            });
        }

        #endregion

        #region Database

        public async Task<bool> SaveAsync()
        {
            if (Identity < 10_000_000)
            {
                return await ServerDbContext.SaveAsync(character);
            }
            return true;
        }

        #endregion

        public static implicit operator DbCharacter(Character character)
        {
            return character.character;
        }

        public struct ExperiencePreview
        {
            public int Level { get; set; }
            public ulong Experience { get; set; }
            public double Percent { get; set; }
        }

        /// <summary>Enumeration type for body types for player characters.</summary>
        public enum BodyType : ushort
        {
            AgileMale = 1003,
            MuscularMale = 1004,
            AgileFemale = 2001,
            MuscularFemale = 2002
        }

        /// <summary>Enumeration type for base classes for player characters.</summary>
        public enum BaseClassType : ushort
        {
            Trojan = 10,
            Warrior = 20,
            Archer = 40,
            Ninja = 50,
            Monk = 60,
            Pirate = 70,
            DragonWarrior = 80,
            Taoist = 100
        }

        public enum PkModeType
        {
            FreePk,
            Peace,
            Team,
            Capture,
            Revenge,
            Syndicate,
            JiangHu
        }

        [Flags]
        public enum JiangPkMode
        {
            None = 0,
            NotHitFriends = 1,
            NotHitClanMembers = 2,
            NotHitGuildMembers = 4,
            NotHitAlliedGuild = 8,
            NoHitAlliesClan = 16
        }

        public enum PrivilegeFlag : uint
        {
            None = 0,
            FirstCreditReady = 1,
            MapItemDisplay = 2,
            FirstCreditClaimed = 4,
            OnMeleeAttack = 8
        }

        [Flags]
        public enum VipFlags
        {
            VipOne = ItemStatusExtraTime | Friends | BlessTime,
            VipTwo = VipOne | BonusLottery | VipFurniture | CityTeleport,
            VipThree = VipTwo | PortalTeleport | CityTeleportTeam,
            VipFour = VipThree | Avatar | DailyQuests | VipHairStyles,
            VipFive = VipFour | FrozenGrotto,
            VipSix = PortalTeleport | Avatar | MoreForVip | FrozenGrotto | TeleportTeam
                      | CityTeleport | CityTeleportTeam | BlessTime | OfflineTrainingGround | ItemStatusExtraTime
                      | Friends | VipHairStyles | Labirint | DailyQuests | VipFurniture | BonusLottery,

            PortalTeleport = 0x1,
            Avatar = 0x2,
            MoreForVip = 0x4,
            FrozenGrotto = 0x8,
            TeleportTeam = 0x10,
            CityTeleport = 0x20,
            CityTeleportTeam = 0x40,
            BlessTime = 0x80,
            OfflineTrainingGround = 0x100,
            /// <summary>
            /// Refinery and Artifacts
            /// </summary>
            ItemStatusExtraTime = 0x200,
            Friends = 0x400,
            VipHairStyles = 0x800,
            Labirint = 0x1000,
            DailyQuests = 0x2000,
            VipFurniture = 0x4000,
            BonusLottery = 0x8000,

            None = 0
        }

        public enum PlayerCountry
        {
            UnitedArabEmirates = 1,
            Argentine,
            Australia,
            Belgium,
            Brazil,
            Canada,
            China,
            Colombia,
            CostaRica,
            CzechRepublic,
            Conquer,
            Germany,
            Denmark,
            DominicanRepublic,
            Egypt,
            Spain,
            Estland,
            Finland,
            France,
            UnitedKingdom,
            HongKong,
            Indonesia,
            India,
            Israel,
            Italy,
            Japan,
            Kuwait,
            SriLanka,
            Lithuania,
            Mexico,
            Macedonia,
            Malaysia,
            Netherlands,
            Norway,
            NewZealand,
            Peru,
            Philippines,
            Poland,
            PuertoRico,
            Portugal,
            Palestine,
            Qatar,
            Romania,
            Russia,
            SaudiArabia,
            Singapore,
            Sweden,
            Thailand,
            Turkey,
            UnitedStates,
            Venezuela,
            Vietnam = 52
        }

        public enum RequestType
        {
            Friend,
            InviteSyndicate,
            JoinSyndicate,
            TeamApply,
            TeamInvite,
            Trade,
            Marriage,
            TradePartner,
            Guide,
            Family,
            CoupleInteraction
        }
    }
}
