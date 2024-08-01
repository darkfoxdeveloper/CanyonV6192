using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class FamilyRepository
    {
        public static async Task<List<DbFamily>> GetAsync()
        {
            await using var ctx = new ServerDbContext();
            return await ctx.Families.ToListAsync();
        }
    }
}
