using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgDeadMark : MsgBase<Client>
    {
        public DeadMarkAction Action { get; set; }
        public uint TargetIdentity { get; set; }

        public override byte[] Encode()
        {
            using PacketWriter writer = new();
            writer.Write((ushort)PacketType.MsgDeadMark);
            writer.Write((int)Action);
            writer.Write(TargetIdentity);
            return writer.ToArray();
        }

        public enum DeadMarkAction
        {
            Add,
            Remove
        }
    }
}
