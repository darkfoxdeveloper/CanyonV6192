using Canyon.GM.Server.Sockets.Game;
using Canyon.Network.Packets.Piglet;
using Canyon.Shared;
using Microsoft.Extensions.Logging;

namespace Canyon.GM.Server.Sockets.Panel.Packets
{
    public sealed class MsgPigletRealmAnnounceMaintenance : MsgPigletRealmAnnounceMaintenance<PanelActor>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MsgPigletRealmAnnounceMaintenance>();

        public override Task ProcessAsync(PanelActor client)
        {
            if (GameServer.Instance?.Actor != null)
            {
                logger.LogInformation("Maintenance announced in {} minutes!!!", Data.WarningMinutes);
                return GameServer.Instance.Actor.SendAsync(this);
            }
            return Task.CompletedTask;
        }
    }
}
