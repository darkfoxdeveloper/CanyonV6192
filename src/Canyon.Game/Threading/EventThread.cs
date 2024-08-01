using Canyon.Game.Services.Managers;
using Canyon.Shared.Threads;

namespace Canyon.Game.Threading
{
    public sealed class EventThread : ThreadBase
    {
        public EventThread()
            : base("EventsProcessing", 500)
        {
        }

        protected override Task OnProcessAsync()
        {
            return EventManager.OnTimerAsync();
        }
    }
}
