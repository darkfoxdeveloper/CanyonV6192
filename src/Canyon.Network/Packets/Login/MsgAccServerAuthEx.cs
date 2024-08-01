using Canyon.Network.Sockets;
using ProtoBuf;

namespace Canyon.Network.Packets.Login
{
    public abstract class MsgAccServerAuthEx<TActor>
        : MsgProtoBufBase<TActor, MsgAccServerAuthEx<TActor>.AuthExData> where TActor : TcpServerActor
    {
        public const int SUCCESS = 0,
            INVALID_USERNAME_PASSWORD = 1,
            UNAUTHORIZED_IP_ADDRESS = 2,
            REALM_DOES_NOT_EXIST = 3,
            DEBUG_MODE = 4,
            DUPLICATED_LOGIN = 5;

        protected MsgAccServerAuthEx()
            : base(PacketType.MsgAccServerAuthEx)
        {
            serializeWithHeaders = true;
        }

        [ProtoContract]
        public struct AuthExData
        {
            [ProtoMember(1)]
            public string RealmId { get; set; }
            [ProtoMember(2)]
            public int ResponseStatus { get; set; }
        }
    }
}
