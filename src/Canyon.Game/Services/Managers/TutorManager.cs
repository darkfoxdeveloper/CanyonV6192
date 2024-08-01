using Canyon.Database.Entities;
using Canyon.Game.Database.Repositories;

namespace Canyon.Game.Services.Managers
{
    public class TutorManager
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<TutorManager>();

        private static readonly List<DbTutorType> tutorType = new();
        private static readonly List<DbTutorBattleLimitType> tutorBattleLimitType = new();

        public static async Task<bool> InitializeAsync()
        {
            logger.LogInformation("Tutor Manager is initializating");

            tutorType.AddRange(await TutorTypeRepository.GetAsync());
            tutorBattleLimitType.AddRange(await TutorBattleLimitTypeRepository.GetAsync());
            return true;
        }

        public static DbTutorBattleLimitType GetTutorBattleLimitType(int delta)
        {
            return tutorBattleLimitType.Aggregate((x, y) => Math.Abs(x.Id - delta) < Math.Abs(y.Id - delta) ? x : y);
        }

        public static DbTutorType GetTutorType(int level)
        {
            return tutorType.FirstOrDefault(x => level >= x.UserMinLevel && level <= x.UserMaxLevel);
        }
    }
}
