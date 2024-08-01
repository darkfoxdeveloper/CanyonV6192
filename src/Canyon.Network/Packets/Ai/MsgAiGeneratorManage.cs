using Canyon.Network.Sockets;

namespace Canyon.Network.Packets.Ai
{
    public abstract class MsgAiGeneratorManage<T> : MsgBase<T> where T : TcpServerActor
    {
        public uint MapId { get; set; }
        public ushort BoundX { get; set; }
        public ushort BoundY { get; set; }
        public ushort BoundCx { get; set; }
        public ushort BoundCy { get; set; }
        public int MaxNpc { get; set; }
        public int RestSecs { get; set; }
        public int MaxPerGen { get; set; }
        public uint Npctype { get; set; }
        public int TimerBegin { get; set; }
        public int TimerEnd { get; set; }
        public int BornX { get; set; }
        public int BornY { get; set; }

        public override void Decode(byte[] bytes)
        {
            using var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            MapId = reader.ReadUInt32();
            BoundX = reader.ReadUInt16();
            BoundY = reader.ReadUInt16();
            BoundCx = reader.ReadUInt16();
            BoundCy = reader.ReadUInt16();
            MaxNpc = reader.ReadInt32();
            RestSecs = reader.ReadInt32();
            MaxPerGen = reader.ReadInt32();
            Npctype = reader.ReadUInt32();
            TimerBegin = reader.ReadInt32();
            TimerEnd = reader.ReadInt32();
            BornX = reader.ReadInt32();
            BornY = reader.ReadInt32();
        }

        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgAiGeneratorManage);
            writer.Write(MapId);
            writer.Write(BoundX);
            writer.Write(BoundY);
            writer.Write(BoundCx);
            writer.Write(BoundCy);
            writer.Write(MaxNpc);
            writer.Write(RestSecs);
            writer.Write(MaxPerGen);
            writer.Write(Npctype);
            writer.Write(TimerBegin);
            writer.Write(TimerEnd);
            writer.Write(BornX);
            writer.Write(BornY);
            return writer.ToArray();
        }
    }
}
