using Canyon.GM.Server.Managers;
using Canyon.GM.Server.Sockets.Panel;
using Canyon.GM.Server.Sockets.Panel.Packets;
using Canyon.Network.Packets.Piglet;
using Canyon.Shared;
using Microsoft.Extensions.Logging;
using Quartz;


namespace Canyon.GM.Server.Threads
{
    [DisallowConcurrentExecution]
    public sealed class BasicThread : IJob
    {
        private readonly ILogger<BasicThread> logger;
        private static readonly TimeOut serverStatusTimeout = new TimeOut();

        public BasicThread(ILogger<BasicThread> logger)
        {
            this.logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            Console.Title = $"[{Program.ServerConfiguration.RealmName}] GM Tools - {DateTime.Now} - Version {Program.Version}";

            if (PanelClient.ConnectionStage == PanelClient.ConnectionState.Disconnected && PanelClient.Instance == null)
            {
                logger.LogInformation("Connecting to GM Server...");
                var instance = new PanelClient();
                if (!await instance.ConnectToAsync(Program.ServerConfiguration.Socket.Address, Program.ServerConfiguration.Socket.Port))
                {
                    _ = instance.StopAsync();
                }
                else
                {
                    PanelClient.Instance = instance;
                    serverStatusTimeout.Startup(60);
                }
            }
            else if (PanelClient.ConnectionStage == PanelClient.ConnectionState.Connected)
            {
                if (serverStatusTimeout.ToNextTime())
                {
                    await PanelClient.Instance.Actor.SendAsync(new MsgPigletUserCount
                    {
                        Data = new MsgPigletUserCount<PanelActor>.UserCountData
                        {
                            Current = UserManager.UserCount,
                            Max = UserManager.MaxUserOnline
                        }
                    });
                }
            }
        }
    }
}
