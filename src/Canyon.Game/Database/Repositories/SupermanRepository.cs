using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class SupermanRepository
    {
        public static async Task<List<DbSuperman>> GetAsync()
        {
            await using ServerDbContext serverDbContext = new();
            return await serverDbContext.Superman.ToListAsync();
        }
    }
}
