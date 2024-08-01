using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class JiangHuQualityRandRepository
    {
        public static async Task<List<DbJiangHuQualityRand>> GetAsync()
        {
            await using var serverDbContext = new ServerDbContext();
            return await serverDbContext.JiangHuQualityRands.ToListAsync();
        }
    }
}
