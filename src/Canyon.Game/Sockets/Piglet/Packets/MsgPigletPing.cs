using Canyon.Network.Packets.Piglet;

namespace Canyon.Game.Sockets.Piglet.Packets
{
    public sealed class MsgPigletPing : MsgPigletPing<PigletActor>
    {
        public override Task ProcessAsync(PigletActor client)
        {
            return Task.CompletedTask;
        }
    }
}
