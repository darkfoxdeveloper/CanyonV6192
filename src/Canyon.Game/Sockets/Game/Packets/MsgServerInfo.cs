using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgServerInfo : MsgBase<Client>
    {
        public int ClassicMode { get; set; }
        public int PotencyMode { get; set; }

        public override byte[] Encode()
        {
            using PacketWriter writer = new();
            writer.Write((ushort)PacketType.MsgServerInfo);
            writer.Write(ClassicMode);
            writer.Write(PotencyMode);
            return writer.ToArray();
        }
    }
}
