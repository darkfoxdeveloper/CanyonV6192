using Canyon.Login.States;
using Canyon.Network.Packets.Login;

namespace Canyon.Login.Sockets.Login.Packets
{
    public sealed class MsgPCNum : MsgPCNum<Client>
    {
        public override Task ProcessAsync(Client client)
        {
            client.Disconnect();
            return Task.CompletedTask;
        }
    }
}
