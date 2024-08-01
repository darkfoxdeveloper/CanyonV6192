using Canyon.Database.Entities;

namespace Canyon.Game.Database.Repositories
{
    public static class StageGoalRepository
    {
        public static List<DbProcessGoal> GetGoals()
        {
            using var ctx = new ServerDbContext();
            return ctx.ProcessGoals.ToList();
        }

        public static List<DbProcessTask> GetTasks()
        {
            using var ctx = new ServerDbContext();
            return ctx.ProcessTasks.ToList();
        }
    }
}
