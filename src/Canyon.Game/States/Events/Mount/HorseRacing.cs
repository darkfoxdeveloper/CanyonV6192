using Canyon.Database.Entities;
using Canyon.Game.Database.Repositories;
using Canyon.Game.Services.Managers;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States.NPCs;
using Canyon.Game.States.User;
using Canyon.Game.States.World;
using Canyon.World.Enums;
using Microsoft.Extensions.Logging;
using static Canyon.Game.States.Events.Mount.HorseRacing;

namespace Canyon.Game.States.Events.Mount
{
    public sealed class HorseRacing : GameEvent
    {
        private const int MAX_EVENT_DURATION = 60 * 30;

        private static readonly ILogger logger = LogFactory.CreateLogger<HorseRacing>();
        private static readonly ILogger rewardLogger = LogFactory.CreateGmLogger("horse_racing_reward");

        private static readonly IReadOnlyDictionary<uint, uint> MapGateRelationship = new Dictionary<uint, uint>
        {
            { 9929, 100039 },
            { 2063, 100041 },
            { 2062, 100040 },
            { 2064, 100042 },
            { 2065, 100043 },
            { 2066, 100044 },
            { 2067, 100045 },
            { 1950, 100021 }
        };

        private static readonly List<DbPetPoint> petPoints = new List<DbPetPoint>();

        private static readonly IReadOnlyList<HorseRacingReward[]> RideItemRewards = new List<HorseRacingReward[]>()
        {
            new HorseRacingReward[] // 1st
            {
                new HorseRacingReward(3, 3000796), // Open~it~to~get~a~random~P7~Dragon~Soul.
                new HorseRacingReward(50, 3004243, 3), // Collect~15~scraps~to~combine~into~a~P7~Weapon~Soul~Pack.
                new HorseRacingReward(50, 3004244, 3), // Collect~15~scraps~to~combine~into~a~P7~Equipment~Soul~Pack.
                new HorseRacingReward(100, 3001798), // Open~to~receive~5~Dragon~Balls.
            },
            new HorseRacingReward[] // 2nd
            {
                new HorseRacingReward(3, 3000796), // Open~it~to~get~a~random~P7~Dragon~Soul.
                new HorseRacingReward(30, 3004243, 1), // Collect~15~scraps~to~combine~into~a~P7~Weapon~Soul~Pack.
                new HorseRacingReward(30, 3004244, 1), // Collect~15~scraps~to~combine~into~a~P7~Equipment~Soul~Pack.
                new HorseRacingReward(100, 3001798), // Open~to~receive~5~Dragon~Balls.
            },
            new HorseRacingReward[] // 3rd
            {
                new HorseRacingReward(100, 3005680), // It~contains~2~Dragon~Balls.~Right~click~to~open.
                new HorseRacingReward(30, 728209), // Open~it~to~get~a~random~P4~or~P6~Dragon~Soul.
            },
            new HorseRacingReward[] // 4th
            {
                new HorseRacingReward(100, 1088000),
                new HorseRacingReward(50, 728209), // Open~it~to~get~a~random~P4~or~P6~Dragon~Soul.
                new HorseRacingReward(50, 728674), // Right~click~to~open~and~receive~a~P3~to~P6~Dragon~Soul~at~random.
                new HorseRacingReward(50, 3004059), // Open~it~to~get~a~random~P6~Dragon~Soul.
            },
            new HorseRacingReward[] // 5th-10th
            {
                new HorseRacingReward(100, 1088000),
                new HorseRacingReward(50, 728209), // Open~it~to~get~a~random~P4~or~P6~Dragon~Soul.
                new HorseRacingReward(50, 728674), // Right~click~to~open~and~receive~a~P3~to~P6~Dragon~Soul~at~random.
                new HorseRacingReward(50, 3004059), // Open~it~to~get~a~random~P6~Dragon~Soul.
            }
        };

        private TimeOut preparationTimeout = new TimeOut();
        private TimeOut startUpTimeOut = new TimeOut();
        private TimeOut endRaceTimeOut = new TimeOut();
        private DynamicNpc horseRacingClerk;

        private DateTime startTime;
        private List<FinishLineUser> winners = new List<FinishLineUser>();

        public HorseRacing() 
            : base("Horse Racing", 1000)
        {
        }

        public override EventType Identity => EventType.HorseRacing;

        public override bool IsActive => endRaceTimeOut.IsActive();

        public override GameMap Map 
        { 
            get => MapManager.GetMap((uint)horseRacingClerk.Data0);
            protected set 
            {

            }
        }

        public override async Task<bool> CreateAsync()
        {
            horseRacingClerk = RoleManager.GetRole<DynamicNpc>(100070);
            if (horseRacingClerk == null)
            {
                logger.LogWarning($"HorseRacingClerk[100070] was not found!");
                return false;
            }

            horseRacingClerk.Data0 = 9929;
            await horseRacingClerk.SaveAsync();

            foreach (var map in MapGateRelationship)
            {
                await PrepareMapAsync(map.Key);
            }

            petPoints.AddRange(await RidepetPointRepository.GetAsync());
            return true;
        }

        public override async Task OnEnterMapAsync(Character sender)
        {
            if (endRaceTimeOut.IsActive() && !endRaceTimeOut.IsTimeOut())
            {
                await sender.SendAsync(new MsgAction
                {
                    Action = MsgAction.ActionType.StartRaceTrack
                });

                await sender.SendAsync(new MsgCompeteRank
                {
                    Mode = MsgCompeteRank.Action.BestTime,
                    Rank = 0,
                    Param = 1800000
                });

                if (winners.Count > 0)
                {
                    foreach (var winner in winners)
                    {
                        await sender.SendAsync(new MsgCompeteRank()
                        {
                            Mode = MsgCompeteRank.Action.AddRecord,
                            Rank = winner.Position,
                            Name = winner.Name,
                            Time = winner.Milliseconds
                        });
                    }
                }
            }

            if (preparationTimeout.IsActive() && !preparationTimeout.IsTimeOut())
            {
                await sender.SendAsync(new MsgAction
                {
                    Action = MsgAction.ActionType.Countdown,
                    Identity = sender.Identity,
                    Command = (uint)preparationTimeout.GetRemain()
                });
            }

            await sender.ClearRaceItemsAsync();
        }

        public async Task PrepareMapAsync(uint idMap)
        {
            GameMap map = MapManager.GetMap(idMap);
            if (map == null)
            {
                logger.LogWarning($"Map {idMap} does not exist");
                return;
            }

            if (!map.IsRaceTrack())
            {
                logger.LogWarning($"Map {idMap} is no race track");
                return;
            }

            if (!MapGateRelationship.TryGetValue(idMap, out var npcId))
            {
                logger.LogWarning($"No gate information for map {idMap}");
                return;
            }

            DynamicNpc fence = RoleManager.FindRole<DynamicNpc>(npcId);
            if (fence == null)
            {
                logger.LogWarning($"No fence {npcId} found for map {idMap}!");
                return;
            }

            if (fence.MapIdentity == idMap)
            {
                return;
            }

            ushort x = (ushort)fence.Data0;
            ushort y = (ushort)fence.Data1;

            await fence.ChangePosAsync(idMap, x, y);
        }

        public async Task PrepareStartupAsync(uint mapId)
        {
            GameMap map = MapManager.GetMap(mapId);
            if (map == null)
            {
                logger.LogWarning($"PrepareStartupAsync Map {mapId} does not exist");
                return;
            }

            if (!map.IsRaceTrack())
            {
                logger.LogWarning($" PrepareStartupAsync Map {mapId} is no race track");
                return;
            }

            preparationTimeout.Startup(55);
            foreach (var player in map.QueryPlayers())
            {
                await player.SendAsync(new MsgAction
                {
                    Action = MsgAction.ActionType.Countdown,
                    Command = 60,
                    Identity = player.Identity
                });
            }
        }

        public async Task AnnounceRaceStartupAsync()
        {
            if (Map == null)
            {
                return;
            }

            if (Map.Identity != horseRacingClerk.Data0)
            {
                logger.LogWarning($"Game map not scheduled for horse racing!");
                return;
            }

            await Map.BroadcastMsgAsync(new MsgAction
            {
                Action = MsgAction.ActionType.StartRaceTrack,
                Identity = 1
            });
            startUpTimeOut.Startup(5);
        }

        public async Task StartRaceAsync()
        {
            if (Map == null)
            {
                logger.LogCritical($"Map {horseRacingClerk.Data0} for horse racing does not exist at race start!!!");
                return;
            }

            if (!MapGateRelationship.TryGetValue(Map.Identity, out var npcId))
            {
                logger.LogWarning($"No gate information for map {Map.Identity}");
                return;
            }

            DynamicNpc fence = RoleManager.FindRole<DynamicNpc>(npcId);
            if (fence == null)
            {
                logger.LogWarning($"No fence {npcId} found for map {Map.Identity}!");
                return;
            }

            logger.LogInformation("Starting race on {}:{}", Map.Identity, Map.Name);
            startTime = DateTime.Now;

            fence.Data0 = fence.X;
            fence.Data1 = fence.Y;
            await fence.ChangePosAsync(5000, 70, 70);
            await fence.SaveAsync();
        }

        public async Task CrossFinishLineAsync(Character user)
        {
            if (!user.Map.QueryRegion(RegionType.RacingEndArea, user.X, user.Y))
            {
                return;
            }

            if (winners.Any(x => x.Identity == user.Identity)) 
            {
                await user.FlyMapAsync(user.RecordMapIdentity, user.RecordMapX, user.RecordMapY);
                return;
            }

            int position = winners.Count + 1;
            var petPointReward = petPoints.FirstOrDefault(x => x.MapId == user.MapIdentity && x.Rank == position);
            int elapsedMilliseconds = (int)(DateTime.Now - startTime).TotalMilliseconds;
            int ridePetPointReward = 0;

            if (petPointReward != null)
            {
                ridePetPointReward = petPointReward.Points;
            }

            winners.Add(new FinishLineUser
            {
                Identity = user.Identity,
                Position = winners.Count + 1,
                Award = ridePetPointReward,
                Milliseconds = elapsedMilliseconds,
                Name = user.Name
            });

            MsgCompeteRank msg;
            if (winners.Count < 4)
            {
                msg = new()
                {
                    Mode = MsgCompeteRank.Action.AddRecord,
                    Rank = winners.Count,
                    Name = user.Name,
                    Time = elapsedMilliseconds,
                };
                await Map.BroadcastMsgAsync(msg);
            }

            await user.AwardHorseRacePointsAsync(ridePetPointReward);
            rewardLogger.LogInformation($"{user.Identity},{user.Name},{user.MapIdentity},{user.X},{user.Y},RidePetPoint,{ridePetPointReward},0,0,0");

            if (winners.Count < 11)
            {
                int rewardIndex = Math.Max(0, Math.Min(winners.Count - 1, RideItemRewards.Count - 1));
                foreach (var reward in RideItemRewards[rewardIndex])
                {
                    if (!await ChanceCalcAsync(reward.Rate))
                    {
                        continue;
                    }

                    for (int i = 0; i < reward.ItemCount; i++)
                    {
                        if (await user.UserPackage.AwardItemAsync(reward.ItemType, Items.Item.ItemPosition.Inventory, reward.Monopoly))
                        {
                            rewardLogger.LogInformation($"{user.Identity},{user.Name},{user.MapIdentity},{user.X},{user.Y},ItemReward,{reward.ItemType},{reward.Rate}%,1,{(reward.Monopoly ? 1 : 0)}");
                        }
                    }
                }
            }

            for (int i = StatusSet.ACCELERATED; i <= StatusSet.CONFUSED; i++) 
            {
                await user.DetachStatusAsync(i);
            }

            await user.FlyMapAsync(user.RecordMapIdentity, user.RecordMapX, user.RecordMapY);

            msg = new()
            {
                Mode = MsgCompeteRank.Action.EndTime,
                Rank = winners.Count,
                Param = elapsedMilliseconds,
                Data = ridePetPointReward,
                Prize = ridePetPointReward,
                Time = elapsedMilliseconds
            };
            await user.SendAsync(msg);
        }

        public override async Task OnTimerAsync()
        {
            switch (Stage)
            {
                case EventStage.Idle:
                    {
                        if (preparationTimeout.IsActive() && preparationTimeout.IsTimeOut())
                        {
                            preparationTimeout.Clear();
                            await AnnounceRaceStartupAsync();
                            return;
                        }
                        if (startUpTimeOut.IsActive() && startUpTimeOut.IsTimeOut())
                        {
                            endRaceTimeOut.Startup(MAX_EVENT_DURATION);
                            startUpTimeOut.Clear();
                            await StartRaceAsync();
                            rewardLogger.LogInformation($"==================================== Start {startTime} ====================================");
                            Stage = EventStage.Running;
                            return;
                        }
                        break;
                    }
                case EventStage.Running:
                    {
                        if (endRaceTimeOut.IsActive() && endRaceTimeOut.IsTimeOut())
                        {
                            Stage = EventStage.Ending;
                            endRaceTimeOut.Clear();
                            return;
                        }
                        break;
                    }
                case EventStage.Ending:
                    {
                        rewardLogger.LogInformation($"==================================== End   {DateTime.Now} ====================================");

                        winners.Clear();

                        foreach (var map in MapGateRelationship)
                        {
                            await PrepareMapAsync(map.Key);
                        }

                        Stage = EventStage.Idle;
                        break;
                    }
            }
        }

        public struct HorseRacingReward
        {
            public HorseRacingReward(ushort rate, uint itemType, byte count = 1, bool monopoly = false)
            {
                Rate = rate;
                ItemType = itemType;
                ItemCount = count;
                Monopoly = monopoly;
            }

            public ushort Rate { get; set; }
            public uint ItemType { get; set; }
            public byte ItemCount { get; set; }
            public bool Monopoly { get; set; }
        }

        public enum ItemType : ushort
        {
            Null = 8329,
            ChaosBomb = 8330,               // 90000860
            SpiritPotion = 8331,            // 90000730
            ExcitementPotion = 8332,        // 90000760
            ScreamBomb = 8334,              // 90000840
            SluggishPotion = 8335,          // 90000830
            GuardPotion = 8336,             // 90000750
            DizzyHammer = 8337,             // 90000850
            TransformItem = 8338,           // ?
            RestorePotion = 8339,           // 90000740
            SuperExcitementPotion = 8340,   // 90000770
        }

        private struct FinishLineUser
        {
            public int Position { get; set; }
            public uint Identity { get; set; }
            public string Name { get; set; }
            public int Milliseconds { get; set; }
            public int Award { get; set; }
        }
    }
}
