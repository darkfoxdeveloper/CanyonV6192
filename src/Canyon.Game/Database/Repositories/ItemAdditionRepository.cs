using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class ItemAdditionRepository
    {
        public static async Task<List<DbItemAddition>> GetAsync()
        {
            await using var db = new ServerDbContext();
            return await db.ItemAdditions.ToListAsync();
        }
    }
}
