using Canyon.Database.Entities;
using Canyon.Game.Database.Repositories;
using Canyon.Game.Services.Managers;
using Canyon.Game.States.Events.Qualifier.UserQualifier;
using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgQualifyingSeasonRankList : MsgBase<Client>
    {
        public List<QualifyingSeasonRankStruct> Members = new();
        public int Count { get; set; }

        public override void Decode(byte[] bytes)
        {
            using var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Count = reader.ReadInt32();
        }

        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgQualifyingSeasonRankList);
            writer.Write(Count = Members.Count);
            foreach (QualifyingSeasonRankStruct member in Members)
            {
                writer.Write(member.Identity);
                writer.Write(member.Name, 16);
                writer.Write(member.Mesh);
                writer.Write(member.Level);
                writer.Write(member.Profession);
                writer.Write(member.Unknown);
                writer.Write(member.Rank);
                writer.Write(member.Score);
                writer.Write(member.Win);
                writer.Write(member.Lose);
            }

            return writer.ToArray();
        }

        public struct QualifyingSeasonRankStruct
        {
            public uint Identity { get; set; }
            public string Name { get; set; }
            public uint Mesh { get; set; }
            public int Level { get; set; }
            public int Profession { get; set; }
            public int Unknown { get; set; }
            public int Rank { get; set; }
            public int Score { get; set; }
            public int Win { get; set; }
            public int Lose { get; set; }
        }

        public override async Task ProcessAsync(Client client)
        {
            ArenaQualifier qualifier = EventManager.GetEvent<ArenaQualifier>();
            if (qualifier == null) 
            {
                await client.SendAsync(this);
                return;    
            }

            var rank = qualifier.GetSeasonRank();
            ushort pos = 1;
            foreach (var obj in rank)
            {
                Members.Add(new QualifyingSeasonRankStruct
                {
                    Rank = pos++,
                    Identity = obj.UserId,
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
