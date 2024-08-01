using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class ItemStatusRepository
    {
        public static async Task<List<DbItemStatus>> GetAsync(uint idItem)
        {
            await using ServerDbContext serverDbContext = new();
            return await serverDbContext.ItemStatus.Where(x => x.ItemId == idItem).ToListAsync();
        }
    }
}
