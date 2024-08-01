using Canyon.Network.Sockets;

namespace Canyon.Network.Packets.Login
{
    /// <remarks>Packet Type 1059</remarks>
    /// <summary>
    ///     Message sent to the game client on connect containing a random seed for generating
    ///     keys in the RC5 password cipher. This message is only used in patches after and
    ///     relative to 5174.
    /// </summary>
    public abstract class MsgEncryptCode<T> : MsgBase<T> where T : TcpServerActor
    {
        // Packet Properties
        public uint Seed { get; set; }

        /// <summary>
        ///     Encodes the packet structure defined by this message class into a byte packet
        ///     that can be sent to the client. Invoked automatically by the client's send
        ///     method. Encodes using byte ordering rules interoperable with the game client.
        /// </summary>
        /// <returns>Returns a byte packet of the encoded packet.</returns>
        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgEncryptCode);
            writer.Write(Seed);
            return writer.ToArray();
        }
    }
}
