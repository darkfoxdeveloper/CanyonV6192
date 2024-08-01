using Canyon.Database.Entities;

namespace Canyon.Game.Database.Repositories
{
    public static class MonsterypeRepository
    {
        public static async Task<List<DbMonstertype>> GetAsync()
        {
            await using var context = new ServerDbContext();
            return context.Monstertype.ToList();
        }
    }
}
