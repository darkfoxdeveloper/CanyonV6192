using Canyon.Database.Entities;
using Canyon.Game.Database.Repositories;
using Canyon.Game.States.Items;
using Canyon.Game.States.User;
using System.Collections.Concurrent;

namespace Canyon.Game.Services.Managers
{
    public class ActivityManager
    {
        public static uint StcEventId = 2822;

        private static readonly ILogger logger = LogFactory.CreateLogger<ActivityManager>();

        private static ConcurrentDictionary<uint, DbActivityRewardType> rewardTypes = new();
        private static ConcurrentDictionary<uint, DbActivityTaskType> taskTypes = new();

        public static async Task InitializeAsync()
        {
            logger.LogInformation($"Activity manager initializing");

            foreach (var rewardType in ActivityRepository.GetRewards())
            {
                rewardTypes.TryAdd(rewardType.Id, rewardType);
            }

            foreach (var task in ActivityRepository.GetTasks())
            {
                taskTypes.TryAdd(task.Id, task);
            }
        }

        public static IList<DbActivityTaskType> GetDisponibleTaskByUser(Character user)
        {
            List<DbActivityTaskType> taskTypes = new List<DbActivityTaskType>();
            foreach (var task in ActivityManager.taskTypes.Values.OrderByDescending(x => x.Id))
            {
                int openRebirth = (int)(task.OpenLev / 1000);
                int openLevel = (int)(task.OpenLev % 1000);
                int closeRebirth = (int)(task.CloseLev / 1000);
                int closeLevel = (int)(task.CloseLev % 1000);

                if (user.Metempsychosis < openRebirth || user.Metempsychosis > closeRebirth)
                {
                    continue;
                }

                if (user.Level < openLevel || user.Level > closeLevel)
                {
                    continue;
                }

                taskTypes.Add(task);
            }
            return taskTypes.DistinctBy(x => x.Type).ToList();
        }

        public static DbActivityTaskType GetTaskById(uint id)
        {
            return taskTypes.TryGetValue(id, out var result) ? result : null;
        }

        public static DbActivityTaskType GetTaskByType(Character user, ActivityType activityType)
        {
            return GetDisponibleTaskByUser(user).FirstOrDefault(x => x.Type == (int)activityType);
        }

        public static ActivityType GetTaskTypeById(uint idTask)
        {
            return ((ActivityType?)taskTypes.Values.FirstOrDefault(x => x.Id == idTask)?.Type) ?? ActivityType.None;
        }

        public static async Task<bool> ClaimRewardAsync(Character user, uint rewardId)
        {
            if (rewardId == 0) {  return false; }

            DbActivityRewardType rewardType = null;
            foreach (var task in rewardTypes.Values
                .Where(x => x.RewardGrade == rewardId)
                .OrderByDescending(x => x.Metempsychosis))
            {
                if (task.Metempsychosis <= user.Metempsychosis)
                {
                    rewardType = task;
                    break;
                }
            }

            if (rewardType == null)
            {
                return false;
            }

            if (user.Statistic.HasEvent(StcEventId, rewardId))
            {
                var evt = user.Statistic.GetStc(StcEventId, rewardId);
                var evtStamp = UnixTimestamp.ToNullableDateTime(evt?.Timestamp);
                if (evtStamp.HasValue 
                    && DateTime.Now.Date <= evtStamp.Value.Date)
                {
                    return false;
                }
            }

            if (user.ActivityPoints < rewardType.ActivityReq)
            {
                return false;
            }

            if (!user.UserPackage.IsPackSpare(3))
            {
                await user.SendAsync(string.Format(StrNotEnoughSpaceN, 3));
                return false;
            }

            if (rewardType.Reward1Num > 0 && rewardType.Reward1 != 0)
            {
                for (int i = 0; i < rewardType.Reward1Num; i++)
                {
                    await user.UserPackage.AwardItemAsync(rewardType.Reward1, Item.ItemPosition.Inventory, rewardType.Reward1Mono != 0, true);
                }
            }

            if (rewardType.Reward2Num > 0 && rewardType.Reward2 != 0)
            {
                for (int i = 0; i < rewardType.Reward2Num; i++)
                {
                    await user.UserPackage.AwardItemAsync(rewardType.Reward2, Item.ItemPosition.Inventory, rewardType.Reward2Mono != 0, true);
                }
            }

            if (rewardType.Reward3Num > 0 && rewardType.Reward3 != 0)
            {
                for (int i = 0; i < rewardType.Reward3Num; i++)
                {
                    await user.UserPackage.AwardItemAsync(rewardType.Reward3, Item.ItemPosition.Inventory, rewardType.Reward3Mono != 0, true);
                }
            }

            await user.Statistic.AddOrUpdateAsync(StcEventId, rewardId, 1, true);
            return true;
        }

        public enum ActivityType
        {
            None,
            /// <summary>
            /// Login to get Active Points.
            /// </summary>
            LoginTheGame,
            /// <summary>
            /// Keep online for 0.5 hour.
            /// </summary>
            HalfHourOnline,
            /// <summary>
            /// Become a VIP player to get Active Points.
            /// </summary>
            VipActiveness,
            /// <summary>
            /// Take Daily Quests at Daily Quest Envoy in Market.
            /// </summary>
            DailyQuest,
            /// <summary>
            /// Compete in the Qualifier.
            /// </summary>
            Qualifier,
            /// <summary>
            /// Complete in the Team Qualifier.
            /// </summary>
            TeamQualifier,
            /// <summary>
            /// Join the Champion`s Arena.
            /// </summary>
            ChampionsArena = 7,
            /// <summary>
            /// Study Chi once on the Chi window.
            /// </summary>
            ChiStudy = 8,
            /// <summary>
            /// Training once on the Chi window.
            /// </summary>
            JiangHu = 9,
            /// <summary>
            /// Sign up for Treasure in the Blue with Squidward Octopus (TwinCity 290,208).
            /// </summary>
            TreasureInBlue = 10,
            /// <summary>
            /// Send flowers/gift to get Active Points.
            /// </summary>
            FlowerGifts = 11,
            /// <summary>
            /// Enlighten low-level players.
            /// </summary>
            Enlightenment,
            /// <summary>
            /// Talk to Lady Luck to play Lottery.
            /// </summary>
            Lottery = 14,
            /// <summary>
            /// Join the Horse Racing everyday.
            /// </summary>
            HorseRacing = 15,
        }
    }
}
