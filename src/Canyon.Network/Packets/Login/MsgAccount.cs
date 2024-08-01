using Canyon.Network.Sockets;
using System.Text;

namespace Canyon.Network.Packets.Login
{
    public abstract class MsgAccount<T> : MsgBase<T> where T : TcpServerActor
    {
        // Packet Properties
        public string Username { get; private set; }
        public byte[] Password { get; private set; }
        public string Realm { get; private set; }

        /// <summary>
        ///     Decodes a byte packet into the packet structure defined by this message class.
        ///     Should be invoked to structure data from the client for processing. Decoding
        ///     follows TQ Digital's byte ordering rules for an all-binary protocol.
        /// </summary>
        /// <param name="bytes">Bytes from the packet processor or client socket</param>
        public override void Decode(byte[] bytes)
        {
            using var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            reader.BaseStream.Seek(8, SeekOrigin.Begin);
            Username = reader.ReadString(32);
            reader.BaseStream.Seek(60, SeekOrigin.Begin);
            Password = reader.ReadBytes(32);
            reader.BaseStream.Seek(136, SeekOrigin.Begin);
            Realm = reader.ReadString(16);
        }

        /// <summary>
        ///     Decrypts the password from read in packet bytes for the <see cref="Decode" />
        ///     method. Trims the end of the password string of null terminators.
        /// </summary>
        /// <param name="buffer">Bytes from the packet buffer</param>
        /// <param name="seed">Seed for generating RC5 keys</param>
        /// <returns>Returns the decrypted password string.</returns>
        protected string DecryptPassword(byte[] buffer, uint seed)
        {
            // debug purposes
            byte[] pSeed =
            {
                46, 22, 32, 87, 95, 48, 8, 2, 4, 34, 59, 83, 21, 2, 243, 1, 1, 2, 80, 37, 202, 31, 99, 75, 7, 4, 6, 23, 100, 221, 82, 134
            };
            int length = pSeed.Length;
            for (int x = 0; x < buffer.Length; x++)
            {
                if (buffer[x] != 0)
                {
                    buffer[x] ^= pSeed[(x * 48 % 32) % length];
                    buffer[x] ^= pSeed[(x * 24 % 16) % length];
                    buffer[x] ^= pSeed[(x * 12 % 8) % length];
                    buffer[x] ^= pSeed[(x * 6 % 4) % length];
                }
            }
            return Encoding.ASCII.GetString(buffer).Trim('\0');
        }
    }
}
