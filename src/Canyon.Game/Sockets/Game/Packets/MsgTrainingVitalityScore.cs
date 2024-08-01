using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgTrainingVitalityScore : MsgBase<Client>
    {
        public byte AttrType { get; set; }
        public int Power { get; set; }
        public string Name { get; set; }

        public override byte[] Encode()
        {
            PacketWriter writer = new();
            writer.Write((ushort)PacketType.MsgTrainingVitalityScore);
            writer.Write(AttrType);
            writer.Write(Power);
            writer.Write(Name, 16);
            return writer.ToArray();
        }
    }
}
