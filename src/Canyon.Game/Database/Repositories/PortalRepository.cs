using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class PortalRepository
    {
        public static async Task<DbPortal> GetAsync(uint idMap, uint idx)
        {
            await using var context = new ServerDbContext();
            return await context.Portals.FirstOrDefaultAsync(x => x.MapId == idMap && x.PortalIndex == idx);
        }
    }
}
