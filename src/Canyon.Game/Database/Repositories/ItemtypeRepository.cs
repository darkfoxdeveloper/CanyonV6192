using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class ItemtypeRepository
    {
        public static async Task<List<DbItemtype>> GetAsync()
        {
            await using var db = new ServerDbContext();
            return await db.Itemtypes.ToListAsync();
        }
    }
}
