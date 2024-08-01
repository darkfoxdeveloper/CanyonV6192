using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgElitePKArenic : MsgBase<Client>
    {
        public ArenicAction Action { get; set; }
        public EffectType Effect { get; set; }
        public uint Identity { get; set; }
        public uint Unknown16 { get; set; }
        public string Name { get; set; } = string.Empty;
        public uint Unknown36 { get; set; }
        public uint Unknown40 { get; set; }
        public uint Unknown44 { get; set; }
        public int TimeLeft { get; set; }

        public override void Decode(byte[] bytes)
        {
            using var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Action = (ArenicAction)reader.ReadInt32();
            Effect = (EffectType)reader.ReadUInt32();
            Identity = reader.ReadUInt32();
            Unknown16 = reader.ReadUInt32();
            Name = reader.ReadString(16);
            Unknown36 = reader.ReadUInt32();
            Unknown40 = reader.ReadUInt32();
            Unknown44 = reader.ReadUInt32();
            TimeLeft = reader.ReadInt32();
        }

        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgElitePKArenic);
            writer.Write((int)Action);
            writer.Write((int)Effect);
            writer.Write(Identity);
            writer.Write(Unknown16);
            writer.Write(Name, 16);
            writer.Write(Unknown36);
            writer.Write(Unknown40);
            writer.Write(Unknown44);
            writer.Write(TimeLeft);
            return writer.ToArray();
        }

        public enum ArenicAction
        {
            None = 0,
            Information,
            BeginMatch,
            Effect,
            EndMatch
        }

        public enum EffectType
        {
            Defeat,
            Victory
        }

    }
}
