using Canyon.Login.States;
using Canyon.Network.Packets.Login;

namespace Canyon.Login.Sockets.Game.Packets
{
    public sealed class MsgAccServerPing : MsgAccServerPing<Realm>
    {
        public override Task ProcessAsync(Realm client)
        {
            return client.SendAsync(new MsgAccServerPing());
        }
    }
}
