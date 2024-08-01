using Canyon.Database.Entities;
using Canyon.Game.Database;
using Canyon.Game.Database.Repositories;
using Canyon.Game.Services.Managers;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States.NPCs;
using Canyon.Game.States.User;
using Canyon.Network.Packets;
using System.Collections.Concurrent;
using static Canyon.Game.Sockets.Game.Packets.MsgQuiz;

namespace Canyon.Game.States.Events
{
    public sealed class QuizShow : GameEvent
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<QuizShow>("");

        private const uint NPC_ID_U = 100012;
        private const int MAX_QUESTION = 20;
        private const int TIME_PER_QUESTION = 30;

        private const int TOTAL_EXP_REWARD = 600;

        private readonly ushort[] ExperienceReward =
        {
            3000,
            1800,
            1200,
            600
        };

        public QuizShow()
            : base("Quiz Show", 500)
        {
        }

        private readonly List<DbQuiz> AllQuestions = new();
        private readonly List<DbQuiz> CurrentQuestions = new();
        private readonly ConcurrentDictionary<uint, QuizUser> Users = new();

        private DynamicNpc quizShowNpc;

        private bool ready = false;
        private int currentQuestionIndex;
        private int lastCorrectReply = -1;
        private readonly TimeOut questionTimer = new(30);

        public override EventType Identity => EventType.QuizShow;

        public QuizStatus Status { get; private set; } = QuizStatus.Idle;

        public override async Task<bool> CreateAsync()
        {
            quizShowNpc = RoleManager.GetRole<DynamicNpc>(NPC_ID_U);
            if (quizShowNpc == null)
            {
                logger.LogError($"Could not load NPC {NPC_ID_U} for {Name}");
                return false;
            }

            quizShowNpc.Data0 = 0;

            AllQuestions.AddRange(await QuizRepository.GetAsync());
            return true;
        }

        public override Task OnLoginAsync(Character user)
        {
            if (Enter(user))
            {
                Users.TryGetValue(user.Identity, out var player);

                var msg = new MsgQuiz
                {
                    Action = QuizAction.AfterReply,
                    Param2 = player?.TimeTaken ?? 0,
                    Param3 = player?.Rank ?? 0,
                    Param6 = player?.Points ?? 0
                };
                List<QuizUser> top3 = GetTop3();
                foreach (QuizUser top in top3)
                {
                    msg.Scores.Add(new QuizRank
                    {
                        Name = top.Name,
                        Time = top.TimeTaken,
                        Score = top.Points
                    });
                }

                return user.SendAsync(msg);
            }
            return Task.CompletedTask;
        }

        public override async Task OnTimerAsync()
        {
            if (Status == QuizStatus.Idle)
            {
                if (quizShowNpc.Data0 == 3 && !ready) // load
                {
                    Users.Clear();
                    CurrentQuestions.Clear();
                    var temp = new List<DbQuiz>(AllQuestions);
                    for (var i = 0; i < Math.Min(temp.Count, MAX_QUESTION); i++)
                    {
                        int idx = await NextAsync(temp.Count) % Math.Max(1, temp.Count);
                        CurrentQuestions.Add(temp[idx]);
                        temp.RemoveAt(idx);
                    }

                    foreach (Character user in RoleManager.QueryRoleByType<Character>())
                    {
                        if (!Users.TryGetValue(user.Identity, out QuizUser res))
                        {
                            Enter(user);
                        }
                        else
                        {
                            res.Canceled = false;
                        }
                    }

                    await BroadcastMsgAsync(new MsgQuiz
                    {
                        Action = QuizAction.Start,
                        Param1 = (ushort)(60 - DateTime.Now.Second),
                        Param2 = MAX_QUESTION,
                        Param3 = TIME_PER_QUESTION,
                        Param4 = ExperienceReward[0],
                        Param5 = ExperienceReward[1],
                        Param6 = ExperienceReward[2]
                    }).ConfigureAwait(false);
                    ready = true;
                    return;
                }

                if (quizShowNpc.Data0 == 4) // start
                {
                    Status = QuizStatus.Running;
                    currentQuestionIndex = -1;
                }
            }
            else
            {
                if (questionTimer.ToNextTime(TIME_PER_QUESTION) && ++currentQuestionIndex < CurrentQuestions.Count)
                {
                    DbQuiz question = CurrentQuestions[currentQuestionIndex];
                    foreach (QuizUser player in Users.Values.Where(x => !x.Canceled))
                    {
                        Character user = RoleManager.GetUser(player.Identity);
                        if (user == null)
                        {
                            continue;
                        }

                        if (!player.Replied)
                        {
                            player.Points += 1;
                            player.TimeTaken += TIME_PER_QUESTION;
                        }

                        player.Replied = false;
                        player.CurrentQuestion = currentQuestionIndex;
                        ushort lastResult = 1;
                        if (currentQuestionIndex > 0)
                        {
                            lastResult = (ushort)(player.Correct ? 1 : 2);
                        }

                        player.Correct = false;
                        _ = user.SendAsync(new MsgQuiz
                        {
                            Action = QuizAction.Question,
                            Param1 = (ushort)(currentQuestionIndex + 1),
                            Param2 = lastResult,
                            Param3 = player.Experience,
                            Param4 = player.TimeTaken,
                            Param5 = player.Points,
                            Strings =
                            {
                                question.Question,
                                question.Answer1,
                                question.Answer2,
                                question.Answer3,
                                question.Answer4
                            }
                        }).ConfigureAwait(false);
                    }

                    lastCorrectReply = question.Result;
                }
                else if (currentQuestionIndex >= CurrentQuestions.Count)
                {
                    Status = QuizStatus.Idle;

                    List<QuizUser> top3 = GetTop3();
                    foreach (QuizUser player in Users.Values.Where(x => !x.Canceled))
                    {
                        if (player.CurrentQuestion < currentQuestionIndex)
                        {
                            player.TimeTaken += TIME_PER_QUESTION;
                        }

                        var expBallReward = 0;
                        if (top3.Any(x => x.Identity == player.Identity))
                        {
                            int rank = GetRanking(player.Identity);
                            if (rank > 0 && rank <= 3)
                            {
                                expBallReward = ExperienceReward[rank];
                            }
                        }
                        else
                        {
                            expBallReward = ExperienceReward[3];
                        }

                        Character user = RoleManager.GetUser(player.Identity);
                        if (user != null)
                        {
                            var msg = new MsgQuiz
                            {
                                Action = QuizAction.Finish,
                                Param1 = player.Rank,
                                Param2 = player.Experience,
                                Param3 = player.TimeTaken,
                                Param4 = player.Points
                            };
                            foreach (QuizUser top in top3)
                            {
                                msg.Scores.Add(new QuizRank
                                {
                                    Name = top.Name,
                                    Time = top.TimeTaken,
                                    Score = top.Points
                                });
                            }
                            await user.SendAsync(msg);

                            await user.SynchroAttributesAsync(ClientUpdateType.QuizPoints, user.QuizPoints, true);
                            if (user.Level < Role.MAX_UPLEV)
                            {
                                await user.AwardExperienceAsync(user.CalculateExpBall(expBallReward));
                            }
                        }
                    }

                    ready = false;
                }
            }
        }

        #region Reply

        public async Task OnReplyAsync(Character user, ushort idxQuestion, ushort reply)
        {
            if (Status != QuizStatus.Running)
            {
                return;
            }

            if (!Users.TryGetValue(user.Identity, out QuizUser player))
            {
                Users.TryAdd(user.Identity, player = new QuizUser
                {
                    Identity = user.Identity,
                    Name = user.Name,
                    TimeTaken = (ushort)(Math.Max(0, currentQuestionIndex - 1) * TIME_PER_QUESTION),
                    CurrentQuestion = currentQuestionIndex
                });
            }

            if (player.CurrentQuestion != currentQuestionIndex)
            {
                return;
            }

            DbQuiz question = CurrentQuestions[idxQuestion - 1];
            ushort points;
            int expBallAmount;
            if (question.Result == reply)
            {
                expBallAmount = TOTAL_EXP_REWARD / MAX_QUESTION;
                player.Points += points = (ushort)Math.Max(1, questionTimer.GetRemain());
                player.TimeTaken +=
                    (ushort)Math.Max(
                        1, Math.Min(TIME_PER_QUESTION, questionTimer.GetInterval() - questionTimer.GetRemain()));
                player.Correct = true;
            }
            else
            {
                expBallAmount = TOTAL_EXP_REWARD / MAX_QUESTION * 4;
                player.Points += points = 1;
                player.TimeTaken += TIME_PER_QUESTION;
                player.Correct = false;
            }

            player.Replied = true;
            user.QuizPoints += points;
            player.Experience += (ushort)expBallAmount;
            await user.AwardExperienceAsync(user.CalculateExpBall(expBallAmount));

            var msg = new MsgQuiz
            {
                Action = QuizAction.AfterReply,
                Param2 = player.TimeTaken,
                Param3 = player.Rank = GetRanking(player.Identity),
                Param6 = player.Points
            };
            List<QuizUser> top3 = GetTop3();
            foreach (QuizUser top in top3)
            {
                msg.Scores.Add(new QuizRank
                {
                    Name = top.Name,
                    Time = top.TimeTaken,
                    Score = top.Points
                });
            }

            await user.SendAsync(msg);
        }

        #endregion

        #region Player

        public bool Enter(Character user)
        {
            return Users.TryAdd(user.Identity, new QuizUser
            {
                Identity = user.Identity,
                Name = user.Name
            });
        }

        public ushort GetRanking(uint idUser)
        {
            ushort pos = 1;
            foreach (QuizUser player in Users.Values
                                               .Where(x => !x.Canceled)
                                               .OrderByDescending(x => x.Points)
                                               .ThenBy(x => x.TimeTaken))
            {
                if (player.Identity == idUser)
                {
                    return pos;
                }

                pos++;
            }

            return pos;
        }

        private List<QuizUser> GetTop3()
        {
            var rank = new List<QuizUser>();
            foreach (QuizUser player in Users.Values
                                               .Where(x => !x.Canceled)
                                               .OrderByDescending(x => x.Points)
                                               .ThenBy(x => x.TimeTaken))
            {
                if (rank.Count == 3)
                {
                    break;
                }

                rank.Add(player);
            }

            return rank;
        }

        #endregion

        #region Broadcast

        public async Task BroadcastMsgAsync(IPacket msg)
        {
            foreach (QuizUser user in Users.Values.Where(x => !x.Canceled))
            {
                Character player = RoleManager.GetUser(user.Identity);
                if (player == null)
                {
                    continue;
                }

                await player.SendAsync(msg);
            }
        }

        #endregion

        #region Cancelation

        public void Cancel(uint idUser)
        {
            if (Users.TryGetValue(idUser, out QuizUser value))
            {
                value.Canceled = true;
            }
        }

        public bool IsCanceled(uint idUser)
        {
            return Users.TryGetValue(idUser, out QuizUser value) && value.Canceled;
        }

        #endregion

        public enum QuizStatus
        {
            Idle,
            Running
        }

        private class QuizUser
        {
            public uint Identity { get; set; }
            public string Name { get; set; }
            public ushort Points { get; set; }
            public ushort Experience { get; set; } // 600 = 1 expball
            public ushort TimeTaken { get; set; }  // in seconds
            public int CurrentQuestion { get; set; }
            public ushort Rank { get; set; }
            public bool Correct { get; set; }
            public bool Replied { get; set; }
            public bool Canceled { get; set; }
        }
    }
}
