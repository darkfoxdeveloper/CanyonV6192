using Canyon.Database;
using Microsoft.Extensions.Configuration;

namespace Canyon.GM.Server
{
    public sealed class ServerConfiguration
    {

        public ServerConfiguration(params string[] args)
        {   
            var configFile = Environment.GetEnvironmentVariable("MACHINE_ENV") == "docker" ?
                             "Canyon.GM.Config.json" :
                             "Canyon.GM.Config.local.json";
            
            new ConfigurationBuilder()
            .AddJsonFile(configFile, optional: true) // Set optional to true if the file might not exist
            .AddCommandLine(args)
            .AddEnvironmentVariables()
            .Build()
            .Bind(this);
        }

        public Guid RealmID { get; set; } 
        public string RealmName { get; set; }
        public string MySQLDumpPath { get; set; }

        public DatabaseConfiguration Database { get; set; }
        public RpcConfiguration Rpc { get; set; }
        public SocketConfiguration Socket { get; set; }
        public WebConfiguration Web { get; set; }
        public FtpConfiguration Ftp { get; set; }
        public FolderConfiguration Folders { get; set; }

        public class WebConfiguration
        {
            public string Ping { get; set; }
        }

        public class RpcConfiguration
        {
            public string Address { get; set; }
            public int Port { get; set; }
            public string UserName { get; set; }
            public string Password { get; set; }
        }

        public class SocketConfiguration
        {
            public string Address { get; set; }
            public int Port { get; set; }

            /// <summary>
            /// Base 64 encoded username encrypted with AES Cipher.
            /// </summary>
            public string UserName { get; set; }
            /// <summary>
            /// Base 64 encoded password encrypted with AES Cipher.
            /// </summary>
            public string Password { get; set; }

            public string AesKey { get; set; }
            public string AesIV { get; set; }
        }

        public class FtpConfiguration
        {
            public string Hostname { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public int Port { get; set; }
        }

        public class FolderConfiguration
        {
            public string GameServer { get; set; }
        }
    }
}
