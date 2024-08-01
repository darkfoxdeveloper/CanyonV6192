using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class PasswayRepository
    {
        public static async Task<List<DbPassway>> GetAsync(uint idMap)
        {
            await using var context = new ServerDbContext();
            return await context.Passways.Where(x => x.MapId == idMap).ToListAsync();
        }
    }
}
