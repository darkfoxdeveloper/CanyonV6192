using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class FamilyBattleEffectShareLimitRepository
    {
        public static async Task<List<DbFamilyBattleEffectShareLimit>> GetAsync()
        {
            await using var ctx = new ServerDbContext();
            return await ctx.FamilyBattleEffectShareLimits.ToListAsync();
        }
    }
}
