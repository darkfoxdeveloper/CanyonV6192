using Canyon.Database.Entities;
using Canyon.Game.Database.Repositories;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States.Items;
using Canyon.Game.States.User;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using static Canyon.Game.States.Items.Item;

namespace Canyon.Game.Services.Managers
{
    public class ProcessGoalManager
    {
        public const uint ProgressStatisticId = 2830;
        public const uint CompletionStatisticId = 2831;
        public const uint RewardClaimStatisticId = 2832;

        private static readonly ILogger logger = LogFactory.CreateLogger<ProcessGoalManager>();

        private static ConcurrentDictionary<uint, DbProcessGoal> goals = new();
        private static ConcurrentDictionary<uint, DbProcessTask> tasks = new();

        public static async Task InitializeAsync()
        {
            logger.LogInformation("Initializing process goal manager");

            foreach (var goal in StageGoalRepository.GetGoals()) 
            { 
                goals.TryAdd(goal.Id, goal);
            }

            foreach (var task in StageGoalRepository.GetTasks())
            {
                tasks.TryAdd(task.Id, task);
            }
        }

        private static List<DbProcessTask> GetTasksByGoalType(GoalType goalType)
        {
            return tasks.Values.Where(x => x.Type == (ushort)goalType).ToList();
        }

        public static async Task SubmitGoalsAsync(Character user, ushort id)
        {
            MsgProcessGoalTask msg = new MsgProcessGoalTask
            {
                Param = id,
                Completed = IsStageCompleted(user, id)
            };
            int start = id * 100;
            int end = id * 100 + 99;
            foreach (var task in tasks.Values.Where(x => x.Id >= start && x.Id <= end))
            {
                int completion = (int)user.Statistic.GetValue(CompletionStatisticId, task.Id);

                if (completion != 0)
                {
                    bool claimed = user.Statistic.GetValue(RewardClaimStatisticId, task.Id) != 0;
                    msg.Goals.Add(new MsgProcessGoalTask.GoalTaskStruct
                    {
                        Id = (int)task.Id,
                        Unknown = 0,
                        Claimed = claimed
                    });
                }
            }
            await user.SendAsync(msg);
        }

        public static async Task<bool> ClaimStageRewardAsync(Character user, ushort id)
        {
            var task = tasks.Values.FirstOrDefault(x => x.Id == id);
            if (task == null)
            {
                return false;
            }

            int completion = (int)user.Statistic.GetValue(CompletionStatisticId, id);
            if (completion == 0)
            {
                return false;
            }

            bool claimed = user.Statistic.GetValue(RewardClaimStatisticId, id) != 0;
            if (claimed)
            {
                return false;
            }

            if (!user.UserPackage.IsPackSpare(3))
            {
                await user.SendAsync(string.Format(StrNotEnoughSpaceN, 3));
                return false;
            }

            for (int i = 0; i < task.Number; i++)
            {
                await user.UserPackage.AwardItemAsync(task.ItemType, ItemPosition.Inventory, task.Monopoly != 0);
            }

            //for (int i = 0; i < task.Number2; i++)
            //{
            //    await user.UserPackage.AwardItemAsync(task.ItemType2, ItemPosition.Inventory, task.Monopoly2 != 0);
            //}

            //for (int i = 0; i < task.Number3; i++)
            //{
            //    await user.UserPackage.AwardItemAsync(task.ItemType3, ItemPosition.Inventory, task.Monopoly3 != 0);
            //}

            await user.Statistic.AddOrUpdateAsync(RewardClaimStatisticId, id, 1, true);
            return true;
        }

        public static bool IsStageCompleted(Character user, ushort id)
        {
            int start = id * 100;
            int end = id * 100 + 99;
            foreach (var task in tasks.Values.Where(x => x.Id >= start && x.Id <= end))
            {
                if (user.Statistic.GetValue(CompletionStatisticId, task.Id) == 0)
                {
                    return false;
                }
            }
            return true;
        }

        public static Task IncreaseProgressAsync(Character user, GoalType goal, long value = 1)
        {
            var progress = user.Statistic.GetStc(ProgressStatisticId, (uint)goal);
            if (progress != null)
            {
                value += progress.Data;
            }
            return SetProgressAsync(user, goal, value);
        }

        public static async Task SetProgressAsync(Character user, GoalType goalType, long value)
        {
            var goals = GetTasksByGoalType(goalType);
            await user.Statistic.AddOrUpdateAsync(ProgressStatisticId, (uint)goalType, (uint)value, true);
            
            foreach (var goal in goals)
            {                
                var completion = user.Statistic.GetStc(CompletionStatisticId, goal.Id);
                if (completion != null && completion.Data != 0)
                {
                    continue;
                }
                
                if (value < goal.Condition)
                {
                    continue;
                }

                await user.Statistic.AddOrUpdateAsync(CompletionStatisticId, goal.Id, 1, true);
            }
        }

        private static async Task ProcessUserChangeGoalDataAsync(Character user, DbProcessTask task, bool forceCompletion)
        {
            var completion = user.Statistic.GetStc(CompletionStatisticId, task.Id);
            if (completion != null && completion.Data > 0)
            {
                return;
            }

            bool completed = forceCompletion;
            if (!forceCompletion)
            {
                uint data = 0;
                switch ((GoalType)task.Type)
                {
                    case GoalType.LevelUp:
                        {
                            data = user.Level;
                            completed = user.Level >= task.Schedule;
                            break;
                        }
                    case GoalType.Metempsychosis:
                        {
                            data = user.Metempsychosis;
                            completed = user.Metempsychosis >= task.Schedule;
                            break;
                        }
                    case GoalType.BegginerTutorialCompletion:
                        {
                            data = 1;
                            var taskDetail = user.TaskDetail.QueryTaskData(task.Condition);
                            if (taskDetail != null && taskDetail.CompleteFlag != 0)
                            {
                                completed = true;
                            }
                            break;
                        }
                    case GoalType.XpSkillKills:
                        {
                            data = (uint)user.KoCount;
                            completed = data >= task.Schedule;
                            break;
                        }
                    case GoalType.EquipmentQuality:
                        {
                            uint count = 0;
                            for (ItemPosition pos = ItemPosition.EquipmentBegin; pos <= ItemPosition.EquipmentEnd; pos++)
                            {
                                Item item = user.UserPackage[pos];
                                if (item == null)
                                {
                                    switch (pos)
                                    {
                                        case ItemPosition.Steed:
                                        case ItemPosition.Gourd:
                                        case ItemPosition.Garment:
                                        case ItemPosition.RightHandAccessory:
                                        case ItemPosition.LeftHandAccessory:
                                        case ItemPosition.SteedArmor:
                                        case (ItemPosition)13:
                                        case (ItemPosition)14:
                                            continue;
                                        default:
                                            break;
                                    }
                                }

                                if (item == null)
                                {
                                    break;
                                }

                                if (!item.IsEquipment())
                                {
                                    continue;
                                }

                                if (item.GetQuality() % 10 < task.Condition)
                                {
                                    continue;
                                }
                                count++;
                            }
                            data = count;
                            completed = count >= task.Schedule;
                            break;
                        }
                    case GoalType.ProfessionPromotion:
                        {
                            completed = user.ProfessionLevel > 0;
                            data = 1;
                            break;
                        }
                    case GoalType.WinQualifier:
                        {
                            data = user.QualifierHistoryWins;
                            completed = data >= task.Schedule;
                            break;
                        }
                    case GoalType.ExperienceMultiplier:
                        {
                            data = 1;
                            completed = user.ExperienceMultiplier > 1 && user.RemainingExperienceSeconds > 0;
                            break;
                        }
                    case GoalType.CreateJoinSyndicate:
                        {
                            data = 1;
                            completed = user.Syndicate != null;
                            break;
                        }
                    case GoalType.MakeJoinTeam:
                        {
                            data = (uint)(user.Team != null ? 1 : 0);
                            completed = data != 0;
                            break;
                        }
                    case GoalType.AddFriends:
                        {
                            completed = user.FriendAmount > (int)task.Schedule;
                            data = (uint)user.FriendAmount;
                            break;
                        }
                    case GoalType.WinTeamQualifier:
                        {
                            data = user.TeamQualifierHistoryWins;
                            completed = data >= task.Schedule;
                            break;
                        }
                    case GoalType.PlayLottery:
                        {

                            break;
                        }
                    case GoalType.Composition:
                        {
                            break;
                        }
                    case GoalType.ElitePkTournament:
                        {
                            break;
                        }
                    case GoalType.TeamPkTournament:
                        {
                            break;
                        }
                    case GoalType.SkillTeamPkTournament:
                        {
                            break;
                        }
                    case GoalType.SuperTalismans:
                        {
                            int count = 0;
                            for (ItemPosition pos = ItemPosition.EquipmentBegin; pos <= ItemPosition.EquipmentEnd; pos++)
                            {
                                if (pos != ItemPosition.AttackTalisman && pos != ItemPosition.DefenceTalisman && pos != ItemPosition.Crop)
                                {
                                    continue;
                                }
                                Item item = user.UserPackage[pos];
                                if (item == null)
                                {
                                    continue;
                                }
                                if (item.GetQuality() == 9)
                                {
                                    count++;
                                }
                            }
                            completed = count >= (int)task.Schedule;
                            data = (uint)count;
                            break;
                        }
                    case GoalType.TotalComposingLevel:
                        {
                            int composingLevel = 0;
                            for (ItemPosition pos = ItemPosition.EquipmentBegin; pos <= ItemPosition.EquipmentEnd; pos++)
                            {
                                switch (pos)
                                {
                                    case ItemPosition.Gourd:
                                    case ItemPosition.Garment:
                                    case ItemPosition.RightHandAccessory:
                                    case ItemPosition.LeftHandAccessory:
                                    case ItemPosition.SteedArmor:
                                        continue;
                                }

                                Item item = user.UserPackage[pos];
                                if (item != null)
                                {
                                    composingLevel += item.Plus;
                                }
                            }
                            completed = composingLevel >= (int)task.Schedule;
                            data = (uint)composingLevel;
                            break;
                        }
                    case GoalType.EquipmentPlus3:
                        {
                            for (ItemPosition pos = ItemPosition.EquipmentBegin; pos <= ItemPosition.EquipmentEnd; pos++)
                            {
                                switch (pos)
                                {
                                    case ItemPosition.Gourd:
                                    case ItemPosition.Garment:
                                    case ItemPosition.RightHandAccessory:
                                    case ItemPosition.LeftHandAccessory:
                                    case ItemPosition.SteedArmor:
                                        continue;
                                }

                                Item item = user.UserPackage[pos];
                                if (item != null && item.Plus >= (byte)task.Schedule)
                                {
                                    data = 1;
                                    completed = true;
                                    break;
                                }
                            }
                            break;
                        }
                    case GoalType.JoinSubClass:
                        {
                            completed = user.AstProf.Count >= (int)task.Schedule;
                            data = (uint)user.AstProf.Count;
                            break;
                        }
                    case GoalType.ChampionsArena:
                        {
                            break;
                        }
                    case GoalType.TotalEmbedGems:
                        {
                            int count = 0;
                            for (ItemPosition pos = ItemPosition.EquipmentBegin; pos <= ItemPosition.EquipmentEnd; pos++)
                            {
                                switch (pos)
                                {
                                    case ItemPosition.Gourd:
                                    case ItemPosition.Garment:
                                    case ItemPosition.RightHandAccessory:
                                    case ItemPosition.LeftHandAccessory:
                                    case ItemPosition.SteedArmor:
                                        continue;
                                }

                                Item item = user.UserPackage[pos];
                                if (item == null)
                                {
                                    continue;
                                }

                                if (item.SocketOne != SocketGem.NoSocket && item.SocketOne != SocketGem.EmptySocket)
                                {
                                    count++;
                                }
                                if (item.SocketTwo != SocketGem.NoSocket && item.SocketTwo != SocketGem.EmptySocket)
                                {
                                    count++;
                                }
                            }
                            completed = count >= (int)task.Schedule;
                            data = (uint)count;
                            break;
                        }
                    case GoalType.TotalEmbedSuperGems:
                        {
                            int count = 0;
                            for (ItemPosition pos = ItemPosition.EquipmentBegin; pos <= ItemPosition.EquipmentEnd; pos++)
                            {
                                switch (pos)
                                {
                                    case ItemPosition.Gourd:
                                    case ItemPosition.Garment:
                                    case ItemPosition.RightHandAccessory:
                                    case ItemPosition.LeftHandAccessory:
                                    case ItemPosition.SteedArmor:
                                        continue;
                                }

                                Item item = user.UserPackage[pos];
                                if (item == null)
                                {
                                    continue;
                                }

                                if (((byte)item.SocketOne) % 10 == 3)
                                {
                                    count++;
                                }
                                if (((byte)item.SocketTwo) % 10 == 3)
                                {
                                    count++;
                                }
                            }
                            completed = count >= (int)task.Schedule;
                            data = (uint)count;
                            break;
                        }
                    case GoalType.DragonSoulLevel:
                        {
                            int count = 0;
                            for (ItemPosition pos = ItemPosition.EquipmentBegin; pos <= ItemPosition.EquipmentEnd; pos++)
                            {
                                switch (pos)
                                {
                                    case ItemPosition.Gourd:
                                    case ItemPosition.Garment:
                                    case ItemPosition.RightHandAccessory:
                                    case ItemPosition.LeftHandAccessory:
                                    case ItemPosition.SteedArmor:
                                        continue;
                                }

                                Item item = user.UserPackage[pos];
                                if (item == null)
                                {
                                    continue;
                                }

                                if (task.Condition == 2)
                                {
                                    if (item.Quench?.CurrentArtifact?.ItemStatus?.Level >= task.Schedule
                                        && item.Quench.CurrentArtifact.IsPermanent)
                                    {
                                        count++;
                                    }
                                }
                                else if (task.Condition == 3)
                                {
                                    if (item.Quench?.CurrentArtifact?.ItemStatus?.Level >= task.Schedule)
                                    {
                                        count++;
                                    }
                                }
                            }
                            completed = count >= 1;
                            data = (uint)count;
                            break;
                        }
                    case GoalType.ChiStudyTotalPoints:
                        {
                            int count = 0;
                            if (user.Fate != null)
                            {
                                count += user.Fate.GetScore(States.Fate.FateType.Dragon);
                                count += user.Fate.GetScore(States.Fate.FateType.Phoenix);
                                count += user.Fate.GetScore(States.Fate.FateType.Tiger);
                                count += user.Fate.GetScore(States.Fate.FateType.Turtle);
                            }
                            completed = count >= (int)task.Schedule;
                            data = (uint)count;
                            break;
                        }
                    case GoalType.Enlightenment:
                        {
                            break;
                        }
                    case GoalType.GuildPkTournament:
                        {
                            break;
                        }
                    case GoalType.CaptureTheFlag:
                        {
                            break;
                        }
                    case GoalType.JiangHuScore:
                        {
                            int count = 0;
                            if (user.JiangHu != null && user.JiangHu.HasJiangHu)
                            {
                                count += (int)user.JiangHu.InnerPower;
                            }
                            completed = count >= (int)task.Schedule;
                            data = (uint)count;
                            break;
                        }
                    case GoalType.HouseLevel:
                        {
                            completed = user.HomeIdentity != 0;
                            data = 1;
                            break;
                        }
                    case GoalType.Marriage:
                        {
                            completed = user.MateIdentity != 0;
                            data = 1;
                            break;
                        }
                    case GoalType.NobilityDonation:
                        {
                            completed = user.NobilityDonation >= (long)task.Schedule;
                            data = (uint)user.NobilityDonation;
                            break;
                        }
                    case GoalType.ElitePkTopRank:
                        {
                            break;
                        }
                    case GoalType.DisCity:
                        {
                            break;
                        }
                    case GoalType.AllDailyQuests:
                        {
                            break;
                        }
                    case GoalType.Tutor:
                        {
                            completed = user.ApprenticeCount > 0;
                            data = (uint)user.ApprenticeCount;
                            break;
                        }
                    case GoalType.BossKiller:
                        {
                            break;
                        }
                    case GoalType.BattlePower:
                        {
                            completed = user.BattlePower >= (int)task.Schedule;
                            data = (uint)user.BattlePower;
                            break;
                        }
                    case GoalType.EquipSteed:
                        {
                            completed = user.Mount != null;
                            data = 1;
                            break;
                        }
                    case GoalType.UpgradeEquipment:
                        {
                            break;
                        }
                    case GoalType.RefineryLevel:
                        {
                            int count = 0;
                            for (ItemPosition pos = ItemPosition.EquipmentBegin; pos <= ItemPosition.EquipmentEnd; pos++)
                            {
                                switch (pos)
                                {
                                    case ItemPosition.Gourd:
                                    case ItemPosition.Garment:
                                    case ItemPosition.RightHandAccessory:
                                    case ItemPosition.LeftHandAccessory:
                                    case ItemPosition.SteedArmor:
                                        continue;
                                }

                                Item item = user.UserPackage[pos];
                                if (item == null)
                                {
                                    continue;
                                }

                                if (task.Condition == 2)
                                {
                                    if (item.Quench?.CurrentRefinery?.ItemStatus?.Level >= task.Schedule
                                        && item.Quench.CurrentRefinery.IsPermanent)
                                    {
                                        count++;
                                    }
                                }
                                else if (task.Condition == 3)
                                {
                                    if (item.Quench?.CurrentRefinery?.ItemStatus?.Level >= task.Schedule)
                                    {
                                        count++;
                                    }
                                }
                            }
                            completed = count >= 1;
                            data = (uint)count;
                            break;
                        }
                }

                if (data != 0)
                {
                    uint currentRecord = user.Statistic.GetValue(ProgressStatisticId, task.Type);
                    currentRecord = Math.Max(currentRecord, data);
                    await user.Statistic.AddOrUpdateAsync(ProgressStatisticId, task.Type, currentRecord, true);
                }
            }
            else
            {
                await user.Statistic.AddOrUpdateAsync(ProgressStatisticId, task.Type, (uint)task.Schedule, true);
            }

            if (completed)
            {
                await user.Statistic.AddOrUpdateAsync(CompletionStatisticId, task.Id, 1, true);
            }
        }

        public static async Task ProcessUserProgressAsync(Character user, GoalType goalType, bool forceCompletion)
        {
            foreach (var task in tasks.Values.Where(x => x.Type == (int)goalType))
            {
                await ProcessUserChangeGoalDataAsync(user, task, forceCompletion);
            }
        }

        public static async Task ProcessUserCurrentGoalsAsync(Character user)
        {
            foreach (DbProcessTask task in tasks.Values)
            {
                await ProcessUserChangeGoalDataAsync(user, task, false);
            }

            MsgProcessGoalInfo msg = new MsgProcessGoalInfo();
            foreach (var goal in goals.Values.OrderBy(x => x.Id))
            {
                msg.Goals.Add(new MsgProcessGoalInfo.GoalInfo
                {
                    Id = (int)goal.Id,
                    ClaimEnable = 0,
                    Unknown5 = (byte)(IsStageCompleted(user, (ushort)goal.Id) ? 1 : 0)
                });
            }
            await user.SendAsync(msg);
        }

        public enum GoalType
        {
            None,
            LevelUp = 1,
            Metempsychosis = 2,
            BegginerTutorialCompletion = 3,
            XpSkillKills = 4,
            EquipmentQuality = 5,
            ProfessionPromotion = 6,
            WinQualifier = 7,
            ExperienceMultiplier = 9,
            CreateJoinSyndicate = 11,
            MakeJoinTeam = 12,
            AddFriends = 13,
            WinTeamQualifier = 14,
            PlayLottery = 15,
            Composition = 16,
            ElitePkTournament = 17,
            TeamPkTournament = 18,
            SkillTeamPkTournament = 19,
            SuperTalismans = 20,
            TotalComposingLevel = 22,
            EquipmentPlus3 = 23,
            JoinSubClass = 24,
            ChampionsArena = 26,
            TotalEmbedGems = 27,
            TotalEmbedSuperGems = 28,
            DragonSoulLevel = 29,
            ChiStudyTotalPoints = 30,
            Enlightenment = 31,
            GuildPkTournament = 32,
            CaptureTheFlag = 33,
            JiangHuScore = 34,
            HouseLevel = 35,
            Marriage = 36,
            NobilityDonation = 37,
            ElitePkTopRank = 38,
            DisCity = 39,
            AllDailyQuests = 40,
            Tutor = 41,
            BossKiller = 42,
            BattlePower = 43,
            EquipSteed = 44,
            UpgradeEquipment = 45,
            RefineryLevel = 46
        }
    }
}
