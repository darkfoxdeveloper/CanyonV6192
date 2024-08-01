using Canyon.Network.Packets.Piglet;
using Org.BouncyCastle.Math;

namespace Canyon.GM.Server.Sockets.Game.Packets
{
    public sealed class MsgPigletHandshake : MsgPigletHandshake<GameActor>
    {
        public MsgPigletHandshake()
        {            
        }

        public MsgPigletHandshake(BigInteger publicKey, BigInteger modulus, byte[] eIv, byte[] dIv) 
            : base(publicKey, modulus, eIv ?? new byte[16], dIv ?? new byte[16])
        {
        }
    }
}
