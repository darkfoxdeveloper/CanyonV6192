using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class CompeteRankRepository
    {
        public static async Task<List<DbSynCompeteRank>> GetSynCompeteRankAsync()
        {
            await using ServerDbContext serverDbContext = new();
            return await serverDbContext.SynCompeteRanks.OrderBy(x => x.Rank).ToListAsync();
        }
    }
}
