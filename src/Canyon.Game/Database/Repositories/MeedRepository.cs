using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class MeedRepository
    {
        public static async Task<List<DbSetMeed>> GetSyndicateMeedAsync(byte type)
        {
            await using ServerDbContext serverDbContext = new();
            return await serverDbContext.SetMeeds.Where(x => x.Type == type).ToListAsync();
        }

        public static async Task<List<DbMeedRecord>> GetUserMeedAsync(byte type)
        {
            await using ServerDbContext serverDbContext = new();
            return await serverDbContext.MeedRecords.Where(x => x.Type == type).ToListAsync();
        }
    }
}
