using Canyon.Game.Services.Managers;
using Canyon.Game.States.User;
using Canyon.Shared.Threads;
using System.Diagnostics;

namespace Canyon.Game.Threading
{
    public sealed class UserThread : ThreadBase
    {
        private readonly TimeOutMS basicProcessingTimer = new();

        private double elapsedMilliseconds;
        public double ElapsedMilliseconds => elapsedMilliseconds;

        public UserThread()
            : base("Character thread", 1000 / 30)
        {
            basicProcessingTimer.Startup(300);
        }

        protected override async Task OnProcessAsync()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            Task onBattleTimerAsync(Character user) => user.OnBattleTimerAsync();
            bool nextProcessing = basicProcessingTimer.ToNextTime();
            foreach (var user in RoleManager.QueryUserSet())
            {
                if (nextProcessing)
                {
                    await user.OnTimerAsync();
                }

                user.QueueAction(() => onBattleTimerAsync(user));
            }

            Interlocked.Exchange(ref elapsedMilliseconds, stopwatch.Elapsed.TotalMilliseconds);
        }
    }
}
