using Canyon.Game.States.Items;
using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgMeteSpecial : MsgBase<Client>
    {
        public int Profession { get; set; }
        public int Body { get; set; }

        public override void Decode(byte[] bytes)
        {
            using var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            reader.ReadInt32();
            Profession = reader.ReadInt32();
            Body = reader.ReadInt32();
        }

        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgMeteSpecial);
            writer.Write(Environment.TickCount);
            writer.Write(Profession);
            writer.Write(Body);
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;
            if (user == null)
            {
                return;
            }

            if (user.Metempsychosis < 2)
            {
                await user.SendAsync(StrMeteSpecialMetempsychosisErr);
                return;
            }

            if (user.Level < 110)
            {
                await user.SendAsync(StrMeteSpecialLevelErr);
                return;
            }

            if (user.Gender == 1) // Male
            {
                if (Body < 3)
                {
                    await user.SendAsync(StrMeteSpecialWrongBodyErr);
                    return;
                }
            }
            else if (user.Gender == 2)
            {
                if (Body > 2)
                {
                    await user.SendAsync(StrMeteSpecialWrongBodyErr);
                    return;
                }
            }
            else
            {
                await user.SendAsync(StrMeteSpecialInvalidGenderErr);
                return;
            }

            if (user.UserPackage.GetItemByType(Item.OBLIVION_DEW) == null)
            {
                await user.SendAsync(StrMeteSpecialOblivionDewErr);
                return;
            }

            if (await user.ReincarnateAsync((ushort)Profession, (ushort)Body))
            {
                await user.UserPackage.SpendItemAsync(Item.OBLIVION_DEW);
                await user.SendAsync(string.Format(StrLinePkSuccess, user.Name));
            }
        }
    }
}
