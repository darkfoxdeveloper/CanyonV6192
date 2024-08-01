using Canyon.GM.Server.Services;
using Canyon.GM.Server.Sockets.Game;
using Canyon.GM.Server.Threads;
using Canyon.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using System.Reflection;

namespace Canyon.GM.Server
{
    internal class Program
    {
        public static string Version => Assembly.GetEntryAssembly()?
                                                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                                                .InformationalVersion ?? "0.0.0.0";

        public static ServerConfiguration ServerConfiguration;

        public static async Task Main(params string[] args)
        {
            ServerConfiguration = new ServerConfiguration(args);
            using IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging =>
                {
                    logging.AddSimpleConsole(x =>
                    {
                        x.SingleLine = true;
                        x.TimestampFormat = "[yyyyMMdd HHmmss.fff] ";
                    });
                    logging.AddFile("Logs/CanyonGM-{Date}.log");
                })
                .ConfigureServices(async (hostContext, services) =>
                {
                    services.AddHttpClient();

                    services.AddQuartz(q =>
                    {
                        q.UseMicrosoftDependencyInjectionJobFactory();

                        JobKey key = new("BasicThreadJob");
                        q.AddJob<BasicThread>(key);

                        q.AddTrigger(opts => opts
                            .ForJob(key)
                            .WithIdentity("BasicThreadJob-Trigger")
                            .WithSchedule(SimpleScheduleBuilder.RepeatSecondlyForever())
                        );

                        key = new("AutomaticBackupJob");
                        q.AddJob<MySQLDumpThread>(key);

                        q.AddTrigger(opts => opts
                            .ForJob(key)
                            .WithIdentity("AutomaticBackupJob-Trigger")
                            .WithCronSchedule("0 0 */6 * * ?")
                        );
                    });

                    // Add the Quartz.NET hosted service
                    services.AddQuartzHostedService(q =>
                    {
                        q.WaitForJobsToComplete = true;
                    });

                    services.AddSingleton<GameServer>();
                    services.AddSingleton<RealmService>();
                    services.AddScoped<BackupService>();

                    var serviceProvider = ServiceProviderHelper.Instance = services.BuildServiceProvider();
                    ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                    LogFactory.Initialize(loggerFactory);

                    // warm up
                    var gameServer = serviceProvider.GetRequiredService<GameServer>();
                    await gameServer.StartAsync(ServerConfiguration.Rpc.Port, ServerConfiguration.Rpc.Address, 5);
                })
                .Build();

            await host.RunAsync();
        }
    }
}