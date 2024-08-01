using Canyon.Database;
using Microsoft.Extensions.Configuration;

namespace Canyon.Game
{
    public class ServerConfiguration
    {
        public static ServerConfiguration Configuration { get; set; }

        public ServerConfiguration(params string[] args)
        {
            var configFile = Environment.GetEnvironmentVariable("MACHINE_ENV") == "docker" ? 
                             "Canyon.Game.Config.json" : 
                             "Canyon.Game.Config.local.json";

            new ConfigurationBuilder()
                .AddJsonFile(configFile, optional: true) // Set optional to true if the file might not exist
                .AddCommandLine(args)
                .AddEnvironmentVariables()
                .Build()
                .Bind(this);
        }

        public AuthAesConfiguration Auth { get; set; }
        public DatabaseConfiguration Database { get; set; }
        public RealmConfiguration Realm { get; set; }
        public LoginConfiguration Login { get; set; }
        public AiConfiguration Ai { get; set; }
        public List<MaintenanceScheduleConfiguration> MaintenanceSchedule { get; set; }
        public PigletConfiguration Piglet { get; set; }
        public bool CooperatorMode { get; set; } = false;

        public class AuthAesConfiguration
        {
            public string SharedKey { get; set; }
            public string SharedIV { get; set; }
        }

        public class RealmConfiguration
        {
            public Guid ServerId { get; set; }
            public string Name { get; set; }

            public string IPAddress { get; set; }
            public int Port { get; set; }

            public int MaxOnlinePlayers { get; set; }

            public string Username { get; set; }
            public string Password { get; set; }

            public DateTime ReleaseDate { get; set; }

            public int Processors { get; set; } = 1;
        }

        public class LoginConfiguration
        {
            public string IPAddress { get; set; }
            public int Port { get; set; }
        }

        public class AiConfiguration
        {
            public string IPAddress { get; set; }
            public int Port { get; set; }

            public string Username { get; set; }
            public string Password { get; set; }
        }

        public class MaintenanceScheduleConfiguration
        {
            public DayOfWeek DayOfWeek { get; set; }
            public string Time { get; set; }
            public int WarningMinutes { get; set; }
        }

        public class PigletConfiguration
        {
            public string IPAddress { get; set; }
            public int Port { get; set; }

            public string Username { get; set; }
            public string Password { get; set; }
        }
    }
}
