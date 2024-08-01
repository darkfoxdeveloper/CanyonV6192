using Canyon.Database.Entities;
using Canyon.Game.Database.Repositories;
using Canyon.Game.Services.Managers;
using Canyon.Game.States.Events.Qualifier.TeamQualifier;
using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgTeamArenaRank : MsgBase<Client>
    {
        public ushort Page { get; set; }
        public ushort Count { get; set; }
        public int Data { get; set; }
        public List<TeamArenaRankStruct> Ranks { get; set; } = new();

        public override void Decode(byte[] bytes)
        {
            using PacketReader reader = new(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Page = reader.ReadUInt16();
            Count = reader.ReadUInt16();
            Data = reader.ReadUInt16();
        }

        public override byte[] Encode()
        {
            using PacketWriter writer = new();
            writer.Write((ushort)PacketType.MsgTeamArenaRank);
            writer.Write(Page);
            writer.Write(Count);
            writer.Write(Data);
            foreach (var rank in Ranks)
            {
                writer.Write(rank.Rank);
                writer.Write(rank.RankType);
                writer.Write(rank.Score);
                writer.Write(rank.Profession);
                writer.Write(rank.Level);
                writer.Write(rank.Gender);
                writer.Write(rank.Name, 16);
                writer.Write(rank.Unknown);
                writer.Write(rank.UnknownUS);
            }
            return writer.ToArray();
        }

        public struct TeamArenaRankStruct
        {
            public int Rank { get; set; }
            public int RankType => 2979015;
            public int Score { get; set; }
            public int Profession { get; set; }
            public int Level { get; set; }
            public byte Gender { get; set; }
            public string Name { get; set; }
            public byte Unknown { get; set; }
            public ushort UnknownUS { get; set; }
        }

        public override async Task ProcessAsync(Client client)
        {
            TeamArenaQualifier qualifier = EventManager.GetEvent<TeamArenaQualifier>();
            if (qualifier == null)
            {
                await client.SendAsync(this);
                return;
            }

            Character user = client.Character;
            int page = Math.Min(0, Page - 1);

            var players = qualifier.GetRanking(page);
            Count = (ushort)players.Count;
            Data = qualifier.RankCount();
            int position = (Page - 1) * 10 + 1;
            foreach (var player in players)
            {
                Ranks.Add(new TeamArenaRankStruct
                {
                    Rank = position++,
                    Gender = (byte)(player.Mesh % 10000 / 1000),
                    Level = player.Level,
                    Name = player.Name,
                    Profession = player.Profession,
                    Score = (int)player.AthletePoint
                });
            }
            await user.SendAsync(this);
        }
    }
}
