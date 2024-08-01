using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class FateRankRepository
    {
        public static async Task<IList<DbFateRank>> GetAsync()
        {
            await using var ctx = new ServerDbContext();
            return await ctx.FateRanks.ToListAsync();
        }
    }
}
