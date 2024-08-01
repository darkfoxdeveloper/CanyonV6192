using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class FamilyAttrRepository
    {
        public static async Task<List<DbFamilyAttr>> GetAsync(uint idFamily)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.FamilyAttrs.Where(x => x.FamilyIdentity == idFamily).ToListAsync();
        }
    }
}
