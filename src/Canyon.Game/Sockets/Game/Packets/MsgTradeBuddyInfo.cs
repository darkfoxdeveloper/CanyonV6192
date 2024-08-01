using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgTradeBuddyInfo : MsgBase<Client>
    {
        public uint Identity { get; set; }
        public byte Level { get; set; }
        public uint Lookface { get; set; }
        public string Name { get; set; }
        public ushort PkPoints { get; set; }
        public byte Profession { get; set; }
        public uint Syndicate { get; set; }
        public int SyndicatePosition { get; set; }
        public ushort Unknown { get; set; }

        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgTradeBuffyInfo);
            writer.Write(Identity);
            writer.Write(Lookface);
            writer.Write(Level);
            writer.Write(Profession);
            writer.Write(PkPoints);
            writer.Write(Syndicate);
            writer.Write(SyndicatePosition);
            writer.Write(Unknown);
            writer.Write(Name, 16);
            return writer.ToArray();
        }
    }
}
