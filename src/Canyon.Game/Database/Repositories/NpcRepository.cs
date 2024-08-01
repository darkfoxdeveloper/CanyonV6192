using Canyon.Database.Entities;

namespace Canyon.Game.Database.Repositories
{
    public static class NpcRepository
    {
        public static async Task<List<DbNpc>> GetAsync()
        {
            await using var context = new ServerDbContext();
            return context.Npcs.ToList();
        }
    }
}
