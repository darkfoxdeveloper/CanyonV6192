using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class JiangHuPlayerPowerRepository
    {
        public static async Task<Dictionary<byte, DbJiangHuPlayerPower>> GetAsync(uint idUser)
        {
            await using var serverDbContext = new ServerDbContext();
            return await serverDbContext.JiangHuPlayerPowers.Where(x => x.PlayerId == idUser).ToDictionaryAsync(x => x.Level);
        }
    }
}
