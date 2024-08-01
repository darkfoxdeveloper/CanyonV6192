using Canyon.Network.Packets.Login;

namespace Canyon.Game.Sockets.Login.Packets
{
    public sealed class MsgAccServerPing : MsgAccServerPing<LoginActor>
    {
        public MsgAccServerPing()
        {            
        }

        public override Task ProcessAsync(LoginActor client)
        {

            return Task.CompletedTask;
        }
    }
}
