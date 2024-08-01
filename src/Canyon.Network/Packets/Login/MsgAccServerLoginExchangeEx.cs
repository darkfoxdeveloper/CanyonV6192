using Canyon.Network.Sockets;
using ProtoBuf;

namespace Canyon.Network.Packets.Login
{
    public abstract class MsgAccServerLoginExchangeEx<TActor>
        : MsgProtoBufBase<TActor, MsgAccServerLoginExchangeEx<TActor>.LoginExchangeData> where TActor : TcpServerActor
    {
        public const int SUCCESS = 0,
            ALREADY_LOGGED_IN = 1;

        protected MsgAccServerLoginExchangeEx()
            : base(PacketType.MsgAccServerLoginExchangeEx)
        {
            serializeWithHeaders = true;
        }

        [ProtoContract]
        public struct LoginExchangeData
        {
            [ProtoMember(1)]
            public ulong Token { get; set; }
            [ProtoMember(2)]
            public uint AccountId { get; set; }
            [ProtoMember(3)]
            public string Request { get; set; }
            [ProtoMember(4)]
            public int Response { get; set; }
        }
    }
}
