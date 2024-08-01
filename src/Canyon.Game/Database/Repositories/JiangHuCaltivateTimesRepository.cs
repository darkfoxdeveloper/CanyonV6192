using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class JiangHuCaltivateTimesRepository
    {
        public static async Task<DbJiangHuCaltivateTimes> GetAsync(uint idUser)
        {
            await using var serverDbContext = new ServerDbContext();
            return await serverDbContext.JiangHuCaltivateTimes.FirstOrDefaultAsync(x => x.PlayerId == idUser);
        }
    }
}
