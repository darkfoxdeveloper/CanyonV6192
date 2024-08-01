using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgMailContent : MsgBase<Client>
    {
        public uint Data { get; set; }
        public string Content { get; set; }

        public override byte[] Encode()
        {
            using PacketWriter writer = new();
            writer.Write((ushort)PacketType.MsgMailContent);
            writer.Write(Data);
            writer.Write(Content, 768);
            return writer.ToArray();
        }
    }
}
