using Canyon.Network.Sockets;
using ProtoBuf;

namespace Canyon.Network.Packets.Login
{
    public abstract class MsgAccServerLoginExchange<TActor>
        : MsgProtoBufBase<TActor, MsgAccServerLoginExchange<TActor>.LoginExchangeData> where TActor : TcpServerActor
    {
        protected MsgAccServerLoginExchange() 
            : base(PacketType.MsgAccServerLoginExchange)
        {
            serializeWithHeaders = true;
        }

        [ProtoContract]
        public struct LoginExchangeData
        {
            [ProtoMember(1)]
            public uint AccountId { get; set; }
            [ProtoMember(2)]
            public string IpAddress { get; set; }
            [ProtoMember(3)]
            public int VipLevel { get; set; }
            [ProtoMember(4)]
            public string Request { get; set; }
            [ProtoMember(5)]
            public ushort AuthorityId { get; set; }
        }

    }
}
