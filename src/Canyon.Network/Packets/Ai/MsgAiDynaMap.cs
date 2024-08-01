using Canyon.Network.Sockets;

namespace Canyon.Network.Packets.Ai
{
    public abstract class MsgAiDynaMap<T> : MsgBase<T> where T : TcpServerActor
    {
        public int Mode { get; set; }
        public uint Identity { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public uint MapDoc { get; set; }
        public ulong MapType { get; set; }
        public uint OwnerIdentity { get; set; }
        public uint MapGroup { get; set; }
        public int ServerIndex { get; set; }
        public uint Weather { get; set; }
        public uint BackgroundMusic { get; set; }
        public uint BackgroundMusicShow { get; set; }
        public uint PortalX { get; set; }
        public uint PortalY { get; set; }
        public uint RebornMap { get; set; }
        public uint RebornPortal { get; set; }
        public byte ResourceLevel { get; set; }
        public byte OwnerType { get; set; }
        public uint LinkMap { get; set; }
        public ushort LinkX { get; set; }
        public ushort LinkY { get; set; }
        public uint Color { get; set; }
        public uint InstanceType { get; set; }
        public uint InstanceMapId { get; set; }

        public override void Decode(byte[] bytes)
        {
            using var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Mode = reader.ReadInt32();
            Identity = reader.ReadUInt32();
            Name = reader.ReadString(NAME_LENGTH);
            Description = reader.ReadString(DESCRIBE_LENGTH);
            MapDoc = reader.ReadUInt32();
            MapType = reader.ReadUInt64();
            OwnerIdentity = reader.ReadUInt32();
            MapGroup = reader.ReadUInt32();
            ServerIndex = reader.ReadInt32();
            Weather = reader.ReadUInt32();
            BackgroundMusic = reader.ReadUInt32();
            BackgroundMusicShow = reader.ReadUInt32();
            PortalX = reader.ReadUInt32();
            PortalY = reader.ReadUInt32();
            RebornMap = reader.ReadUInt32();
            RebornPortal = reader.ReadUInt32();
            ResourceLevel = reader.ReadByte();
            OwnerType = reader.ReadByte();
            LinkMap = reader.ReadUInt32();
            LinkX = reader.ReadUInt16();
            LinkY = reader.ReadUInt16();
            Color = reader.ReadUInt32();
            InstanceType = reader.ReadUInt32();
            InstanceMapId = reader.ReadUInt32();
        }

        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgAiDynaMap);
            writer.Write(Mode);
            writer.Write(Identity);
            writer.Write(Name, NAME_LENGTH);
            writer.Write(Description, DESCRIBE_LENGTH);
            writer.Write(MapDoc);
            writer.Write(MapType);
            writer.Write(OwnerIdentity);
            writer.Write(MapGroup);
            writer.Write(ServerIndex);
            writer.Write(Weather);
            writer.Write(BackgroundMusic);
            writer.Write(BackgroundMusicShow);
            writer.Write(PortalX);
            writer.Write(PortalY);
            writer.Write(RebornMap);
            writer.Write(RebornPortal);
            writer.Write(ResourceLevel);
            writer.Write(OwnerType);
            writer.Write(LinkMap);
            writer.Write(LinkX);
            writer.Write(LinkY);
            writer.Write(Color);
            writer.Write(InstanceType);
            writer.Write(InstanceMapId);
            return writer.ToArray();
        }

        private const int NAME_LENGTH = 105;
        private const int DESCRIBE_LENGTH = 381;
    }
}
