using Canyon.Database.Entities;
using Canyon.Game.Database;
using static Canyon.Game.Services.Managers.ActivityManager;

namespace Canyon.Game.States
{
    public sealed class ActivityTask
    {
        private readonly DbActivityUserTask userTask;

        public ActivityTask(DbActivityUserTask userTask)
        {
            this.userTask = userTask;
            Type = GetTaskTypeById(userTask.ActivityId);
        }

        public ActivityType Type { get; init; }
        
        public uint ActivityId => userTask.ActivityId;
        
        public byte CompleteFlag
        {
            get => userTask.CompleteFlag;
            set => userTask.CompleteFlag = value;
        }

        public byte Schedule
        {
            get => userTask.Schedule;
            set => userTask.Schedule = value;
        }

        public Task SaveAsync()
        {
            return ServerDbContext.SaveAsync(userTask);
        }

        public Task DeleteAsync()
        {
            return ServerDbContext.DeleteAsync(userTask);
        }
    }
}
