using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class TutorTypeRepository
    {
        public static async Task<List<DbTutorType>> GetAsync()
        {
            await using var ctx = new ServerDbContext();
            return await ctx.TutorTypes.ToListAsync();
        }
    }
}
