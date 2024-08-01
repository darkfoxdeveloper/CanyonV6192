using Canyon.Database.Entities;
using Canyon.Game.Database;
using Canyon.Game.Database.Repositories;
using Canyon.Game.Services.Managers;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States.Events.Interfaces;
using Canyon.Game.States.Magics;
using Canyon.Game.States.User;
using Canyon.Shared.Managers;
using System.Collections.Concurrent;
using System.Drawing;
using static Canyon.Game.Sockets.Game.Packets.MsgArenicWitness;

namespace Canyon.Game.States.Events.Qualifier.UserQualifier
{
    public sealed class ArenaQualifier : GameEvent, IWitnessEvent
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<ArenaQualifier>();

        public const int MIN_LEVEL = 70;
        public const int PRICE_PER_1500_POINTS = 9000000;
        public const uint BASE_MAP_ID_U = 900000;
        public const uint TRIUMPH_HONOR_REWARD = 5000;
        public const int MATCH_TIME_SECONDS = 180;

        public static IdentityManager MapIdentityGenerator = new(900001, 900999);

        public static DbMap BaseMap { get; private set; }

        private static readonly uint[] StartPoints =
        {
            1500, // over 70
            2200, // over 90
            2700, // over 100
            3200, // over 110
            4000  // over 120
        };

        private Dictionary<uint, ArenicInformation> pastSeasonArenics = new();
        private ConcurrentDictionary<uint, ArenicInformation> arenics = new();
        private readonly ConcurrentDictionary<uint, ArenaQualifierUser> awaitingQueue = new();
        private readonly ConcurrentDictionary<uint, ArenaQualifierUserMatch> matches = new();
        private readonly ConcurrentDictionary<uint, DbArenicHonor> rewards = new();

        public ArenaQualifier()
            : base("ArenaQualifier", 500)
        {
        }

        #region Event Override

        public override EventType Identity => EventType.UserArenaQualifier;

        public override async Task<bool> CreateAsync()
        {
            Map = MapManager.GetMap(BASE_MAP_ID_U);

            if (Map == null)
            {
                logger.LogWarning($"Base Map {BASE_MAP_ID_U} not found Arena Qualifier");
                return false;
            }

            var dbArenics = await ArenicRepository.GetAsync(DateTime.Now.AddDays(-1), 0);
            foreach (var dbArenic in dbArenics
                .Where(x => x.DayWins > 0 || x.DayLoses > 0)
                .OrderByDescending(x => x.AthletePoint)
                .ThenByDescending(x => x.DayWins)
                .ThenBy(x => x.DayLoses))
            {
                ArenicInformation arenicInformation = new ArenicInformation(dbArenic);
                if (!await arenicInformation.InitializeAsync())
                {
                    continue;
                }

                pastSeasonArenics.TryAdd(arenicInformation.UserId, arenicInformation);

                if (pastSeasonArenics.Count >= 10)
                {
                    break;
                }
            }

            dbArenics = await ArenicRepository.GetAsync(DateTime.Now, 0);
            foreach (var dbArenic in dbArenics)
            {
                ArenicInformation arenicInformation = new ArenicInformation(dbArenic);
                if (!await arenicInformation.InitializeAsync())
                {
                    continue;
                }

                arenics.TryAdd(arenicInformation.UserId, arenicInformation);
            }

            foreach (DbArenicHonor honor in await ArenicHonorRepository.GetAsync(0))
            {
                rewards.TryAdd(honor.Id, honor);
            }

            return true;
        }

        public override bool IsAllowedToJoin(Role sender)
        {
            if (sender is not Character user)
            {
                return false;
            }

            if (user.Level < MIN_LEVEL)
            {
                return false;
            }

            if (user.Map.IsPrisionMap() 
                || user.Map.IsTeleportDisable()
                || user.Map.IsArenicMapInGeneral())
            {
                return false;
            }

            if (user.QualifierPoints == 0)
            {
                return false;
            }

            if (user.IsInQualifierEvent())
            {
                return false;
            }
            return true;
        }

        public override Task OnLoginAsync(Character user)
        {
            if (user.QualifierPoints == 0)
            {
                user.QualifierPoints = GetInitialPoints(user.Level);
            }
            return Task.CompletedTask;
        }

        public override bool IsInEventMap(uint idMap)
        {
            return FindMatchByMap(idMap) != null;
        }

        public override bool IsAttackEnable(Role sender, Magic magic = null)
        {
            var match = FindMatchByMap(sender.MapIdentity);
            if (match != null)
            {
                if (match.Player1.Identity != sender.Identity && match.Player2.Identity != sender.Identity)
                {
                    return false; // witness no attack
                }
                return match.IsAttackEnable;
            }
            return true; // return true, because if not in event already it must be able to attack
        }

        public override Task OnEnterAsync(Character sender)
        {
            return SendArenaInformationAsync(sender);
        }

        public override Task OnExitAsync(Character sender)
        {
            return SendArenaInformationAsync(sender);
        }

        public override Task OnBeAttackAsync(Role attacker, Role target, int damage = 0, Magic magic = null)
        {
            if (attacker is not Character || target is not Character)
            {
                return Task.CompletedTask;
            }

            if (attacker.MapIdentity - attacker.MapIdentity % BASE_MAP_ID_U != BASE_MAP_ID_U || attacker.MapIdentity != target.MapIdentity)
            {
                return Task.CompletedTask; // ??? should remove?
            }

            ArenaQualifierUserMatch match = FindMatchByMap(attacker.MapIdentity);
            if (match == null)
            {
                return Task.CompletedTask; // ??? Should remove???
            }

            if (attacker.Identity == match.Player1.Identity)
            {
                match.Points1 += damage;
            }
            else if (attacker.Identity == match.Player2.Identity)
            {
                match.Points2 += damage;
            }
            else
            {
                return Task.CompletedTask;
            }

            return match.SendBoardAsync();
        }

        public override async Task OnBeKillAsync(Role attacker, Role target, Magic magic = null)
        {
            var match = FindMatchByMap(target.MapIdentity);
            if (match != null)
            {
                await match.FinishAsync(attacker as Character, target as Character);
            }
        }

        public override bool IsInscribed(uint idUser)
        {
            return FindInQueue(idUser) != null || FindMatch(idUser) != null;
        }

        public override async Task OnDailyResetAsync()
        {
            // hummm
            foreach (var arenic in arenics.Values)
            {
                if (GetReward(arenic.UserId, out var qualifierReward))
                {
                    Character onlineUser = RoleManager.GetUser(arenic.UserId);
                    if (onlineUser != null)
                    {
                        onlineUser.HonorPoints += qualifierReward.DayPrize;
                        onlineUser.HistoryHonorPoints += qualifierReward.DayPrize;

                        onlineUser.QualifierPoints = GetInitialPoints(onlineUser.Level);
                        onlineUser.QualifierDayWins = 0;
                        onlineUser.QualifierDayLoses = 0;
                    }
                    else
                    {
                        await ServerDbContext.ScalarAsync($"UPDATE cq_user " +
                            $"SET athlete_cur_honor_point=athlete_cur_honor_point+{qualifierReward.DayPrize}, " +
                            $"athlete_hisorty_honor_point=athlete_hisorty_honor_point+{qualifierReward.DayPrize}, " +
                            $"athlete_point={GetInitialPoints(arenic.Level)}, " +
                            $"athlete_day_wins=0, athlete_day_loses=0 " +
                            $"WHERE id={arenic.UserId} LIMIT 1;");
                    }
                }
            }

            arenics.Clear();
        }

        public override async Task OnTimerAsync()
        {
            foreach (ArenaQualifierUser queueUser in awaitingQueue.Values)
            {
                Character player = RoleManager.GetUser(queueUser.Identity);
                if (player == null)
                {
                    await UnsubscribeAsync(queueUser.Identity);
                    continue;
                }

                ArenaQualifierUser target = await FindTargetAsync(queueUser);
                if (target == null)
                {
                    continue;
                }

                Character targetPlayer = RoleManager.GetUser(target.Identity);
                if (targetPlayer == null)
                {
                    await UnsubscribeAsync(target.Identity);
                    continue;
                }

                if (!IsAllowedToJoin(player))
                {
                    await UnsubscribeAsync(player.Identity);
                    continue;
                }

                if (!IsAllowedToJoin(targetPlayer))
                {
                    await UnsubscribeAsync(player.Identity);
                    continue;
                }

                var match = new ArenaQualifierUserMatch();
                if (!await match.CreateAsync(player, queueUser, targetPlayer, target) ||
                    !matches.TryAdd(match.MapIdentity, match))
                {
                    await UnsubscribeAsync(queueUser.Identity);
                    await UnsubscribeAsync(target.Identity);
                    continue;
                }

                awaitingQueue.TryRemove(player.Identity, out _);
                awaitingQueue.TryRemove(target.Identity, out _);

                player.QualifierStatus = ArenaStatus.WaitingInactive;
                targetPlayer.QualifierStatus = ArenaStatus.WaitingInactive;

                await SendArenaInformationAsync(player);
                await SendArenaInformationAsync(targetPlayer);
            }

            foreach (ArenaQualifierUserMatch match in matches.Values)
            {
                if (match.Status == ArenaQualifierUserMatch.MatchStatus.ReadyToDispose)
                {
                    matches.TryRemove(match.MapIdentity, out _);
                    continue;
                }

                await match.OnTimerAsync();
            }
        }

        #endregion

        #region Inscribe

        public async Task<bool> InscribeAsync(Character user)
        {
            if (HasUser(user.Identity))
            {
                return false; // already joined ????
            }

            if (!IsAllowedToJoin(user))
            {
                return false;
            }

            if (EnterQueue(user) != null)
            {
                await user.SignInEventAsync(this);
            }

            return true;
        }

        public async Task<bool> UnsubscribeAsync(uint idUser)
        {
            Character user = RoleManager.GetUser(idUser);

            LeaveQueue(idUser);
            if (user != null)
            {
                user.QualifierStatus = ArenaStatus.NotSignedUp;
            }

            return true;
        }

        #endregion

        #region Match Management

        public ArenaQualifierUserMatch FindMatch(uint idUser)
        {
            return matches.Values.FirstOrDefault(x => x.Player1.Identity == idUser || x.Player2.Identity == idUser);
        }

        public ArenaQualifierUserMatch FindMatchByMap(uint idMap)
        {
            return matches.TryGetValue(idMap, out ArenaQualifierUserMatch match) ? match : null;
        }

        public List<ArenaQualifierUserMatch> QueryMatches(int from, int limit)
        {
            return matches.Values
                .Where(x => x.IsRunning)
                            .Skip(from)
                            .Take(limit)
                            .ToList();
        }

        public async Task FinishMatchAsync(ArenaQualifierUserMatch match)
        {
            if (match.Player1 != null)
            {
                if (!arenics.TryGetValue(match.Player1.Identity, out var arenic)
                    || arenic.Date.Date != DateTime.Now.Date)
                {
                    arenics.TryRemove(match.Player1.Identity, out _);
                    arenics.TryAdd(match.Player1.Identity, arenic = new ArenicInformation(match.Player1, 0));
                }

                await match.Player1.UpdateTaskActivityAsync(ActivityManager.ActivityType.Qualifier);

                arenic.AthletePoint = match.Player1.QualifierPoints;
                arenic.DayWins = match.Player1.QualifierDayWins;
                arenic.DayLoses = match.Player1.QualifierDayLoses;
                arenic.CurrentHonor = match.Player1.HonorPoints;
                arenic.HistoryHonor = match.Player1.HistoryHonorPoints;
                await arenic.SaveAsync();

                LuaScriptManager.Run(match.Player1, null, null, string.Empty, $"UserArenicCompetes({arenic.UserId},{arenic.DayWins + arenic.DayLoses})");
            }

            if (match.Player2 != null)
            {
                if (!arenics.TryGetValue(match.Player2.Identity, out var arenic)
                    || arenic.Date.Date != DateTime.Now.Date)
                {
                    arenics.TryRemove(match.Player2.Identity, out _);
                    arenics.TryAdd(match.Player2.Identity, arenic = new ArenicInformation(match.Player2, 0));
                }

                await match.Player2.UpdateTaskActivityAsync(ActivityManager.ActivityType.Qualifier);

                arenic.AthletePoint = match.Player2.QualifierPoints;
                arenic.DayWins = match.Player2.QualifierDayWins;
                arenic.DayLoses = match.Player2.QualifierDayLoses;
                arenic.CurrentHonor = match.Player2.HonorPoints;
                arenic.HistoryHonor = match.Player2.HistoryHonorPoints;
                await arenic.SaveAsync();

                LuaScriptManager.Run(match.Player1, null, null, string.Empty, $"UserArenicCompetes({arenic.UserId},{arenic.DayWins + arenic.DayLoses})");
            }
        }

        public async Task<ArenaQualifierUser> FindTargetAsync(ArenaQualifierUser request)
        {
            var possibleTargets = new List<ArenaQualifierUser>();
            foreach (ArenaQualifierUser target in awaitingQueue.Values
                                                            .Where(x => x.Identity != request.Identity
                                                                        && IsMatchEnable(request.Grade, x.Grade)))
            {
                possibleTargets.Add(target);
            }

            if (possibleTargets.Count == 0)
            {
                return null;
            }

            return possibleTargets[await NextAsync(possibleTargets.Count) % possibleTargets.Count];
        }

        #endregion

        #region User Management

        public int PlayersOnQueue => awaitingQueue.Count;

        public ArenaQualifierUser FindInQueue(uint idUser)
        {
            return awaitingQueue.TryGetValue(idUser, out ArenaQualifierUser value) ? value : null;
        }

        public ArenaQualifierUser EnterQueue(Character user)
        {
            if (FindInQueue(user.Identity) != null)
            {
                return null;
            }

            var queueUser = new ArenaQualifierUser
            {
                Identity = user.Identity,
                Name = user.Name,
                Level = user.Level,
                Points = (int)user.QualifierPoints,
                PreviousPkMode = user.PkMode,
                Profession = user.Profession
            };
            if (awaitingQueue.TryAdd(user.Identity, queueUser))
            {
                user.QualifierStatus = ArenaStatus.WaitingForOpponent;
                return queueUser;
            }

            return null;
        }

        public ArenaQualifierUser LeaveQueue(uint idUser)
        {
            if (awaitingQueue.TryRemove(idUser, out ArenaQualifierUser result))
            {
                Character user = RoleManager.GetUser(idUser);
                if (user != null)
                {
                    user.QualifierStatus = ArenaStatus.NotSignedUp;
                }

                return result;
            }

            return null;
        }

        public bool HasUser(uint idUser)
        {
            return FindInQueue(idUser) != null || FindMatch(idUser) != null;
        }

        public bool IsInsideMatch(uint idUser)
        {
            return FindMatch(idUser) != null;
        }

        #endregion

        #region Default Data

        public static bool IsMatchEnable(int userGrade, int targetGrade)
        {
            int nDelta = userGrade - targetGrade;
            if (nDelta < 0)
            {
                nDelta *= -1;
            }

            return nDelta < 2;
        }

        public static uint GetInitialPoints(byte level)
        {
            if (level < MIN_LEVEL)
            {
                return 0;
            }

            if (level < 90)
            {
                return StartPoints[0];
            }

            if (level < 100)
            {
                return StartPoints[1];
            }

            if (level < 110)
            {
                return StartPoints[2];
            }

            if (level < 120)
            {
                return StartPoints[3];
            }

            return StartPoints[4];
        }

        public uint GetDailyReward(uint userId)
        {
            return GetDailyReward(GetPlayerRanking(userId));
        }

        public uint GetDailyReward(int rank)
        {
            return rewards.TryGetValue((uint)rank, out var result) ? result.DayPrize : 0;
        }

        #endregion

        #region Rankings

        public List<ArenicInformation> GetSeasonRank()
        {
            return pastSeasonArenics.Values
                .OrderByDescending(x => x.AthletePoint)
                .ThenByDescending(x => x.DayWins)
                .ThenBy(x => x.DayLoses)
                .ToList();
        }

        public int GetPlayerRanking(uint idUser)
        {
            var rank = 0;
            foreach (var arenic in arenics.Values
                .Where(x => x.DayWins > 0 || x.DayLoses > 0)
                .OrderByDescending(x => x.AthletePoint)
                .ThenByDescending(x => x.DayWins)
                .ThenBy(x => x.DayLoses))
            {
                if (idUser == arenic.UserId)
                {
                    return rank + 1;
                }

                rank++;
            }

            return 0;
        }

        public bool GetReward(uint idUser, out DbArenicHonor value)
        {
            if (rewards.TryGetValue((uint)GetPlayerRanking(idUser), out DbArenicHonor result))
            {
                value = result;
                return true;
            }

            value = default;
            return false;
        }

        public List<ArenicInformation> GetRanking(int page)
        {
            const int ipp = 10;
            return arenics.Values
                .Where(x => x.DayWins > 0 || x.DayLoses > 0)
                .OrderByDescending(x => x.AthletePoint)
                .ThenByDescending(x => x.DayWins)
                .ThenBy(x => x.DayLoses)
                .Skip(ipp * page)
                .Take(ipp)
                .ToList();
        }

        public int RankCount()
        {
            return arenics.Values.Where(x => x.DayWins > 0 || x.DayLoses > 0).Count();
        }

        #endregion

        #region Witness

        public async Task WatchAsync(Character user, uint target)
        {
            ArenaQualifierUserMatch match = FindMatch(target);
            if (match == null)
            {
                return;
            }

            if (FindMatch(user.Identity) != null)
            {
                return;
            }

            if (!match.IsRunning)
            {
                return;
            }

            Point targetPos = await match.Map.QueryRandomPositionAsync();
            if (targetPos == default)
            {
                return;
            }

            if (user.Map.IsRecordDisable())
            {
                uint idMap = DEFAULT_MAP_ID;
                Point pos = new(DEFAULT_MAP_X, DEFAULT_MAP_Y);
                if (user.Map.GetRebornMap(ref idMap, ref pos))
                {
                    await user.SavePositionAsync(idMap, (ushort)pos.X, (ushort)pos.Y);
                }
                else
                {
                    await user.SavePositionAsync(DEFAULT_MAP_ID, DEFAULT_MAP_X, DEFAULT_MAP_Y);
                }
            }
            else
            {
                await user.SavePositionAsync(user.MapIdentity, user.X, user.Y);
            }

            await user.FlyMapAsync(match.MapIdentity, targetPos.X, targetPos.Y);
        }

        public async Task WitnessExitAsync(Character user)
        {
            if (!user.IsArenicWitness())
            {
                return;
            }

            await user.SendAsync(new MsgArenicWitness
            {
                Action = WitnessAction.Leave
            });
            await user.FlyMapAsync(user.RecordMapIdentity, user.RecordMapX, user.RecordMapY);
        }

        public async Task WitnessVoteAsync(Character user, uint target)
        {
            ArenaQualifierUserMatch match = FindMatch(target);
            if (match == null)
            {
                return;
            }

            if (match.MapIdentity != user.MapIdentity)
            {
                return;
            }

            match.Wave(user, target);
            await match.SendBoardAsync();
        }

        public bool IsWitness(Character user)
        {
            if (FindMatch(user.Identity) != null)
                return false;

            var match = FindMatchByMap(user.MapIdentity);
            if (match == null)
            {
                return false;
            }

            if (match.Player1.Identity == user.Identity || match.Player2.Identity == user.Identity)
            {
                return false;
            }

            return true;
        }

        #endregion

        #region Socket

        public static Task SendArenaInformationAsync(Character target)
        {
            return target.SendAsync(new MsgQualifyingDetailInfo
            {
                Ranking = target.QualifierRank,
                CurrentHonor = target.HonorPoints,
                HistoryHonor = target.HistoryHonorPoints,
                Points = target.QualifierPoints,
                TodayWins = target.QualifierDayWins,
                TodayLoses = target.QualifierDayLoses,
                TotalWins = target.QualifierHistoryWins,
                TotalLoses = target.QualifierHistoryLoses,
                Status = target.QualifierStatus,
                TriumphToday9 = (byte)Math.Min(9, target.QualifierDayWins),
                TriumphToday20 = (byte)Math.Min(20, target.QualifierDayGames)
            });
        }

        #endregion
    }
}
