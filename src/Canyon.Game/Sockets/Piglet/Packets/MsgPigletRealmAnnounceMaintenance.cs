using Canyon.Game.Services.Managers;
using Canyon.Network.Packets.Piglet;

namespace Canyon.Game.Sockets.Piglet.Packets
{
    public sealed class MsgPigletRealmAnnounceMaintenance : MsgPigletRealmAnnounceMaintenance<PigletActor>
    {
        public override Task ProcessAsync(PigletActor client)
        {
            return MaintenanceManager.AnnounceMaintenanceAsync(Data.WarningMinutes);
        }
    }
}
