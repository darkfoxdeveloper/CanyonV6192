using Canyon.Network.Sockets;

namespace Canyon.Network.Packets.Ai
{
    public abstract class MsgAiRoleLogin<T> : MsgBase<T> where T : TcpServerActor
    {
        public int Timestamp { get; set; }
        public RoleLoginNpcType NpcType { get; set; }
        /// <remarks>Must be 0 if no processing at NPC Server.</remarks>
        public int Generator { get; set; }
        public uint Identity { get; set; }
        public string Name { get; set; }
        /// <remarks>May be Monster Type ID for Monster and Call Pet.</remarks>
        public int LookFace { get; set; }
        public uint MapId { get; set; }
        public ushort MapX { get; set; }
        public ushort MapY { get; set; }

        /// <inheritdoc />
        public override byte[] Encode()
        {
            using PacketWriter writer = new();
            writer.Write((ushort)PacketType.MsgAiRoleLogin);
            writer.Write(Environment.TickCount);
            writer.Write((int)NpcType);
            writer.Write(Generator);
            writer.Write(Identity);
            writer.Write(Name, 16);
            writer.Write(LookFace);
            writer.Write(MapId);
            writer.Write(MapX);
            writer.Write(MapY);
            return writer.ToArray();
        }

        /// <inheritdoc />
        public override void Decode(byte[] bytes)
        {
            using PacketReader reader = new(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Timestamp = reader.ReadInt32();
            NpcType = (RoleLoginNpcType)reader.ReadInt32();
            Generator = reader.ReadInt32();
            Identity = reader.ReadUInt32();
            Name = reader.ReadString(16);
            LookFace = reader.ReadInt32();
            MapId = reader.ReadUInt32();
            MapX = reader.ReadUInt16();
            MapY = reader.ReadUInt16();
        }
    }

    public enum RoleLoginNpcType
    {
        None,
        Monster,
        CallPet,
        Npc,
        DynamicNpc
    }
}
