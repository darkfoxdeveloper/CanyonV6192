using Canyon.Database.Entities;

namespace Canyon.Game.Database.Repositories
{
    public static class TaskRepository
    {
        public static async Task<List<DbTask>> GetAsync()
        {
            await using var db = new ServerDbContext();
            return db.Tasks.ToList();
        }
    }
}
