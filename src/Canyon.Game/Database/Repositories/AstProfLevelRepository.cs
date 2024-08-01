using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class AstProfLevelRepository
    {
        public static async Task<List<DbAstProfLevel>> GetAsync(uint idUser)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.AstProfLevels.Where(x => x.UserIdentity == idUser).ToListAsync();
        }
    }
}
