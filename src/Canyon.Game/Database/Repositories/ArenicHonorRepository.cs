using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class ArenicHonorRepository
    {
        public static async Task<List<DbArenicHonor>> GetAsync(byte type)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.ArenicHonors.Where(x => x.Type == type).ToListAsync();
        }
    }
}
