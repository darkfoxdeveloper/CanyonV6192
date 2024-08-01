using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgMailNotify : MsgBase<Client>
    {
        public MailNotification Action { get; set; }

        public override byte[] Encode()
        {
            using PacketWriter writer = new();
            writer.Write((ushort)PacketType.MsgMailNotify);
            writer.Write((int)Action);
            return writer.ToArray();
        }

        public enum MailNotification : ushort
        {
            DeletionFailed = 1,
            Notification = 3,
            UnreadMail = 4
        }
    }
}
