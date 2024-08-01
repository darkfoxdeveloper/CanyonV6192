using Canyon.Game.States.User;
using Canyon.Network.Packets;
using static Canyon.Game.States.User.Character;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgNationality : MsgBase<Client>
    {
        public uint Identity { get; set; }
        public ushort Country { get; set; }

        public override byte[] Encode()
        {
            PacketWriter writer = new();
            writer.Write((ushort)PacketType.MsgNationality);
            writer.Write(Identity);
            writer.Write(Country);
            return writer.ToArray();
        }

        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Identity = reader.ReadUInt32();
            Country = reader.ReadUInt16();
        }

        public override Task ProcessAsync(Client client)
        {
            if (!Enum.IsDefined(typeof(PlayerCountry), (int)Country))
            {
                return Task.CompletedTask;
            }

            client.Character.Nationality = (PlayerCountry)Country;
            Identity = client.Character.Identity;
            return client.Character.BroadcastRoomMsgAsync(this, true);
        }
    }
}
