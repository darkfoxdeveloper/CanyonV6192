using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class NewbieInfoRepository
    {
        public static async Task<DbNewbieInfo> GetAsync(uint prof)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.NewbieInfo.FirstOrDefaultAsync(x => x.Profession == prof);
        }
    }
}
