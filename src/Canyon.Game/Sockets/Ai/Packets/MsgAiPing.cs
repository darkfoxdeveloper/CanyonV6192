using Canyon.Network.Packets.Ai;

namespace Canyon.Game.Sockets.Ai.Packets
{
    public sealed class MsgAiPing : MsgAiPing<AiClient>
    {
        public override Task ProcessAsync(AiClient client)
        {
            RecvTimestamp = Environment.TickCount;
            RecvTimestampMs = Environment.TickCount64;
            return client.SendAsync(this);
        }
    }
}
