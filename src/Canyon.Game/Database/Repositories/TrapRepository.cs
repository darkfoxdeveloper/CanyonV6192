using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class TrapRepository
    {
        public static async Task<List<DbTrap>> GetAsync()
        {
            await using ServerDbContext ctx = new ServerDbContext();
            return await ctx.Traps
                .Include(x => x.Type)
                .ToListAsync();
        }
    }
}
