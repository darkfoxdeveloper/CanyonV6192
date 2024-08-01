using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class JiangHuCaltivateConditionRepository
    {
        public static async Task<List<DbJiangHuCaltivateCondition>> GetAsync()
        {
            await using var serverDbContext = new ServerDbContext();
            return await serverDbContext.JiangHuCaltivateConditions.ToListAsync();
        }
    }
}
