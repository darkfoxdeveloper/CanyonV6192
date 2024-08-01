using Canyon.GM.Server.Sockets.Game;
using Canyon.GM.Server.Sockets.Panel;
using Canyon.GM.Server.Sockets.Panel.Packets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Canyon.GM.Server.Services
{
    public sealed class RealmService
    {
        private const string GAME_SERVER_EXE = "Canyon.Game.exe";
        private const string AI_SERVER_EXE = "Canyon.Ai.exe";

        private readonly ILogger<RealmService> logger;
        private readonly IConfiguration configuration;

        public RealmService(
            ILogger<RealmService> logger
            )
        {
            this.logger = logger;
        }

        public async Task StartServerAsync()
        {
            // we will assume for now that if the game server is up, the npc server is also up
            // TODO add a connection with npcserver for management
            if (GameServer.Instance.IsConnected)
            {
                return;
            }

            logger.LogInformation("Game server has not started! Starting new instance");

            string folder = Program.ServerConfiguration.Folders.GameServer;
            string gameServerPath = Path.Combine(folder, GAME_SERVER_EXE);
            string aiServerPath = Path.Combine(folder, AI_SERVER_EXE);

            if (!File.Exists(gameServerPath))
            {
                logger.LogError("Game server does not exist!!!");
                return;
            }

            if (!File.Exists(aiServerPath))
            {
                logger.LogError("NPC server does not exist!!!");
                return;
            }

            var gameServer = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = GAME_SERVER_EXE,
                    WorkingDirectory = folder
                }
            };

            gameServer.OutputDataReceived += OutputDataReceived;
            gameServer.ErrorDataReceived += OutputErrorReceived;

            gameServer.Start();

            var aiServer = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = AI_SERVER_EXE,
                    WorkingDirectory = folder
                }
            };

            aiServer.OutputDataReceived += OutputDataReceived;
            aiServer.ErrorDataReceived += OutputErrorReceived;

            aiServer.Start();
        }

        public async Task AnnounceMaintenanceAsync(int announceMinutes)
        {
            if (!GameServer.Instance.IsConnected)
            {
                logger.LogWarning("Game Server is not connected! No maintenance scheduled.");
                return;
            }

            logger.LogInformation("Maintenance in {} minutes!", announceMinutes);

            await GameServer.Instance.Actor.SendAsync(new MsgPigletRealmAnnounceMaintenance
            {
                Data = new Network.Packets.Piglet.MsgPigletRealmAnnounceMaintenance<PanelActor>.AnnounceData
                {
                    WarningMinutes = announceMinutes
                }
            });
        }

        public async Task StopServerAsync()
        {
            if (!GameServer.Instance.IsConnected)
            {
                logger.LogWarning("Game Server is not connected!");
                return;
            }

            logger.LogInformation("Closing server by piglet request");
            await GameServer.Instance.Actor.SendAsync(new MsgPigletShutdown
            {
                Data = new Network.Packets.Piglet.MsgPigletShutdown<PanelActor>.ShutdownData
                {
                    Id = int.MaxValue
                }
            });            
        }

        private void OutputDataReceived(object sender, DataReceivedEventArgs args)
        {
            if (!string.IsNullOrEmpty(args.Data) && PanelClient.Instance.Actor != null)
            {
                PanelClient.Instance.Actor.SendAsync(new MsgPigletLogOutputRedirect
                {
                    Data = new Network.Packets.Piglet.MsgPigletLogOutputRedirect<PanelActor>.LogData
                    {
                        LogLevel = "MESSAGE",
                        Message = args.Data[..Math.Min(args.Data.Length, 2000)]
                    }
                }).GetAwaiter().GetResult();
            }
        }

        private void OutputErrorReceived(object sender, DataReceivedEventArgs args)
        {
            PanelClient.Instance.Actor.SendAsync(new MsgPigletLogOutputRedirect
            {
                Data = new Network.Packets.Piglet.MsgPigletLogOutputRedirect<PanelActor>.LogData
                {
                    LogLevel = "ERROR",
                    Message = args.Data[..Math.Min(args.Data.Length, 2000)]
                }
            }).GetAwaiter().GetResult();
        }

    }
}
