using Canyon.Database.Entities;

namespace Canyon.Game.Database.Repositories
{
    public static class DynamicNpcRespository
    {
        public static async Task<List<DbDynanpc>> GetAsync()
        {
            await using var context = new ServerDbContext();
            return context.DynamicNpcs.ToList();
        }
    }
}
