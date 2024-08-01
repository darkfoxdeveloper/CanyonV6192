using Canyon.Login.Sockets.Game;
using Canyon.Login.Sockets.Login;
using Canyon.Login.Threading;
using Canyon.Network.Services;
using Canyon.Shared;
using Canyon.Shared.Loggers;
using Canyon.Shared.Rest;
using Canyon.Shared.Threads;
using Microsoft.Extensions.Logging;

namespace Canyon.Login
{
    public class Kernel
    {
        private static ILogger logger;
        private static SchedulerFactory SchedulerFactory { get; set; }

        public static LogProcessor LogProcessor { get; set; }
        public static ServerConfiguration ServerConfiguration { get; set; }
        public static SocketConnection Sockets = new();
        public static RestClient RestClient = new();

        public static async Task<bool> StartUpAsync(ServerConfiguration serverConfiguration)
        {
            logger = LogFactory.CreateLogger<Kernel>();

#if !USE_MYSQL_DB
            try
            {
                await RestClient.AuthorizeAsync(ServerConfiguration.Authentication.Identity, ServerConfiguration.Authentication.ClientId, ServerConfiguration.Authentication.ClientSecret, ServerConfiguration.Authentication.Scope);
            }
            catch (Exception ex)
            {
                logger.LogWarning("An error occured when doing first authorization. SSO may be unavailable. Error: {}", ex.Message);
            }
#endif

            try
            {
                Sockets.GameServer = new GameServer();
                _ = Sockets.GameServer.StartAsync(serverConfiguration.RealmNetwork.Port, serverConfiguration.RealmNetwork.IPAddress);

                Sockets.LoginServer = new LoginServer(serverConfiguration);
                _ = Sockets.LoginServer.StartAsync(serverConfiguration.Network.Port);

                SchedulerFactory = new SchedulerFactory();
                await SchedulerFactory.StartAsync();
                await SchedulerFactory.ScheduleAsync<BasicThread>("* * * * * ?");
                return true;
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Error initializing server!!! {}", ex.Message);
                return false;
            }
        }

        public static async Task ShutdownAsync()
        {
            await SchedulerFactory.StopAsync();
        }

        public static Task<int> NextAsync(int minValue, int maxValue) => RandomnessService.NextAsync(minValue, maxValue);

        public class SocketConnection
        {
            public LoginServer LoginServer { get; set; }
            public GameServer GameServer { get; set; }
        }
    }
}
