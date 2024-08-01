using Canyon.Network.Sockets;

namespace Canyon.Network.Packets.Ai
{
    public abstract class MsgAiPlayerLogout<T> : MsgBase<T> where T : TcpServerActor
    {
        public int Timestamp { get; set; }
        public uint Id { get; set; }

        public override void Decode(byte[] bytes)
        {
            using var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Timestamp = reader.ReadInt32();
            Id = reader.ReadUInt32();
        }

        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgAiPlayerLogout);
            writer.Write(Timestamp);
            writer.Write(Id);
            return writer.ToArray();
        }
    }
}
