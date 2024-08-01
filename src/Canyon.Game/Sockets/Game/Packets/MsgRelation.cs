using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgRelation : MsgBase<Client>
    {
        public int BattlePower { get; set; }
        public bool IsSpouse { get; set; }
        public bool IsTradePartner { get; set; }
        public bool IsTutor { get; set; }
        public int Level { get; set; }
        public uint SenderIdentity { get; set; }
        public uint TargetIdentity { get; set; }

        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgRelation);
            writer.Write(SenderIdentity);
            writer.Write(TargetIdentity);
            writer.Write(Level);
            writer.Write(BattlePower);
            writer.Write(IsSpouse ? 1 : 0);
            writer.Write(IsTutor ? 1 : 0);
            writer.Write(IsTradePartner ? 1 : 0);
            return writer.ToArray();
        }
    }
}
