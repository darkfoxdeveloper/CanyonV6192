using Canyon.Database.Entities;
using Canyon.Game.Database;
using Canyon.Game.Database.Repositories;
using Canyon.Game.Services.Managers;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States.User;
using static Canyon.Game.Sockets.Game.Packets.MsgRank;

namespace Canyon.Game.States
{
    public sealed class Fate
    {
        private readonly Character user;
        private FateData dbFate;
        private readonly List<DbFateProtect> fateProtect = new();

        private int criticalStrike,
            skillCriticalStrike,
            immunity,
            breakthrough,
            counteraction,
            healthPoints,
            attack,
            magicAttack,
            magicDefense,
            finalDamage,
            finalMagicDamage,
            finalDefense,
            finalMagicDefense;

        public Fate(Character user)
        {
            this.user = user;
        }

        public int CriticalStrike => criticalStrike;
        public int SkillCriticalStrike => skillCriticalStrike;
        public int Immunity => immunity;
        public int Breakthrough => breakthrough;
        public int Counteraction => counteraction;
        public int HealthPoints => healthPoints;
        public int Attack => attack;
        public int MagicAttack => magicAttack;
        public int MagicDefense => magicDefense;
        public int FinalDamage => finalDamage;
        public int FinalMagicDamage => finalMagicDamage;
        public int FinalDefense => finalDefense;
        public int FinalMagicDefense => finalMagicDefense;

        public async Task InitializeAsync()
        {
            dbFate = await FateManager.GetPlayerAsync(user.Identity);
            if (dbFate != null)
            {
                fateProtect.AddRange(await FateProtectRepository.GetAsync(user.Identity));
                await SendAsync(true);
                FateManager.UpdateStatus(user);
                await SubmitRankAsync();
                await SendExpiryInfoAsync();
            }
        }

        public async Task UnlockAsync(FateType type)
        {
            var init = FateManager.GetInitializationRequirements(type);
            if (init == null)
            {
                return;
            }

            if (init.Data3 > user.Metempsychosis)
            {
                return;
            }

            if (init.Data2 > user.Level && init.Data3 >= user.Metempsychosis)
            {
                return;
            }

            if (type > FateType.Dragon && IsLocked(type - 1))
            {
                return;
            }

            if (type > FateType.Dragon && init.Data4 > user.Fate.GetScore(type - 1))
            {
                return;
            }

            if (!IsLocked(type))
            {
                return;
            }

            if (dbFate == null)
            {
                dbFate = new FateData(new DbFatePlayer
                {
                    PlayerId = user.Identity
                })
                {
                    Name = user.Name,
                    Lookface = user.Mesh,
                    Level = user.Level,
                    Mate = user.MateName,
                    FirstProfession = user.FirstProfession,
                    PreviousProfession = user.PreviousProfession,
                    Profession = user.Profession
                };
                FateManager.Add(dbFate);

                user.ChiPoints = Math.Max(4000, user.ChiPoints);
            }

            await FateManager.InitialFateAttributeAsync(user, type, dbFate);
            await user.SendAsync(new MsgPlayerAttribInfo(user));
        }

        public async Task GenerateAsync(FateType type, TrainingSave save)
        {
            if (IsLocked(type))
            {
                return;
            }

            int cost = 50;
            if (save.HasFlag(TrainingSave.Attr1))
            {
                cost += 50;
            }

            if (save.HasFlag(TrainingSave.Attr2))
            {
                cost += 50;
            }

            if (save.HasFlag(TrainingSave.Attr3))
            {
                cost += 50;
            }

            if (save.HasFlag(TrainingSave.Attr4))
            {
                cost += 50;
            }

            if (!await user.SpendStrengthValueAsync(cost, false))
            {
                return;
            }

            await FateManager.GenerateAsync(user, type, dbFate, save);
            await user.SendAsync(new MsgPlayerAttribInfo(user));

            await user.UpdateTaskActivityAsync(ActivityManager.ActivityType.ChiStudy);
        }

        public bool IsValidProtection(FateType fateType)
        {
            DbFateProtect protect = fateProtect.FirstOrDefault(x => x.FateNo == (byte)fateType);
            if (protect == null)
            {
                return false;
            }
            return protect.ExpiryDate > UnixTimestamp.Now;
        }

        public async Task<bool> ProtectAsync(FateType fateType, bool update)
        {
            if (IsLocked(fateType))
            {
                return false;
            }

            DbFateProtect protect = fateProtect.FirstOrDefault(x => x.FateNo == (byte)fateType);
            if (protect != null)
            {
                if (!update)
                {
                    protect.ExpiryDate = UnixTimestamp.FromDateTime(DateTime.Now.AddDays(5));
                }
            }
            else
            {
                protect = new DbFateProtect
                {
                    PlayerId = user.Identity,
                    FateNo = (byte)fateType,
                    ExpiryDate = UnixTimestamp.FromDateTime(DateTime.Now.AddDays(5))
                };
            }
            DbFatePlayer fate = dbFate;
            if (fateType == FateType.Dragon)
            {
                protect.Attrib1 = fate.Fate1Attrib1;
                protect.Attrib2 = fate.Fate1Attrib2;
                protect.Attrib3 = fate.Fate1Attrib3;
                protect.Attrib4 = fate.Fate1Attrib4;
            }
            else if (fateType == FateType.Phoenix)
            {
                protect.Attrib1 = fate.Fate2Attrib1;
                protect.Attrib2 = fate.Fate2Attrib2;
                protect.Attrib3 = fate.Fate2Attrib3;
                protect.Attrib4 = fate.Fate2Attrib4;
            }
            else if (fateType == FateType.Tiger)
            {
                protect.Attrib1 = fate.Fate3Attrib1;
                protect.Attrib2 = fate.Fate3Attrib2;
                protect.Attrib3 = fate.Fate3Attrib3;
                protect.Attrib4 = fate.Fate3Attrib4;
            }
            else if (fateType == FateType.Turtle)
            {
                protect.Attrib1 = fate.Fate4Attrib1;
                protect.Attrib2 = fate.Fate4Attrib2;
                protect.Attrib3 = fate.Fate4Attrib3;
                protect.Attrib4 = fate.Fate4Attrib4;
            }
            else
            {
                return false;
            }

            fateProtect.Add(protect);
            await SaveAsync();
            await SendProtectInfoAsync();
            return true;
        }

        public int GetRestorationCost(FateType fateType)
        {
            DbFateProtect protect = fateProtect.FirstOrDefault(x => x.FateNo == (byte)fateType);
            if (protect == null)
            {
                return -1;
            }

            return (int)((UnixTimestamp.Now - protect.ExpiryDate) / 39.875);
        }

        public async Task<bool> ExtendAsync(FateType fateType)
        {
            if (IsLocked(fateType))
            {
                return false;
            }

            DbFateProtect protect = fateProtect.FirstOrDefault(x => x.FateNo == (byte)fateType);
            if (protect == null)
            {
                return false;
            }

            protect.ExpiryDate = UnixTimestamp.FromDateTime(DateTime.Now.AddDays(5));
            await SaveAsync();
            await SendProtectInfoAsync();
            return true;
        }

        public async Task<bool> RestoreProtectionAsync(FateType fateType)
        {
            if (IsLocked(fateType))
            {
                return false;
            }

            DbFateProtect protect = fateProtect.FirstOrDefault(x => x.FateNo == (byte)fateType);
            if (protect == null)
            {
                return false;
            }

            DbFatePlayer fate = dbFate;
            if (fateType == FateType.Dragon)
            {
                fate.Fate1Attrib1 = protect.Attrib1;
                fate.Fate1Attrib2 = protect.Attrib2;
                fate.Fate1Attrib3 = protect.Attrib3;
                fate.Fate1Attrib4 = protect.Attrib4;
            }
            else if (fateType == FateType.Phoenix)
            {
                fate.Fate2Attrib1 = protect.Attrib1;
                fate.Fate2Attrib2 = protect.Attrib2;
                fate.Fate2Attrib3 = protect.Attrib3;
                fate.Fate2Attrib4 = protect.Attrib4;
            }
            else if (fateType == FateType.Tiger)
            {
                fate.Fate3Attrib1 = protect.Attrib1;
                fate.Fate3Attrib2 = protect.Attrib2;
                fate.Fate3Attrib3 = protect.Attrib3;
                fate.Fate3Attrib4 = protect.Attrib4;
            }
            else if (fateType == FateType.Turtle)
            {
                fate.Fate4Attrib1 = protect.Attrib1;
                fate.Fate4Attrib2 = protect.Attrib2;
                fate.Fate4Attrib3 = protect.Attrib3;
                fate.Fate4Attrib4 = protect.Attrib4;
            }
            else
            {
                return false;
            }

            await SaveAsync();
            await SendAsync(true);
            FateManager.UpdateStatus(user);
            await SubmitRankAsync();
            await SendProtectInfoAsync();
            return true;
        }

        public async Task<bool> AbandonAsync(FateType fateType)
        {
            DbFateProtect protect = fateProtect.FirstOrDefault(x => x.FateNo == (byte)fateType);
            if (protect == null)
            {
                return false;
            }

            for (int i = 0; i < fateProtect.Count; i++)
            {
                if (fateProtect[i].Id == protect.Id)
                {
                    fateProtect.RemoveAt(i);
                    break;
                }
            }

            await ServerDbContext.DeleteAsync(protect);
            await SendAsync(true);
            FateManager.UpdateStatus(user);
            await SubmitRankAsync();
            await SendProtectInfoAsync();
            return true;
        }

        public bool IsLocked(FateType type)
        {
            DbFatePlayer fate = dbFate;
            if (fate == null)
            {
                return true;
            }

            if (type == FateType.Dragon)
            {
                return fate.Fate1Attrib1 == 0;
            }
            else if (type == FateType.Phoenix)
            {
                return fate.Fate2Attrib1 == 0;
            }
            else if (type == FateType.Tiger)
            {
                return fate.Fate3Attrib1 == 0;
            }
            else if (type == FateType.Turtle)
            {
                return fate.Fate4Attrib1 == 0;
            }
            return true;
        }

        public int GetScore(FateType type)
        {
            return FateManager.GetScore(dbFate, type);
        }

        public int GetPower(TrainingAttrType attr)
        {
            return FateManager.GetPower(dbFate, attr);
        }

        public void RefreshPower()
        {
            DbFatePlayer fate = dbFate?.GetDatabase();
            if (fate != null)
            {
                ResetAttributes();
                AddAttribute(fate.Fate1Attrib1);
                AddAttribute(fate.Fate1Attrib2);
                AddAttribute(fate.Fate1Attrib3);
                AddAttribute(fate.Fate1Attrib4);

                AddAttribute(fate.Fate2Attrib1);
                AddAttribute(fate.Fate2Attrib2);
                AddAttribute(fate.Fate2Attrib3);
                AddAttribute(fate.Fate2Attrib4);

                AddAttribute(fate.Fate3Attrib1);
                AddAttribute(fate.Fate3Attrib2);
                AddAttribute(fate.Fate3Attrib3);
                AddAttribute(fate.Fate3Attrib4);

                AddAttribute(fate.Fate4Attrib1);
                AddAttribute(fate.Fate4Attrib2);
                AddAttribute(fate.Fate4Attrib3);
                AddAttribute(fate.Fate4Attrib4);
            }
        }

        private void ResetAttributes()
        {
            criticalStrike = 0;
            skillCriticalStrike = 0;
            immunity = 0;
            breakthrough = 0;
            counteraction = 0;
            healthPoints = 0;
            attack = 0;
            magicAttack = 0;
            magicDefense = 0;
            finalDamage = 0;
            finalMagicDamage = 0;
            finalDefense = 0;
            finalMagicDefense = 0;
        }

        public void AddAttribute(int value)
        {
            int power = value % 10000;
            switch (FateManager.ReferenceType(value))
            {
                case TrainingAttrType.Criticalstrike:
                    {
                        criticalStrike += power;
                        break;
                    }

                case TrainingAttrType.Skillcriticalstrike:
                    {
                        skillCriticalStrike += power;
                        break;
                    }

                case TrainingAttrType.Immunity:
                    {
                        immunity += power;
                        break;
                    }

                case TrainingAttrType.Breakthrough:
                    {
                        breakthrough += power;
                        break;
                    }

                case TrainingAttrType.Counteraction:
                    {
                        counteraction += power;
                        break;
                    }

                case TrainingAttrType.Health:
                    {
                        healthPoints += power;
                        break;
                    }

                case TrainingAttrType.Attack:
                    {
                        attack += power;
                        break;
                    }

                case TrainingAttrType.Magicattack:
                    {
                        magicAttack += power;
                        break;
                    }

                case TrainingAttrType.Mdefense:
                    {
                        magicDefense += power;
                        break;
                    }

                case TrainingAttrType.Finalattack:
                    {
                        finalDamage += power;
                        break;
                    }

                case TrainingAttrType.Finalmagicattack:
                    {
                        finalMagicDamage += power;
                        break;
                    }

                case TrainingAttrType.Damagereduction:
                    {
                        finalDefense += power;
                        break;
                    }

                case TrainingAttrType.Magicdamagereduction:
                    {
                        finalMagicDefense += power;
                        break;
                    }
            }
        }

        public Task SendAsync(bool update, Character target = null)
        {
            MsgTrainingVitalityInfo msg = new();
            msg.Mode = (ushort)(update ? 1 : 0);
            msg.Identity = user.Identity;

            DbFatePlayer fate = dbFate;
            if (fate != null)
            {
                if (fate.Fate1Attrib1 != 0)
                {
                    msg.Datas.Add(new MsgTrainingVitalityInfo.TrainingData
                    {
                        Type = (byte)FateType.Dragon,
                        Power1 = fate.Fate1Attrib1,
                        Power2 = fate.Fate1Attrib2,
                        Power3 = fate.Fate1Attrib3,
                        Power4 = fate.Fate1Attrib4
                    });
                }

                if (fate.Fate2Attrib1 != 0)
                {
                    msg.Datas.Add(new MsgTrainingVitalityInfo.TrainingData
                    {
                        Type = (byte)FateType.Phoenix,
                        Power1 = fate.Fate2Attrib1,
                        Power2 = fate.Fate2Attrib2,
                        Power3 = fate.Fate2Attrib3,
                        Power4 = fate.Fate2Attrib4
                    });
                }

                if (fate.Fate3Attrib1 != 0)
                {
                    msg.Datas.Add(new MsgTrainingVitalityInfo.TrainingData
                    {
                        Type = (byte)FateType.Tiger,
                        Power1 = fate.Fate3Attrib1,
                        Power2 = fate.Fate3Attrib2,
                        Power3 = fate.Fate3Attrib3,
                        Power4 = fate.Fate3Attrib4
                    });
                }

                if (fate.Fate4Attrib1 != 0)
                {
                    msg.Datas.Add(new MsgTrainingVitalityInfo.TrainingData
                    {
                        Type = (byte)FateType.Turtle,
                        Power1 = fate.Fate4Attrib1,
                        Power2 = fate.Fate4Attrib2,
                        Power3 = fate.Fate4Attrib3,
                        Power4 = fate.Fate4Attrib4
                    });
                }
            }

            if (target == null)
            {
                msg.Strength = user.ChiPoints;
                msg.Data = fate?.AttribLockInfo ?? 0;
                return user.SendAsync(msg);
            }
            return target.SendAsync(msg);
        }

        public async Task SubmitRankAsync()
        {
            MsgRank msg;
            int rank = FateManager.GetPlayerRank(user.Identity, RankType.ChiDragon);
            msg = new MsgRank
            {
                Mode = RequestType.QueryInfo,
                Identity = 60000001
            };
            msg.Infos.Add(new QueryStruct
            {
                Type = (ulong)(rank + 1),
                Amount = (ulong)user.Fate.GetScore(FateType.Dragon),
                Identity = user.Identity,
                Name = user.Name
            });
            await user.SendAsync(msg);

            rank = FateManager.GetPlayerRank(user.Identity, RankType.ChiPhoenix);
            msg = new MsgRank
            {
                Mode = RequestType.QueryInfo,
                Identity = 60000002
            };
            msg.Infos.Add(new QueryStruct
            {
                Type = (ulong)(rank + 1),
                Amount = (ulong)user.Fate.GetScore(FateType.Phoenix),
                Identity = user.Identity,
                Name = user.Name
            });
            await user.SendAsync(msg);

            rank = FateManager.GetPlayerRank(user.Identity, RankType.ChiTiger);
            msg = new MsgRank
            {
                Mode = RequestType.QueryInfo,
                Identity = 60000003
            };
            msg.Infos.Add(new QueryStruct
            {
                Type = (ulong)(rank + 1),
                Amount = (ulong)user.Fate.GetScore(FateType.Tiger),
                Identity = user.Identity,
                Name = user.Name
            });
            await user.SendAsync(msg);

            rank = FateManager.GetPlayerRank(user.Identity, RankType.ChiTurtle);
            msg = new MsgRank
            {
                Mode = RequestType.QueryInfo,
                Identity = 60000004
            };
            msg.Infos.Add(new QueryStruct
            {
                Type = (ulong)(rank + 1),
                Amount = (ulong)user.Fate.GetScore(FateType.Turtle),
                Identity = user.Identity,
                Name = user.Name
            });
            await user.SendAsync(msg);
        }

        public Task SendProtectInfoAsync()
        {
            if (fateProtect.Count == 0)
            {
                return Task.CompletedTask;
            }
            
            MsgTrainingVitalityProtectInfo msg = new MsgTrainingVitalityProtectInfo();
            foreach (var protect in fateProtect)
            {
                string expireTimeString = UnixTimestamp.ToDateTime(protect.ExpiryDate).ToString("yyMMddHHmm");
                uint.TryParse(expireTimeString, out var expireTime);
                msg.Protects.Add(new MsgTrainingVitalityProtectInfo.ProtectInfo
                {
                    FateType = (FateType)protect.FateNo,
                    Seconds = (int)expireTime,
                    Attribute1 = protect.Attrib1,
                    Attribute2 = protect.Attrib2,
                    Attribute3 = protect.Attrib3,
                    Attribute4 = protect.Attrib4,
                });
            }
            return user.SendAsync(msg);
        }

        public Task SendExpiryInfoAsync()
        {
            MsgTrainingVitalityExpiryNotify msg = new MsgTrainingVitalityExpiryNotify();
            foreach (var protect in fateProtect)
            {
                if (!IsValidProtection((FateType)protect.FateNo))
                {
                    msg.Fates.Add((FateType)protect.FateNo);
                }
            }
            if (msg.Count > 0)
            {
                return user.SendAsync(msg);
            }
            return Task.CompletedTask;
        }

        public async Task<bool> SaveAsync()
        {
            DbFatePlayer fate = dbFate;
            if (fate != null)
            {
                await ServerDbContext.SaveAsync(fate);
            }
            if (fateProtect.Count > 0)
            {
                await ServerDbContext.SaveRangeAsync(fateProtect);
            }
            return true;
        }

        public enum FateType
        {
            None,
            Dragon = 1,
            Phoenix,
            Tiger,
            Turtle
        }

        [Flags]
        public enum TrainingSave
        {
            None = 0,
            Attr1 = 0x1,
            Attr2 = 0x2,
            Attr3 = 0x4,
            Attr4 = 0x8,
            All = Attr1 | Attr2 | Attr3 | Attr4
        }

        public enum TrainingAttrType
        {
            None = 0,
            Criticalstrike = 1,
            Skillcriticalstrike = 2,
            Immunity = 3,
            Breakthrough = 4,
            Counteraction = 5,
            Health = 6,
            Attack = 7,
            Magicattack = 8,
            Mdefense = 9,
            Finalattack = 10,
            Finalmagicattack = 11,
            Damagereduction = 12,
            Magicdamagereduction = 13
        }
    }
}
