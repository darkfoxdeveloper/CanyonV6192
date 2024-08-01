//#if DEBUG
//#define QUICK_LOADING // USE THIS TO TEST INTRA CONNECTIONS!!! NO MAP OR ANYTHING WILL BE LOADED
//#endif
using Canyon.Game.Services.Managers;
using Canyon.Game.Services.Processors;
using Canyon.Game.Sockets.Ai;
using Canyon.Game.Sockets.Game;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.Threading;
using Canyon.Network;
using Canyon.Network.Packets;
using Canyon.Network.Services;
using Canyon.Shared.Loggers;
using Canyon.Shared.Threads;
using Canyon.World;
using System.Drawing;
using System.Runtime.Caching;

namespace Canyon.Game
{
    public class Kernel
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<Kernel>();

        private static SchedulerFactory schedulerFactory;
        private static UserThread userThread;
        private static EventThread eventThread;
        private static RoleThread roleThread;

        public static readonly SocketConnection Sockets = new();
        public static readonly NetworkMonitor NetworkMonitor = new();
        public static readonly MemoryCache Logins = MemoryCache.Default;
        public static List<uint> Registration = new();

        public static async Task<bool> InitializeAsync()
        {
            try
            {
#if !QUICK_LOADING
                await MapDataManager.LoadDataAsync().ConfigureAwait(true);
                await MapManager.InitializeAsync().ConfigureAwait(true);
                
                await DynamicGlobalDataManager.InitializeAsync().ConfigureAwait(true);
                await ServerStatisticManager.InitializeAsync().ConfigureAwait(true);
                await ExperienceManager.InitializeAsync().ConfigureAwait(true);
                await RoleManager.InitializeAsync().ConfigureAwait(true);
                await ItemManager.InitializeAsync().ConfigureAwait(true);
                await MagicManager.InitializeAsync().ConfigureAwait(true);
                await SyndicateManager.InitializeAsync().ConfigureAwait(true);
                await FamilyManager.InitializeAsync().ConfigureAwait(true);
                await EventManager.InitializeAsync().ConfigureAwait(true);
                await LotteryManager.InitializeAsync().ConfigureAwait(true);
                await PeerageManager.InitializeAsync().ConfigureAwait(true);
                await TutorManager.InitializeAsync().ConfigureAwait(true);
                await FlowerManager.InitializeAsync().ConfigureAwait(true);
                await PigeonManager.InitializeAsync().ConfigureAwait(true);
                await AstProfManager.InitializeAsync().ConfigureAwait(true);
                await FateManager.InitializeAsync().ConfigureAwait(true);
                await JiangHuManager.InitializeAsync().ConfigureAwait(true);
                await AuctionManager.InitializeAsync().ConfigureAwait(true);
                await MineManager.InitializeAsync().ConfigureAwait(true);
                await BattleSystemManager.InitializeAsync().ConfigureAwait(true);
                await AchievementManager.InitializeAsync().ConfigureAwait(true);
                await ActivityManager.InitializeAsync().ConfigureAwait(true);
                await ProcessGoalManager.InitializeAsync().ConfigureAwait(true);
                await InnerStrengthManager.InitializeAsync().ConfigureAwait(true);

                LuaScriptManager.Run("Event_Server_Start()");
#endif

                schedulerFactory = new SchedulerFactory();
                await schedulerFactory.StartAsync();
                await schedulerFactory.ScheduleAsync<BasicThread>("* * * * * ?");
                await schedulerFactory.ScheduleAsync<AutomaticActionThread>("0 * * * * ?");

                userThread = new UserThread();
                await userThread.StartAsync();

                eventThread = new EventThread();
                await eventThread.StartAsync();

                roleThread = new RoleThread();
                await roleThread.StartAsync();

                logger.LogInformation("Initializating sockets");

                Sockets.GameServer = new GameServer(ServerConfiguration.Configuration);
                _ = Sockets.GameServer.StartAsync(ServerConfiguration.Configuration.Realm.Port, ServerConfiguration.Configuration.Realm.IPAddress);

                logger.LogInformation("Game socket listening on {IpAddress}:{Port}", ServerConfiguration.Configuration.Realm.IPAddress, ServerConfiguration.Configuration.Realm.Port);

                Sockets.NpcServer = new NpcServer();
                _ = Sockets.NpcServer.StartAsync(ServerConfiguration.Configuration.Ai.Port, ServerConfiguration.Configuration.Ai.IPAddress);

                logger.LogInformation("Npc socket listening on {IpAddress}:{Port}", ServerConfiguration.Configuration.Ai.IPAddress, ServerConfiguration.Configuration.Ai.Port);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "{Message}", ex.Message);
                return false;
            }

            return true;
        }

        public static async Task StopAsync()
        {
            await RoleManager.KickOutAllAsync("Server is closing.", true).ConfigureAwait(true);

            var processorCompletion = Task.WhenAny(Services.Processor.CompletionAsync(), Task.Delay(5000));

            await eventThread.StopAsync();
            await userThread.StopAsync();
            await roleThread.StopAsync();
            await ServerStatisticManager.SaveAsync();

            for (var i = 5; i >= 0; i--)
            {
                logger.LogWarning("Server will close in {Seconds} seconds...", i);
                await Task.Delay(1000);
            }
            await processorCompletion;

            await schedulerFactory.StopAsync();
        }

        public static void SetMaintenance()
        {
            try
            {
                Sockets.NpcServer.Close();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error when closing NPC Server socket!!! {}", ex.Message);
            }
        }

        #region Random

        public static class RandomServices
        {
            /// <summary>
            ///     Returns the next random number from the generator.
            /// </summary>
            /// <param name="maxValue">One greater than the greatest legal return value.</param>
            public static Task<int> NextAsync(int maxValue)
            {
                return NextAsync(0, maxValue);
            }

            public static Task<double> NextRateAsync(double range)
            {
                return Services.Randomness.NextRateAsync(range);
            }

            /// <summary>Writes random numbers from the generator to a buffer.</summary>
            /// <param name="buffer">Buffer to write bytes to.</param>
            public static Task NextBytesAsync(byte[] buffer)
            {
                return Services.Randomness.NextBytesAsync(buffer);
            }

            /// <summary>
            ///     Returns the next random number from the generator.
            /// </summary>
            /// <param name="minValue">The least legal value for the Random number.</param>
            /// <param name="maxValue">One greater than the greatest legal return value.</param>
            public static Task<int> NextAsync(int minValue, int maxValue)
            {
                return Services.Randomness.NextIntegerAsync(minValue, maxValue);
            }

            public static async Task<bool> ChanceCalcAsync(int chance, int outOf)
            {
                return await NextAsync(outOf) < chance;
            }

            /// <summary>
            ///     Calculates the chance of success based in a rate.
            /// </summary>
            /// <param name="chance">Rate in percent.</param>
            /// <returns>True if the rate is successful.</returns>
            public static async Task<bool> ChanceCalcAsync(double chance)
            {
                const int divisor = 10_000_000;
                const int maxValue = 100 * divisor;
                try
                {
                    return await NextAsync(0, maxValue) <= chance * divisor;
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, "ChanceCalcAsync(double): {Message}", ex.Message);
                    return false;
                }
            }

            public static bool ChanceCalc(int chance, int outOf)
            {
                return Services.Randomness.NextInteger(0, outOf) < chance;
            }

            public static bool ChanceCalc(double chance)
            {
                const int divisor = 10_000_000;
                const int maxValue = 100 * divisor;
                try
                {
                    return Services.Randomness.NextInteger(0, maxValue) <= chance * divisor;
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, "ChanceCalcAsync(double): {Message}", ex.Message);
                    return false;
                }
            }
        }

        #endregion

        #region Broadcast

        public static class BroadcastServices
        {
            public static Task BroadcastWorldMsgAsync(string message, TalkChannel channel = TalkChannel.System, Color? color = null)
            {
                return BroadcastWorldMsgAsync(new MsgTalk(0, channel, color ?? Color.Red, message));
            }

            public static Task BroadcastWorldMsgAsync(IPacket msg, uint ignore = 0)
            {
                byte[] encoded = msg.Encode();
                foreach (var user in RoleManager.QueryUserSet())
                {
                    if (user.Identity == ignore)
                    {
                        continue;
                    }
                    _ = user.SendAsync(encoded).ConfigureAwait(false);
                }
                return Task.CompletedTask;
            }

            public static Task BroadcastNpcMsgAsync(IPacket msg)
            {
                if (NpcServer.NpcClient != null)
                {
                    Sockets.NpcServer.Send(NpcServer.NpcClient, msg.Encode());
                }
                return Task.CompletedTask;
            }

            public static Task BroadcastNpcMsgAsync(IPacket msg, Func<Task> func)
            {
                if (NpcServer.NpcClient != null)
                {
                    Sockets.NpcServer.Send(NpcServer.NpcClient, msg.Encode(), func);
                }
                return Task.CompletedTask;
            }
        }

        #endregion

        #region Global Constants

        public class GlobalConstants
        {
            public const uint DEFAULT_MAP_ID = 1002;
            public const ushort DEFAULT_MAP_X = 300;
            public const ushort DEFAULT_MAP_Y = 278;
        }

        #endregion

        public static class Services
        {
            public static readonly RandomnessService Randomness = new();
            public static ServerProcessor Processor;
            public static LogProcessor LogProcessor;
        }

        public class SocketConnection
        {
            public GameServer GameServer { get; set; }
            public NpcServer NpcServer { get; set; }

        }
    }
}
