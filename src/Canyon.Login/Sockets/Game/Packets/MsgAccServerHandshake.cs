using Canyon.Login.States;
using Canyon.Network.Packets.Login;
using Org.BouncyCastle.Math;

namespace Canyon.Login.Sockets.Game.Packets
{
    public sealed class MsgAccServerHandshake : MsgAccServerHandshake<Realm>
    {
        public MsgAccServerHandshake()
        {            
        }

        public MsgAccServerHandshake(BigInteger publicKey, BigInteger modulus, byte[] eIv, byte[] dIv)
            : base(publicKey, modulus, eIv ?? new byte[16], dIv ?? new byte[16])
        {
        }
    }
}
