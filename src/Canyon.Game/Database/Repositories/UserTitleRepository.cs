using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class UserTitleRepository
    {
        public static async Task<List<DbUserTitle>> GetAsync(uint idPlayer)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.UserTitles
                            .Where(x => x.PlayerId == idPlayer && x.DelTime > DateTime.Now)
                            .ToListAsync();
        }
    }
}
