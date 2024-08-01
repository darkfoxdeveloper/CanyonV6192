using Canyon.Game.Services.Managers;
using Canyon.Game.States.Events.Elite;
using Canyon.Game.States.User;
using Canyon.Network.Packets;
using Newtonsoft.Json;
using static Canyon.Game.States.Events.Tournament.BaseTournamentMatch<Canyon.Game.States.Events.Elite.ElitePkParticipant, Canyon.Game.States.User.Character>;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgPkEliteMatchInfo : MsgBase<Client>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MsgPkEliteMatchInfo>();

        public ElitePkMatchType Mode { get; set; }
        public ushort Page { get; set; }
        public ushort MsgIndex { get; set; }
        public int TotalMatches { get; set; }
        public ushort Group { get; set; }
        public ElitePkGuiType Gui { get; set; }
        public ushort TimeLeft { get; set; }
        public int MatchCount { get; set; }
        public List<MatchInfo> Matches { get; set; } = new();

        public override void Decode(byte[] bytes)
        {
            using var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Mode = (ElitePkMatchType)reader.ReadUInt16();
            Page = reader.ReadUInt16();
            MsgIndex = reader.ReadUInt16();
            TotalMatches = reader.ReadInt32();
            Group = reader.ReadUInt16();
            Gui = (ElitePkGuiType)reader.ReadUInt16();
            TimeLeft = reader.ReadUInt16();
            MatchCount = reader.ReadInt32();
        }

        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgPkEliteMatchInfo);
            writer.Write((ushort)Mode); // 4
            writer.Write(Page); // 6
            writer.Write(MsgIndex); // 8
            writer.Write(TotalMatches); // 10
            writer.Write(Group); // 14
            writer.Write((ushort)Gui); // 16
            writer.Write(TimeLeft); // 18
            writer.Write(MatchCount = Matches.Count); // 20
            foreach (var match in Matches)
            {
                writer.Write(match.MatchIdentity); // 0
                writer.Write((ushort)match.ContestantInfos.Count); // 4
                writer.Write((ushort)match.Index); // 6
                writer.Write((ushort)match.Status); // 8
                foreach (var contestant in match.ContestantInfos)
                {
                    writer.Write(contestant.Identity); // 0
                    writer.Write(contestant.Mesh); // 4
                    writer.Write(contestant.Name, 16); // 8
                    writer.Write((int)contestant.Flag); // 28
                    writer.Write((ushort)(contestant.Winner ? 1 : 0)); // 32
                }
                
                if (match.ContestantInfos.Count < 3)
                {
                    int fillSize = Math.Max(0, 3 - match.ContestantInfos.Count) * 30;
                    writer.Write(new byte[fillSize]);
                }
            }
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            ElitePkTournament elitePkTournament = EventManager.GetEvent<ElitePkTournament>();
            if (elitePkTournament == null)
            {
                await user.SendAsync(this);
                return;
            }

            switch (Mode)
            {
                case ElitePkMatchType.RequestInformation:
                    {
                        // if ElitePK is running, send Gui and Timeleft [timeleft=1 running or seconds for waiting].
                        Gui = elitePkTournament.GetCurrentStage(Group);
                        if (Gui != ElitePkGuiType.Top8Ranking)
                        {
                            TimeLeft = 1;
                        }
                        await user.SendAsync(this);
                        break;
                    }

                case ElitePkMatchType.MainPage:
                    {
                        await elitePkTournament.SubmitEventWindowAsync(user, Group, Page);
                        break;
                    }

                default:
                    {
                        logger.LogWarning("Mode [{Action}] is not being handled.\n{Json}", Mode, JsonConvert.SerializeObject(this));
                        break;
                    }
            }
        }

        public enum ElitePkMatchType : ushort
        {
            MainPage = 0,
            StaticUpdate = 1,
            GuiUpdate = 2,
            UpdateList = 3,
            RequestInformation = 4,
            StopWagers = 5,
            EventState = 6
        }

        public enum ElitePkGuiType : ushort
        {
            Top8Ranking = 0,
            Knockout = 2,
            Knockout16 = 3,
            Top8Qualifier = 4,
            Top4Qualifier = 5,
            Top2Qualifier = 6,
            Top3Qualifier = 7,
            Top1Qualifier = 8,
            ReconstructTop = 9
        }

        public struct MatchInfo
        {
            public uint MatchIdentity { get; set; }
            public int Index { get; set; } // ??
            public MatchStatus Status { get; set; }
            public List<MatchContestantInfo> ContestantInfos { get; set; }
            public MatchContestantLostInfo ExtraInfo { get; set; }
        }

        public struct MatchContestantInfo
        {
            public uint Identity { get; set; }
            public uint Mesh { get; set; }
            public string Name { get; set; }
            public int ServerId { get; set; }
            public ContestantFlag Flag { get; set; }
            public bool Winner { get; set; } // ushort
        }

        public struct MatchContestantLostInfo
        {
            public int Unknown0 { get; set; } // 0
            public ushort Unknown4 { get; set; } // 4
            public int MatchIndex { get; set; } // 6
            public MatchStatus Status { get; set; } // 10
            public int Group { get; set; } // 14
            public uint Id { get; set; } // 18
            public int Unknown22 { get; set; } // 22
            public string Name { get; set; } // 26
        }
    }
}
