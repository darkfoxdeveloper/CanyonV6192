using Canyon.Network.Packets.Piglet;

namespace Canyon.GM.Server.Sockets.Game.Packets
{
    public sealed class MsgPigletPing : MsgPigletPing<GameActor>
    {
        public override Task ProcessAsync(GameActor client)
        {
            return client.SendAsync(new MsgPigletPing
            {
                Data = new PingData
                {
                    TickCount = Environment.TickCount64
                }
            });
        }
    }
}
