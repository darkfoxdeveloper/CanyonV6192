namespace Canyon.Network.Packets.Ai
{
    public abstract class MsgAiInteract<T> : MsgBase<T>
    {
        public int Timestamp { get; set; }
        public AiInteractAction Action { get; set; }
        public uint Identity { get; set; }
        public uint TargetIdentity { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public ushort MagicType { get; set; }
        public ushort MagicLevel { get; set; }
        public int Data { get; set; }

        public override void Decode(byte[] bytes)
        {
            using var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Timestamp = reader.ReadInt32();
            Action = (AiInteractAction)reader.ReadInt32();
            Identity = reader.ReadUInt32();
            TargetIdentity = reader.ReadUInt32();
            X = reader.ReadInt32();
            Y = reader.ReadInt32();
            MagicType = reader.ReadUInt16();
            MagicLevel = reader.ReadUInt16();
            Data = reader.ReadInt32();
        }

        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgAiInteract);
            writer.Write(Timestamp = Environment.TickCount);
            writer.Write((int) Action);
            writer.Write(Identity);
            writer.Write(TargetIdentity);
            writer.Write(X);
            writer.Write(Y);
            writer.Write(MagicType);
            writer.Write(MagicLevel);
            writer.Write(Data);
            return writer.ToArray();
        }
    }

    public enum AiInteractAction
    {
        None,
        Attack,
        MagicAttack,
        MagicAttackWarning
    }
}
