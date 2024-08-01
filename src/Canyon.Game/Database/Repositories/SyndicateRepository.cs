using Canyon.Database.Entities;

namespace Canyon.Game.Database.Repositories
{
    public static class SyndicateRepository
    {
        public static async Task<List<DbSyndicate>> GetAsync()
        {
            await using var db = new ServerDbContext();
            return db.Syndicates.ToList();
        }

        public static async Task<List<DbSynAdvertisingInfo>> GetAdvertiseAsync()
        {
            await using var db = new ServerDbContext();
            return db.SynAdvertisingInfos.Where(x => x.EndDate > UnixTimestamp.Now).ToList();
        }
    }
}
