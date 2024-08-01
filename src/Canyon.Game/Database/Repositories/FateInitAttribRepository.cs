using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class FateInitAttribRepository
    {
        public static async Task<IList<DbInitFateAttrib>> GetAsync()
        {
            await using var ctx = new ServerDbContext();
            return await ctx.InitFateAttribs.ToListAsync();
        }
    }
}
