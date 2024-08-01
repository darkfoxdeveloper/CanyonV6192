using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class ArenicRepository
    {
        public static async Task<List<DbArenic>> GetAsync(DateTime date, int type)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.Arenics
                            .Where(x => x.Date == uint.Parse(date.Date.ToString("yyyyMMdd")) && x.Type == type)
                            .ToListAsync();
        }
    }
}
