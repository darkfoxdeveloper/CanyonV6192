using Canyon.Game.States;
using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgTrainingVitalityExpiryNotify : MsgBase<Client>
    {
        public int Count => Fates.Count;
        public List<Fate.FateType> Fates { get; private set; } = new();

        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgTrainingVitalityExpiryNotify);
            writer.Write(Count);
            foreach (var fate in Fates)
            {
                writer.Write((byte)fate);
            }
            return writer.ToArray();
        }
    }
}
