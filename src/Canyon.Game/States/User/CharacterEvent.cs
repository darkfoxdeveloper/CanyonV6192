using Canyon.Database.Entities;
using Canyon.Game.Database;
using Canyon.Game.Database.Repositories;
using Canyon.Game.Services.Managers;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States.Events;
using Canyon.Game.States.Events.Qualifier.TeamQualifier;
using Canyon.Game.States.Events.Qualifier.UserQualifier;
using Canyon.Game.States.MessageBoxes;
using System.Collections.Concurrent;
using static Canyon.Game.Services.Managers.ActivityManager;
using static Canyon.Game.States.Events.Mount.HorseRacing;

namespace Canyon.Game.States.User
{
    public partial class Character
    {
        #region Game Action

        public long Iterator { get; set; } = -1;
        public long[] VarData { get; } = new long[MAX_VAR_AMOUNT];
        public string[] VarString { get; } = new string[MAX_VAR_AMOUNT];

        private readonly List<string> setTaskId = new();

        public uint LastAddItemIdentity { get; set; }

        public UserStatistic Statistic { get; init; }
        public TaskDetail TaskDetail { get; init; }

        public MessageBox MessageBox { get; set; }

        public uint InteractingItem { get; set; }
        public uint InteractingNpc { get; set; }
        public uint InteractingMouseAction { get; set; }
        public string InteractingMouseFunction { get; set; }

        public bool CheckItem(DbTask task)
        {
            if (!string.IsNullOrEmpty(task.Itemname1) && !StrNone.Equals(task.Itemname1))
            {
                if (UserPackage[task.Itemname1] == null)
                {
                    return false;
                }

                if (!string.IsNullOrEmpty(task.Itemname2) && !StrNone.Equals(task.Itemname2))
                {
                    if (UserPackage[task.Itemname2] == null)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public void CancelInteraction()
        {
            setTaskId.Clear();
            InteractingItem = 0;
            InteractingNpc = 0;
        }

        public byte PushTaskId(string idTask)
        {
            if (!"0".Equals(idTask) && setTaskId.Count < MAX_MENUTASKSIZE)
            {
                setTaskId.Add(idTask);
                return (byte)setTaskId.Count;
            }

            return 0;
        }

        public void ClearTaskId()
        {
            setTaskId.Clear();
        }

        public string GetTaskId(int idx)
        {
            return idx > 0 && idx <= setTaskId.Count ? setTaskId[idx - 1] : "0";
        }

        public bool TestTask(DbTask task)
        {
            if (task == null)
            {
                return false;
            }

            try
            {
                if (!CheckItem(task))
                {
                    return false;
                }

                if (Silvers < task.Money)
                {
                    return false;
                }

                if (task.Profession != 0 && Profession != task.Profession)
                {
                    return false;
                }

                if (task.Sex != 0 && task.Sex != 999 && task.Sex != Gender)
                {
                    return false;
                }

                if (PkPoints < task.MinPk || PkPoints > task.MaxPk)
                {
                    return false;
                }

                if (task.Marriage >= 0)
                {
                    if (task.Marriage == 0 && MateIdentity != 0)
                    {
                        return false;
                    }

                    if (task.Marriage == 1 && MateIdentity == 0)
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Test task error", ex.Message);
                return false;
            }

            return true;
        }

        public async Task AddTaskMaskAsync(int idx)
        {
            if (idx < 0 || idx >= 32)
            {
                return;
            }

            character.TaskMask |= 1u << idx;
            await SaveAsync();
        }

        public async Task ClearTaskMaskAsync(int idx)
        {
            if (idx < 0 || idx >= 32)
            {
                return;
            }

            character.TaskMask &= ~(1u << idx);
            await SaveAsync();
        }

        public bool CheckTaskMask(int idx)
        {
            if (idx < 0 || idx >= 32)
            {
                return false;
            }

            return (character.TaskMask & (1u << idx)) != 0;
        }

        #endregion

        #region Event

        public ConcurrentDictionary<GameEvent.EventType, GameEvent> GameEvents { get; init; } = new();

        public T GetEvent<T>() where T : GameEvent
        {
            return GameEvents.Values.FirstOrDefault(x => x.GetType().Equals(typeof(T))) as T;
        }

        public GameEvent GetCurrentEvent()
        {
            return GameEvents.Values.FirstOrDefault(x => x.IsInEventMap(MapIdentity));
        }

        public async Task<bool> SignInEventAsync(GameEvent e)
        {            
            if (!e.IsAllowedToJoin(this) || !GameEvents.TryAdd(e.Identity, e))
            {
                return false;
            }

            await e.OnEnterAsync(this);
            return true;
        }

        public async Task<bool> SignOutEventAsync(GameEvent e)
        {
            if (GameEvents.TryRemove(e.Identity, out _))
            {
                await e.OnExitAsync(this);
            }
            return true;
        }

        public bool IsInQualifierEvent()
        {
            return GameEvents.Any(x => GameEvent.IsQualifierEvent(x.Key));
        }

        #endregion

        #region Arena Qualifier

        public int QualifierRank => EventManager.GetEvent<ArenaQualifier>()?.GetPlayerRanking(Identity) ?? 0;

        public ArenaStatus QualifierStatus { get; set; } = ArenaStatus.NotSignedUp;

        public uint QualifierPoints
        {
            get => character.AthletePoint;
            set => character.AthletePoint = value;
        }

        public uint QualifierDayWins
        {
            get => character.AthleteDayWins;
            set => character.AthleteDayWins = value;
        }

        public uint QualifierDayLoses
        {
            get => character.AthleteDayLoses;
            set => character.AthleteDayLoses = value;
        }

        public uint QualifierDayGames => QualifierDayWins + QualifierDayLoses;

        public uint QualifierHistoryWins
        {
            get => character.AthleteHistoryWins;
            set => character.AthleteHistoryWins = value;
        }

        public uint QualifierHistoryLoses
        {
            get => character.AthleteHistoryLoses;
            set => character.AthleteHistoryLoses = value;
        }

        public uint HonorPoints
        {
            get => character.AthleteCurrentHonorPoints;
            set => character.AthleteCurrentHonorPoints = value;
        }

        public uint HistoryHonorPoints
        {
            get => character.AthleteHistoryHonorPoints;
            set => character.AthleteHistoryHonorPoints = value;
        }

        public bool IsArenicWitness()
        {
            var arenicEvents = EventManager.QueryWitnessEvents();
            foreach (var witnessEvent in arenicEvents)
            {
                if (witnessEvent.IsWitness(this))
                    return true;
            }
            return false;
        }

        #endregion

        #region Team Arena Qualifier

        public int TeamQualifierRank => EventManager.GetEvent<TeamArenaQualifier>()?.GetPlayerRanking(Identity) ?? 0;

        public ArenaStatus TeamQualifierStatus => Team?.QualifierStatus ?? ArenaStatus.NotSignedUp;

        public uint TeamQualifierPoints
        {
            get => character.TeamAthletePoint;
            set => character.TeamAthletePoint = value;
        }

        public uint TeamQualifierDayWins
        {
            get => character.TeamAthleteDayWins;
            set => character.TeamAthleteDayWins = value;
        }

        public uint TeamQualifierDayLoses
        {
            get => character.TeamAthleteDayLoses;
            set => character.TeamAthleteDayLoses = value;
        }

        public uint TeamQualifierDayGames => QualifierDayWins + QualifierDayLoses;

        public uint TeamQualifierHistoryWins
        {
            get => character.TeamAthleteHistoryWins;
            set => character.TeamAthleteHistoryWins = value;
        }

        public uint TeamQualifierHistoryLoses
        {
            get => character.TeamAthleteHistoryLoses;
            set => character.TeamAthleteHistoryLoses = value;
        }

        #endregion

        #region Progress Bar

        public ProgressBar AwaitingProgressBar { get; set; }

        public class ProgressBar
        {
            private readonly TimeOut timeOut;

            public ProgressBar(int seconds)
            {
                timeOut = new TimeOut();
                timeOut.Startup(seconds);
            }

            public uint IdNext { get; init; }
            public uint IdNextFail { get; init; }
            public uint Command { get; init; }
            public bool Completed => timeOut.IsActive() && timeOut.IsTimeOut();
        }

        #endregion

        #region Achievements

        public Achievements Achievements { get; set; }

        #endregion

        public Task SendMenuMessageAsync(string message)
        {
            return SendAsync(new MsgTaskDialog
            {
                InteractionType = MsgTaskDialog.TaskInteraction.MessageBox,
                Text = message
            });
        }

        #region Activity Tasks

        private readonly TimeOut activityLoginHalfHourTimer = new();

        public ConcurrentDictionary<uint, ActivityTask> ActivityTasks = new ();

        public int ActivityPoints
        {
            get
            {
                int result = 0;
                foreach (var task in ActivityTasks.Values)
                {
                    var activity = GetTaskById(task.ActivityId);
                    result += (activity?.Activity ?? 0) * task.Schedule;
                }
                return result;
            }
        }

        public async Task InitializeActivityTasksAsync()
        {
            var tasks = ActivityRepository.GetUserTasks(Identity);
            if (tasks.Count > 0)
            {
                foreach (var task in tasks)
                {
                    var type = GetTaskTypeById(task.ActivityId);
                    if (type != ActivityType.None)
                    {
                        ActivityTasks.TryAdd(task.ActivityId, new ActivityTask(task));
                    }
                }

                await CheckForActivityTaskUpdatesAsync();
            }
            else
            {
                foreach (var availableTask in GetDisponibleTaskByUser(this))
                {
                    var task = new ActivityTask(new DbActivityUserTask
                    {
                        ActivityId = availableTask.Id,
                        UserId = Identity
                    });
                    ActivityTasks.TryAdd(availableTask.Id, task);
                    await task.SaveAsync();
                }
            }

            await SubmitActivityListAsync();
            await SubmitActivityPointsAsync();

            for (int i = 1; i <= 5; i++)
            {
                var evt = Statistic.GetStc(StcEventId, (uint)i);
                var evtStamp = UnixTimestamp.ToNullableDateTime(evt?.Timestamp);
                if (evtStamp.HasValue
                    && DateTime.Now.Date <= evtStamp.Value.Date)
                {
                    await SendAsync(new MsgActivityTaskReward
                    {
                        RewardGrade = (byte) i
                    });
                }
            }

            activityLoginHalfHourTimer.Startup(60 * 30);
        }

        public async Task<bool> CheckForActivityTaskUpdatesAsync()
        {
            bool result = false;
            foreach (var availableTask in GetDisponibleTaskByUser(this))
            {
                if (ActivityTasks.Values.All(x => (byte) x.Type != availableTask.Type))
                {
                    var dbTask = ActivityRepository.GetUserTasks(Identity).FirstOrDefault(x => x.ActivityId == availableTask.Id);
                    dbTask ??= new DbActivityUserTask
                    {
                        ActivityId = availableTask.Id,
                        UserId = Identity
                    };
                    var task = new ActivityTask(dbTask);
                    ActivityTasks.TryAdd(availableTask.Id, task);
                    await task.SaveAsync();
                    result = true;
                }
            }
            return result;
        }

        public async Task UpdateTaskActivityAsync(ActivityType activityType)
        {
            ActivityTask activityTask = null;
            foreach (var activity in  ActivityTasks.Values)
            {
                var act = GetTaskTypeById(activity.ActivityId);
                if (act == activityType)
                {
                    activityTask = activity;
                    break;
                }
            }

            if (activityTask == null)
            {
                return;
            }

            if (activityTask.CompleteFlag != 0)
            {
                return;
            }

            byte newAmount = (byte)(activityTask.Schedule + 1);
            var task = GetTaskById(activityTask.ActivityId);
            if (newAmount > task.MaxNum)
            {
                return;
            }

            activityTask.Schedule = newAmount;
            if (activityTask.Schedule >= task.MaxNum)
            {
                activityTask.CompleteFlag = 1;
            }

            await activityTask.SaveAsync();

            await SendAsync(new MsgActivityTask
            {
                Mode = MsgActivityTask.Action.UpdateActivityTask,
                Activities = new List<MsgActivityTask.Activity>
                {
                    new MsgActivityTask.Activity
                    {
                        Id = activityTask.ActivityId,
                        Completed = activityTask.CompleteFlag,
                        Progress = activityTask.Schedule
                    }
                }
            });

            await SubmitActivityPointsAsync();
        }

        public Task SubmitActivityListAsync()
        {
            MsgActivityTask msg = new();
            msg.Mode = MsgActivityTask.Action.UpdateActivityTask;
            foreach (var task in ActivityTasks.Values)
            {
                msg.Activities.Add(new MsgActivityTask.Activity
                {
                    Id = task.ActivityId,
                    Completed = task.CompleteFlag,
                    Progress = task.Schedule
                });
            }
            return SendAsync(msg);
        }

        public Task SubmitActivityPointsAsync()
        {
            return SendAsync(new MsgStatisticDaily
            {
                Mode = 1,
                EventId = 2,
                DataType = 1,
                ActivityPoints = ActivityPoints,
                Unknown17 = 2,
                Unknown21 = 2
            });
        }

        public async Task ActivityTasksDailyResetAsync()
        {
            ActivityTasks.Clear();
            await CheckForActivityTaskUpdatesAsync();
            activityLoginHalfHourTimer.Update();
            await UpdateTaskActivityAsync(ActivityType.LoginTheGame);
            if (VipLevel > 0)
            {
                await UpdateTaskActivityAsync(ActivityType.VipActiveness);
            }
            await SubmitActivityListAsync();
            await SubmitActivityPointsAsync();
        }

        #endregion

        #region Horse Racing

        private const ushort SCREAM_BOMB_MAGICTYPE = 9988;
        private const ushort SLUGISH_POTION_MAGICTYPE = 9989;

        private KeyValuePair<ItemType, int>[] raceItems = new KeyValuePair<ItemType, int>[5];

        public async Task<bool> AwardRaceItemAsync(ItemType itemType)
        {
            for (int i = 0; i < raceItems.Length; i++)
            {
                if (raceItems[i].Key == ItemType.Null || raceItems[i].Key == 0 || raceItems[i].Key == itemType)
                {
                    if (raceItems[i].Key != itemType)
                    {
                        raceItems[i] = new KeyValuePair<ItemType, int>(itemType, 1);
                    }
                    else
                    {
                        raceItems[i] = new KeyValuePair<ItemType, int>(itemType, raceItems[i].Value + 1);
                    }

                    await SendAsync(new MsgRaceTrackProp
                    {
                        Amount = (ushort)raceItems[i].Value,
                        PotionType = itemType,
                        Index = i + 1
                    });
                    return true;
                }
            }
            return false;
        }

        public async Task SpendRaceItemAsync(int index, Role target = null)
        {
            if (raceItems[index].Key == ItemType.Null || raceItems[index].Key == 0)
            {
                return;
            }

            if (raceItems[index].Value <= 0)
            {
                raceItems[index] = new KeyValuePair<ItemType, int>();
                return;
            }

            var itemType = raceItems[index].Key;
            int power = QueryStatus((int)itemType)?.Power ?? 0;

            await DetachStatusAsync((int)itemType);

            if (raceItems[index].Value - 1 > 0)
            {
                raceItems[index] = new KeyValuePair<ItemType, int>(raceItems[index].Key, raceItems[index].Value - 1);
            }
            else
            {
                raceItems[index] = new KeyValuePair<ItemType, int>();
            }
            await SendAsync(new MsgRaceTrackProp
            {
                Amount = (ushort)raceItems[index].Value,
                PotionType = itemType,
                Index = index + 1
            });

            switch (itemType)
            {
                case ItemType.ChaosBomb:
                    {
                        var magicType = MagicManager.GetMagictype(SCREAM_BOMB_MAGICTYPE, 0);
                        if (magicType == null)
                        {
                            break;
                        }

                        MsgMagicEffect msg = new MsgMagicEffect
                        {
                            AttackerIdentity = Identity,
                            MapX = X,
                            MapY = Y,
                            MagicIdentity = (ushort)magicType.Type
                        };
                        List<Character> targets = new List<Character>();
                        foreach (var targetUser in Screen.Roles.Values.Where(x => x is Character).Cast<Character>())
                        {
                            if (targetUser.GetDistance(this) < magicType.Distance)
                            {
                                msg.Append(targetUser.Identity, 1, true);
                                targets.Add(targetUser);
                            }
                        }
                        await BroadcastRoomMsgAsync(msg, true);

                        foreach (var targetUser in targets)
                        {
                            await targetUser.AttachStatusAsync(StatusSet.CONFUSED, 0, 10, 0);
                        }
                        break;
                    }
                case ItemType.SpiritPotion:
                    {
                        break;
                    }
                case ItemType.ExcitementPotion:
                    {
                        await DetachStatusAsync(StatusSet.DECELERATED);
                        await AttachStatusAsync(StatusSet.ACCELERATED, 50, 15, 0);
                        break;
                    }
                case ItemType.ScreamBomb:
                    {
                        var magicType = MagicManager.GetMagictype(SCREAM_BOMB_MAGICTYPE, 0);
                        if (magicType == null)
                        {
                            break;
                        }

                        MsgMagicEffect msg = new MsgMagicEffect
                        {
                            AttackerIdentity = Identity,
                            MapX = X,
                            MapY = Y,
                            MagicIdentity = (ushort)magicType.Type
                        };
                        List<Character> targets = new List<Character>();
                        foreach (var targetUser in Screen.Roles.Values.Where(x => x is Character).Cast<Character>())
                        {
                            if (targetUser.GetDistance(this) < magicType.Distance)
                            {
                                msg.Append(targetUser.Identity, 1, true);
                                targets.Add(targetUser);
                            }
                        }
                        await BroadcastRoomMsgAsync(msg, true);

                        foreach (var targetUser in targets)
                        {
                            await targetUser.AttachStatusAsync(StatusSet.FRIGHTENED, 0, 10, 0);
                        }
                        break;
                    }
                case ItemType.SluggishPotion:
                    {
                        var magicType = MagicManager.GetMagictype(SLUGISH_POTION_MAGICTYPE, 0);
                        if (magicType == null)
                        {
                            break;
                        }

                        MsgMagicEffect msg = new MsgMagicEffect
                        {
                            AttackerIdentity = Identity,
                            MapX = X,
                            MapY = Y,
                            MagicIdentity = (ushort)magicType.Type
                        };
                        List<Character> targets = new List<Character>();
                        foreach (var targetUser in Screen.Roles.Values.Where(x => x is Character).Cast<Character>())
                        {
                            if (targetUser.GetDistance(this) < magicType.Distance)
                            {
                                msg.Append(targetUser.Identity, 1, true);
                                targets.Add(targetUser);
                            }
                        }
                        await BroadcastRoomMsgAsync(msg, true);

                        foreach (var targetUser in targets)
                        {
                            await targetUser.AttachStatusAsync(StatusSet.DECELERATED, 50, 10, 0);
                        }
                        break;
                    }
                case ItemType.GuardPotion:
                    {
                        await AttachStatusAsync(StatusSet.GODLY_SHIELD, 0, 10, 0);
                        break;
                    }
                case ItemType.DizzyHammer:
                    {
                        if (target is Character targetUser)
                        {
                            await target.AttachStatusAsync(StatusSet.DIZZY, 0, 5, 0);
                        }
                        break;
                    }
                case ItemType.TransformItem:
                    {
                        for (int i = 0; i < raceItems.Length; i++)
                        {
                            var item = raceItems[i];

                            if (item.Key == 0 || item.Key == ItemType.Null) continue;

                            ItemType old = item.Key;
                            int min = (int)ItemType.ChaosBomb;
                            int max = (int)ItemType.SuperExcitementPotion;
                            item = new KeyValuePair<ItemType, int>((ItemType)await NextAsync(min, max), item.Value);

                            if (old == item.Key || item.Key == ItemType.TransformItem || item.Key == (ItemType)8333) // no frozen trap
                            {
                                i--;
                                continue;
                            }

                            raceItems[i] = item;

                            await SendAsync(new MsgRaceTrackProp
                            {
                                Amount = (ushort)item.Value,
                                PotionType = item.Key,
                                Index = i + 1
                            });
                        }
                        break;
                    }
                case ItemType.RestorePotion:
                    {
                        await AddAttributesAsync(ClientUpdateType.Vigor, power);
                        break;
                    }
                case ItemType.SuperExcitementPotion:
                    {
                        await DetachStatusAsync(StatusSet.DECELERATED);
                        await AttachStatusAsync(StatusSet.ACCELERATED, 200, 15, 0);
                        break;
                    }
            }
        }

        public async Task ClearRaceItemsAsync()
        {
            for (int i = 0; i < raceItems.Length; i++)
            {
                raceItems[i] = new KeyValuePair<ItemType, int>();
            }
        }

        #endregion

        #region Golden League

        private DbGoldenLeagueData goldenLeagueData;

        public uint GoldenLeaguePoints
        {
            get => goldenLeagueData?.CurrentPoint ?? 0;
            set => goldenLeagueData.CurrentPoint = value;
        }
        public uint GoldenLeaguePointsHistory
        {
            get => goldenLeagueData?.TotalPoint ?? 0;
            set => goldenLeagueData.TotalPoint = value;
        }

        public async Task LoadGoldenLeagueDataAsync()
        {
            goldenLeagueData = await GoldenLeagueRepository.GetAsync(Identity) ?? new DbGoldenLeagueData
            {
                UserId = Identity
            };
            await SendGoldenLeaguePointsAsync();
        }

        public async Task<bool> ChangeGoldenLeaguePointsAsync(long amount, bool notify = false)
        {
            if (amount > 0)
            {
                await AwardGoldenLeaguePointsAsync(amount);
                return true;
            }
            if (amount < 0)
            {
                return await SpendMoneyAsync(amount * -1, notify);
            }
            return false;
        }

        public async Task AwardGoldenLeaguePointsAsync(long amount)
        {
            GoldenLeaguePoints = (uint)Math.Max(0, Math.Min(GoldenLeaguePoints + (ulong)amount, 10_000_000));
            await SaveGoldenLeagueDataAsync();
            await SendGoldenLeaguePointsAsync();
        }

        public async Task<bool> SpendGoldenLeaguePointsAsync(long amount)
        {
            if ((ulong)amount > GoldenLeaguePoints)
            {
                return false;
            }
            GoldenLeaguePoints = (uint)Math.Max(0, Math.Min(GoldenLeaguePoints - (ulong)amount, 10_000_000));
            await SaveGoldenLeagueDataAsync();
            await SendGoldenLeaguePointsAsync();
            return true;
        }

        public Task SendGoldenLeaguePointsAsync()
        {
            return SendAsync(new MsgGLRankingList(GoldenLeaguePoints, 0));
        }

        private Task SaveGoldenLeagueDataAsync()
        {
            if (goldenLeagueData == null)
            {
                return Task.CompletedTask;
            }
            return ServerDbContext.SaveAsync(goldenLeagueData);
        }

        #endregion
    }
}
