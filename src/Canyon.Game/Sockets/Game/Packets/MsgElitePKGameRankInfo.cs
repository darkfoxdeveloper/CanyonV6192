using Canyon.Game.Services.Managers;
using Canyon.Game.States.Events.Elite;
using Canyon.Game.States.User;
using Canyon.Network.Packets;
using Newtonsoft.Json;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgElitePKGameRankInfo : MsgBase<Client>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MsgElitePKGameRankInfo>();

        public int Mode { get; set; }
        public int Group { get; set; }
        public int GroupStatus { get; set; }
        public int Count { get; set; }
        public int Unknown20 { get; set; }
        public int Unknown24 { get; set; }
        public int Unknown28 { get; set; }
        public List<ElitePkRankStruct> Rank { get; set; } = new();

        public override void Decode(byte[] bytes)
        {
            using var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Mode = reader.ReadInt32(); // 4
            Group = reader.ReadInt32(); // 8
            GroupStatus = reader.ReadInt32(); // 12
            Count = reader.ReadInt32(); // 16
            Unknown20 = reader.ReadInt32(); 
            Unknown24 = reader.ReadInt32();
            Unknown28 = reader.ReadInt32();
        }

        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgElitePkGameRankInfo);
            writer.Write(Mode);
            writer.Write(Group);
            writer.Write(GroupStatus);
            writer.Write(Count = Rank.Count);
            writer.Write(Unknown20);
            writer.Write(Unknown24);
            writer.Write(Unknown28);
            foreach (var rank in Rank)
            {
                writer.Write(rank.Rank);
                writer.Write(rank.Name, 16);
                writer.Write(rank.Mesh);
                writer.Write(rank.Identity);
                writer.Write(0L);
            }
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            // Group 0 - <100
            // Group 1 - 100-119
            // Group 2 - 120-129
            // Group 3 - >=130
            Character user = client.Character;
            ElitePkTournament elitePkTournament = EventManager.GetEvent<ElitePkTournament>();
            if (elitePkTournament == null)
            {
                return;
            }

            switch (Mode)
            {
                case 0:
                    {
                        await elitePkTournament.SubmitEventWindowAsync(user, Group, 0);
                        break;
                    }
                default:
                    {
                        logger.LogWarning("Mode [{Action}] is not being handled.\n{Json}", Mode, JsonConvert.SerializeObject(this));
                        break;
                    }
            }
        }

        public struct ElitePkRankStruct
        {
            public int Rank { get; set; }
            public string Name { get; set; }
            public uint Mesh { get; set; }
            public uint Identity { get; set; }
        }
    }
}
