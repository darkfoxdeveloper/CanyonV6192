#if DEBUG
#define DISABLE_GM_TOOLS
#endif
using Canyon.Game.Services.Managers;
using Canyon.Game.Sockets.Login;
using Canyon.Game.Sockets.Login.Packets;
using Canyon.Game.Sockets.Piglet;
using Canyon.Game.Sockets.Piglet.Packets;
using Quartz;

namespace Canyon.Game.Threading
{
    [DisallowConcurrentExecution]
    public sealed class BasicThread : IJob
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<BasicThread>();

        private const string CONSOLE_TITLE = "[{0}] Conquer Online Game Server {9} - Players[{1}] Limit[{2}] Max[{3}] - {4} - Start: {8} - {5} - RoleTimerTicks[{6}] RoleCount[{7}]";

        private static readonly TimeOut accountReconnect = new(5);
        private static readonly TimeOut accountPing = new(15);
        private static readonly TimeOut pigletPing = new(15);
        private static readonly TimeOut accountSync = new();
        private static readonly DateTime serverStartTime;

        private static long lastUpdateTick = 0;

        static BasicThread()
        {
            serverStartTime = DateTime.Now;

            accountReconnect.Update();
            accountPing.Update();
            accountSync.Startup(60);
        }

        public async Task Execute(IJobExecutionContext context)
        {
            Console.Title = string.Format(CONSOLE_TITLE,
                ServerConfiguration.Configuration.Realm.Name,
                RoleManager.OnlinePlayers,
                ServerConfiguration.Configuration.Realm.MaxOnlinePlayers,
                RoleManager.MaxOnlinePlayers,
                DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                Kernel.NetworkMonitor.UpdateStatsAsync((int)(Environment.TickCount - lastUpdateTick)),
                RoleManager.RoleTimerTicks,
                RoleManager.ProcessedRoles,
                serverStartTime.ToString("yyyy/MM/dd HH:mm:ss"),
                Program.Version);

            if (LoginClient.ConnectionStage == LoginClient.ConnectionState.Disconnected)
            {
                logger.LogInformation("Connecting to account server...");

                LoginClient.Instance = new LoginClient();
                if (!await LoginClient.Instance.ConnectToAsync(ServerConfiguration.Configuration.Login.IPAddress, ServerConfiguration.Configuration.Login.Port))
                {
                    _ = LoginClient.Instance.StopAsync();
                    LoginClient.Instance = null;
                }
            }
            else if (LoginClient.ConnectionStage == LoginClient.ConnectionState.Connected && accountPing.ToNextTime())
            {
                await LoginClient.Instance.Actor.SendAsync(new MsgAccServerPing());
            }

#if !DISABLE_GM_TOOLS
            // TODO: Re-enable GM Tools
            // if (PigletClient.ConnectionStage == PigletClient.ConnectionState.Disconnected)
            // {
            //     logger.LogInformation("Connecting to GM Server...");
            //     PigletClient.Instance = new PigletClient();

            //     if (!await PigletClient.Instance.ConnectToAsync(ServerConfiguration.Configuration.Piglet.IPAddress, ServerConfiguration.Configuration.Piglet.Port))
            //     {
            //         _ = PigletClient.Instance.StopAsync();
            //         PigletClient.Instance = null;
            //     }
            //     else
            //     {
            //         pigletPing.Update();
            //     }
            // }
            // else if (PigletClient.ConnectionStage == PigletClient.ConnectionState.Connected)
            // {
            //     if (pigletPing.ToNextTime())
            //     {
            //         await PigletClient.Instance.Actor.SendAsync(new MsgPigletPing
            //         {
            //             Data = new Network.Packets.Piglet.MsgPigletPing<PigletActor>.PingData
            //             {
            //                 TickCount = Environment.TickCount64
            //             }
            //         });
            //     }
            // }
#endif

            await MaintenanceManager.OnTimerAsync();

            lastUpdateTick = Environment.TickCount;
        }

    }
}
