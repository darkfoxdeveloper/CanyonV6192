using Canyon.Network.Sockets;
using ProtoBuf;

namespace Canyon.Network.Packets.Piglet
{
    public abstract class MsgPigletClaimFirstCredit<TActor>
        : MsgProtoBufBase<TActor, MsgPigletClaimFirstCredit<TActor>.ClaimFirstCreditData> where TActor : TcpServerActor
    {
        public MsgPigletClaimFirstCredit()
            : base(PacketType.MsgPigletClaimFirstCredit)
        {
            serializeWithHeaders = true;
        }

        [ProtoContract]
        public struct ClaimFirstCreditData
        {
            [ProtoMember(1)]
            public uint AccountId { get; set; }
        }
    }
}
