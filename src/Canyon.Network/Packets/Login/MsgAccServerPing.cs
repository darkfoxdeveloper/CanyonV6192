using Canyon.Network.Sockets;
using ProtoBuf;

namespace Canyon.Network.Packets.Login
{
    public abstract class MsgAccServerPing<TActor> 
        : MsgProtoBufBase<TActor, MsgAccServerPing<TActor>.PingData> where TActor : TcpServerActor
    {
        protected MsgAccServerPing()
            : base(PacketType.MsgAccServerPing)
        {
            serializeWithHeaders = true;
            Data = new PingData
            {
                Timestamp = Environment.TickCount64
            };
        }

        [ProtoContract]
        public struct PingData
        {
            [ProtoMember(1)]
            public long Timestamp { get; set; }
        }
    }
}
