using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgGLHeroDetail : MsgBase<Client>
    {
        public byte Grade { get; set; }
        public byte Rank { get; set; }
        public byte TotalMatches { get; set; }
        public byte WinStreak { get; set; }
        public int TotalPoints { get; set; }
        public int Points { get; set; }

        public override void Decode(byte[] bytes)
        {
            using var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Grade = reader.ReadByte();
            Rank = reader.ReadByte();
            TotalMatches = reader.ReadByte();
            WinStreak = reader.ReadByte();
            TotalPoints = reader.ReadInt32();
            Points = reader.ReadInt32();
        }

        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgGLHeroDetail);
            writer.Write(Grade); // 4
            writer.Write(Rank); // 5
            writer.Write(TotalMatches); // 6
            writer.Write(WinStreak); // 7
            writer.Write(TotalPoints); // 8
            writer.Write(Points); // 12
            return writer.ToArray();
        }
    }
}
