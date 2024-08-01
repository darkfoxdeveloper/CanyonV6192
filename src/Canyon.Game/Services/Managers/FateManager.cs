using Canyon.Database.Entities;
using Canyon.Game.Database.Repositories;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States;
using Canyon.Game.States.User;
using System.Collections.Concurrent;
using static Canyon.Game.Sockets.Game.Packets.MsgRank;
using static Canyon.Game.States.Fate;

namespace Canyon.Game.Services.Managers
{
    public class FateManager
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<FateManager>();
        private static readonly ConcurrentDictionary<uint, FateData> mFatePlayers = new();
        private static readonly List<DbFateRank> mFateRanks = new();
        private static readonly List<DbFateRand> mFateRands = new();
        private static readonly List<DbFateRule> mFateRules = new();
        private static readonly List<DbInitFateAttrib> mInitFateAttribs = new();
        private static readonly List<DbConfig> mFateOpenRule = new();
        private static DbConfig mSpendPointsValue = null;
        private static DbConfig mAwardPointsValue = null;

        public static async Task InitializeAsync()
        {
            logger.LogInformation("Initializing fate manager");
            foreach (var player in await FatePlayerRepository.GetAsync())
            {
                var user = await CharacterRepository.FindByIdentityAsync(player.PlayerId);
                if (user == null)
                {
                    continue;
                }

                var mate = await CharacterRepository.FindByIdentityAsync(user.Mate);

                var data = new FateData(player)
                {
                    Name = user.Name,
                    Level = user.Level,
                    Mate = mate?.Name ?? StrNone,
                    Profession = user.Profession,
                    FirstProfession = user.FirstProfession,
                    PreviousProfession = user.PreviousProfession,
                    Lookface = user.Mesh
                };
                mFatePlayers.TryAdd(player.PlayerId, data);
            }
            mFateRands.AddRange(await FateRandRepository.GetAsync());
            mFateRanks.AddRange(await FateRankRepository.GetAsync());
            mFateRules.AddRange(await FateRuleRepository.GetAsync());
            mInitFateAttribs.AddRange(await FateInitAttribRepository.GetAsync());
            mSpendPointsValue = (await ConfigRepository.GetAsync(90004)).FirstOrDefault();
            mAwardPointsValue = (await ConfigRepository.GetAsync(90005)).FirstOrDefault();
            mFateOpenRule.AddRange(await ConfigRepository.GetAsync(90006));
        }

        public static async Task<FateData> GetPlayerAsync(uint idUser)
        {
            if (mFatePlayers.TryGetValue(idUser, out var player))
            {
                return player;
            }

            var dbPlayer = await FatePlayerRepository.GetAsync(idUser);
            if (dbPlayer is not null)
            {
                var user = await CharacterRepository.FindByIdentityAsync(idUser);
                if (user == null)
                {
                    return null;
                }

                var mate = await CharacterRepository.FindByIdentityAsync(user.Mate);
                player = new FateData(dbPlayer)
                {
                    Name = user.Name,
                    Level = user.Level,
                    Mate = mate?.Name ?? StrNone,
                    Profession = user.Profession,
                    FirstProfession = user.FirstProfession,
                    PreviousProfession = user.PreviousProfession,
                    Lookface = user.Mesh
                };
                mFatePlayers.TryAdd(idUser, player);
                return player;
            }
            return null;
        }

        public static bool Add(FateData fate)
        {
            return mFatePlayers.TryAdd(fate.Identity, fate);
        }

        public static async Task InitialFateAttributeAsync(Character user, FateType type, DbFatePlayer fate)
        {
            var initialFate = mInitFateAttribs.FirstOrDefault(x => x.ProfSort == user.ProfessionSort);
            if (initialFate == null)
            {
                await GenerateAsync(user, type, fate, TrainingSave.None);
                return;
            }

            switch (type)
            {
                case FateType.Dragon:
                    {
                        SetAttribute(fate, type, 1, initialFate.Fate1Attrib1);
                        SetAttribute(fate, type, 2, initialFate.Fate1Attrib2);
                        SetAttribute(fate, type, 3, initialFate.Fate1Attrib3);
                        SetAttribute(fate, type, 4, initialFate.Fate1Attrib4);
                        break;
                    }
                case FateType.Phoenix:
                    {
                        SetAttribute(fate, type, 1, initialFate.Fate2Attrib1);
                        SetAttribute(fate, type, 2, initialFate.Fate2Attrib2);
                        SetAttribute(fate, type, 3, initialFate.Fate2Attrib3);
                        SetAttribute(fate, type, 4, initialFate.Fate2Attrib4);
                        break;
                    }
                case FateType.Tiger:
                    {
                        SetAttribute(fate, type, 1, initialFate.Fate3Attrib1);
                        SetAttribute(fate, type, 2, initialFate.Fate3Attrib2);
                        SetAttribute(fate, type, 3, initialFate.Fate3Attrib3);
                        SetAttribute(fate, type, 4, initialFate.Fate3Attrib4);
                        break;
                    }
                case FateType.Turtle:
                    {
                        SetAttribute(fate, type, 1, initialFate.Fate4Attrib1);
                        SetAttribute(fate, type, 2, initialFate.Fate4Attrib2);
                        SetAttribute(fate, type, 3, initialFate.Fate4Attrib3);
                        SetAttribute(fate, type, 4, initialFate.Fate4Attrib4);
                        break;
                    }
            }

            await user.Fate.SaveAsync();
            await user.Fate.SendAsync(true);

            UpdateStatus(user);
            await user.Fate.SubmitRankAsync();
        }

        public static async Task<bool> GenerateAsync(Character user, FateType type, DbFatePlayer fate, TrainingSave flag)
        {
            int oldRank = GetPlayerRank(user.Identity, (RankType)type);

            DbFatePlayer backup = new()
            {
                Fate1Attrib1 = fate.Fate1Attrib1,
                Fate1Attrib2 = fate.Fate1Attrib2,
                Fate1Attrib3 = fate.Fate1Attrib3,
                Fate1Attrib4 = fate.Fate1Attrib4,
                Fate2Attrib1 = fate.Fate2Attrib1,
                Fate2Attrib2 = fate.Fate2Attrib2,
                Fate2Attrib3 = fate.Fate2Attrib3,
                Fate2Attrib4 = fate.Fate2Attrib4,
                Fate3Attrib1 = fate.Fate3Attrib1,
                Fate3Attrib2 = fate.Fate3Attrib2,
                Fate3Attrib3 = fate.Fate3Attrib3,
                Fate3Attrib4 = fate.Fate3Attrib4,
                Fate4Attrib1 = fate.Fate4Attrib1,
                Fate4Attrib2 = fate.Fate4Attrib2,
                Fate4Attrib3 = fate.Fate4Attrib3,
                Fate4Attrib4 = fate.Fate4Attrib4
            };

            bool success = true;

            var fateRands = mFateRands.Where(x => x.FateNo == (int)type).OrderBy(x => x.RangeRate).ToArray();
            var rules = mFateRules.Where(x => x.FateNo == (int)type).ToList();
            List<TrainingAttrType> generated = new();
            for (int i = 1; i < 5; i++)
            {
                if (((int)flag & (1 << (i - 1))) != 0)
                {
                    generated.Add(ReferenceType(GetPowerByIndex(fate, type, i)));
                    continue;
                }
            }

            for (int i = 1; i < 5; i++)
            {
                if (((int)flag & (1 << (i - 1))) != 0)
                {
                    continue;
                }

                int rand = await NextAsync(rules.Count) % rules.Count;
                var rule = rules[rand];
                TrainingAttrType attr = (TrainingAttrType)rule.AttrType;

                rules.RemoveAt(rand);

                if (generated.Any(x => (int)x == (int)attr))
                {
                    generated.Add(attr);
                    i--;
                    continue;
                }

                generated.Add(attr);

                int rate = await NextAsync(100000);
                double dRate = await NextRateAsync(0.999);
                DbFateRand fateRand = null;
                for (int x = 0; x < fateRands.Length; x++)
                {
                    if (rate < fateRands[x].RangeRate)
                    {
                        fateRand = fateRands[x];
                        break;
                    }
                }

                if (fateRand == null)
                {
                    success = false;
                    break;
                }

                dRate += fateRand.RangeNo;
                if (fateRand.FateNo == 100)
                {
                    dRate = Math.Floor(dRate);
                }

                int delta = (int)((rule.AttribValueMax - rule.AttribValueMin) * (dRate / 100));
                int power = Math.Min(rule.AttribValueMax, Math.Max(rule.AttribValueMin, rule.AttribValueMin + delta));
                power += ((int)attr * 10000);
                if (GetScore(type, power) >= 90)
                {
                    await BroadcastWorldMsgAsync(new MsgTrainingVitalityScore
                    {
                        Name = user.Name,
                        AttrType = (byte)type,
                        Power = power
                    });
                }

                SetAttribute(fate, type, i, power);
            }

            if (!success)
            {
                fate.Fate1Attrib1 = backup.Fate1Attrib1;
                fate.Fate1Attrib2 = backup.Fate1Attrib2;
                fate.Fate1Attrib3 = backup.Fate1Attrib3;
                fate.Fate1Attrib4 = backup.Fate1Attrib4;

                fate.Fate2Attrib1 = backup.Fate2Attrib1;
                fate.Fate2Attrib2 = backup.Fate2Attrib2;
                fate.Fate2Attrib3 = backup.Fate2Attrib3;
                fate.Fate2Attrib4 = backup.Fate2Attrib4;

                fate.Fate3Attrib1 = backup.Fate3Attrib1;
                fate.Fate3Attrib2 = backup.Fate3Attrib2;
                fate.Fate3Attrib3 = backup.Fate3Attrib3;
                fate.Fate3Attrib4 = backup.Fate3Attrib4;

                fate.Fate4Attrib1 = backup.Fate4Attrib1;
                fate.Fate4Attrib2 = backup.Fate4Attrib2;
                fate.Fate4Attrib3 = backup.Fate4Attrib3;
                fate.Fate4Attrib4 = backup.Fate4Attrib4;
                return false;
            }

            int typeInt = (int)type;
            byte flagByte = (byte)flag;

            fate.AttribLockInfo &= ~(fate.AttribLockInfo >> (4 * (typeInt - 1)));
            fate.AttribLockInfo |= (uint)(flagByte << (4 * (typeInt - 1)));

            await user.Fate.SaveAsync();
            await user.Fate.SendAsync(true);

            int currentRank = GetPlayerRank(user.Identity, (RankType)type);
            UpdateStatus(user);

            if (currentRank != oldRank)
            {
                foreach (var fatePlayer in mFatePlayers.Values)
                {
                    Character player = RoleManager.GetUser(fatePlayer.Identity);
                    if (player != null)
                    {
                        player.QueueAction(() =>
                        {
                            UpdateStatus(player);
                            return Task.CompletedTask;
                        });
                    }
                }

                await user.Fate.SubmitRankAsync();
            }
            return true;
        }

        public static void UpdateStatus(Character user)
        {
            if (user?.Fate != null)
            {
                user.Fate.RefreshPower();
                UpdateRankStatus(user, 1); // dragon rank
                UpdateRankStatus(user, 2); // phoenix rank
                UpdateRankStatus(user, 3); // tiger rank
                UpdateRankStatus(user, 4); // turtle rank
            }
        }

        private static void UpdateRankStatus(Character user, int fateType)
        {
            if (user.Fate.IsLocked((FateType)fateType))
            {
                return;
            }

            if (user.Fate.GetScore((FateType)fateType) == 400)
            {
                var fateRank = mFateRanks.FirstOrDefault(x => x.FateNo == fateType && x.Sort == 1);
                if (fateRank != null)
                {
                    user.Fate.AddAttribute(fateRank.Attrib1);
                    user.Fate.AddAttribute(fateRank.Attrib2);
                    user.Fate.AddAttribute(fateRank.Attrib3);
                    user.Fate.AddAttribute(fateRank.Attrib4);
                }
            }
            else
            {
                int rank = GetPlayerRank(user.Identity, (RankType)fateType);
                if (rank > -1 && rank < 50)
                {
                    var fateRank = mFateRanks.FirstOrDefault(x => x.FateNo == fateType && x.Sort == rank + 1);
                    if (fateRank != null)
                    {
                        user.Fate.AddAttribute(fateRank.Attrib1);
                        user.Fate.AddAttribute(fateRank.Attrib2);
                        user.Fate.AddAttribute(fateRank.Attrib3);
                        user.Fate.AddAttribute(fateRank.Attrib4);
                    }
                }
            }
        }

        public static int GetPower(DbFatePlayer fate, TrainingAttrType attr)
        {
            if (fate == null)
            {
                return 0;
            }

            int result = 0;
            if (ReferenceType(fate.Fate1Attrib1) == attr)
            {
                result += Power(fate.Fate1Attrib1);
            }
            else if (ReferenceType(fate.Fate1Attrib2) == attr)
            {
                result += Power(fate.Fate1Attrib2);
            }
            else if (ReferenceType(fate.Fate1Attrib3) == attr)
            {
                result += Power(fate.Fate1Attrib3);
            }
            else if (ReferenceType(fate.Fate1Attrib4) == attr)
            {
                result += Power(fate.Fate1Attrib4);
            }

            if (ReferenceType(fate.Fate2Attrib1) == attr)
            {
                result += Power(fate.Fate2Attrib1);
            }
            else if (ReferenceType(fate.Fate2Attrib2) == attr)
            {
                result += Power(fate.Fate2Attrib2);
            }
            else if (ReferenceType(fate.Fate2Attrib3) == attr)
            {
                result += Power(fate.Fate2Attrib3);
            }
            else if (ReferenceType(fate.Fate2Attrib4) == attr)
            {
                result += Power(fate.Fate2Attrib4);
            }

            if (ReferenceType(fate.Fate3Attrib1) == attr)
            {
                result += Power(fate.Fate3Attrib1);
            }
            else if (ReferenceType(fate.Fate3Attrib2) == attr)
            {
                result += Power(fate.Fate3Attrib2);
            }
            else if (ReferenceType(fate.Fate3Attrib3) == attr)
            {
                result += Power(fate.Fate3Attrib3);
            }
            else if (ReferenceType(fate.Fate3Attrib4) == attr)
            {
                result += Power(fate.Fate3Attrib4);
            }

            if (ReferenceType(fate.Fate4Attrib1) == attr)
            {
                result += Power(fate.Fate4Attrib1);
            }
            else if (ReferenceType(fate.Fate4Attrib2) == attr)
            {
                result += Power(fate.Fate4Attrib2);
            }
            else if (ReferenceType(fate.Fate4Attrib3) == attr)
            {
                result += Power(fate.Fate4Attrib3);
            }
            else if (ReferenceType(fate.Fate4Attrib4) == attr)
            {
                result += Power(fate.Fate4Attrib4);
            }

            return result;
        }

        public static int GetPowerByIndex(DbFatePlayer fate, FateType type, int num)
        {
            if (type == FateType.Dragon)
            {
                if (num == 1)
                {
                    return fate.Fate1Attrib1;
                }

                if (num == 2)
                {
                    return fate.Fate1Attrib2;
                }

                if (num == 3)
                {
                    return fate.Fate1Attrib3;
                }

                if (num == 4)
                {
                    return fate.Fate1Attrib4;
                }
            }
            else if (type == FateType.Phoenix)
            {
                if (num == 1)
                {
                    return fate.Fate2Attrib1;
                }

                if (num == 2)
                {
                    return fate.Fate2Attrib2;
                }

                if (num == 3)
                {
                    return fate.Fate2Attrib3;
                }

                if (num == 4)
                {
                    return fate.Fate2Attrib4;
                }
            }
            else if (type == FateType.Tiger)
            {
                if (num == 1)
                {
                    return fate.Fate3Attrib1;
                }

                if (num == 2)
                {
                    return fate.Fate3Attrib2;
                }

                if (num == 3)
                {
                    return fate.Fate3Attrib3;
                }

                if (num == 4)
                {
                    return fate.Fate3Attrib4;
                }
            }
            else if (type == FateType.Turtle)
            {
                if (num == 1)
                {
                    return fate.Fate4Attrib1;
                }

                if (num == 2)
                {
                    return fate.Fate4Attrib2;
                }

                if (num == 3)
                {
                    return fate.Fate4Attrib3;
                }

                if (num == 4)
                {
                    return fate.Fate4Attrib4;
                }
            }
            return 0;
        }

        private static void SetAttribute(DbFatePlayer fate, FateType type, int num, int value)
        {
            if (type == FateType.Dragon)
            {
                if (num == 1)
                {
                    fate.Fate1Attrib1 = value;
                }

                if (num == 2)
                {
                    fate.Fate1Attrib2 = value;
                }

                if (num == 3)
                {
                    fate.Fate1Attrib3 = value;
                }

                if (num == 4)
                {
                    fate.Fate1Attrib4 = value;
                }
            }
            else if (type == FateType.Phoenix)
            {
                if (num == 1)
                {
                    fate.Fate2Attrib1 = value;
                }

                if (num == 2)
                {
                    fate.Fate2Attrib2 = value;
                }

                if (num == 3)
                {
                    fate.Fate2Attrib3 = value;
                }

                if (num == 4)
                {
                    fate.Fate2Attrib4 = value;
                }
            }
            else if (type == FateType.Tiger)
            {
                if (num == 1)
                {
                    fate.Fate3Attrib1 = value;
                }

                if (num == 2)
                {
                    fate.Fate3Attrib2 = value;
                }

                if (num == 3)
                {
                    fate.Fate3Attrib3 = value;
                }

                if (num == 4)
                {
                    fate.Fate3Attrib4 = value;
                }
            }
            else if (type == FateType.Turtle)
            {
                if (num == 1)
                {
                    fate.Fate4Attrib1 = value;
                }

                if (num == 2)
                {
                    fate.Fate4Attrib2 = value;
                }

                if (num == 3)
                {
                    fate.Fate4Attrib3 = value;
                }

                if (num == 4)
                {
                    fate.Fate4Attrib4 = value;
                }
            }
        }

        public static int GetScore(uint idUser, FateType type)
        {
            if (mFatePlayers.TryGetValue(idUser, out var player))
            {
                return GetScore(player, type);
            }

            return 0;
        }

        public static int GetScore(DbFatePlayer record, FateType type)
        {
            int total = 0;

            if (record == null)
            {
                return total;
            }

            if (type == FateType.Dragon)
            {
                total += GetScore(type, record.Fate1Attrib1);
                total += GetScore(type, record.Fate1Attrib2);
                total += GetScore(type, record.Fate1Attrib3);
                total += GetScore(type, record.Fate1Attrib4);
            }
            else if (type == FateType.Phoenix)
            {
                total += GetScore(type, record.Fate2Attrib1);
                total += GetScore(type, record.Fate2Attrib2);
                total += GetScore(type, record.Fate2Attrib3);
                total += GetScore(type, record.Fate2Attrib4);
            }
            else if (type == FateType.Tiger)
            {
                total += GetScore(type, record.Fate3Attrib1);
                total += GetScore(type, record.Fate3Attrib2);
                total += GetScore(type, record.Fate3Attrib3);
                total += GetScore(type, record.Fate3Attrib4);
            }
            else if (type == FateType.Turtle)
            {
                total += GetScore(type, record.Fate4Attrib1);
                total += GetScore(type, record.Fate4Attrib2);
                total += GetScore(type, record.Fate4Attrib3);
                total += GetScore(type, record.Fate4Attrib4);
            }
            return total;
        }

        private static int GetScore(FateType type, int attrib)
        {
            if (attrib == 0)
            {
                return 0;
            }
            var rule = GetRule(type, ReferenceType(attrib));
            int refPower = ReferencePower(rule, attrib);
            double baseValue = rule.AttribValueMax - rule.AttribValueMin;
            return (int)((refPower / baseValue) * 100);
        }

        public static TrainingAttrType ReferenceType(int power)
        {
            return (TrainingAttrType)(power / 10000);
        }

        public static int ReferencePower(DbFateRule rule, int power)
        {
            return (power % 10000) - rule.AttribValueMin;
        }

        public static int Power(int power)
        {
            return power % 10000;
        }

        public static async Task SendRankAsync(Character sender, MsgRank msg, RankType mode)
        {
            List<QueryStruct> rankData = new();
            int position = -1;
            FateType type = FateType.None;
            switch (mode)
            {
                case RankType.ChiDragon:
                    {
                        rankData = mFatePlayers.Values
                            .OrderByDescending(x => GetScore(x, FateType.Dragon))
                            .Take(50)
                            .Select(p => new QueryStruct
                            {
                                Identity = p.Identity,
                                Name = p.Name
                            }).ToList();
                        type = FateType.Dragon;
                        break;
                    }

                case RankType.ChiPhoenix:
                    {
                        rankData = mFatePlayers.Values
                            .OrderByDescending(x => GetScore(x, FateType.Phoenix))
                            .Take(50)
                            .Select(p => new QueryStruct
                            {
                                Identity = p.Identity,
                                Name = p.Name,
                            }).ToList();
                        type = FateType.Phoenix;
                        break;
                    }

                case RankType.ChiTiger:
                    {
                        rankData = mFatePlayers.Values
                            .OrderByDescending(x => GetScore(x, FateType.Tiger))
                            .Take(50)
                            .Select(p => new QueryStruct
                            {
                                Identity = p.Identity,
                                Name = p.Name,
                            }).ToList();
                        type = FateType.Tiger;
                        break;
                    }

                case RankType.ChiTurtle:
                    {
                        rankData = mFatePlayers.Values
                            .OrderByDescending(x => GetScore(x, FateType.Turtle))
                            .Take(50)
                            .Select(p => new QueryStruct
                            {
                                Identity = p.Identity,
                                Name = p.Name,
                            }).ToList();
                        type = FateType.Turtle;
                        break;
                    }
                default:
                    return;
            }

            int idx = msg.PageNumber * 10;
            if (idx >= rankData.Count)
            {
                return;
            }

            msg.Data1 = (ushort)rankData.Count;

            int count = 0;
            for (; idx < rankData.Count && count < 10; idx++, count++)
            {
                msg.Infos.Add(new QueryStruct
                {
                    Type = (ulong)(idx + 1),
                    Identity = rankData[idx].Identity,
                    Amount = (ulong)GetScore(rankData[idx].Identity, type),
                    Name = rankData[idx].Name
                });
            }
            await sender.SendAsync(msg);
        }

        public static int GetPlayerRank(uint idUser, RankType mode)
        {
            List<QueryStruct> rankData = new();
            switch (mode)
            {
                case RankType.ChiDragon:
                    {
                        rankData = mFatePlayers.Values
                            .OrderByDescending(x => GetScore(x, FateType.Dragon))
                            .Take(50)
                            .Select(p => new QueryStruct
                            {
                                Identity = p.Identity,
                                Name = p.Name
                            }).ToList();
                        break;
                    }

                case RankType.ChiPhoenix:
                    {
                        rankData = mFatePlayers.Values
                            .OrderByDescending(x => GetScore(x, FateType.Phoenix))
                            .Take(50)
                            .Select(p => new QueryStruct
                            {
                                Identity = p.Identity,
                                Name = p.Name,
                            }).ToList();
                        break;
                    }

                case RankType.ChiTiger:
                    {
                        rankData = mFatePlayers.Values
                            .OrderByDescending(x => GetScore(x, FateType.Tiger))
                            .Take(50)
                            .Select(p => new QueryStruct
                            {
                                Identity = p.Identity,
                                Name = p.Name,
                            }).ToList();
                        break;
                    }

                case RankType.ChiTurtle:
                    {
                        rankData = mFatePlayers.Values
                            .OrderByDescending(x => GetScore(x, FateType.Turtle))
                            .Take(50)
                            .Select(p => new QueryStruct
                            {
                                Identity = p.Identity,
                                Name = p.Name,
                            }).ToList();
                        break;
                    }
                default:
                    {
                        return -1;
                    }
            }
            return rankData.FindIndex(x => x.Identity == idUser);
        }

        public static DbFateRule GetRule(FateType type, TrainingAttrType attrType)
        {
            return mFateRules.FirstOrDefault(x => x.FateNo == (int)type && x.AttrType == (int)attrType);
        }

        public static DbConfig GetInitializationRequirements(FateType type)
        {
            return mFateOpenRule.FirstOrDefault(x => x.Data1 == (int)type);
        }
    }
}
