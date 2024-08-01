using Canyon.Database;
using Canyon.Game.Database;
using Canyon.Game.Services.Managers;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States.User;
using Canyon.Network.Security;
using Canyon.Shared.Loggers;
using Canyon.Shared.Managers;
using System.Drawing;
using System.Reflection;

namespace Canyon.Game
{
    class Program
    {
        public static string Version => Assembly.GetEntryAssembly()?
                                                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                                                .InformationalVersion ?? "0.0.0.0";

        const int NO_ERROR = 0;
        const int INITIALIZATION_ERROR = -1;
        const int DATABASE_ERROR = -2;
        const int NO_RELEASE_DATE_ERROR = -3;
        const int RELEASE_DATE_INVALID_ERROR = -4;
        const int INVALID_RELEASE_SETTINGS = -5;

        private static ILogger logger;
        private static readonly CancellationTokenSource cancellationTokenSource = new();

        public static async Task<int> Main(params string[] args)
        {
            Console.Title = "Loading...";

            Console.WriteLine("\tCanyon: Game Server");
            Console.WriteLine($"\t\tCopyright 2022-{DateTime.Now:yyyy} Felipe Vieira Vendramini \"Konichu\"");
            Console.WriteLine($"\t\tVersion: {Version}");
            Console.WriteLine("\tSome Rights Reserved");
            Console.WriteLine();

            Console.WriteLine("Initializating log factory");

            Kernel.Services.LogProcessor = new LogProcessor(CancellationToken.None);
            _ = Kernel.Services.LogProcessor.StartAsync(CancellationToken.None);

            LogFactory.Initialize(Kernel.Services.LogProcessor, "Canyon.Game");
            logger = LogFactory.CreateLogger<Program>();

            logger.LogInformation("Starting game server settings...");
            ServerConfiguration.Configuration = new ServerConfiguration(args);
            AbstractDbContext.Configuration = ServerConfiguration.Configuration.Database;

            if (ServerConfiguration.Configuration.Realm.ReleaseDate == default)
            {
                logger.LogError("Please, set up a release date for the server starting from now or before.");
                Console.ReadLine();
                return NO_RELEASE_DATE_ERROR;
            }

            if (ServerConfiguration.Configuration.Realm.ReleaseDate > DateTime.Now)
            {
                logger.LogError($"This server is setup to start on {ServerConfiguration.Configuration.Realm.ReleaseDate}");
                Console.ReadLine();
                return RELEASE_DATE_INVALID_ERROR;
            }

            logger.LogInformation("Initializating realm '{RealmName}'", ServerConfiguration.Configuration.Realm.Name);
            logger.LogInformation("This server has been oppened in {Date}", ServerConfiguration.Configuration.Realm.ReleaseDate);

            if (ServerConfiguration.Configuration.CooperatorMode)
            {
                logger.LogWarning("Server is open in Cooperator Mode! Normal players may not be able to login to the game server.");
                RoleManager.ToggleCooperatorMode();
            }

            /**
             * After this commit the server credentials has been changed so we can open this repository to public.
             * In order to avoid vulnerabilities, we are not allowing RELEASE mode to use our DEBUG credentials 
             * so people who don't know how to configure the server don't get caught with others connecting in their
             * realms.
             */
#if !DEBUG
            if (ServerConfiguration.Configuration.Realm.Username.Equals("yD3Ni6tMW1NNU1QH")
                || ServerConfiguration.Configuration.Realm.Password.Equals("jETqqIKi9LuFvOgu"))
            {
                logger.LogCritical("Server has not been configured properly! Change your Realm credentials!");
                Console.ReadLine();
                return INVALID_RELEASE_SETTINGS;
            }

            if (ServerConfiguration.Configuration.Ai.Username.Equals("yD3Ni6tMW1NNU1QH")
                || ServerConfiguration.Configuration.Ai.Password.Equals("jETqqIKi9LuFvOgu"))
            {
                logger.LogCritical("Server has not been configured properly! Change your AI credentials!");
                Console.ReadLine();
                return INVALID_RELEASE_SETTINGS;
            }
#endif

            logger.LogInformation($"Checking if database '{ServerConfiguration.Configuration.Database.Schema}' is accessible");
            if (!ServerDbContext.Ping())
            {
                logger.LogCritical("Database is inaccessible.");
                Console.ReadLine();
                return DATABASE_ERROR;
            }
            logger.LogInformation("Database is valid");

            logger.LogInformation("Initializating required services");

            Kernel.Services.Processor = new(cancellationTokenSource.Token);
            var tasks = new List<Task>
            {
                Kernel.Services.Randomness.StartAsync(cancellationTokenSource.Token),
                NDDiffieHellman.ProbablePrimes.StartAsync(cancellationTokenSource.Token),
                Kernel.Services.Processor.StartAsync(cancellationTokenSource.Token)
            };
            Task.WaitAll(tasks.ToArray());

            if (!await Kernel.InitializeAsync())
            {
                logger.LogCritical("Initialization failed!");
                Console.ReadLine();
                return INITIALIZATION_ERROR;
            }

            //AbstractDbContext.UseLog = true;

            await CommandCenterAsync();

            cancellationTokenSource.Cancel();
            await Kernel.StopAsync();
            return NO_ERROR;
        }

        private static async Task<bool> CommandCenterAsync()
        {
            string text;
            do
            {
                text = Console.ReadLine();
                if (string.IsNullOrEmpty(text))
                {
                    continue;
                }

                try
                {
                    string[] splitCmd = text.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    switch (splitCmd[0])
                    {
                        case "/version":
                            {
                                logger.LogInformation("Current version: {Version}", Version);
                                break;
                            }

                        case "/processor":
                            {
                                Console.WriteLine(Kernel.Services.Processor.ToString());
                                break;
                            }

                        case "/clear":
                        case "/cls":
                            {
                                Console.Clear();
                                break;
                            }
                        case "/message":
                            {
                                await BroadcastWorldMsgAsync(splitCmd[1], TalkChannel.Service, Color.White);
                                break;
                            }
                        case "/reloadactionall":
                            {
                                await EventManager.LoadActionsAsync();
                                break;
                            }

                        case "/roleids":
                            {
                                int mapItems = IdentityManager.MapItem.IdentitiesCount();
                                int petIds = IdentityManager.Pet.IdentitiesCount();
                                int furniture = IdentityManager.Furniture.IdentitiesCount();
                                int traps = IdentityManager.Traps.IdentitiesCount();

                                logger.LogInformation($"=============================================================");
                                logger.LogInformation($"Identities remaining:");
                                logger.LogInformation($"MapItem: {mapItems}");
                                logger.LogInformation($"Pets: {petIds}");
                                logger.LogInformation($"Furniture: {furniture}");
                                logger.LogInformation($"Traps: {traps}");
                                logger.LogInformation($"=============================================================");

                                break;
                            }

                        case "/kickoutall":
                            {
                                await RoleManager.KickOutAllAsync("");
                                break;
                            }

                        case "/generate_machine_report":
                            {
                                var onlineUsers = RoleManager.QueryUserSet();
                                Dictionary<string, List<Client>> usersByIp = new();
                                foreach (var user in onlineUsers.Select(x => x.Client).OrderBy(x => x.IpAddress))
                                {
                                    if (!usersByIp.TryGetValue(user.IpAddress, out var list))
                                    {
                                        list = new List<Client>();
                                        usersByIp.Add(user.IpAddress, list);
                                    }

                                    list.Add(user);
                                }

                                using var ipWriter = new StreamWriter(Path.Combine(Environment.CurrentDirectory, $"{DateTime.Now:yyyyMMddHHmmss}_UserIpReport.txt"), false);
                                ipWriter.WriteLine($"Total Players Online: {onlineUsers.Count}");
                                foreach (var ipAddress in usersByIp)
                                {
                                    ipWriter.WriteLine($"==================== {ipAddress.Key} [{ipAddress.Value.Count}] ====================");
                                    foreach (var user in ipAddress.Value.OrderBy(x => x.LastLogin))
                                    {
                                        ipWriter.WriteLine($"{user.AccountIdentity},{user.Identity},{user.Character.Name},{user.MacAddress},{user.Character.LastLogin}");
                                    }
                                    ipWriter.WriteLine();
                                }
                                ipWriter.Close();

                                Dictionary<string, List<Client>> usersByMac = new();
                                foreach (var user in onlineUsers.Select(x => x.Client).OrderBy(x => x.MacAddress))
                                {
                                    if (!usersByMac.TryGetValue(user.MacAddress, out var list))
                                    {
                                        list = new List<Client>();
                                        usersByMac.Add(user.MacAddress, list);
                                    }

                                    list.Add(user);
                                }

                                using var macWriter = new StreamWriter(Path.Combine(Environment.CurrentDirectory, $"{DateTime.Now:yyyyMMddHHmmss}_UserMacReport.txt"), false);
                                macWriter.WriteLine($"Total Players Online: {onlineUsers.Count}");
                                foreach (var macAddress in usersByMac)
                                {
                                    macWriter.WriteLine($"==================== {macAddress.Key} [{macAddress.Value.Count}] ====================");
                                    foreach (var user in macAddress.Value.OrderBy(x => x.LastLogin))
                                    {
                                        macWriter.WriteLine($"{user.AccountIdentity},{user.Identity},{user.Character.Name},{user.IpAddress},{user.Character.LastLogin}");
                                    }
                                    macWriter.WriteLine();
                                }
                                macWriter.Close();
                                break;
                            }

                        case "/maintenance":
                            {
                                await MaintenanceManager.AnnounceMaintenanceAsync();
                                break;
                            }

                        case "/SetCooperatorMode":
                            {
                                RoleManager.ToggleCooperatorMode();
                                break;
                            }

                        case "/reloadlua":
                            {
                                LuaScriptManager.Reload();
                                break;
                            }

                    }
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, "{}", ex.Message);
                }
            }
            while (!"exit".Equals(text));
            return true;
        }
    }
}