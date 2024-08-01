using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class PointAllotRepository
    {
        public static async Task<List<DbPointAllot>> GetAsync()
        {
            await using var db = new ServerDbContext();
            return await db.PointAllots.ToListAsync();
        }
    }
}
