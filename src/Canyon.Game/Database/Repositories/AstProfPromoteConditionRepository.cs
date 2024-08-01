using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class AstProfPromoteConditionRepository
    {
        public static async Task<List<DbAstProfPromoteCondition>> GetAsync()
        {
            await using var ctx = new ServerDbContext();
            return await ctx.AstProfPromoteConditions.ToListAsync();
        }
    }
}
