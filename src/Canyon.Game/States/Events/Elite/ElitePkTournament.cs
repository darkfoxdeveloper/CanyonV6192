using Canyon.Game.Services.Managers;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States.Events.Tournament;
using Canyon.Game.States.Magics;
using Canyon.Game.States.User;
using Canyon.Game.States.World;
using Canyon.Shared.Managers;
using static Canyon.Game.Sockets.Game.Packets.MsgPkEliteMatchInfo;

namespace Canyon.Game.States.Events.Elite
{
    public sealed class ElitePkTournament : GameEvent
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<ElitePkTournament>();

        public static readonly uint[] WaitingMaps =
        {
            2075,
            2076,
            2077,
            2078
        };

        private const uint BASE_PK_MAP_ID = 910000;

        private ElitePkGroupTournament[] groups = new ElitePkGroupTournament[4];

        public static readonly IdentityManager ElitePkMap = new(BASE_PK_MAP_ID, BASE_PK_MAP_ID + 9999);

        public ElitePkTournament() 
            : base("Elite PK Tournament", 1000)
        {
        }

        public override EventType Identity => EventType.ElitePkTournament;

        public static GameMap BasePkMap { get; private set; }

        public override bool IsActive => Stage == EventStage.Running;

        public override async Task<bool> CreateAsync()
        {
            groups[0] = new ElitePkGroupTournament(new TournamentRules
            {
                MinLevel = 70,
                MaxLevel = 100,
                MinNobility = MsgPeerage.NobilityRank.Serf,
                MaxNobility = MsgPeerage.NobilityRank.King
            }, WaitingMaps[0], 0, "70-100");
            await groups[0].InitializeAsync();

            groups[1] = new ElitePkGroupTournament(new TournamentRules
            {
                MinLevel = 100,
                MaxLevel = 119,
                MinNobility = MsgPeerage.NobilityRank.Serf,
                MaxNobility = MsgPeerage.NobilityRank.King
            }, WaitingMaps[1], 1, "100-119");
            await groups[1].InitializeAsync();

            groups[2] = new ElitePkGroupTournament(new TournamentRules
            {
                MinLevel = 120,
                MaxLevel = 129,
                MinNobility = MsgPeerage.NobilityRank.Serf,
                MaxNobility = MsgPeerage.NobilityRank.King
            }, WaitingMaps[2], 2, "120-129");
            await groups[2].InitializeAsync();

            groups[3] = new ElitePkGroupTournament(new TournamentRules
            {
                MinLevel = 130,
                MaxLevel = ExperienceManager.GetLevelLimit(),
                MinNobility = MsgPeerage.NobilityRank.Serf,
                MaxNobility = MsgPeerage.NobilityRank.King
            }, WaitingMaps[3], 3, "130+");
            await groups[3].InitializeAsync();

            BasePkMap = MapManager.GetMap(BASE_PK_MAP_ID);
            if (BasePkMap == null)
            {
                logger.LogWarning($"Could not start Elite PK Tournament! Base PK map not found.");
                return false;
            }

            return true;
        }

        public override bool IsAllowedToJoin(Role sender)
        {
            return base.IsAllowedToJoin(sender);
        }

        public override bool IsAttackEnable(Role sender, Magic magic = null)
        {
            return base.IsAttackEnable(sender, magic);
        }

        public override bool IsInEventMap(uint idMap)
        {
            return base.IsInEventMap(idMap);
        }

        public override bool IsInscribed(uint idUser)
        {
            return base.IsInscribed(idUser);
        }

        public override Task OnBeAttackAsync(Role attacker, Role target, int damage = 0, Magic magic = null)
        {
            return base.OnBeAttackAsync(attacker, target, damage, magic);
        }

        public override Task OnBeKillAsync(Role attacker, Role target, Magic magic = null)
        {
            return base.OnBeKillAsync(attacker, target, magic);
        }

        public override async Task OnEnterAsync(Character sender)
        {
            for (int i = 0; i < groups.Length; i++)
            {
                if (groups[i].IsParticipantAllowedToJoin(sender))
                {
                    await groups[i].InscribeAsync(new ElitePkParticipant(sender));
                    break;
                }
            }
        }

        public override Task OnExitAsync(Character sender)
        {
            return base.OnExitAsync(sender);
        }

        public override async Task OnTimerAsync()
        {
            switch (Stage)
            {
                case EventStage.Idle:
                    {
                        // Set event start time for all levels, they will handle the correct start time for each one
                        //foreach (var group in groups)
                        //{
                        //    await group.StartupAsync();
                        //}
                        break;
                    }

                case EventStage.Running:
                    {
                        // run the ontimer action for each epk instance until event ends, ontimer must handle everything
                        foreach (var group in groups)
                        {
                            await group.OnTimerAsync();
                        }

                        if (groups.All(x => x.GetStage() == TournamentStage.Final))
                        {
                            Stage = EventStage.Ending;
                        }
                        break;
                    }

                case EventStage.Ending:
                    {
                        // deliver all rewards by email after all epkt matches end
                        break;
                    }
            }
        }

#if DEBUG

        private List<Character> fakePlayers = new();

        public async Task EmulateEventAsync(int[] fakePlayersCount)
        {
            if (fakePlayersCount.Length != 4)
            {
                logger.LogInformation("Cannot debug event without all groups set");
                return;
            }

            logger.LogWarning("CMD Emulating event");
            int[] professionPool =
            {
                15,25,45,55,65,75,85,135,145
            };
            for (int i = 0; i < fakePlayersCount.Length; i++)
            {
                int levelMin;
                int levelMax;
                if (i == 0)
                {
                    levelMin = 70;
                    levelMax = 99;
                }
                else if (i == 1)
                {
                    levelMin = 100;
                    levelMax = 119;
                }
                else if (i == 2)
                {
                    levelMin = 120;
                    levelMax = 129;
                }
                else
                {
                    levelMin = 130;
                    levelMax = 140;
                }

                uint idMap = WaitingMaps[i];
                GameMap gameMap = MapManager.GetMap(idMap);

                for (int p = 0; p < fakePlayersCount[i]; p++)
                {
                    int level = await NextAsync(levelMin, levelMax);
                    int profession = professionPool[await NextAsync(professionPool.Length + 1) % professionPool.Length];
                    Character fakePlayer = await RoleManager.FakePlayerLoginAsync(level, profession, profession, profession);
                    fakePlayers.Add(fakePlayer);

                    var point = await gameMap.QueryRandomPositionAsync();
                    await fakePlayer.FlyMapAsync(idMap, point.X, point.Y);
                }
            }

            await StartAsync();
        }

        public async Task StartAsync()
        {
            logger.LogWarning("CMD Starting event");
            foreach (var group in groups)
            {
                await group.StartupAsync();
            }
            Stage = EventStage.Running;
        }

        public async Task StepAsync(int group)
        {
            if (group < 0 || group > 3)
            {
                return;
            }

            logger.LogWarning("CMD Step event");
            await groups[group].DebugStepAsync();
        }

        public async Task StopAsync()
        {
            logger.LogWarning("CMD Stopping event");

        }

#endif

        public bool IsAllowedToJoin(Character user, int groupIndex)
        {
            if (groupIndex < 0 || groupIndex > 3)
            {
                return false;
            }

            for (int i = 0; i < groups.Length; i++)
            {
                bool isContestant = groups[i].IsContestant(user.Identity);
                if (isContestant && i != groupIndex)
                {
                    return false; // joined previously in another group
                }
            }

            ElitePkGroupTournament elitePk = groups[groupIndex];
            return elitePk.IsParticipantAllowedToJoin(user);
        }

        public ElitePkGuiType GetCurrentStage(int group)
        {
            if (group < 0 || group > 3)
            {
                return ElitePkGuiType.Top8Ranking;
            }

            switch (groups[group].GetStage())
            {
                case TournamentStage.Knockout: return ElitePkGuiType.Knockout16;
                case TournamentStage.Knockout8: return ElitePkGuiType.Top8Qualifier;
                case TournamentStage.QuarterFinals: return ElitePkGuiType.Top4Qualifier;
                case TournamentStage.SemiFinals: return ElitePkGuiType.Top2Qualifier;
                case TournamentStage.ThirdPlace: return ElitePkGuiType.Top3Qualifier;
                case TournamentStage.Finals: return ElitePkGuiType.Top1Qualifier;
                case TournamentStage.Final: return ElitePkGuiType.Top8Ranking;
            }
            return ElitePkGuiType.Top8Ranking;
        }

        public async Task SubmitEventWindowAsync(Character target, int group, int page)
        {
            if (group < 0 || group > 3)
            {
                return;
            }

            var stage = GetCurrentStage(group);
            if (IsActive && stage != ElitePkGuiType.Top8Ranking)
            {
                await groups[group].SubmitEventWindowAsync(target, page);
            }
            else
            {
                await groups[group].SendRankingAsync(target);
            }
        }
    }
}