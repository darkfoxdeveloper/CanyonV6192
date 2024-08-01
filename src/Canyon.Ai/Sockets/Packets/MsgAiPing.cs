using Canyon.Network.Packets.Ai;

namespace Canyon.Ai.Sockets.Packets
{
    public sealed class MsgAiPing : MsgAiPing<GameServer>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MsgAiPing>();

        public override Task ProcessAsync(GameServer client)
        {
            if (RecvTimestamp != 0 && RecvTimestampMs != 0)
            {
                int ping = (Environment.TickCount - RecvTimestamp) / 2;
                long pingMs = (Environment.TickCount64 - RecvTimestampMs) / 2;

                if (ping > 1000 || pingMs > 1000)
                    logger.LogWarning($"Inter server network lag detected! Ping: {ping}s ({pingMs}ms)");
            }
            return Task.CompletedTask;
        }
    }
}
