using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class JiangHuAttribRandRepository
    {
        public static async Task<List<DbJiangHuAttribRand>> GetAsync()
        {
            await using var serverDbContext = new ServerDbContext();
            return await serverDbContext.JiangHuAttribRands.ToListAsync();
        }
    }
}
