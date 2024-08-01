using Canyon.Game.Services.Managers;
using Canyon.Network.Packets.Piglet;

namespace Canyon.Game.Sockets.Piglet.Packets
{
    public sealed class MsgPigletShutdown : MsgPigletShutdown<PigletActor>
    {
        public override Task ProcessAsync(PigletActor client)
        {
            if (Data.Id == int.MaxValue)
            {
                return MaintenanceManager.CloseServerAsync();
            }
            return Task.CompletedTask;
        }
    }
}
