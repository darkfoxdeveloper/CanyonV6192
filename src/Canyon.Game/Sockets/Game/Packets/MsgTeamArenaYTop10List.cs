using Canyon.Game.Services.Managers;
using Canyon.Game.States.Events.Qualifier.TeamQualifier;
using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgTeamArenaYTop10List : MsgBase<Client>
    {
        public int Data { get; set; }
        public List<QualifyingSeasonRankStruct> Members { get; set; } = new();

        public override void Decode(byte[] bytes)
        {
            using PacketReader reader = new(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Data = reader.ReadInt32();
        }

        public override byte[] Encode()
        {
            using PacketWriter writer = new();
            writer.Write((ushort)PacketType.MsgTeamArenaYTop10List);
            writer.Write(Data);
            foreach (var rank in Members.Take(10))
            {
                writer.Write(rank.Name, 16);
                writer.Write(rank.Rank); // 16
                writer.Write(rank.Mesh); // 20
                writer.Write(rank.Profession); // 24
                writer.Write(rank.Level); // 28
                writer.Write(rank.Score); // 32
                writer.Write(rank.Win); // 36
                writer.Write(rank.Lose); // 40
            }
            return writer.ToArray();
        }

        public struct QualifyingSeasonRankStruct
        {
            public string Name { get; set; }
            public uint Mesh { get; set; }
            public int Level { get; set; }
            public int Profession { get; set; }
            public int Rank { get; set; }
            public int Score { get; set; }
            public int Win { get; set; }
            public int Lose { get; set; }
        }

        public override async Task ProcessAsync(Client client)
        {
            TeamArenaQualifier qualifier = EventManager.GetEvent<TeamArenaQualifier>();
            if (qualifier == null)
            {
                await client.SendAsync(this);
                return;
            }

            var rank = qualifier.GetSeasonRank();
            ushort pos = 1;
            Data = rank.Count;
            foreach (var obj in rank)
            {
                Members.Add(new QualifyingSeasonRankStruct
                {
                    Rank = pos++,
                    Name = obj.Name,
                    Level = obj.Level,
                    Profession = obj.Profession,
                    Win = (int)obj.DayWins,
                    Lose = (int)obj.DayLoses,
                    Mesh = obj.Mesh,
                    Score = (int)obj.AthletePoint
                });
            }

            await client.SendAsync(this);
        }
    }
}
