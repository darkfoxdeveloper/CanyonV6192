using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class MagicTypeRepository
    {
        public static async Task<List<DbMagictype>> GetAsync()
        {
            await using var db = new ServerDbContext();
            return await db.Magictype.ToListAsync();
        }
    }
}
