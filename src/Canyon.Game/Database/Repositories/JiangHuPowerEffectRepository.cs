using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class JiangHuPowerEffectRepository
    {
        public static async Task<List<DbJiangHuPowerEffect>> GetAsync()
        {
            await using var serverDbContext = new ServerDbContext();
            return await serverDbContext.JiangHuPowerEffects.ToListAsync();
        }
    }
}
