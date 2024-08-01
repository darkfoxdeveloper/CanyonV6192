using Canyon.Login.States;
using Canyon.Network.Packets.Login;

namespace Canyon.Login.Sockets.Login.Packets
{
    public sealed class MsgEncryptCode : MsgEncryptCode<Client>
    {
        public MsgEncryptCode(uint seed)
        {
            Seed = seed;
        }
    }
}
