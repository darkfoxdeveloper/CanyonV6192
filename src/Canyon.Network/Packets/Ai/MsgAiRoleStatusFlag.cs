using Canyon.Network.Sockets;

namespace Canyon.Network.Packets.Ai
{
    public abstract class MsgAiRoleStatusFlag<T> : MsgBase<T> where T : TcpServerActor
    {
        public int Mode { get; set; }
        public uint Identity { get; set; }
        public int Flag { get; set; }
        public int Steps { get; set; }
        public int Duration { get; set; }
        public uint Caster { get; set; }

        public override void Decode(byte[] bytes)
        {
            using var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Mode = reader.ReadInt32();
            Identity = reader.ReadUInt32();
            Flag = reader.ReadInt32();
            Steps = reader.ReadInt32();
            Duration = reader.ReadInt32();
            Caster = reader.ReadUInt32();
        }

        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgAiRoleStatusFlag);
            writer.Write(Mode);
            writer.Write(Identity);
            writer.Write(Flag);
            writer.Write(Steps);
            writer.Write(Duration);
            writer.Write(Caster);
            return writer.ToArray();
        }
    }
}
