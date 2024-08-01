using Canyon.Database.Entities;
using Canyon.Game.Services.Managers;
using Canyon.Game.States;
using Quartz;
using System.Collections.Concurrent;

namespace Canyon.Game.Threading
{
    [DisallowConcurrentExecution]
    public sealed class AutomaticActionThread : IJob
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<AutomaticActionThread>();

        private const int _ACTION_SYSTEM_EVENT = 2030000;
        private const int _ACTION_SYSTEM_EVENT_LIMIT = 100;

        private readonly ConcurrentDictionary<uint, DbAction> actions;

        public AutomaticActionThread()
        {
            actions = new ConcurrentDictionary<uint, DbAction>(1, _ACTION_SYSTEM_EVENT_LIMIT);

            for (var a = 0; a < _ACTION_SYSTEM_EVENT_LIMIT; a++)
            {
                DbAction action = EventManager.GetAction((uint)(_ACTION_SYSTEM_EVENT + a));
                if (action != null)
                {
                    actions.TryAdd(action.Id, action);
                }
            }
        }

        public async Task Execute(IJobExecutionContext context)
        {
            foreach (DbAction action in actions.Values)
            {
                try
                {
                    await GameAction.ExecuteActionAsync(action.Id, null, null, null, "");
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, "Error on processing automatic actions!! {Message}", ex.Message);
                }
            }
        }
    }
}
