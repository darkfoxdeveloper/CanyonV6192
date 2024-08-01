using Canyon.Database.Entities;
using Canyon.Game.Database.Repositories;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States.User;
using Org.BouncyCastle.Utilities;
using System;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Canyon.Game.Services.Managers
{
    public class AstProfManager
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<AstProfManager>();
        private static readonly List<DbAstProfInaugurationCondition> astProfInit = new();
        private static readonly List<DbAstProfPromoteCondition> astProfPromoteCondition = new();

        private static readonly int[,] AstProfPower =
        {
            {0, 100, 200, 300, 400, 600, 800, 1000, 1200, 1500, 0},
            {0, 100, 200, 300, 400, 600, 800, 1000, 1200, 1500, 0},
            {0, 100, 200, 300, 400, 600, 800, 1000, 1200, 1500, 0},
            {0, 100, 200, 300, 400, 600, 800, 1000, 1200, 1500, 0},
            {0, 008, 016, 024, 032, 040, 048, 0056, 0064, 0072, 0},
            {0, 100, 200, 300, 400, 500, 600, 0700, 0800, 1000, 0},
            {0, 000, 000, 000, 000, 000, 000, 0000, 0000, 0000, 0}, // 7
            {0, 000, 000, 000, 000, 000, 000, 0000, 0000, 0000, 0}, // 8
            {0, 100, 200, 300, 400, 500, 600, 0800, 1000, 1200, 0}
        };

        public static async Task InitializeAsync()
        {
            logger.LogInformation("Sub class manager is now initializating");

            foreach (var inaug in await AstProfInaugurationConditionRepository.GetAsync())
            {
                astProfInit.Add(inaug);
            }

            foreach (var promote in await AstProfPromoteConditionRepository.GetAsync())
            {
                astProfPromoteCondition.Add(promote);
            }
        }

        public static async Task<bool> CanInaugurateAsync(Character user, AstProfType type)
        {
            var astProfInit = AstProfManager.astProfInit.FirstOrDefault(x => x.AstProfType == (byte)type);
            if (astProfInit == null)
            {
                return false;
            }

            if (astProfInit.Metempsychosis > user.Metempsychosis)
            {
                return false;
            }

            if (astProfInit.UserLevel > user.Level && astProfInit.Metempsychosis > user.Metempsychosis)
            {
                return false;
            }

            int count1 = (int)Math.Max(1, astProfInit.ItemType1Amount);
            if (astProfInit.ItemType1 != 0)
            {
                if (!user.UserPackage.MultiCheckItem(astProfInit.ItemType1, astProfInit.ItemType1, count1))
                {
                    return false;
                }
            }

            int count2 = (int)Math.Max(1, astProfInit.ItemType2Amount);
            if (astProfInit.ItemType2 != 0)
            {

                if (!user.UserPackage.MultiCheckItem(astProfInit.ItemType2, astProfInit.ItemType2, count2))
                {
                    return false;
                }
            }

            if (astProfInit.ItemType1 != 0)
            {
                await user.UserPackage.MultiSpendItemAsync(astProfInit.ItemType1, astProfInit.ItemType1, count1);
            }

            if (astProfInit.ItemType1 != 0)
            {
                await user.UserPackage.MultiSpendItemAsync(astProfInit.ItemType2, astProfInit.ItemType2, count2);
            }

            return true;
        }

        public static async Task<bool> CanUpLevAsync(Character user, AstProfType type, int currentLevel)
        {
            if (currentLevel >= 9)
            {
                return false;
            }

            var exp = ExperienceManager.GetAstProfExperience(type, currentLevel);
            if (exp == null)
            {
                return false;
            }

            if (!await user.SpendCultivationAsync((int)exp.Exp))
            {
                return false;
            }

            return true;
        }

        public static bool CanPromote(Character user, AstProfType type, byte level)
        {
            int rank = GetRank(user, type) + 1;
            if (rank > 9)
            {
                return false;
            }

            var astProfPromote = astProfPromoteCondition.FirstOrDefault(x => x.AstProfType == (byte)type && x.AstProfRank == rank);
            if (astProfPromote == null || astProfPromote.AstProfLevel > level)
            {
                return false;
            }

            if (astProfPromote.Metempsychosis > user.Metempsychosis)
            {
                return false;
            }

            if (astProfPromote.UserLevel > user.Level && astProfPromote.Metempsychosis > user.Metempsychosis)
            {
                return false;
            }

            return true;
        }

        public static byte GetRank(Character user, AstProfType type)
        {
            //byte[] ranks = BitConverter.GetBytes(user.AstProfRanks);
            //int idx = Math.Max(1, Math.Min(7, (int)type));
            //return ranks[idx];
            int idx = Math.Max(1, Math.Min(7, (int)type));
            return (byte)((user.AstProfRanks >> (8 * idx)) & 0xff);
        }

        public static void SetRank(Character user, AstProfType type, byte value)
        {
            //byte[] ranks = BitConverter.GetBytes(user.AstProfRanks);
            //int idx = Math.Max(1, Math.Min(7, (int)type));
            //ranks[idx] = value;
            //user.AstProfRanks = BitConverter.ToUInt64(ranks);
            int idx = Math.Max(1, Math.Min(7, (int)type));
            ulong tempRanks = user.AstProfRanks;
            tempRanks &= ~((ulong)0xff << (8 * idx)); // Clear the current byte
            tempRanks |= ((ulong)value << (8 * idx)); // Set the new byte
            user.AstProfRanks = tempRanks;
        }

        public static int GetPower(AstProfType type, int level)
        {
            switch (type)
            {
                case AstProfType.ChiMaster:
                case AstProfType.Sage:
                case AstProfType.Apothecary:
                case AstProfType.Performer:
                case AstProfType.Wrangler:
                case AstProfType.MartialArtist:
                case AstProfType.Warlock:
                    return AstProfPower[(int)type - 1, level];
                default: return 0;
            }
        }
    }
}
