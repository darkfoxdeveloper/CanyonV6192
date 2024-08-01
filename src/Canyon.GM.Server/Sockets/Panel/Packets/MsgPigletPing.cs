using Canyon.Network.Packets.Piglet;

namespace Canyon.GM.Server.Sockets.Panel.Packets
{
    public sealed class MsgPigletPing : MsgPigletPing<PanelActor>
    {
        public MsgPigletPing()
        {
            Data = new PingData
            {
                TickCount = Environment.TickCount64
            };
        }

        public override Task ProcessAsync(PanelActor client)
        {
            return client.SendAsync(new MsgPigletPing());
        }
    }
}
