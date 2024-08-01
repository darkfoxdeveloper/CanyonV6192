using Canyon.Database;
using Canyon.Network.Services;
using Canyon.Shared;
using Canyon.Shared.Loggers;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Canyon.Login
{
    class Program
    {
        public static string Version => Assembly.GetEntryAssembly()?
                                                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                                                .InformationalVersion ?? "0.0.0.0";

        private static ILogger logger;

        public static async Task<int> Main(params string[] args)
        {
            Console.Title = "Loading...";

            Console.WriteLine("\tCanyon: Login Server");
            Console.WriteLine($"\t\tCopyright 2022-{DateTime.Now:yyyy} Felipe Vieira Vendramini \"Konichu\"");
            Console.WriteLine($"\t\tVersion: {Version}");
            Console.WriteLine("\tSome Rights Reserved");
            Console.WriteLine();

            Console.WriteLine("Initializating log factory");

            Kernel.LogProcessor = new LogProcessor(CancellationToken.None);
            _ = Kernel.LogProcessor.StartAsync(CancellationToken.None);

            LogFactory.Initialize(Kernel.LogProcessor, "Canyon.Login");
            logger = LogFactory.CreateLogger<Program>();

            logger.LogInformation("Starting World Conquer Online login service");

            // Read configuration file and command-line arguments
            var config = new ServerConfiguration(args);
            if (!config.Valid)
            {
                logger.LogCritical("Invalid server configuration file");
                Console.ReadLine();
                return 1;
            }

            Kernel.ServerConfiguration = config;
#if USE_MYSQL_DB
            if (config.Database == null)
            {
                const string defaultDbSettings = "\"Database\": {\n" +
                    "\t\"Hostname\": \"localhost\"\n" +
                    "\t\"Username\": \"root\"\n" +
                    "\t\"Password\": \"1234\"\n" +
                    "\t\"Schema\": \"cq\"\n" +
                    "\t\"Port\": 3306\n" +
                    "}";
                logger.LogCritical("Server database configuration is missing! Did you mean to compile with SSO usage? Example settings:\n{}", defaultDbSettings);
                Console.ReadLine();
                return 2;
            }
            AbstractDbContext.Configuration = config.Database;
#endif

            logger.LogInformation("Starting services");

            var tasks = new List<Task>
            {
                RandomnessService.Instance.StartAsync(CancellationToken.None)
            };
            Task.WaitAll(tasks.ToArray());

            await Kernel.StartUpAsync(config);

            logger.LogInformation("Server is fully initialized");

            await CommandCenterAsync();

            await Kernel.ShutdownAsync();

            logger.LogInformation("Closing World Conquer Online login service");
            return 0;
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
                                logger.LogInformation("Version: {Version}", Version);
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