using Canyon.Database.Entities;
using Canyon.GM.Server.Sockets.Game;
using Canyon.Network.Packets.Piglet;
using Canyon.Shared;
using Microsoft.Extensions.Logging;

namespace Canyon.GM.Server.Sockets.Panel.Packets
{
    public sealed class MsgPigletLoginEx : MsgPigletLoginEx<PanelActor>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MsgPigletLoginEx>();

        public override async Task ProcessAsync(PanelActor client)
        {
            logger.LogInformation($"GM Panel connected!!");
            await client.SendAsync(new MsgPigletRealmStatus
            {
                Data = new MsgPigletRealmStatus<PanelActor>.RealmStatusData
                {
                    Status = GameServer.Instance?.ServerStatus ?? 0
                }
            });
        }
    }
}
