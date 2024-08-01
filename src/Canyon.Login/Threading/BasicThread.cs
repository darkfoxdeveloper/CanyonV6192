using Canyon.Login.Managers;
using Quartz;

namespace Canyon.Login.Threading
{
    [DisallowConcurrentExecution]
    public sealed class BasicThread : IJob
    {
        private const string CONSOLE_TITLE = "Conquer Online Login Server - Servers[{0}] - {1}";

        public BasicThread()
        {
        }

        public Task Execute(IJobExecutionContext context)
        {
            Console.Title = string.Format(CONSOLE_TITLE, RealmManager.Count, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"));



            return Task.CompletedTask;
        }

    }
}
