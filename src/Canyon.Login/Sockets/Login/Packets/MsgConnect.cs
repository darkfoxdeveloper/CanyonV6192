using Canyon.Login.States;
using Canyon.Network.Packets.Login;

namespace Canyon.Login.Sockets.Login.Packets
{
    public sealed class MsgConnect : MsgConnect<Client>
    {
        public override Task ProcessAsync(Client client)
        {
            return Task.CompletedTask;
        }
    }
}
