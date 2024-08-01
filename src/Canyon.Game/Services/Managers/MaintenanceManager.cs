using Canyon.Game.Sockets.Ai.Packets;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.Sockets.Piglet;
using Canyon.Game.Sockets.Piglet.Packets;
using Canyon.Network.Packets.Ai;
using Canyon.Network.Packets.Piglet;
using System.Drawing;

namespace Canyon.Game.Services.Managers
{
    public class MaintenanceManager
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MaintenanceManager>();

        public const int ServerDown = 0;
        public const int ServerBusy = 1;
        public const int ServerFull = 2;
        public const int ServerUp = 3;

        private static TimeOut timeOut = new TimeOut();
        private static int minutesToShutdown = 0;
        private static int currentAlerts = 0;

        public static async Task CloseServerAsync()
        {
            logger.LogWarning("Closing server by piglet request");

            await BroadcastNpcMsgAsync(new MsgAiAction
            {
                Action = AiActionType.Shutdown
            }, async () =>
            {
                Kernel.SetMaintenance();
                await RoleManager.KickOutAllAsync();
            });

            await OnCloseServerAsync();
        }

        public static async Task AnnounceMaintenanceAsync(int minutes = 5)
        {
            logger.LogWarning("Maintenance announce in the next {} minutes", minutes);

            minutesToShutdown = Math.Max(1, minutes);
            currentAlerts = 1;

            RoleManager.SetMaintenanceStart();

            await BroadcastNpcMsgAsync(new MsgAiAction
            {
                Action = AiActionType.Shutdown
            }, 
            () =>
            {
                Kernel.SetMaintenance();
                return Task.CompletedTask;
            });

            if (PigletClient.Instance?.Actor?.Socket?.Connected == true)
            {
                await PigletClient.Instance.Actor.SendAsync(new MsgPigletRealmStatus
                {
                    Data = new MsgPigletRealmStatus<PigletActor>.RealmStatusData
                    {
                        Status = ServerBusy
                    }
                });
            }

            await BroadcastWorldMsgAsync(string.Format(StrServerMaintenanceScheduleStart, minutesToShutdown), TalkChannel.Talk, Color.White);
            currentAlerts = 0;
            timeOut.Startup(30);
        }

        private static async Task OnCloseServerAsync()
        {
            if (PigletClient.Instance?.Actor?.Socket?.Connected == true)
            {
                await PigletClient.Instance.Actor.SendAsync(new MsgPigletRealmStatus
                {
                    Data = new MsgPigletRealmStatus<PigletActor>.RealmStatusData
                    {
                        Status = ServerDown
                    }
                });
            }

            await RoleManager.KickOutAllAsync(StrServerMaintenanceShutdown, true);
            await Kernel.StopAsync();
            Environment.Exit(0);
        }

        public static async Task OnTimerAsync()
        {
            if (!timeOut.IsActive() || !timeOut.ToNextTime())
            {
                return;
            }

            int remainingAlerts = (minutesToShutdown * 2) - currentAlerts;
            if (remainingAlerts > 0)
            {
                string message;
                if (remainingAlerts == 1) // last one, 30 seconds
                {
                    message = string.Format(StrServerMaintenanceSeconds, 30);
                }
                else if (remainingAlerts == 2) // 1 minute
                {
                    message = string.Format(StrServerMaintenanceMinute, 1);
                }
                else if (remainingAlerts == 3) // 1 minute and 30 seconds
                {
                    message = string.Format(StrServerMaintenanceMinuteSeconds, 1, 30);
                }
                else if (currentAlerts % 2 == 0) // N minutes
                {
                    message = string.Format(StrServerMaintenanceMinutes, (remainingAlerts / 2));
                }
                else // N minutes and 30 seconds
                {
                    message = string.Format(StrServerMaintenanceMinutesSeconds, (remainingAlerts / 2), 30);
                }

                await BroadcastWorldMsgAsync(string.Format(StrServerMaintenanceWarning, message), TalkChannel.Center, Color.White);
                currentAlerts++;
                return;
            }

            await OnCloseServerAsync();
        }
    }
}
