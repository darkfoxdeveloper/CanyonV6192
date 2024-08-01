using Canyon.Database.Entities;
using Canyon.Game.Database.Repositories;
using System.Collections.Concurrent;

namespace Canyon.Game.Services.Managers
{
    public class JiangHuManager
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<JiangHuManager>();
        private static readonly Dictionary<byte, List<DbJiangHuAttribRand>> AttributeRates = new();
        private static readonly Dictionary<byte, List<DbJiangHuQualityRand>> QualityRates = new();
        private static readonly Dictionary<byte, DbJiangHuCaltivateCondition> CaltivateConditions = new();
        private static readonly Dictionary<JiangHuAttrType, List<DbJiangHuPowerEffect>> PowerEffects = new();

        private static readonly ConcurrentDictionary<uint, int> remainingJiangHuTime = new();

        public static async Task InitializeAsync()
        {
            logger.LogInformation("Initializing Jiang Hu data");

            var attributes = await JiangHuAttribRandRepository.GetAsync();
            foreach (var attribute in attributes)
            {
                if (!AttributeRates.TryGetValue(attribute.PowerLevel, out var rates))
                {
                    rates = new List<DbJiangHuAttribRand>();
                    AttributeRates.Add(attribute.PowerLevel, rates);
                }

                rates.Add(attribute);
            }

            var qualities = await JiangHuQualityRandRepository.GetAsync();
            foreach (var quality in qualities)
            {
                if (!QualityRates.TryGetValue(quality.PowerLevel, out var rates))
                {
                    rates = new List<DbJiangHuQualityRand>();
                    QualityRates.Add(quality.PowerLevel, rates);
                }

                rates.Add(quality);
            }

            var conditions = await JiangHuCaltivateConditionRepository.GetAsync();
            foreach (var condition in conditions)
            {
                CaltivateConditions.TryAdd(condition.PowerLevel, condition);
            }

            var powerEffects = await JiangHuPowerEffectRepository.GetAsync();
            foreach (var effect in powerEffects)
            {
                if (!PowerEffects.TryGetValue((JiangHuAttrType)effect.Type, out var effects))
                {
                    effects = new List<DbJiangHuPowerEffect>();
                    PowerEffects.Add((JiangHuAttrType)effect.Type, effects);
                }

                effects.Add(effect);
            }
        }

        public static DbJiangHuPowerEffect GetPowerEffect(JiangHuAttrType type, JiangHuQuality quality)
        {
            if (!PowerEffects.TryGetValue(type, out var value))
            {
                return null;
            }

            return value.FirstOrDefault(x => x.Quality == (byte)quality);
        }

        public static List<DbJiangHuAttribRand> GetAttributeRates(byte powerLevel)
        {
            return AttributeRates.TryGetValue(powerLevel, out var rates) ? new List<DbJiangHuAttribRand>(rates) : null;
        }

        public static List<DbJiangHuQualityRand> GetQualityRates(byte powerLevel)
        {
            return QualityRates.TryGetValue(powerLevel, out var rates) ? new List<DbJiangHuQualityRand>(rates) : null;
        }

        public static DbJiangHuCaltivateCondition GetCaltivateCondition(byte powerLevel)
        {
            return CaltivateConditions.TryGetValue(powerLevel, out var value) ? value : null;
        }

        public static void StoreJiangHuRemainingTime(uint idUser, int seconds)
        {
            remainingJiangHuTime.TryAdd(idUser, seconds);
        }

        public static int GetJiangHuRemainingTime(uint idUser)
        {
            return remainingJiangHuTime.TryRemove(idUser, out var value) ? value : 0;
        }

        public const byte MAX_TALENT = 4;
        public const int MAX_FREE_COURSE = 100;
        public const double POINTS_TO_COURSE = 10000d;
        public const int EXIT_KONG_FU_SECONDS = 600;
        public const int MAX_FREE_COURSES_DAILY = 10;

        public static readonly double[] SequenceBonus =
        {
            1.0d,
            1.0d,
            1.1d,
            1.13d,
            1.15d,
            1.18d,
            1.21d,
            1.25d,
            1.3d,
            1.5d
        };

        public static readonly double[] SequenceInnerStrength =
        {
            1.0d,
            1.0d,
            1.1d,
            1.2d,
            1.3d,
            1.4d,
            1.5d,
            1.6d,
            1.8d,
            2.0d,
        };

        public static readonly uint[] PowerValue =
        {
            100, 120, 150, 200, 300, 500
        };

        public static readonly uint[] OutTwinCityAddPoints =
        {
            10, 20, 26, 31, 52
        };

        public static readonly uint[] TwinCityAddPoints =
        {
            312, 625, 781, 937, 1562
        };

        public enum JiangHuQuality : byte
        {
            None,
            Common,
            Sharp,
            Pure,
            Rare,
            Ultra,
            Epic
        }

        public enum JiangHuAttrType : byte
        {
            None,
            MaxLife,
            Attack,
            MagicAttack,
            Defense,
            MagicDefense,
            FinalDamage,
            FinalMagicDamage,
            FinalDefense,
            FinalMagicDefense,
            CriticalStrike,
            SkillCriticalStrike,
            Immunity,
            Breakthrough,
            Counteraction,
            MaxMana
        }
    }
}
