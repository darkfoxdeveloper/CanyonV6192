using Canyon.Database.Entities;
using Canyon.Game.Database;
using Canyon.Game.Database.Repositories;
using Canyon.Game.Services.Managers;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States.Items;
using Canyon.Game.States.User;
using Canyon.World.Enums;
using System.Collections.Concurrent;
using static Canyon.Game.Services.Managers.JiangHuManager;
using static Canyon.Game.Sockets.Game.Packets.MsgOwnKongfuBase;
using static Canyon.Game.Sockets.Game.Packets.MsgOwnKongfuImproveFeedback;

namespace Canyon.Game.States
{
    public sealed class JiangHu
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<JiangHu>();
        private static readonly ILogger gmLog = LogFactory.CreateGmLogger("jiang_hu");

        private readonly Character user;
        private DbJiangHuPlayer jiangHu;
        private DbJiangHuCaltivateTimes jiangHuTimes;
        private ConcurrentDictionary<byte, DbJiangHuPlayerPower> powers = new();
        private readonly TimeOut exitKongFuTimer = new();

        private const uint TRAINING_COURSE_TYPE = 1;
        private const uint TRAINING_COURSE_SUBTYPE_FREE = 1;
        private const uint TRAINING_COURSE_SUBTYPE_PAID = 2;
        private const uint TRAINING_COURSE_SUBTYPE_STAGE = 3;

        private int maxLife,
            maxMana,
            attack,
            magicAttack,
            defense,
            magicDefense,
            finalDamage,
            finalMagicDamage,
            finalDefense,
            finalMagicDefense,
            criticalStrike,
            skillCriticalStrike,
            immunity,
            breakthrough,
            counteraction;

        public JiangHu(Character user)
        {
            this.user = user;
        }

        public int MaxLife => maxLife;
        public int MaxMana => maxMana;
        public int Attack => attack;
        public int MagicAttack => magicAttack;
        public int Defense => defense;
        public int MagicDefense => magicDefense;
        public int FinalDamage => finalDamage;
        public int FinalMagicDamage => finalMagicDamage;
        public int FinalDefense => finalDefense;
        public int FinalMagicDefense => finalMagicDefense;
        public int CriticalStrike => criticalStrike;
        public int SkillCriticalStrike => skillCriticalStrike;
        public int Breakthrough => breakthrough;
        public int Immunity => immunity;
        public int Counteraction => counteraction;

        public bool HasJiangHu => jiangHu != null;

        public string Name => jiangHu?.Name ?? "None";

        public int CurrentStage => powers.Count; // power_level

        public int Grade { get; private set; }

        public byte Talent
        {
            get => jiangHu?.GenuineQiLevel ?? 0;
            set => jiangHu.GenuineQiLevel = Math.Max((byte)0, Math.Min(MAX_TALENT, value));
        }

        public bool IsActive => user.PkMode == Character.PkModeType.JiangHu || (exitKongFuTimer.IsActive() && !exitKongFuTimer.IsTimeOut());

        public int SecondsTillNextCourse
        {
            get
            {
                int maxSeconds;
                if (user.MapIdentity == 1002 && IsActive)
                {
                    maxSeconds = (int)(10000d / TwinCityAddPoints[Talent] * 60) * 60;
                }
                else
                {
                    maxSeconds = (int)(10000d / OutTwinCityAddPoints[Talent] * 60) * 60;
                }

                int amount = (int)((FreeCaltivateParam / POINTS_TO_COURSE) * maxSeconds);
                return UnixTimestamp.Now + (int)(maxSeconds - maxSeconds * (amount % 10000 / 10000d));
            }
        }

        public uint FreeCourses => FreeCaltivateParam / 10000;

        public uint FreeCaltivateParam
        {
            get => jiangHu?.FreeCaltivateParam ?? 0;
            set
            {
                jiangHu.FreeCaltivateParam = (uint)Math.Max(0, Math.Min(value, MAX_FREE_COURSE * POINTS_TO_COURSE));
            }
        }

        public byte FreeCoursesUsedToday
        {
            get => jiangHuTimes?.FreeTimes ?? 0;
            set
            {
                jiangHuTimes ??= new DbJiangHuCaltivateTimes
                {
                    PlayerId = user.Identity
                };
                jiangHuTimes.FreeTimes = value;
            }
        }

        public uint PaidCoursesUsedToday
        {
            get => jiangHuTimes?.PaidTimes ?? 0;
            set
            {
                jiangHuTimes ??= new DbJiangHuCaltivateTimes
                {
                    PlayerId = user.Identity
                };
                jiangHuTimes.PaidTimes = value;
            }
        }

        public uint InnerPower
        {
            get => jiangHu.TotalPowerValue;
            set => jiangHu.TotalPowerValue = value;
        }

        public uint MaxInnerPowerHistory
        {
            get;
            set;
        }

        public async Task InitializeAsync()
        {
            jiangHu = await JiangHuPlayerRepository.GetAsync(user.Identity);
            if (jiangHu == null)
            {
                return;
            }

            awardPointsTimer.Startup(60);

            jiangHuTimes = await JiangHuCaltivateTimesRepository.GetAsync(user.Identity);
            powers = new(await JiangHuPlayerPowerRepository.GetAsync(user.Identity));

            int minutesSinceLastLogin = (int)(user.LastLogin - user.LastLogout).TotalMinutes;
            if (minutesSinceLastLogin > 0)
            {
                FreeCaltivateParam += (uint)(OutTwinCityAddPoints[Talent] * minutesSinceLastLogin);
            }

            FreeCaltivateParam = (uint)Math.Max(0, Math.Min(MAX_FREE_COURSE * POINTS_TO_COURSE, FreeCaltivateParam));

            var oldInnerPower = InnerPower;
            UpdateAllAttributes();
            if (oldInnerPower != InnerPower)
            {
                await SaveAsync();
            }

            int remainingJiangHu = GetJiangHuRemainingTime(user.Identity);
            if (remainingJiangHu > 0)
            {
                exitKongFuTimer.Startup(remainingJiangHu);
            }

            await SendStarsAsync();
            await SendStarAsync();
            await SendStatusAsync();
        }

        public async Task<bool> CreateAsync(string gongFuName)
        {
            logger.LogInformation($"Creating JiangHu [{gongFuName}] for user [{user.Identity},{user.Name}]");

            if (jiangHu != null)
            {
                return false;
            }

            if (user.Metempsychosis < 2)
            {
                return false;
            }

            if (user.Level < 30)
            {
                return false;
            }

            if (!RoleManager.IsValidName(gongFuName))
            {
                logger.LogWarning($"JiangHu [{gongFuName}] denied due to invalid name.");
                return false;
            }

            DbJiangHuPlayer temp = await JiangHuPlayerRepository.GetAsync(gongFuName);
            if (temp != null)
            {
                return false;
            }

            jiangHu = new DbJiangHuPlayer
            {
                Name = gongFuName,
                PlayerId = user.Identity,
                GenuineQiLevel = 3,
                FreeCaltivateParam = (uint)(POINTS_TO_COURSE * 5)
            };

            await GenerateAsync(1, 1, 0, KongFuImproveFeedbackMode.FreeCourse);
            FreeCoursesUsedToday = 0;

            await SaveAsync();

            awardPointsTimer.Startup(60);

            await SendInfoAsync();
            await SendStarsAsync();
            await SendStarAsync();
            await SendTimeAsync();
            await SendTalentAsync();
            await SendStatusAsync();
            await user.SetPkModeAsync(Character.PkModeType.JiangHu);
            return true;
        }

        public async Task StudyAsync(byte powerLevel, byte star, byte high, KongFuImproveFeedbackMode mode)
        {
            DbJiangHuCaltivateCondition condition = GetCaltivateCondition((byte)CurrentStage);
            if (powerLevel > CurrentStage)
            {
                if (powerLevel != CurrentStage + 1)
                {
                    return;
                }

                if (powerLevel > 9)
                {
                    return;
                }

                if (GetStageInnerPower((byte)CurrentStage) < condition.NeedPowerValue)
                {
                    return;
                }

                if (powers.TryGetValue(powerLevel, out _))
                {
                    return;
                }

                powers.TryAdd(powerLevel, new DbJiangHuPlayerPower
                {
                    Level = powerLevel,
                    PlayerId = user.Identity
                });
            }

            byte latestStar = GetLatestStar();
            if (star > latestStar && star != latestStar + 1 && powerLevel >= CurrentStage)
            {
                return; // invalid star
            }

            int emoneyAmount = 0;
            List<Item> useItems = new List<Item>();
            if (mode == KongFuImproveFeedbackMode.FreeCourse)
            {
                if (Talent == 0)
                {
                    return;
                }

                if (FreeCoursesUsedToday >= MAX_FREE_COURSES_DAILY)
                {
                    return;
                }

                if (FreeCourses == 0)
                {
                    Item freeTrainingPill = user.UserPackage.GetActiveItemByType(Item.FREE_TRAINING_PILL);
                    if (freeTrainingPill == null)
                    {
                        return;
                    }
                    useItems.Add(freeTrainingPill);
                }

                if (!await user.SpendCultivationAsync(10))
                    return;

                FreeCaltivateParam -= (uint)POINTS_TO_COURSE;
                await SpendTalentAsync();
            }
            else
            {
                emoneyAmount = (int)(condition.NeedCultivateValue * Math.Min(Math.Max(1, PaidCoursesUsedToday), 50) + condition.NeedCultivateValue);
                if (mode == KongFuImproveFeedbackMode.FavouredTraining)
                {
                    List<Item> favouredPills = new();
                    int needItem = emoneyAmount / 10;
                    int itemCount = user.UserPackage.MultiGetItem(Item.FAVORED_TRAINING_PILL, Item.FAVORED_TRAINING_PILL, needItem, ref useItems);
                    if (itemCount < needItem)
                    {
                        return;
                    }
                    emoneyAmount = 0;
                }
            }

            switch (high)
            {
                case 1:
                    {
                        if (user.VipLevel < 2)
                        {
                            high = 0;
                        }
                        else
                        {
                            Item specialTrainingPill = user.UserPackage.GetItemByType(Item.SPECIAL_TRAINING_PILL);
                            if (specialTrainingPill == null)
                            {
                                if (user.ConquerPoints >= 5)
                                {
                                    emoneyAmount += 5;
                                }
                                else
                                {
                                    high = 0;
                                }
                            }
                            else
                            {
                                useItems.Add(specialTrainingPill);
                            }
                        }
                        break;
                    }
                case 2:
                    {
                        if (user.VipLevel < 5)
                        {
                            high = 0;
                        }
                        else
                        {
                            Item seniorTrainingPill = user.UserPackage.GetItemByType(Item.SENIOR_TRAINING_PILL);
                            if (seniorTrainingPill == null)
                            {
                                if (user.ConquerPoints >= 50)
                                {
                                    emoneyAmount += 50;
                                }
                                else
                                {
                                    high = 0;
                                }
                            }
                            else
                            {
                                useItems.Add(seniorTrainingPill);
                            }
                        }
                        break;
                    }
            }

            if (emoneyAmount > 0 && !await user.SpendBoundConquerPointsAsync(emoneyAmount, true))
            {
                return;
            }

            foreach (var item in useItems)
            {
                await user.UserPackage.SpendItemAsync(item);
            }

            jiangHu.PowerLevel = (byte)CurrentStage;

            await GenerateAsync(powerLevel, star, high, mode);
            await SendTalentAsync();
            await SendTimeAsync(user);

            await SaveAsync();

            await user.UpdateTaskActivityAsync(ActivityManager.ActivityType.JiangHu);
            
            if (mode == KongFuImproveFeedbackMode.FreeCourse)
            {
                await user.Statistic.IncrementDailyValueAsync(TRAINING_COURSE_TYPE, TRAINING_COURSE_SUBTYPE_FREE);
            }
            else
            {
                await user.Statistic.IncrementDailyValueAsync(TRAINING_COURSE_TYPE, TRAINING_COURSE_SUBTYPE_PAID);
                LuaScriptManager.Run(user, null, null, string.Empty, $"TrainingGongfu({user.Identity},{jiangHuTimes.PaidTimes})");
            }
        }

        public async Task AwardTalentAsync(byte talent = 1)
        {
            Talent += talent;
            await SendTalentAsync();
            await SaveAsync();
        }

        public async Task<bool> SpendTalentAsync(byte talent = 1)
        {
            if (Talent < talent)
            {
                return false;
            }

            Talent -= talent;
            await SendTalentAsync();
            await SaveAsync();
            return true;
        }

        public Task ExitJiangHuAsync()
        {
            exitKongFuTimer.Startup(EXIT_KONG_FU_SECONDS);
            return SendStatusAsync();
        }

        public async Task SendStatusAsync()
        {
            if (jiangHu == null)
            {
                return;
            }

            MsgOwnKongfuBase msg = new()
            {
                Mode = KongfuBaseMode.SendStatus
            };
            msg.Strings.Add(user.Identity.ToString());
            msg.Strings.Add((Talent + 1).ToString());
            msg.Strings.Add(IsActive ? "1" : "2");
            await user.SendAsync(msg);
        }

        private ushort GetStarIdentity(JiangHuAttrType type, JiangHuQuality quality) => (ushort)((ushort)type + (ushort)quality * 256);

        public async Task SendStarsAsync(Character target = null)
        {
            if (!HasJiangHu)
            {
                return;
            }

            MsgOwnKongfuImproveSummaryInfo msg = new()
            {
                Name = Name,
                Stage = (byte)CurrentStage,
                FreeTalentToday = MAX_FREE_COURSES_DAILY,
                FreeTalentUsed = FreeCoursesUsedToday,
                Talent = (byte)(Talent + 1),
                Points = user.StudyPoints,
                BoughtTimes = (int)PaidCoursesUsedToday
            };
            foreach (var level in powers.OrderBy(x => x.Key).Select(x => x.Value))
            {
                msg.Identities.Add(GetStarIdentity((JiangHuAttrType)level.Type1, (JiangHuQuality)level.Quality1));
                msg.Identities.Add(GetStarIdentity((JiangHuAttrType)level.Type2, (JiangHuQuality)level.Quality2));
                msg.Identities.Add(GetStarIdentity((JiangHuAttrType)level.Type3, (JiangHuQuality)level.Quality3));
                msg.Identities.Add(GetStarIdentity((JiangHuAttrType)level.Type4, (JiangHuQuality)level.Quality4));
                msg.Identities.Add(GetStarIdentity((JiangHuAttrType)level.Type5, (JiangHuQuality)level.Quality5));
                msg.Identities.Add(GetStarIdentity((JiangHuAttrType)level.Type6, (JiangHuQuality)level.Quality6));
                msg.Identities.Add(GetStarIdentity((JiangHuAttrType)level.Type7, (JiangHuQuality)level.Quality7));
                msg.Identities.Add(GetStarIdentity((JiangHuAttrType)level.Type8, (JiangHuQuality)level.Quality8));
                msg.Identities.Add(GetStarIdentity((JiangHuAttrType)level.Type9, (JiangHuQuality)level.Quality9));
            }
            if (target != null)
            {
                msg.Timer = 0xd1d401;
                await target.SendAsync(msg);
            }
            else
            {
                msg.Timer = 0xec8600;
                await user.SendAsync(msg);
            }
        }

        /// <summary>
        /// Submits the latest star and power level to the target user.
        /// </summary>
        public Task SendStarAsync(Character target = null)
        {
            MsgOwnKongfuBase msg = new()
            {
                Mode = KongfuBaseMode.UpdateStar
            };
            msg.Strings.Add(user.Identity.ToString());
            msg.Strings.Add(CurrentStage.ToString());
            msg.Strings.Add(GetLatestStar().ToString());
            if (target != null)
            {
                return target.SendAsync(msg);
            }
            return user.Screen.BroadcastRoomMsgAsync(msg);
        }

        public Task SendTalentAsync(Character target = null)
        {
            MsgOwnKongfuBase msg = new()
            {
                Mode = KongfuBaseMode.UpdateTalent
            };
            msg.Strings.Add(user.Identity.ToString());
            msg.Strings.Add((Talent + 1).ToString());
            if (target != null)
            {
                return target.SendAsync(msg);
            }
            return user.Screen.BroadcastRoomMsgAsync(msg);
        }

        public Task SendTimeAsync(Character target = null)
        {
            MsgOwnKongfuBase msg = new()
            {
                Mode = KongfuBaseMode.UpdateTime
            };
            msg.Strings.Add(FreeCaltivateParam.ToString());
            msg.Strings.Add(SecondsTillNextCourse.ToString());
            if (target != null)
            {
                return target.SendAsync(msg);
            }
            return user.Screen.BroadcastRoomMsgAsync(msg);
        }

        public Task SendInfoAsync(Character target = null)
        {
            MsgOwnKongfuBase msg = new()
            {
                Mode = KongfuBaseMode.SendInfo
            };
            msg.Strings.Add(user.Identity.ToString());
            msg.Strings.Add(CurrentStage.ToString());
            msg.Strings.Add(GetLatestStar().ToString());
            if (target != null)
            {
                return target.SendAsync(msg);
            }
            return user.Screen.BroadcastRoomMsgAsync(msg);
        }

        public async Task SaveAsync()
        {
            if (jiangHu != null)
            {
                await ServerDbContext.SaveAsync(jiangHu);
                if (jiangHuTimes != null)
                {
                    await ServerDbContext.SaveAsync(jiangHuTimes);
                }
                await ServerDbContext.SaveRangeAsync(powers.Values.ToList());
            }
        }

        public async Task LogoutAsync(ServerDbContext ctx)
        {
            if (exitKongFuTimer.IsActive() && !exitKongFuTimer.IsTimeOut())
            {
                StoreJiangHuRemainingTime(user.Identity, exitKongFuTimer.GetRemain());
            }
            else if (IsActive)
            {
                StoreJiangHuRemainingTime(user.Identity, EXIT_KONG_FU_SECONDS);
            }
            if (jiangHu != null)
            {
                ctx.JiangHuPlayers.Update(jiangHu);
                if (jiangHuTimes != null)
                {
                    ctx.JiangHuCaltivateTimes.Update(jiangHuTimes);
                }
                ctx.JiangHuPlayerPowers.UpdateRange(powers.Values.ToList());
            }
        }

        private JiangHuStar latestSavedStar;

        public async Task GenerateAsync(byte powerLevel, byte star, int high, KongFuImproveFeedbackMode mode)
        {
            QueryStar(powerLevel, star, out JiangHuStar currentStar);

            int max = 0;
            var qualityRates = GetQualityRates(powerLevel);
            Dictionary<int, JiangHuQuality> qualities = new();
            foreach (var q in qualityRates)
            {
                if ((JiangHuQuality)q.PowerQuality == JiangHuQuality.Epic && currentStar.Quality < JiangHuQuality.Ultra)
                {
                    continue;
                }

                switch (high)
                {
                    case 0:
                        {
                            if (q.CommonRate == 0)
                            {
                                continue;
                            }
                            max += q.CommonRate;
                            qualities.Add(max, (JiangHuQuality)q.PowerQuality);
                            break;
                        }
                    case 1:
                        {
                            if (q.CritRate == 0)
                            {
                                continue;
                            }
                            max += q.CritRate;
                            qualities.Add(max, (JiangHuQuality)q.PowerQuality);
                            break;
                        }
                    case 2:
                        {
                            if (q.HighCritRate == 0)
                            {
                                continue;
                            }
                            max += q.HighCritRate;
                            qualities.Add(max, (JiangHuQuality)q.PowerQuality);
                            break;
                        }
                }
            }

            JiangHuQuality quality = JiangHuQuality.None;
            int rand = await NextAsync(max);
            foreach (var q in qualities.OrderBy(x => x.Key))
            {
                if (rand < q.Key)
                {
                    quality = q.Value;
                    break;
                }
            }

            max = 0;
            var attributeRates = GetAttributeRates(powerLevel);
            Dictionary<int, JiangHuAttrType> types = new();
            foreach (var attr in attributeRates)
            {
                max += attr.Rate;
                types.Add(max, (JiangHuAttrType)attr.PowerAttribute);
            }

            JiangHuAttrType type = JiangHuAttrType.None;
            rand = await NextAsync(max);
            foreach (var itype in types.OrderBy(x => x.Key))
            {
                if (rand < itype.Key)
                {
                    type = itype.Value;
                    break;
                }
            }

            if (!currentStar.Equals(default))
            {
                latestSavedStar = currentStar;
            }

            var attribute = SetAttribute(powerLevel, star, quality, type);
            if (attribute != null)
            {
                await ServerDbContext.SaveAsync(attribute);
            }

            if (mode == KongFuImproveFeedbackMode.FreeCourse)
            {
                FreeCoursesUsedToday += 1;
            }
            else
            {
                PaidCoursesUsedToday += 1;
            }

            await user.SendAsync(new MsgOwnKongfuImproveFeedback
            {
                FreeCourse = (int)FreeCaltivateParam,
                Star = star,
                Stage = powerLevel,
                FreeCourseUsedToday = FreeCoursesUsedToday,
                PaidRounds = (int)PaidCoursesUsedToday,
                Attribute = GetStarIdentity(type, quality)
            });

            await ServerDbContext.SaveAsync(jiangHuTimes);
            UpdateAllAttributes();
            await UpdateAsync();

            gmLog.LogInformation($"{user.Identity},{user.Name},generate,{type},{quality},{GetStageInnerPower(powerLevel)},{InnerPower},{MaxInnerPowerHistory},{Grade}");

#if DEBUG
            logger.LogDebug($"[{user.Identity},{user.Name}] JiangHu New Attribute [{type},{quality}] InnerPower [{GetStageInnerPower(powerLevel)},{InnerPower},{MaxInnerPowerHistory}] Grade[{Grade}]");
#endif
        }

        public async Task<bool> RestoreAsync(byte powerLevel, byte star)
        {
            if (latestSavedStar.Equals(default))
            {
                return false;
            }

            if (!QueryStar(powerLevel, star, out var current))
            {
                return false;
            }

            if (current.PowerLevel != latestSavedStar.PowerLevel || current.Star != latestSavedStar.Star)
            {
                return false;
            }

            var attribute = SetAttribute(powerLevel, star, latestSavedStar.Quality, latestSavedStar.Type);
            if (attribute != null)
            {
                await ServerDbContext.SaveAsync(attribute);
            }

            latestSavedStar = default;

            UpdateAllAttributes();
            await UpdateAsync();

            gmLog.LogInformation($"{user.Identity},{user.Name},restore,{latestSavedStar.Type},{latestSavedStar.Quality},{GetStageInnerPower(powerLevel)},{InnerPower},{MaxInnerPowerHistory},{Grade}");

#if DEBUG
            logger.LogDebug($"[{user.Identity},{user.Name}] JiangHu Retore Attribute [{latestSavedStar.Type},{latestSavedStar.Quality}] InnerPower [{GetStageInnerPower(powerLevel)},{InnerPower},{MaxInnerPowerHistory}] Grade[{Grade}]");
#endif

            return true;
        }

        public async Task UpdateAsync()
        {
            byte nextStage = (byte)(CurrentStage + 1);
            DbJiangHuCaltivateCondition condition = GetCaltivateCondition((byte)CurrentStage);
            if (GetLatestStar() == 9
                && CurrentStage < 9
                && condition.NeedCultivateValue < GetStageInnerPower((byte)CurrentStage)
                && !powers.TryGetValue(nextStage, out _))
            {
                powers.TryAdd(nextStage, new DbJiangHuPlayerPower
                {
                    Level = nextStage,
                    PlayerId = user.Identity
                });
                await SendStarsAsync();
            }

            await user.SendAsync(new MsgPlayerAttribInfo(user));
        }

        public int GetStageInnerPower(byte powerLevel)
        {
            if (powerLevel < 1 || powerLevel > 9)
            {
                return 0;
            }

            if (!powers.TryGetValue(powerLevel, out var powerValue))
            {
                return 0;
            }

            List<JiangHuStar> stars = new();
            stars.Add(new JiangHuStar((JiangHuQuality)powerValue.Quality1, (JiangHuAttrType)powerValue.Type1, powerLevel, 1));
            stars.Add(new JiangHuStar((JiangHuQuality)powerValue.Quality2, (JiangHuAttrType)powerValue.Type2, powerLevel, 2));
            stars.Add(new JiangHuStar((JiangHuQuality)powerValue.Quality3, (JiangHuAttrType)powerValue.Type3, powerLevel, 3));
            stars.Add(new JiangHuStar((JiangHuQuality)powerValue.Quality4, (JiangHuAttrType)powerValue.Type4, powerLevel, 4));
            stars.Add(new JiangHuStar((JiangHuQuality)powerValue.Quality5, (JiangHuAttrType)powerValue.Type5, powerLevel, 5));
            stars.Add(new JiangHuStar((JiangHuQuality)powerValue.Quality6, (JiangHuAttrType)powerValue.Type6, powerLevel, 6));
            stars.Add(new JiangHuStar((JiangHuQuality)powerValue.Quality7, (JiangHuAttrType)powerValue.Type7, powerLevel, 7));
            stars.Add(new JiangHuStar((JiangHuQuality)powerValue.Quality8, (JiangHuAttrType)powerValue.Type8, powerLevel, 8));
            stars.Add(new JiangHuStar((JiangHuQuality)powerValue.Quality9, (JiangHuAttrType)powerValue.Type9, powerLevel, 9));

            int innerPower = 0;
            int currentInnerPower = 0;
            int align = 1;
            JiangHuAttrType lastType = JiangHuAttrType.None;
            foreach (var star in stars.Where(x => x.Type != JiangHuAttrType.None))
            {
                if (lastType != JiangHuAttrType.None && lastType != star.Type)
                {
                    innerPower += (int)(currentInnerPower * SequenceInnerStrength[align]);

                    align = 1;
                    currentInnerPower = 0;
                }

                currentInnerPower += (int)PowerValue[(int)(star.Quality - 1)];

                if (lastType != JiangHuAttrType.None && lastType == star.Type)
                {
                    align++;
                    
                }

                lastType = star.Type;
            }

            if (currentInnerPower != 0)
            {
                innerPower += (int)(currentInnerPower * SequenceInnerStrength[align]);
            }
            return innerPower;
        }

        public byte GetLatestStar()
        {
            if (powers.TryGetValue((byte)CurrentStage, out var value))
            {
                if (value.Type1 == 0)
                {
                    return 0;
                }

                if (value.Type2 == 0)
                {
                    return 1;
                }

                if (value.Type3 == 0)
                {
                    return 2;
                }

                if (value.Type4 == 0)
                {
                    return 3;
                }

                if (value.Type5 == 0)
                {
                    return 4;
                }

                if (value.Type6 == 0)
                {
                    return 5;
                }

                if (value.Type7 == 0)
                {
                    return 6;
                }

                if (value.Type8 == 0)
                {
                    return 7;
                }

                if (value.Type9 == 0)
                {
                    return 8;
                }

                return 9;
            }
            return 0;
        }

        public DbJiangHuPlayerPower SetAttribute(byte level, byte star, JiangHuQuality quality, JiangHuAttrType type)
        {
            if (!powers.TryGetValue(level, out var powerValue))
            {
                powerValue = new DbJiangHuPlayerPower
                {
                    Level = level,
                    PlayerId = user.Identity
                };
                powers.TryAdd(level, powerValue);
            }

            switch (star)
            {
                case 1:
                    {
                        powerValue.Quality1 = (byte)quality;
                        powerValue.Type1 = (byte)type;
                        break;
                    }
                case 2:
                    {
                        powerValue.Quality2 = (byte)quality;
                        powerValue.Type2 = (byte)type;
                        break;
                    }
                case 3:
                    {
                        powerValue.Quality3 = (byte)quality;
                        powerValue.Type3 = (byte)type;
                        break;
                    }
                case 4:
                    {
                        powerValue.Quality4 = (byte)quality;
                        powerValue.Type4 = (byte)type;
                        break;
                    }
                case 5:
                    {
                        powerValue.Quality5 = (byte)quality;
                        powerValue.Type5 = (byte)type;
                        break;
                    }
                case 6:
                    {
                        powerValue.Quality6 = (byte)quality;
                        powerValue.Type6 = (byte)type;
                        break;
                    }
                case 7:
                    {
                        powerValue.Quality7 = (byte)quality;
                        powerValue.Type7 = (byte)type;
                        break;
                    }
                case 8:
                    {
                        powerValue.Quality8 = (byte)quality;
                        powerValue.Type8 = (byte)type;
                        break;
                    }
                case 9:
                    {
                        powerValue.Quality9 = (byte)quality;
                        powerValue.Type9 = (byte)type;
                        break;
                    }
            }

            return powerValue;
        }

        private void InternalResetAttributes()
        {
            maxLife = 0;
            maxMana = 0;
            attack = 0;
            magicAttack = 0;
            defense = 0;
            magicDefense = 0;
            finalDamage = 0;
            finalMagicDamage = 0;
            finalDefense = 0;
            finalMagicDefense = 0;
            criticalStrike = 0;
            skillCriticalStrike = 0;
            breakthrough = 0;
            immunity = 0;
            counteraction = 0;
        }

        public bool QueryStar(byte level, byte star, out JiangHuStar jiangHuStar)
        {
            jiangHuStar = default;
            if (!powers.TryGetValue(level, out var powerValue))
            {
                return false;
            }

            switch (star)
            {
                case 1: jiangHuStar = new JiangHuStar((JiangHuQuality)powerValue.Quality1, (JiangHuAttrType)powerValue.Type1, level, star); break;
                case 2: jiangHuStar = new JiangHuStar((JiangHuQuality)powerValue.Quality2, (JiangHuAttrType)powerValue.Type2, level, star); break;
                case 3: jiangHuStar = new JiangHuStar((JiangHuQuality)powerValue.Quality3, (JiangHuAttrType)powerValue.Type3, level, star); break;
                case 4: jiangHuStar = new JiangHuStar((JiangHuQuality)powerValue.Quality4, (JiangHuAttrType)powerValue.Type4, level, star); break;
                case 5: jiangHuStar = new JiangHuStar((JiangHuQuality)powerValue.Quality5, (JiangHuAttrType)powerValue.Type5, level, star); break;
                case 6: jiangHuStar = new JiangHuStar((JiangHuQuality)powerValue.Quality6, (JiangHuAttrType)powerValue.Type6, level, star); break;
                case 7: jiangHuStar = new JiangHuStar((JiangHuQuality)powerValue.Quality7, (JiangHuAttrType)powerValue.Type7, level, star); break;
                case 8: jiangHuStar = new JiangHuStar((JiangHuQuality)powerValue.Quality8, (JiangHuAttrType)powerValue.Type8, level, star); break;
                case 9: jiangHuStar = new JiangHuStar((JiangHuQuality)powerValue.Quality9, (JiangHuAttrType)powerValue.Type9, level, star); break;
                default:
                    {
                        return false;
                    }
            }

            return true;
        }

        public void UpdateAllAttributes()
        {
            InnerPower = 0;
            InternalResetAttributes();

            JiangHuQuality quality = JiangHuQuality.Epic;
            int grade = 0;
            for (byte i = 1; i <= CurrentStage; i++)
            {
                UpdateAttributes(i, out var g, out var q);
                grade += g;
                if (q < quality)
                {
                    quality = q;
                }
            }

            Grade = grade + (int) quality;
        }

        public void UpdateAttributes(byte level, out int grade, out JiangHuQuality qualityAlign)
        {
            grade = 0;
            qualityAlign = JiangHuQuality.None;

            if (!powers.TryGetValue(level, out var powerValue))
            {
                return;
            }

            List<JiangHuStar> stars = new();
            stars.Add(new JiangHuStar((JiangHuQuality)powerValue.Quality1, (JiangHuAttrType)powerValue.Type1, level, 1));
            stars.Add(new JiangHuStar((JiangHuQuality)powerValue.Quality2, (JiangHuAttrType)powerValue.Type2, level, 2));
            stars.Add(new JiangHuStar((JiangHuQuality)powerValue.Quality3, (JiangHuAttrType)powerValue.Type3, level, 3));
            stars.Add(new JiangHuStar((JiangHuQuality)powerValue.Quality4, (JiangHuAttrType)powerValue.Type4, level, 4));
            stars.Add(new JiangHuStar((JiangHuQuality)powerValue.Quality5, (JiangHuAttrType)powerValue.Type5, level, 5));
            stars.Add(new JiangHuStar((JiangHuQuality)powerValue.Quality6, (JiangHuAttrType)powerValue.Type6, level, 6));
            stars.Add(new JiangHuStar((JiangHuQuality)powerValue.Quality7, (JiangHuAttrType)powerValue.Type7, level, 7));
            stars.Add(new JiangHuStar((JiangHuQuality)powerValue.Quality8, (JiangHuAttrType)powerValue.Type8, level, 8));
            stars.Add(new JiangHuStar((JiangHuQuality)powerValue.Quality9, (JiangHuAttrType)powerValue.Type9, level, 9));

            JiangHuQuality lowestQuality = JiangHuQuality.Epic;

            List<int> currentPowerList = new();
            int align = 1;
            JiangHuAttrType lastType = JiangHuAttrType.None;
            uint currentInnerPower = 0;
            foreach (var star in stars.Where(x => x.Type != JiangHuAttrType.None).OrderBy(x => x.Star))
            {
                if (lastType != JiangHuAttrType.None && lastType != star.Type)
                {
                    int power = 0;
                    foreach (var pow in currentPowerList)
                    {
                        power += (int)(pow * SequenceBonus[align]);
                    }

                    InnerPower += (uint)(currentInnerPower * SequenceInnerStrength[align]);

                    AddAttribute(lastType, power);

                    currentPowerList.Clear();
                    align = 1;
                    currentInnerPower = 0;
                }

                currentPowerList.Add(GetPowerEffect(star.Type, star.Quality)?.AttribValue ?? 0);
                currentInnerPower += PowerValue[(int)(star.Quality - 1)];

                if (lastType != JiangHuAttrType.None && lastType == star.Type)
                {
                    align++;
                }

                if (star.Quality < lowestQuality)
                {
                    lowestQuality = star.Quality;
                }

                lastType = star.Type;
            }

            if (currentPowerList.Count > 0)
            {
                int power = 0;
                foreach (var pow in currentPowerList)
                {
                    power += (int)(pow * SequenceBonus[align]);
                }
                if (currentInnerPower != 0)
                {
                    InnerPower += (uint)(currentInnerPower * SequenceInnerStrength[align]);
                }
                AddAttribute(lastType, power);

                if (stars.Count == 9)
                {
                    grade = 1;
                }

                qualityAlign = lowestQuality;
            }

            MaxInnerPowerHistory = Math.Max(InnerPower, MaxInnerPowerHistory);
        }

        public void AddAttribute(JiangHuAttrType type, int value)
        {
            switch (type)
            {
                case JiangHuAttrType.MaxLife:
                    maxLife += value;
                    break;
                case JiangHuAttrType.Attack:
                    attack += value;
                    break;
                case JiangHuAttrType.MagicAttack:
                    magicAttack += value;
                    break;
                case JiangHuAttrType.Defense:
                    defense += value;
                    break;
                case JiangHuAttrType.MagicDefense:
                    magicDefense += value;
                    break;
                case JiangHuAttrType.FinalDamage:
                    finalDamage += value;
                    break;
                case JiangHuAttrType.FinalMagicDamage:
                    finalMagicDamage += value;
                    break;
                case JiangHuAttrType.FinalDefense:
                    finalDefense += value;
                    break;
                case JiangHuAttrType.FinalMagicDefense:
                    finalMagicDefense += value;
                    break;
                case JiangHuAttrType.CriticalStrike:
                    criticalStrike += value;
                    break;
                case JiangHuAttrType.SkillCriticalStrike:
                    skillCriticalStrike += value;
                    break;
                case JiangHuAttrType.Immunity:
                    immunity += value;
                    break;
                case JiangHuAttrType.Breakthrough:
                    breakthrough += value;
                    break;
                case JiangHuAttrType.Counteraction:
                    counteraction += value;
                    break;
                case JiangHuAttrType.MaxMana:
                    maxMana += value;
                    break;
            }
        }

        public async Task DailyClearAsync()
        {
            if (jiangHuTimes != null)
            {
                jiangHuTimes.FreeTimes = 0;
                jiangHuTimes.PaidTimes = 0;
                await SaveAsync();
                await SendStarsAsync();
            }
        }

        private readonly TimeOut awardPointsTimer = new();

        public async Task OnTimerAsync()
        {
            if (!HasJiangHu)
            {
                return;
            }

            if (awardPointsTimer.ToNextTime() && FreeCaltivateParam < MAX_FREE_COURSE * POINTS_TO_COURSE)
            {
                if (
                    user.IsAlive
                    && user.Map.IsJiangHuMap()
                    && user.Map.QueryRegion(RegionType.JiangHuBonusArea, user.X, user.Y)
                    && IsActive)
                {
                    FreeCaltivateParam += TwinCityAddPoints[Talent];
                }
                else
                {
                    FreeCaltivateParam += OutTwinCityAddPoints[Talent];
                }
            }

            if (exitKongFuTimer.IsActive() && exitKongFuTimer.IsTimeOut())
            {
                await SendStatusAsync();
                exitKongFuTimer.Clear();
            }
        }
    }
}
