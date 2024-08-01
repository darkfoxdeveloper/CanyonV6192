using Canyon.Database.Entities;
using Canyon.Game.Database.Repositories;
using Canyon.Game.States.NeiGong;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;

namespace Canyon.Game.Services.Managers
{
    public class InnerStrengthManager
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<InnerStrengthManager>();
        private const byte FINAL_RAND_INFO = 10;

        private static readonly ConcurrentDictionary<ushort, Dictionary<byte, DbInnerStrenghtTypeLevInfo>> typeLevInfo = new();
        private static readonly ConcurrentDictionary<byte, InnerStrengthTypeInfo> typeInfo = new();
        private static readonly ConcurrentDictionary<byte, DbInnerStrenghtSecretType> secretTypeInfo = new();
        private static readonly ConcurrentDictionary<byte, List<DbInnerStrengthRand>> rands = new();
        private static NeiGongInfo neiGongInfo;

        public static async Task InitializeAsync()
        {
            logger.LogInformation("Initializing Inner Strength Manager");

            neiGongInfo = new NeiGongInfo();

            foreach (var info in await InnerStrenghtRepository.GetTypeLevAsync())
            {
                if (typeLevInfo.ContainsKey(info.Type))
                {
                    typeLevInfo[info.Type].Add(info.Level, info);
                }
                else
                {
                    typeLevInfo.TryAdd(info.Type, new Dictionary<byte, DbInnerStrenghtTypeLevInfo>());
                    typeLevInfo[info.Type].Add(info.Level, info);
                }
            }

            foreach (var info in await InnerStrenghtRepository.GetTypesAsync())
            {
                typeInfo.TryAdd((byte)info.Identity, new InnerStrengthTypeInfo(info));
            }

            foreach (var info in await InnerStrenghtRepository.GetSecretTypeAsync())
            {
                secretTypeInfo.TryAdd((byte)info.Identity, info);
            }

            foreach (var range in await InnerStrenghtRepository.GetRandRangeAsync())
            {
                if (rands.ContainsKey(range.StrengthNo))
                {
                    rands[range.StrengthNo].Add(range);
                }
                else
                {
                    rands.TryAdd(range.StrengthNo, new ());
                    rands[range.StrengthNo].Add(range);
                }
            }
        }

        public static DbInnerStrenghtTypeLevInfo QueryTypeLevInfo(byte type, byte level)
        {
            if (typeLevInfo.TryGetValue(type, out var infos))
            {
                if (infos.TryGetValue(level, out var info))
                {
                    return info;
                }
            }
            return null;
        }

        public static DbInnerStrenghtSecretType QuerySecretType(byte type)
        {
            if (secretTypeInfo.TryGetValue(type, out var info))
            {
                return info;
            }
            return null;
        }

        public static InnerStrengthTypeInfo QueryTypeInfo(byte type)
        {
            if (typeInfo.TryGetValue(type, out var info))
            {
                return info;
            }
            return null;
        }

        public static List<DbInnerStrenghtTypeLevInfo> QueryTypeLevelInfosForAttributes(byte type, byte level)
        {
            if (typeLevInfo.TryGetValue(type, out var infos))
            {
                return infos.Values.Where(x => x.Level < level).ToList();
            }
            return new List<DbInnerStrenghtTypeLevInfo>();
        }

        public static int GetStrenghtMaxLevel(byte type)
        {
            if (typeLevInfo.TryGetValue(type, out var typeInfo))
            {
                return typeInfo.Values.Max(x => x.Level);
            }
            return 0;
        }

        public static async Task<int> CalculateCurrentValueAsync(int type, int currentLevel, int currentAbolish)
        {
            var typeInfo = QueryTypeInfo((byte)type);
            var maxLevel = GetStrenghtMaxLevel((byte)type);
            var maxAllowedValue = GetMaxValueAllowed(currentAbolish, typeInfo.AbolishCount);
            if (currentLevel == maxLevel)
            {
                return maxAllowedValue;
            }

            // to simplify the code, i'll just do this calc
            int deltaLevelValue = (int)Math.Ceiling(maxAllowedValue / (float)maxLevel);
            int newValue = await NextAsync(deltaLevelValue * (currentLevel - 1), deltaLevelValue * currentLevel);
            return Math.Min(maxAllowedValue, newValue);
        }

        public static int CalculateMaxValue(int type, int currentValue, int currentLevel, int currentAbolish)
        {
            var typeInfo = QueryTypeInfo((byte)type);
            if (currentValue < 100)
            {
                return 0;
            }
            if (currentLevel < GetStrenghtMaxLevel((byte)type))
            {
                return 0;
            }
            if (currentAbolish < typeInfo.AbolishCount)
            {
                return 0;
            }
            return 100;
        }

        public static int GetMaxValueAllowed(int currentAbolish, int maxAbolish)
        {
            if (currentAbolish == maxAbolish)
            {
                return 100;
            }
            if ((currentAbolish == 0 && maxAbolish == 1) || currentAbolish == 1)
            {
                return 55;
            }
            return 33;
        }

        public class InnerStrengthTypeInfo
        {
            private readonly DbInnerStrenghtTypeInfo innerStrenghtTypeInfo;
            private NeiGongInfo.StrengthType strengthType;

            public InnerStrengthTypeInfo(DbInnerStrenghtTypeInfo innerStrenghtTypeInfo)
            {
                this.innerStrenghtTypeInfo = innerStrenghtTypeInfo;
                strengthType = neiGongInfo.GetStrengthType((byte)innerStrenghtTypeInfo.Identity);
            }

            public byte SecretType => innerStrenghtTypeInfo.SecretType;

            public byte Rand1 => (byte)innerStrenghtTypeInfo.RandType1;
            public byte Rand2 => (byte)innerStrenghtTypeInfo.RandType2;
            public byte Rand3 => (byte)innerStrenghtTypeInfo.RandType3;

            public int AbolishCulture => (int)innerStrenghtTypeInfo.AbolishCulture;

            public int AbolishCount
            {
                get
                {
                    if (Rand1 == FINAL_RAND_INFO)
                    {
                        return 0;
                    }
                    if (Rand2 == FINAL_RAND_INFO)
                    {
                        return 1;
                    }
                    return 2;
                }
            }

            public int RequiredLevel => strengthType?.RequiredLevel ?? 0;
            public int RequiredNeiGongValue => strengthType?.RequiredNeiGongValue ?? 0;
            public int RequiredPreNeiGong => strengthType?.RequiredPreNeiGong ?? 0;
            public uint RequiredItemType => strengthType?.RequiredItemType ?? 0;


            public int MaxLife => (int)innerStrenghtTypeInfo.MaxLife;
            public int PhysicAttackNew => (int)innerStrenghtTypeInfo.PhysicAttackNew;
            public int MagicAttack => (int)innerStrenghtTypeInfo.MagicAttack;
            public int PhysicDefenseNew => (int)innerStrenghtTypeInfo.PhysicDefenseNew;
            public int MagicDefense => (int)innerStrenghtTypeInfo.MagicDefense;
            public int FinalPhysicAdd => innerStrenghtTypeInfo.FinalPhysicAdd;
            public int FinalMagicAdd => innerStrenghtTypeInfo.FinalMagicAdd;
            public int FinalPhysicReduce => innerStrenghtTypeInfo.FinalPhysicReduce;
            public int FinalMagicReduce => innerStrenghtTypeInfo.FinalMagicReduce;
            public int PhysicCrit => innerStrenghtTypeInfo.PhysicCrit;
            public int MagicCrit => innerStrenghtTypeInfo.MagicCrit;
            public int DefenseCrit => innerStrenghtTypeInfo.DefenseCrit;
            public int SmashRate => innerStrenghtTypeInfo.SmashRate;
            public int FirmDefenseRate => innerStrenghtTypeInfo.FirmDefenseRate;

        }
    }
}
