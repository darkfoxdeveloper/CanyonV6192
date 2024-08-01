using Canyon.Network.Sockets;

namespace Canyon.Network.Packets.Login
{
    public abstract class MsgPCNum<T> : MsgBase<T> where T : TcpServerActor
    {
        public uint AccountIdentity;
        public string MacAddress;

        public override void Decode(byte[] bytes)
        {
            using PacketReader reader = new(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            AccountIdentity = reader.ReadUInt32();
            MacAddress = reader.ReadString(12);
        }

        public override byte[] Encode()
        {
            using PacketWriter writer = new();
            writer.Write((ushort)PacketType.MsgPCNum);
            writer.Write(AccountIdentity);
            writer.Write(MacAddress, 12);
            return writer.ToArray();
        }
    }
}
