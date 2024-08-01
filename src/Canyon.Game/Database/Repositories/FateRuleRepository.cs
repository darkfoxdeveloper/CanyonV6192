using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class FateRuleRepository
    {
        public static async Task<IList<DbFateRule>> GetAsync()
        {
            await using var ctx = new ServerDbContext();
            return await ctx.FateRules.ToListAsync();
        }
    }
}
