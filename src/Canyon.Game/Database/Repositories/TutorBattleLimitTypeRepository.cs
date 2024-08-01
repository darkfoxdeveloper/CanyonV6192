using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class TutorBattleLimitTypeRepository
    {
        public static async Task<List<DbTutorBattleLimitType>> GetAsync()
        {
            await using var ctx = new ServerDbContext();
            return await ctx.TutorBattleLimitTypes.ToListAsync();
        }
    }
}
