using Canyon.Network.Sockets;
using ProtoBuf;

namespace Canyon.Network.Packets.Login
{
    public abstract class MsgAccServerAuth<TActor>
        : MsgProtoBufBase<TActor, MsgAccServerAuth<TActor>.ServerAuthData> where TActor : TcpServerActor
    {
        protected MsgAccServerAuth() 
            : base(PacketType.MsgAccServerAuth)
        {
            serializeWithHeaders = true;
        }

        [ProtoContract]
        public struct ServerAuthData
        {
            [ProtoMember(1)]
            public string Username { get; set; }
            [ProtoMember(2)]
            public string Password { get; set; }
            [ProtoMember(3)]
            public string RealmId { get; set; }
        }
    }
}
