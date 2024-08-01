using Canyon.Game.States.User;
using Canyon.Network.Packets.Login;

namespace Canyon.Game.Sockets.Game.Packets
{
    /// <remarks>Packet Type 1055</remarks>
    /// <summary>
    ///     Message containing an authentication response from the server to the client. Can
    ///     either accept a client with realm connection details and an access token, or
    ///     reject a client with a reason on why the login attempt was rejected.
    /// </summary>
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
