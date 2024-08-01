using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class FlowerRepository
    {
        public static async Task<List<DbFlower>> GetAsync()
        {
            await using var ctx = new ServerDbContext();
            return await ctx.Flowers.ToListAsync();
        }
    }
}
