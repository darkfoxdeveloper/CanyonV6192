using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class AstProfInaugurationConditionRepository
    {
        public static async Task<List<DbAstProfInaugurationCondition>> GetAsync()
        {
            await using var ctx = new ServerDbContext();
            return await ctx.AstProfInaugurationConditions.ToListAsync();
        }
    }
}
