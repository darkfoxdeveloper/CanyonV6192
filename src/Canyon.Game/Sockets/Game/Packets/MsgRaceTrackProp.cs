using Canyon.Game.States;
using Canyon.Game.States.Events.Mount;
using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgRaceTrackProp : MsgBase<Client>
    {
        public ushort Amount { get; set; }
        public HorseRacing.ItemType PotionType { get; set; }
        public int Index { get; set; }
        public int Data { get; set; }

        public override void Decode(byte[] bytes)
        {
            using var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Amount = reader.ReadUInt16(); // 4
            PotionType = (HorseRacing.ItemType)reader.ReadUInt16(); // 6
            Index = reader.ReadInt32(); // 8
            Data = reader.ReadInt32(); // 12
        }

        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgRaceTrackProp);
            writer.Write(Amount); // 4
            writer.Write((ushort)PotionType); // 8
            writer.Write(Index); // 6
            writer.Write(Data); // 12
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            Role target = null;
            if (Data != 0)
            {
                target = client.Character.Map.QueryAroundRole(client.Character, (uint)Data);
            }

            await client.Character.SpendRaceItemAsync(Index - 1, target);
        }
    }
}
