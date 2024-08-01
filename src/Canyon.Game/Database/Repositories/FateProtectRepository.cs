using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class FateProtectRepository
    {
        public static async Task<IList<DbFateProtect>> GetAsync(uint idUser)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.FateProtects.Where(x => x.PlayerId == idUser).ToListAsync();
        }
    }
}
