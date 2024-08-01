using Canyon.Network.Sockets;

namespace Canyon.Network.Packets.Ai
{
    public abstract class MsgAiPing<T> : MsgBase<T> where T : TcpServerActor
    {
        public int Timestamp { get; set; }
        public long TimestampMs { get; set; }
        public int RecvTimestamp { get; set; }
        public long RecvTimestampMs { get; set; }

        public override void Decode(byte[] bytes)
        {
            using var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Timestamp = reader.ReadInt32();
            TimestampMs = reader.ReadInt64();
            RecvTimestamp = reader.ReadInt32();
            RecvTimestampMs = reader.ReadInt64();
        }

        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgAiPing);
            writer.Write(Timestamp);
            writer.Write(TimestampMs);
            writer.Write(RecvTimestamp);
            writer.Write(RecvTimestampMs);
            return writer.ToArray();
        }
    }
}
