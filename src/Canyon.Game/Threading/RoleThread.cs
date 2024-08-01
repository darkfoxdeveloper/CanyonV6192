using Canyon.Game.Services.Managers;
using Canyon.Shared.Threads;

namespace Canyon.Game.Threading
{
    public class RoleThread : ThreadBase
    {
        public RoleThread()
            : base("Role thread", 250)
        {
        }

        protected override async Task OnProcessAsync()
        {
            await RoleManager.OnRoleTimerAsync();
        }
    }
}
