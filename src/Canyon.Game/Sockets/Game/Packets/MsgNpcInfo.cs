using Canyon.Game.States;
using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgNpcInfo : MsgBase<Client>
    {
        public int Timestamp { get; set; }
        public uint Identity { get; set; }
        public ushort PosX { get; set; }
        public ushort PosY { get; set; }
        public ushort Lookface { get; set; }
        public ushort NpcType { get; set; }
        public ushort Sort { get; set; }
        public uint Unknown12 { get; set; }
        public string Name { get; set; }

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
            Timestamp = reader.ReadInt32();
            Identity = reader.ReadUInt32();
            Unknown12 = reader.ReadUInt32();
            PosX = reader.ReadUInt16();
            PosY = reader.ReadUInt16();
            Lookface = reader.ReadUInt16();
            NpcType = reader.ReadUInt16();
            byte temp = reader.ReadByte();
            List<string> names = reader.ReadStrings();
            if (names.Count > 0)
            {
                Name = names[0];
            }
        }

        /// <summary>
        ///     Encodes the packet structure defined by this message class into a byte packet
        ///     that can be sent to the client. Invoked automatically by the client's send
        ///     method. Encodes using byte ordering rules interoperable with the game client.
        /// </summary>
        /// <returns>Returns a byte packet of the encoded packet.</returns>
        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgNpcInfo);
            writer.Write(Timestamp); // 4
            writer.Write(Identity); // 8
            writer.Write(Unknown12); // 12
            writer.Write(PosX); // 16
            writer.Write(PosY); // 18
            writer.Write(Lookface); // 20            
            writer.Write(NpcType); // 22
            //writer.Write(Sort); // 24
            writer.Write(Name); // 26
            return writer.ToArray();
        }

        public override Task ProcessAsync(Client client)
        {
            return GameAction.ExecuteActionAsync(client.Character.InteractingItem, client.Character, null, null,
                $"{PosX} {PosY} {Lookface} {Identity} {NpcType}");
        }
    }
}
