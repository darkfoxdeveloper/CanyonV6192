using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class FateRandRepository
    {
        public static async Task<IList<DbFateRand>> GetAsync()
        {
            await using var ctx = new ServerDbContext();
            return await ctx.FateRands.ToListAsync();
        }
    }
}
