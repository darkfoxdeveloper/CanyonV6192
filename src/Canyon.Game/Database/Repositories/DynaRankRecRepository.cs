using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class DynaRankRecRepository
    {
        public static async Task<List<DbDynaRankRec>> GetAsync(uint rankType)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.DynaRankRecs.Where(x => x.RankType == rankType).ToListAsync();
        }
    }
}
