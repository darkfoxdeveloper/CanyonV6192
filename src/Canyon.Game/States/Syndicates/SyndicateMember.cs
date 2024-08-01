using Canyon.Database.Entities;
using Canyon.Game.Database;
using Canyon.Game.Database.Repositories;
using Canyon.Game.Services.Managers;
using Canyon.Game.States.User;
using static Canyon.Game.Sockets.Game.Packets.MsgPeerage;

namespace Canyon.Game.States.Syndicates
{
    public sealed class SyndicateMember
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<SyndicateMember>();

        private DbSyndicateAttr memberAttributes;

        public uint UserIdentity => memberAttributes.UserIdentity;
        public uint LookFace { get; private set; }
        public int Gender => (int)(LookFace % 10000 / 1000);
        public string UserName { get; private set; }
        public uint MateIdentity { get; private set; }
        public NobilityRank NobilityRank => PeerageManager.GetRanking(UserIdentity);

        public uint SyndicateIdentity => memberAttributes.SynId;

        public int Silvers
        {
            get => (int)memberAttributes.Proffer;
            set => memberAttributes.Proffer = value;
        }

        public ulong SilversTotal
        {
            get => memberAttributes.ProfferTotal;
            set => memberAttributes.ProfferTotal = value;
        }

        public string SyndicateName { get; private set; }

        public byte Level { get; set; }
        public int Profession { get; set; }

        public SyndicateRank Rank
        {
            get => (SyndicateRank)memberAttributes.Rank;
            set => memberAttributes.Rank = (ushort)value;
        }

        public string RankName
        {
            get
            {
                switch (Rank)
                {
                    case SyndicateRank.GuildLeader:
                        return "Guild Leader";
                    case SyndicateRank.LeaderSpouse:
                        return "Leader Spouse";
                    case SyndicateRank.LeaderSpouseAide:
                        return "Leader Spouse Aide";
                    case SyndicateRank.DeputyLeader:
                        return "Deputy Leader";
                    case SyndicateRank.DeputyLeaderAide:
                        return "Deputy Leader Aide";
                    case SyndicateRank.DeputyLeaderSpouse:
                        return "Deputy Leader Spouse";
                    case SyndicateRank.HonoraryDeputyLeader:
                        return "Honorary Deputy Leader";
                    case SyndicateRank.Manager:
                        return "Manager";
                    case SyndicateRank.ManagerAide:
                        return "Manager Aide";
                    case SyndicateRank.ManagerSpouse:
                        return "Manager Spouse";
                    case SyndicateRank.HonoraryManager:
                        return "Honorary Manager";
                    case SyndicateRank.Supervisor:
                        return "Supervisor";
                    case SyndicateRank.SupervisorAide:
                        return "Supervisor Aide";
                    case SyndicateRank.SupervisorSpouse:
                        return "Supervisor Spouse";
                    case SyndicateRank.TulipSupervisor:
                        return "Tulip Supervisor";
                    case SyndicateRank.ArsenalSupervisor:
                        return "Arsenal Supervisor";
                    case SyndicateRank.CpSupervisor:
                        return "CP Supervisor";
                    case SyndicateRank.GuideSupervisor:
                        return "Guide Supervisor";
                    case SyndicateRank.LilySupervisor:
                        return "Lily Supervisor";
                    case SyndicateRank.OrchidSupervisor:
                        return "Orchid Supervisor";
                    case SyndicateRank.SilverSupervisor:
                        return "Silver Supervisor";
                    case SyndicateRank.RoseSupervisor:
                        return "Rose Supervisor";
                    case SyndicateRank.PkSupervisor:
                        return "PK Supervisor";
                    case SyndicateRank.HonorarySupervisor:
                        return "Honorary Supervisor";
                    case SyndicateRank.Steward:
                        return "Steward";
                    case SyndicateRank.StewardSpouse:
                        return "Steward Spouse";
                    case SyndicateRank.DeputySteward:
                        return "Deputy Steward";
                    case SyndicateRank.HonorarySteward:
                        return "Honorary Steward";
                    case SyndicateRank.Aide:
                        return "Aide";
                    case SyndicateRank.TulipAgent:
                        return "Tulip Agent";
                    case SyndicateRank.OrchidAgent:
                        return "Orchid Agent";
                    case SyndicateRank.CpAgent:
                        return "CP Agent";
                    case SyndicateRank.ArsenalAgent:
                        return "Arsenal Agent";
                    case SyndicateRank.SilverAgent:
                        return "Silver Agent";
                    case SyndicateRank.GuideAgent:
                        return "Guide Agent";
                    case SyndicateRank.PkAgent:
                        return "PK Agent";
                    case SyndicateRank.RoseAgent:
                        return "Rose Agent";
                    case SyndicateRank.LilyAgent:
                        return "Lily Agent";
                    case SyndicateRank.Agent:
                        return "Agent Follower";
                    case SyndicateRank.TulipFollower:
                        return "Tulip Follower";
                    case SyndicateRank.OrchidFollower:
                        return "Orchid Follower";
                    case SyndicateRank.CpFollower:
                        return "CP Follower";
                    case SyndicateRank.ArsenalFollower:
                        return "Arsenal Follower";
                    case SyndicateRank.SilverFollower:
                        return "Silver Follower";
                    case SyndicateRank.GuideFollower:
                        return "Guide Follower";
                    case SyndicateRank.PkFollower:
                        return "PK Follower";
                    case SyndicateRank.RoseFollower:
                        return "Rose Follower";
                    case SyndicateRank.LilyFollower:
                        return "Lily Follower";
                    case SyndicateRank.Follower:
                        return "Follower";
                    case SyndicateRank.SeniorMember:
                        return "Member";
                    case SyndicateRank.Member:
                        return "Member";
                    default:
                        return "Error";
                }
            }
        }

        public DateTime JoinDate => UnixTimestamp.ToDateTime(memberAttributes.JoinDate);

        public Character User => RoleManager.GetUser(UserIdentity);
        public bool IsOnline => User != null;

        public uint ConquerPointsDonation
        {
            get => memberAttributes.Emoney;
            set => memberAttributes.Emoney = value;
        }

        public uint ConquerPointsTotalDonation
        {
            get => memberAttributes.EmoneyTotal;
            set => memberAttributes.EmoneyTotal = value;
        }

        public uint GuideDonation
        {
            get => memberAttributes.Guide;
            set => memberAttributes.Guide = value;
        }

        public uint GuideTotalDonation
        {
            get => memberAttributes.GuideTotal;
            set => memberAttributes.GuideTotal = value;
        }

        public int PkDonation
        {
            get => memberAttributes.Pk;
            set => memberAttributes.Pk = value;
        }

        public int PkTotalDonation
        {
            get => memberAttributes.PkTotal;
            set => memberAttributes.PkTotal = value;
        }

        public uint ArsenalDonation
        {
            get => memberAttributes.Arsenal;
            set => memberAttributes.Arsenal = value;
        }

        public uint RedRoseDonation
        {
            get => memberAttributes.Flower;
            set => memberAttributes.Flower = value;
        }

        public uint WhiteRoseDonation
        {
            get => memberAttributes.WhiteFlower;
            set => memberAttributes.WhiteFlower = value;
        }

        public uint OrchidDonation
        {
            get => memberAttributes.Orchid;
            set => memberAttributes.Orchid = value;
        }

        public uint TulipDonation
        {
            get => memberAttributes.Tulip;
            set => memberAttributes.Tulip = value;
        }

        public uint Merit
        {
            get => memberAttributes.Merit;
            set => memberAttributes.Merit = value;
        }

        public DateTime? LastLogout
        {
            get => UnixTimestamp.ToNullableDateTime((int)memberAttributes.LastLogout);
            set => memberAttributes.LastLogout = (uint)UnixTimestamp.FromDateTime(value);
        }

        public DateTime? PositionExpiration
        {
            get => UnixTimestamp.ToNullableDateTime((int)memberAttributes.Expiration);
            set => memberAttributes.Expiration = (uint)UnixTimestamp.FromDateTime(value);
        }

        public uint MasterIdentity
        {
            get => memberAttributes.MasterId;
            set => memberAttributes.MasterId = value;
        }

        public uint AssistantIdentity
        {
            get => memberAttributes.AssistantIdentity;
            set => memberAttributes.AssistantIdentity = value;
        }

        public int TotalDonation => (int)(Silvers / 10000 + ConquerPointsDonation * 20 + GuideDonation + PkDonation +
                                           ArsenalDonation + RedRoseDonation + WhiteRoseDonation + OrchidDonation +
                                           TulipDonation);

        public int UsableDonation => (int)(Silvers / 10000 + ConquerPointsDonation * 20 + GuideDonation + PkDonation +
                                            ArsenalDonation + RedRoseDonation + WhiteRoseDonation + OrchidDonation +
                                            TulipDonation);

        public async Task<bool> CreateAsync(DbSyndicateAttr attr, Syndicate syn)
        {
            if (attr == null || syn == null || memberAttributes != null)
            {
                return false;
            }

            memberAttributes = attr;

            DbCharacter user = await CharacterRepository.FindByIdentityAsync(attr.UserIdentity);
            if (user == null)
            {
                return false;
            }

            UserName = user.Name;
            LookFace = user.Mesh;
            MateIdentity = user.Mate;
            SyndicateName = syn.Name;
            Level = user.Level;
            Profession = user.Profession;
            return true;
        }

        public async Task<bool> CreateAsync(Character user, Syndicate syn, SyndicateRank rank)
        {
            if (user == null || syn == null || memberAttributes != null)
            {
                return false;
            }

            memberAttributes = new DbSyndicateAttr
            {
                UserIdentity = user.Identity,
                SynId = syn.Identity,
                Arsenal = 0,
                Emoney = 0,
                EmoneyTotal = 0,
                Merit = 0,
                Guide = 0,
                GuideTotal = 0,
                JoinDate = (uint)UnixTimestamp.Now,
                Pk = 0,
                PkTotal = 0,
                Proffer = 0,
                ProfferTotal = 0,
                Rank = (ushort)rank
            };

            if (!await ServerDbContext.CreateAsync(memberAttributes))
            {
                return false;
            }

            UserName = user.Name;
            LookFace = user.Mesh;
            SyndicateName = syn.Name;
            Level = user.Level;
            MateIdentity = user.MateIdentity;
            Profession = user.Profession;

            logger.LogInformation($"User [{user.Identity}, {user.Name}] has joined [{syn.Identity}, {syn.Name}]");
            return true;
        }

        public void ChangeName(string newName)
        {
            UserName = newName;
        }

        public enum SyndicateRank : ushort
        {
            GuildLeader = 1000,

            DeputyLeader = 990,
            HonoraryDeputyLeader = 980,
            LeaderSpouse = 920,

            Manager = 890,
            HonoraryManager = 880,

            TulipSupervisor = 859,
            OrchidSupervisor = 858,
            CpSupervisor = 857,
            ArsenalSupervisor = 856,
            SilverSupervisor = 855,
            GuideSupervisor = 854,
            PkSupervisor = 853,
            RoseSupervisor = 852,
            LilySupervisor = 851,
            Supervisor = 850,
            HonorarySupervisor = 840,

            Steward = 690,
            HonorarySteward = 680,
            DeputySteward = 650,
            DeputyLeaderSpouse = 620,
            DeputyLeaderAide = 611,
            LeaderSpouseAide = 610,
            Aide = 602,

            TulipAgent = 599,
            OrchidAgent = 598,
            CpAgent = 597,
            ArsenalAgent = 596,
            SilverAgent = 595,
            GuideAgent = 594,
            PkAgent = 593,
            RoseAgent = 592,
            LilyAgent = 591,
            Agent = 590,
            SupervisorSpouse = 521,
            ManagerSpouse = 520,
            SupervisorAide = 511,
            ManagerAide = 510,

            TulipFollower = 499,
            OrchidFollower = 498,
            CpFollower = 497,
            ArsenalFollower = 496,
            SilverFollower = 495,
            GuideFollower = 494,
            PkFollower = 493,
            RoseFollower = 492,
            LilyFollower = 491,
            Follower = 490,
            StewardSpouse = 420,

            SeniorMember = 210,
            Member = 200,

            None = 0
        }

        public Task<bool> SaveAsync()
        {
            return ServerDbContext.SaveAsync(memberAttributes);
        }

        public Task<bool> DeleteAsync()
        {
            return ServerDbContext.DeleteAsync(memberAttributes);
        }
    }
}
