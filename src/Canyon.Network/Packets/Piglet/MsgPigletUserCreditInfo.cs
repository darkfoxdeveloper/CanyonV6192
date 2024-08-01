using Canyon.Network.Sockets;
using ProtoBuf;

namespace Canyon.Network.Packets.Piglet
{
    public abstract class MsgPigletUserCreditInfo<TActor> : MsgProtoBufBase<TActor, MsgPigletUserCreditInfo<TActor>.FirstCreditData> where TActor : TcpServerActor
    {
        public MsgPigletUserCreditInfo()
            : base(PacketType.MsgPigletUserCreditInfo)
        {
            serializeWithHeaders = true;
        }

        [ProtoContract]
        public struct FirstCreditData
        {
            [ProtoMember(1)]
            public uint UserIdentity { get; set; }
        }
    }
}
