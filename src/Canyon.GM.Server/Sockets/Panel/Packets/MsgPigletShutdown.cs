using Canyon.GM.Server.Sockets.Game;
using Canyon.Network.Packets.Piglet;
using Canyon.Shared;
using Microsoft.Extensions.Logging;

namespace Canyon.GM.Server.Sockets.Panel.Packets
{
    public sealed class MsgPigletShutdown : MsgPigletShutdown<PanelActor>
    {
        private static ILogger logger = LogFactory.CreateLogger<MsgPigletShutdown>();

        public override Task ProcessAsync(PanelActor client)
        {
            if (GameServer.Instance?.Actor != null)
            {
                logger.LogInformation("Received close server request from GM Panel");
                return GameServer.Instance.Actor.SendAsync(this);
            }
            return Task.CompletedTask;
        }
    }
}
