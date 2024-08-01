using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgCheatingProgram : MsgBase<Client>
    {
        public MsgCheatingProgram(uint id, string message)
        {
            Identity = id;
            Messages.Add(message);
        }

        public uint Identity { get; set; }
        public List<string> Messages { get; set; } = new();

        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgCheatingProgram);
            writer.Write(Identity);
            writer.Write(Messages);
            return writer.ToArray();
        }
    }
}
