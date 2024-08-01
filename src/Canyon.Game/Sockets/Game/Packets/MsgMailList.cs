using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgMailList : MsgBase<Client>
    {
        public int Count { get; set; }
        public int Page { get; set; }
        public ushort Unknown { get; set; }
        public ushort MaxPages { get; set; }
        public List<MailListStruct> MailList { get; set; } = new List<MailListStruct>();

        public override void Decode(byte[] bytes)
        {
            using PacketReader reader = new(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Count = reader.ReadInt32();
            Page = reader.ReadInt32();
            Unknown = reader.ReadUInt16();
            MaxPages = reader.ReadUInt16();
        }

        public override byte[] Encode()
        {
            using PacketWriter writer = new();
            writer.Write((ushort)PacketType.MsgMailList);
            writer.Write(Count = MailList.Count);
            writer.Write(Page);
            //writer.Write(Unknown);
            writer.Write((int)MaxPages);
            foreach (var mail in MailList)
            {
                writer.Write(mail.EmailIdentity);
                writer.Write(mail.SenderName, 32);
                writer.Write(mail.Header, 32);
                writer.Write(mail.Money);
                writer.Write(mail.ConquerPoints);
                writer.Write(mail.Timestamp);
                writer.Write(mail.HasAttachment);
                writer.Write(mail.ItemType);
            }
            return writer.ToArray();
        }

        public struct MailListStruct
        {
            public uint EmailIdentity { get; set; }
            public string SenderName { get; set; }
            public string Header { get; set; }
            public uint Money { get; set; }
            public uint ConquerPoints { get; set; }
            public int Timestamp { get; set; }
            public int HasAttachment { get; set; }
            public uint ItemType { get; set; }
        }

        public override Task ProcessAsync(Client client)
        {
            return client.Character.MailBox.SendListAsync(Page);
        }
    }
}
