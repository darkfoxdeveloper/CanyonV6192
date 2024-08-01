using Canyon.Network.Sockets;

namespace Canyon.Network.Packets.Ai
{
    public abstract class MsgAiAction<T> : MsgBase<T> where T : TcpServerActor
    {
        public AiActionType Action { get; set; }
        public uint Identity { get; set; }
        public ushort X { get; set; }
        public ushort Y { get; set; }
        public int Direction { get; set; }
        public uint TargetIdentity { get; set; }
        public ushort TargetX { get; set; }
        public ushort TargetY { get; set; }
        public long Timestamp { get; set; }

        public override void Decode(byte[] bytes)
        {
            using var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Action = (AiActionType)reader.ReadInt32();
            Identity = reader.ReadUInt32();
            X = reader.ReadUInt16();
            Y = reader.ReadUInt16();
            Direction = reader.ReadInt32();
            TargetIdentity = reader.ReadUInt32();
            TargetX = reader.ReadUInt16();
            TargetY = reader.ReadUInt16();
            Timestamp = reader.ReadInt64();
        }

        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgAiAction);
            writer.Write((int)Action);
            writer.Write(Identity);
            writer.Write(X);
            writer.Write(Y);
            writer.Write(Direction);
            writer.Write(TargetIdentity);
            writer.Write(TargetX);
            writer.Write(TargetY);
            writer.Write(Environment.TickCount64);
            return writer.ToArray();
        }
    }

    public enum AiActionType
    {
        None,
        RequestLogin,
        Walk,
        Run,
        Jump,
        SetDirection,
        SetAction,
        SynchroPosition,
        LeaveMap,
        FlyMap,
        SetProtection,
        ClearProtection,
        QueryRole,
        Shutdown
    }
}
