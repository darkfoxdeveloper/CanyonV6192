using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class TutorAccessRepository
    {
        public static async Task<DbTutorAccess> GetAsync(uint idGuide)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.TutorAccess.FirstOrDefaultAsync(x => x.GuideIdentity == idGuide);
        }
    }
}
