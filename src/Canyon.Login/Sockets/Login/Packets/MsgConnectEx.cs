using Canyon.Login.States;
using Canyon.Network.Packets.Login;

namespace Canyon.Login.Sockets.Login.Packets
{
    public sealed class MsgConnectEx : MsgConnectEx<Client>
    {
        public MsgConnectEx(RejectionCode rejectionCode) : base(rejectionCode)
        {
        }

        public MsgConnectEx(string ipAddress, uint port, ulong token) : base(ipAddress, port, token)
        {
        }
    }
}
