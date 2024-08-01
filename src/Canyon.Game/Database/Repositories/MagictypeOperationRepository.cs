using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class MagictypeOperationRepository
    {
        public static async Task<List<DbMagictypeOp>> GetAsync()
        {
            await using var db = new ServerDbContext();
            return await db.MagictypeOperations.ToListAsync();
        }
    }
}
