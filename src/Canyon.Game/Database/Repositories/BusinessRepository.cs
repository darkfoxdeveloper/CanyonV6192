using Canyon.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Canyon.Game.Database.Repositories
{
    public static class BusinessRepository
    {
        public static async Task<List<DbBusiness>> GetAsync(uint sender)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.Business.Where(x => x.UserId == sender || x.BusinessId == sender)
                            .Include(x => x.User)
                            .Include(x => x.Business)
                            .ToListAsync();
        }
    }
}
