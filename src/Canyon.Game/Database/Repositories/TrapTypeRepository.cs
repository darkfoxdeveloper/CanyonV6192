using Canyon.Database.Entities;

namespace Canyon.Game.Database.Repositories
{
    public static class TrapTypeRepository
    {
        public static async Task<DbTrapType> GetAsync(uint id)
        {
            await using ServerDbContext ctx = new ServerDbContext();
            return await ctx.TrapsType.FindAsync(id);
        }
    }
}
