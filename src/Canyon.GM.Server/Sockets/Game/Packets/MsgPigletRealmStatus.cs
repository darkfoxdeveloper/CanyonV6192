using Canyon.GM.Server.Sockets.Panel;
using Canyon.Network.Packets.Piglet;

namespace Canyon.GM.Server.Sockets.Game.Packets
{
    public sealed class MsgPigletRealmStatus : MsgPigletRealmStatus<GameActor>
    {
        public override async Task ProcessAsync(GameActor client)
        {
            GameServer.Instance.ServerStatus = Data.Status;
            if (PanelClient.Instance?.Actor != null)
            {
                await PanelClient.Instance.Actor.SendAsync(Encode());
            }
        }
    }
}
