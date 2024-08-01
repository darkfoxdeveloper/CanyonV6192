using Canyon.Database.Entities;

namespace Canyon.Game.Database.Repositories
{
    public static class TaskDetailRepository
    {
        public static async Task<List<DbTaskDetail>> GetAsync(uint idUser)
        {
            await using var db = new ServerDbContext();
            return db.TaskDetails.Where(x => x.UserIdentity == idUser).ToList();
        }
    }
}
