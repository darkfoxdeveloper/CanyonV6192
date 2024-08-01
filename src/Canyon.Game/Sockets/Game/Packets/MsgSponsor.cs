using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgSponsor : MsgBase<Client>
    {
        public WageAction Action { get; set; }
        public ushort Response { get; set; }
        public int Amount { get; set; }
        public List<MatchWage> Wages { get; set; } = new();

        public override void Decode(byte[] bytes)
        {
            using var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Action = (WageAction)reader.ReadUInt16();
            Response = reader.ReadUInt16();
            Amount = reader.ReadInt32();
            for (int i = 0; i < Wages.Count; i++)
            {
                uint playerOne = reader.ReadUInt32();
                uint playerTwo = reader.ReadUInt32();
                int wageOne = reader.ReadInt32();
                int wageTwo = reader.ReadInt32();

                Wages.Add(new MatchWage
                {
                    PlayerOne = playerOne,
                    PlayerTwo = playerTwo,
                    WagerOne = wageOne,
                    WagerTwo = wageTwo
                });
            }
        }

        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgSponsor);
            writer.Write((ushort)Action);
            writer.Write(Response);
            writer.Write(Amount = Wages.Count);
            foreach (var wage in Wages)
            {
                writer.Write(wage.PlayerOne);
                writer.Write(wage.PlayerTwo);
                writer.Write(wage.WagerOne);
                writer.Write(wage.WagerTwo);
            }
            return writer.ToArray();
        }

        public override Task ProcessAsync(Client client)
        {
            return Task.CompletedTask;
        }

        public struct MatchWage
        {
            public uint PlayerOne { get; set; }
            public uint PlayerTwo { get; set; }
            public int WagerOne { get; set; }
            public int WagerTwo { get; set; }
        }

        public enum WageAction : uint
        {
            AddWage,
            Unknown1,
            RequestList,
        }
    }
}
