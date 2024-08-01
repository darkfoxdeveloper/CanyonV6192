using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class DailyResetRepository
    {
        public static async Task<DbDailyReset> GetLatestAsync()
        {
            await using var ctx = new ServerDbContext();
            return await ctx.DailyResets.OrderByDescending(x => x.Id).FirstOrDefaultAsync();
        }
    }
}
