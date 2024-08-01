using Canyon.Game.States;
using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgTrainingVitalityProtectInfo : MsgBase<Client>
    {
        public int Count { get; set; }
        public List<ProtectInfo> Protects { get; set; } = new();

        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgTrainingVitalityProtectInfo);
            writer.Write(Count = Protects.Count);
            foreach (var protect in Protects)
            {
                writer.Write((byte)protect.FateType);
                writer.Write(protect.Seconds);
                writer.Write(protect.Attribute1);
                writer.Write(protect.Attribute2);
                writer.Write(protect.Attribute3);
                writer.Write(protect.Attribute4);
            }
            return writer.ToArray();
        }

        public struct ProtectInfo
        {
            public Fate.FateType FateType { get; set; }
            public int Seconds { get; set; }
            public int Attribute1 { get; set; }
            public int Attribute2 { get; set; }
            public int Attribute3 { get; set; }
            public int Attribute4 { get; set; }
        }
    }
}
