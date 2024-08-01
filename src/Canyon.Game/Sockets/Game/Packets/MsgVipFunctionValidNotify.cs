using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgVipFunctionValidNotify : MsgBase<Client>
    {
        public int Flags { get; set; }

        public override byte[] Encode()
        {
            PacketWriter writer = new();
            writer.Write((ushort)PacketType.MsgVipFunctionValidNotify);
            writer.Write(Flags);
            return writer.ToArray();
        }
    }
}
