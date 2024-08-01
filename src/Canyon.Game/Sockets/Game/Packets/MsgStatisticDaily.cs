using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgStatisticDaily : MsgBase<Client>
    {
        public byte Mode { get; set; }
        public int EventId { get; set; }
        public int DataType { get; set; }
        public int ActivityPoints { get; set; }
        public int Unknown17 { get; set; }
        public int Unknown21 { get; set; }

        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgStatisticDaily);
            writer.Write(Mode);
            writer.Write(EventId);
            writer.Write(DataType);
            writer.Write(ActivityPoints);
            writer.Write(Unknown17);
            writer.Write(Unknown21);
            return writer.ToArray();
        }
    }
}
