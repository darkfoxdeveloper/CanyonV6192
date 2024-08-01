using Canyon.Database.Entities;
using Canyon.Game.Database.Repositories;

namespace Canyon.Game.Services.Managers
{
    public class BattleSystemManager
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<BattleSystemManager>();
        private static readonly List<DbDisdain> disdains = new();

        public static async Task InitializeAsync()
        {
            logger.LogInformation("Battle system manager is initialing");
            disdains.AddRange(await DisdainRepository.GetAsync());
        }

        public static DbDisdain GetDisdain(int delta)
        {
            return disdains.Aggregate((x, y) => Math.Abs(x.DeltaLev - delta) < Math.Abs(y.DeltaLev - delta) ? x : y);
        }
    }
}
