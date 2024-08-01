namespace Canyon.Network.Packets.Ai
{
    public abstract class MsgAiLoginExchangeEx<T> : MsgBase<T>
    {
        public AiLoginResult Result { get; set; }
        public string Message { get; set; } = "";

        public override void Decode(byte[] bytes)
        {
            using PacketReader reader = new(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Result = (AiLoginResult)reader.ReadInt32();
            Message = reader.ReadString(128);
        }

        public override byte[] Encode()
        {
            using PacketWriter writer = new();
            writer.Write((ushort)PacketType.MsgAiLoginExchangeEx);
            writer.Write((int)Result);
            writer.Write(Message, 128);
            return writer.ToArray();
        }

        public enum AiLoginResult
        {
            Success,
            AlreadySignedIn,
            InvalidPassword,
            InvalidAddress,
            AlreadyBound,
            UnknownError
        }
    }
}
