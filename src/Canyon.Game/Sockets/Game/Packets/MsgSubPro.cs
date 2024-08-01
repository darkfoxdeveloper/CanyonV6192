using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgSubPro : MsgBase<Client>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MsgSubPro>();

        public int Timestamp { get; set; }
        public AstProfAction Action { get; set; }
        public ulong Points { get; set; }
        public AstProfType Class
        {
            get => (AstProfType)Points;
            set => Points = (ulong)value;
        }
        public byte Level
        {
            get => BitConverter.GetBytes(Points)[1];
            set
            {
                byte[] val = BitConverter.GetBytes(Points);
                val[1] = value;
                Points = BitConverter.ToUInt64(val);
            }
        }
        public ulong Study { get; set; }
        public ulong AwardedStudy { get; set; }
        public int Count { get; set; }
        public List<AstProfStruct> Professions { get; set; } = new List<AstProfStruct>();

        public override void Decode(byte[] bytes)
        {
            using PacketReader reader = new(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Timestamp = reader.ReadInt32(); // 4
            Action = (AstProfAction)reader.ReadUInt16(); // 8
            Points = reader.ReadUInt64(); // 10
            Study = reader.ReadUInt64(); // 18
            Count = reader.ReadByte(); // 26
            for (int i = 0; i < Count; i++)
            {
                Professions.Add(new AstProfStruct
                {
                    Class = (AstProfType)reader.ReadByte(),
                    Level = reader.ReadByte(),
                    Rank = reader.ReadByte()
                });
            }
        }

        public override byte[] Encode()
        {
            using PacketWriter writer = new();
            writer.Write((ushort)PacketType.MsgSubPro);
            writer.Write(Timestamp); // 4
            writer.Write((ushort)Action); // 8
            writer.Write(Points); // 10
            writer.Write(Study); // 18
            writer.Write(Count = Professions.Count); // 26
            for (int i = 0; i < Professions.Count; i++)
            {
                writer.Write((byte)Professions[i].Class);
                writer.Write(Professions[i].Level);
                writer.Write(Professions[i].Rank);
            }
            return writer.ToArray();
        }

        public override Task ProcessAsync(Client client)
        {
            switch (Action)
            {
                case AstProfAction.Switch: return client.Character.AstProf.ActivateAsync(Class);
                case AstProfAction.RequestUplev: return client.Character.AstProf.UpLevAsync(Class);
                case AstProfAction.MartialPromoted: return client.Character.AstProf.PromoteAsync(Class);
                case AstProfAction.Info: return client.Character.AstProf.SendAsync();
                case AstProfAction.LearnRemote: return client.Character.AstProf.LearnAsync(Class);
                case AstProfAction.PromoteRemote: return client.Character.AstProf.PromoteAsync(Class);
                default:
                    {
                        logger.LogWarning($"MsgSubPro Unhandled action [{Action}].\r\n\t{PacketDump.Hex(Encode())}");
                        return Task.CompletedTask;
                    }
            }
        }
    }

    public struct AstProfStruct
    {
        public AstProfType Class { get; set; }
        public byte Level { get; set; }
        public byte Rank { get; set; }
    }

    public enum AstProfAction : ushort
    {
        Switch = 0,
        Activate = 1,
        RequestUplev = 2,
        MartialUplev = 3,
        Learn = 4,
        MartialPromoted = 5,
        Info = 6,
        ShowGui = 7,
        UpdateStudy = 8,
        LearnRemote = 9,
        PromoteRemote = 10
    }

    public enum AstProfType : byte
    {
        None = 0,
        MartialArtist = 1,
        Warlock = 2,
        ChiMaster = 3,
        Sage = 4,
        Apothecary = 5,
        Performer = 6,
        Wrangler = 9
    }
}
