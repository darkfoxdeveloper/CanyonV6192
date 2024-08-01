using Canyon.Database.Entities;
using Canyon.Game.Database;
using Canyon.Game.Database.Repositories;
using Canyon.Game.Services.Managers;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States.Events.Tournament;
using Canyon.Game.States.User;
using Canyon.Game.States.World;
using Canyon.Network.Packets;

namespace Canyon.Game.States.Events.Elite
{
    public sealed class ElitePkGroupTournament : GameTournamentEvent<ElitePkParticipant, Character>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<ElitePkGroupTournament>();

        private const int ELITE_PK_CHAMPION_LOW_TITLE = 12,
            ELITE_PK_2ND_LOW_TITLE = 13,
            ELITE_PK_3RD_LOW_TITLE = 14,
            ELITE_PK_TOP8_LOW_TITLE = 15;

        private const int ELITE_PK_CHAMPION_HIGH_TITLE = 16,
            ELITE_PK_2ND_HIGH_TITLE = 17,
            ELITE_PK_3RD_HIGH_TITLE = 18,
            ELITE_PK_TOP8_HIGH_TITLE = 19;

        private readonly int[] removeTitleArray =
        {
            ELITE_PK_CHAMPION_HIGH_TITLE,
            ELITE_PK_2ND_HIGH_TITLE,
            ELITE_PK_3RD_HIGH_TITLE,
            ELITE_PK_TOP8_HIGH_TITLE,
            ELITE_PK_CHAMPION_LOW_TITLE,
            ELITE_PK_2ND_LOW_TITLE,
            ELITE_PK_3RD_LOW_TITLE,
            ELITE_PK_TOP8_LOW_TITLE
        };

        private readonly uint waitingMapId;
        private GameMap awaitingMap;

        private readonly int index;
        private readonly string name;
        private RankingInfo rankingInfo;
        private DateTime startTime;
        private InternalTournamentStage stage = InternalTournamentStage.Error;

        public ElitePkGroupTournament(TournamentRules tournamentRules, uint waitingMapId, int index, string name)
            : base(tournamentRules)
        {
            this.waitingMapId = waitingMapId;
            this.index = index;
            this.name = name;

            CurrentMatchIdentity = 400000 + index * 1000;
        }

        public async Task InitializeAsync()
        {
            var pkInfo = await PkInfoRepository.GetPkInfoAsync(PkInfoRepository.PkInfoType.ElitePk, index);
            if (pkInfo == null)
            {
                pkInfo = new DbPkInfo
                {
                    Type = (ushort)PkInfoRepository.PkInfoType.ElitePk,
                    Subtype = (ushort)index
                };
            }

            rankingInfo = new RankingInfo(pkInfo);
            await rankingInfo.InitializeAsync();

            awaitingMap = MapManager.GetMap(waitingMapId);
            if (awaitingMap == null)
            {
                return;
            }

            stage = InternalTournamentStage.Idle;
        }

        public override async Task<bool> InscribeAsync(ElitePkParticipant entity)
        {
            if (participants.Any(x => x.Identity == entity.Identity))
            {
                return false;
            }

            participants.Add(entity);
            return true;
        }

        public override bool IsContestant(uint identity)
        {
            return participants.Any(x => x.Identity == identity);
        }

        public override bool IsParticipantAllowedToJoin(Character participant)
        {
            if (participant.Level < tournamentRules.MinLevel)
            {
                return false;
            }
            if (participant.Level > tournamentRules.MaxLevel)
            {
                return false;
            }
            if (participant.Metempsychosis < tournamentRules.Metempsychosis)
            {
                return false;
            }
            if (participant.NobilityRank < tournamentRules.MinNobility)
            {
                return false;
            }
            if (participant.NobilityRank > tournamentRules.MaxNobility)
            {
                return false;
            }
            return true;
        }

        public Task SendRankingAsync(Character target)
        {
            MsgElitePKGameRankInfo rankInfo = new MsgElitePKGameRankInfo
            {
                Group = index,
                GroupStatus = 0,
            };
            int rank = 0;
            rankInfo.Rank.Add(new MsgElitePKGameRankInfo.ElitePkRankStruct
            {
                Rank = rank++,
                Identity = rankingInfo.Id1,
                Mesh = rankingInfo.Lookface1,
                Name = rankingInfo.Name1
            });
            rankInfo.Rank.Add(new MsgElitePKGameRankInfo.ElitePkRankStruct
            {
                Rank = rank++,
                Identity = rankingInfo.Id2,
                Mesh = rankingInfo.Lookface2,
                Name = rankingInfo.Name2
            });
            rankInfo.Rank.Add(new MsgElitePKGameRankInfo.ElitePkRankStruct
            {
                Rank = rank++,
                Identity = rankingInfo.Id3,
                Mesh = rankingInfo.Lookface3,
                Name = rankingInfo.Name3
            });
            rankInfo.Rank.Add(new MsgElitePKGameRankInfo.ElitePkRankStruct
            {
                Rank = rank++,
                Identity = rankingInfo.Id4,
                Mesh = rankingInfo.Lookface4,
                Name = rankingInfo.Name4
            });
            rankInfo.Rank.Add(new MsgElitePKGameRankInfo.ElitePkRankStruct
            {
                Rank = rank++,
                Identity = rankingInfo.Id5,
                Mesh = rankingInfo.Lookface5,
                Name = rankingInfo.Name5
            });
            rankInfo.Rank.Add(new MsgElitePKGameRankInfo.ElitePkRankStruct
            {
                Rank = rank++,
                Identity = rankingInfo.Id6,
                Mesh = rankingInfo.Lookface6,
                Name = rankingInfo.Name6
            });
            rankInfo.Rank.Add(new MsgElitePKGameRankInfo.ElitePkRankStruct
            {
                Rank = rank++,
                Identity = rankingInfo.Id7,
                Mesh = rankingInfo.Lookface7,
                Name = rankingInfo.Name7
            });
            rankInfo.Rank.Add(new MsgElitePKGameRankInfo.ElitePkRankStruct
            {
                Rank = rank++,
                Identity = rankingInfo.Id8,
                Mesh = rankingInfo.Lookface8,
                Name = rankingInfo.Name8
            });
            return target.SendAsync(rankInfo);
        }

        public override async Task SubmitEventWindowAsync(Character target = null, int page = 0)
        {
            MsgPkEliteMatchInfo.ElitePkGuiType guiType;
            var stage = GetStage();
            switch (stage)
            {
                case TournamentStage.Final:
                    {
                        guiType = MsgPkEliteMatchInfo.ElitePkGuiType.ReconstructTop;
                        break;
                    }

                case TournamentStage.Knockout:
                    {
                        guiType = MsgPkEliteMatchInfo.ElitePkGuiType.Knockout16;
                        break;
                    }

                case TournamentStage.Knockout8:
                    {
                        guiType = MsgPkEliteMatchInfo.ElitePkGuiType.Top8Qualifier;
                        break;
                    }

                case TournamentStage.QuarterFinals:
                    {
                        guiType = MsgPkEliteMatchInfo.ElitePkGuiType.Top4Qualifier;
                        break;
                    }

                case TournamentStage.SemiFinals:
                    {
                        guiType = MsgPkEliteMatchInfo.ElitePkGuiType.Top2Qualifier;
                        break;
                    }

                case TournamentStage.ThirdPlace:
                    {
                        guiType = MsgPkEliteMatchInfo.ElitePkGuiType.Top3Qualifier;
                        break;
                    }

                case TournamentStage.Finals:
                    {
                        guiType = MsgPkEliteMatchInfo.ElitePkGuiType.Top1Qualifier;
                        break;
                    }

                default:
                    {
                        return;
                    }
            }

            MsgPkEliteMatchInfo msg;
            if (target != null) // send main page only on user request
            {
                msg = new()
                {
                    Group = (ushort)this.index,
                    Gui = guiType,
                    Mode = MsgPkEliteMatchInfo.ElitePkMatchType.MainPage
                };
                await SendMessageAsync(msg, target);
            }

            msg = new()
            {
                Group = (ushort)this.index,
                Gui = guiType,
            };
            if (wagesTimeOut.IsActive() && !wagesTimeOut.IsTimeOut())
            {
                msg.TimeLeft = (ushort)wagesTimeOut.GetRemain();
            }

            int index = 0;
            if (guiType <= MsgPkEliteMatchInfo.ElitePkGuiType.Top4Qualifier)
            {
                msg.Mode = MsgPkEliteMatchInfo.ElitePkMatchType.GuiUpdate;
                await SendMessageAsync(msg, target);

                msg.Mode = MsgPkEliteMatchInfo.ElitePkMatchType.UpdateList;
                msg.TotalMatches = matches.Count;
                
                List<BaseTournamentMatch<ElitePkParticipant, Character>> topMatches;
                if (guiType < MsgPkEliteMatchInfo.ElitePkGuiType.Top8Qualifier)
                {
                    index = 5 * page;
                    topMatches = matches.Skip(index).Take(5).ToList();
                }
                else
                {
                    topMatches = matches;
                }

                foreach (var match in topMatches)
                {
                    msg.Matches.Add(BuildMatchInfo(match, index++));
                }
                await SendMessageAsync(msg, target);
            }
            else 
            {
                index = await SendPreviousMatchesGuiUpdateAsync(stage, target);
                await SendPreviousMatchesUpdateListAsync(stage, target);

                if (matches.Count > 0 && stage < TournamentStage.Final)
                {
                    msg.Mode = MsgPkEliteMatchInfo.ElitePkMatchType.UpdateList;
                    msg.Gui = guiType;
                    msg.TotalMatches = matches.Count;

                    foreach (var match in matches)
                    {
                        msg.Matches.Add(BuildMatchInfo(match, index++));
                    }
                    await SendMessageAsync(msg, target);
                }
            }

            if (stage >= TournamentStage.Finals)
            {
                var top3 = tournamentParticipantReward.Where(x => x.Rank > TournamentParticipantRank.Top8).OrderBy(x => x.Rank).Take(3);
                MsgElitePKGameRankInfo rankInfo = new MsgElitePKGameRankInfo
                {
                    Mode = 2,
                    Group = this.index,
                    GroupStatus = (int)guiType,
                    Unknown20 = 2,
                    Unknown24 = (int)(target?.Identity ?? 0)
                };
                var champion = tournamentParticipantReward.FirstOrDefault(x => x.Rank == TournamentParticipantRank.Champion);
                if (champion != null)
                {
                    rankInfo.Rank.Add(new MsgElitePKGameRankInfo.ElitePkRankStruct
                    {
                        Identity = champion.Participant.Identity,
                        Mesh = champion.Participant.Lookface,
                        Name = champion.Participant.Name,
                        Rank = 0
                    });
                    rankInfo.Rank.Add(new MsgElitePKGameRankInfo.ElitePkRankStruct
                    {
                        Identity = champion.Participant.Identity,
                        Mesh = champion.Participant.Lookface,
                        Name = champion.Participant.Name,
                        Rank = 1
                    });
                }
                var second = tournamentParticipantReward.FirstOrDefault(x => x.Rank == TournamentParticipantRank.Top2);
                if (second != null)
                {
                    rankInfo.Rank.Add(new MsgElitePKGameRankInfo.ElitePkRankStruct
                    {
                        Identity = second.Participant.Identity,
                        Mesh = second.Participant.Lookface,
                        Name = second.Participant.Name,
                        Rank = 2
                    });
                }
                var third = tournamentParticipantReward.FirstOrDefault(x => x.Rank == TournamentParticipantRank.Top3);
                if (third != null)
                {
                    rankInfo.Rank.Add(new MsgElitePKGameRankInfo.ElitePkRankStruct
                    {
                        Identity = third.Participant.Identity,
                        Mesh = third.Participant.Lookface,
                        Name = third.Participant.Name,
                        Rank = 3
                    });
                }
                await SendMessageAsync(rankInfo, target);
            }
        }

        private async Task<int> SendPreviousMatchesGuiUpdateAsync(TournamentStage stage, Character target = null)
        {
            MsgPkEliteMatchInfo msg = new()
            {
                Group = (ushort)this.index,
                Mode = MsgPkEliteMatchInfo.ElitePkMatchType.GuiUpdate,
                TotalMatches = matches.Count
            };
            int index = 0;
            if (stage > TournamentStage.QuarterFinals)
            {
                msg.Gui = MsgPkEliteMatchInfo.ElitePkGuiType.Top4Qualifier;
                msg.Matches.AddRange(this.previousMatches.Where(x => x.Stage == TournamentStage.QuarterFinals).Select(x => BuildMatchInfo(x, index++)));
                await SendMessageAsync(msg, target);
            }
            if (stage > TournamentStage.SemiFinals)
            {
                msg.Gui = MsgPkEliteMatchInfo.ElitePkGuiType.Top2Qualifier;
                msg.Matches.AddRange(this.previousMatches.Where(x => x.Stage == TournamentStage.SemiFinals).Select(x => BuildMatchInfo(x, index++)));
                await SendMessageAsync(msg, target);
            }
            if (stage > TournamentStage.ThirdPlace)
            {
                msg.Gui = MsgPkEliteMatchInfo.ElitePkGuiType.Top3Qualifier;
                msg.Matches.AddRange(this.previousMatches.Where(x => x.Stage == TournamentStage.ThirdPlace).Select(x => BuildMatchInfo(x, index++)));
                await SendMessageAsync(msg, target);
            }
            if (stage > TournamentStage.Finals)
            {
                msg.Gui = MsgPkEliteMatchInfo.ElitePkGuiType.Top1Qualifier;
                msg.Matches.AddRange(this.previousMatches.Where(x => x.Stage == TournamentStage.Finals).Select(x => BuildMatchInfo(x, index++)));
                await SendMessageAsync(msg, target);
            }
            return index;
        }

        private async Task<int> SendPreviousMatchesUpdateListAsync(TournamentStage stage, Character target = null)
        {
            MsgPkEliteMatchInfo msg = new()
            {
                Group = (ushort)this.index,
                Mode = MsgPkEliteMatchInfo.ElitePkMatchType.UpdateList
            };
            int index = 0;
            if (stage > TournamentStage.QuarterFinals)
            {
                msg.Gui = MsgPkEliteMatchInfo.ElitePkGuiType.Top4Qualifier;
                msg.Matches.AddRange(this.previousMatches.Where(x => x.Stage == TournamentStage.QuarterFinals).Select(x => BuildMatchInfo(x, index++)));
                await SendMessageAsync(msg, target);
            }
            if (stage > TournamentStage.SemiFinals)
            {
                msg.Gui = MsgPkEliteMatchInfo.ElitePkGuiType.Top2Qualifier;
                msg.Matches.AddRange(this.previousMatches.Where(x => x.Stage == TournamentStage.SemiFinals).Select(x => BuildMatchInfo(x, index++)));
                await SendMessageAsync(msg, target);
            }
            if (stage > TournamentStage.ThirdPlace)
            {
                msg.Gui = MsgPkEliteMatchInfo.ElitePkGuiType.Top3Qualifier;
                msg.Matches.AddRange(this.previousMatches.Where(x => x.Stage == TournamentStage.ThirdPlace).Select(x => BuildMatchInfo(x, index++)));
                //await SendMessageAsync(msg, target);
                msg.Matches.Clear();
            }
            if (stage > TournamentStage.Finals)
            {
                msg.Gui = MsgPkEliteMatchInfo.ElitePkGuiType.Top1Qualifier;
                msg.Matches.AddRange(this.previousMatches.Where(x => x.Stage == TournamentStage.Finals).Select(x => BuildMatchInfo(x, index++)));
                await SendMessageAsync(msg, target);
            }
            return index;
        }

        private Task SendMessageAsync(IPacket packet, Character target = null)
        {
            if (target == null)
                return BroadcastWorldMsgAsync(packet);
            return target.SendAsync(packet);
        }

        private MsgPkEliteMatchInfo.MatchInfo BuildSingleMatchInfo(SingleTournamentMatch<ElitePkParticipant, Character> match, int index)
        {
            List<MsgPkEliteMatchInfo.MatchContestantInfo> contestants = new();
            if (match.Participant1 != null)
            {
                contestants.Add(new MsgPkEliteMatchInfo.MatchContestantInfo
                {
                    Identity = match.Participant1.Identity,
                    Name = match.Participant1.Name,
                    Winner = match.Winner?.Identity == match.Participant1.Identity,
                    Flag = match.Flag1,
                    Mesh = match.Participant1.Lookface
                });
            }
            if (match.Participant2 != null)
            {
                contestants.Add(new MsgPkEliteMatchInfo.MatchContestantInfo
                {
                    Identity = match.Participant2.Identity,
                    Name = match.Participant2.Name,
                    Winner = match.Winner?.Identity == match.Participant2.Identity,
                    Flag = match.Flag2,
                    Mesh = match.Participant2.Lookface
                });
            }
            return new MsgPkEliteMatchInfo.MatchInfo
            {
                ContestantInfos = contestants,
                Index = index,
                MatchIdentity = (uint)match.Identity,
                Status = match.MatchFlag
            };
        }

        private MsgPkEliteMatchInfo.MatchInfo BuildMatchInfo(BaseTournamentMatch<ElitePkParticipant, Character> match, int index)
        {
            if (match is SingleTournamentMatch<ElitePkParticipant, Character> singleMatch)
            {
                return BuildSingleMatchInfo(singleMatch, index);
            }
            else
            {
                List<MsgPkEliteMatchInfo.MatchContestantInfo> contestants = new();
                var doubleMatch = match as DoubleTournamentMatch<ElitePkParticipant, Character>;
                if (doubleMatch.Participant1 != null)
                {
                    contestants.Add(new MsgPkEliteMatchInfo.MatchContestantInfo
                    {
                        Identity = doubleMatch.Participant1.Identity,
                        Name = doubleMatch.Participant1.Name,
                        Winner = doubleMatch.Winner?.Identity == doubleMatch.Participant1.Identity,
                        Flag = doubleMatch.Flag1,
                        Mesh = doubleMatch.Participant1.Lookface
                    });
                }
                if (doubleMatch.Participant2 != null)
                {
                    contestants.Add(new MsgPkEliteMatchInfo.MatchContestantInfo
                    {
                        Identity = doubleMatch.Participant2.Identity,
                        Name = doubleMatch.Participant2.Name,
                        Winner = doubleMatch.Winner?.Identity == doubleMatch.Participant2.Identity,
                        Flag = doubleMatch.Flag2,
                        Mesh = doubleMatch.Participant2.Lookface
                    });
                }
                if (doubleMatch.Participant3 != null)
                {
                    contestants.Add(new MsgPkEliteMatchInfo.MatchContestantInfo
                    {
                        Identity = doubleMatch.Participant3.Identity,
                        Name = doubleMatch.Participant3.Name,
                        Winner = doubleMatch.Winner?.Identity == doubleMatch.Participant3.Identity,
                        Flag = doubleMatch.Flag3,
                        Mesh = doubleMatch.Participant3.Lookface
                    });
                }
                return new MsgPkEliteMatchInfo.MatchInfo
                {
                    ContestantInfos = contestants,
                    Index = index,
                    MatchIdentity = (uint)match.Identity,
                    Status = match.MatchFlag
                };
            }
        }

        public async Task StartupAsync()
        {
            // we will set the current event start time, each index will *120 to start each tournament every two minutes
            // index 0 will start in the next iteration
            int addMinutes = 6 - index * 2;
            startTime = DateTime.Now.AddMinutes(addMinutes);
            stage = InternalTournamentStage.Awaiting;
        }

        public async Task OnTimerAsync()
        {
#if DEBUG
            if (index != 3)
            {
                return;
            }
#endif

            if (stage == InternalTournamentStage.Idle || stage == InternalTournamentStage.Error)
            {
                return;
            }

            if (stage == InternalTournamentStage.Awaiting)
            {
                if (DateTime.Now < startTime)
                {
                    return;
                }

                // first we clean up last winners
                await CleanUpTitlesAsync(rankingInfo.Id1);
                await CleanUpTitlesAsync(rankingInfo.Id2);
                await CleanUpTitlesAsync(rankingInfo.Id3);
                await CleanUpTitlesAsync(rankingInfo.Id4);
                await CleanUpTitlesAsync(rankingInfo.Id5);
                await CleanUpTitlesAsync(rankingInfo.Id6);
                await CleanUpTitlesAsync(rankingInfo.Id7);
                await CleanUpTitlesAsync(rankingInfo.Id8);

                rankingInfo.Id1 = 0;
                rankingInfo.Id2 = 0;
                rankingInfo.Id3 = 0;
                rankingInfo.Id4 = 0;
                rankingInfo.Id5 = 0;
                rankingInfo.Id6 = 0;
                rankingInfo.Id7 = 0;
                rankingInfo.Id8 = 0;

                rankingInfo.Lookface1 = 0;
                rankingInfo.Lookface2 = 0;
                rankingInfo.Lookface3 = 0;
                rankingInfo.Lookface4 = 0;
                rankingInfo.Lookface5 = 0;
                rankingInfo.Lookface6 = 0;
                rankingInfo.Lookface7 = 0;
                rankingInfo.Lookface8 = 0;

                rankingInfo.Name1 = string.Empty;
                rankingInfo.Name2 = string.Empty;
                rankingInfo.Name3 = string.Empty;
                rankingInfo.Name4 = string.Empty;
                rankingInfo.Name5 = string.Empty;
                rankingInfo.Name6 = string.Empty;
                rankingInfo.Name7 = string.Empty;
                rankingInfo.Name8 = string.Empty;

                await rankingInfo.SaveAsync();

                await OnAwaitEndsAsync();
                return;
            }

            if (GetStage() == TournamentStage.Final)
            {
                // this group ended, check on main branch if all are over
                await SetRankingAsync();
                return;
            }

            await OnExecuteAsync();
        }

        private async Task CleanUpTitlesAsync(uint idUser)
        {
            if (idUser == 0)
            {
                return;
            }
            foreach (var title in removeTitleArray)
            {
                await Character.RemoveTitleAsync(idUser, title);
            }
        }

        private async Task OnAwaitEndsAsync()
        {
            // get participants that match the criteria and subscribe them in the event
            foreach (var player in awaitingMap.QueryPlayers())
            {
                if (!IsParticipantAllowedToJoin(player))
                {
                    await player.FlyMapAsync(player.RecordMapIdentity, player.RecordMapX, player.RecordMapY);
                    continue;
                }

                await InscribeAsync(new ElitePkParticipant(player));
            }

            GenerateMatches();
            wagesTimeOut.Startup(WAGES_TIMEOUT);

            foreach (var match in matches)
            {
                await PrepareMapAsync(match);
            }

            stage = InternalTournamentStage.Running;
        }

        private const int WAGES_TIMEOUT = 10;
        private const int DESTROY_MAPS_TIMEOUT = 5;
        private TimeOut wagesTimeOut = new TimeOut(10);
        private TimeOut matchesTimeOut = new TimeOut();
        private TimeOut destroyMatchTimeOut = new TimeOut();
        private MatchStage matchStage = MatchStage.Wages;

        private int CalculateMatchTime()
        {
            if (GetStage() <= TournamentStage.QuarterFinals)
            {
                return 180;
            }
            return 300;
        }

        private async Task OnExecuteAsync()
        {
            switch (matchStage)
            {
                case MatchStage.Wages:
                    {
                        // on wages finish, we will first prepare all maps, then teleport everyone at once and start the timers

                        if (wagesTimeOut.IsActive() && wagesTimeOut.IsTimeOut())
                        {
                            foreach (var match in matches)
                            {
                                await FlyToMapAsync(match);
                            }

                            wagesTimeOut.Clear();
                            matchesTimeOut.Startup(CalculateMatchTime() + 15);
                            matchStage = MatchStage.Running;
                            await SubmitEventWindowAsync();
                        }
                        break;
                    }

                case MatchStage.Running:
                    {
                        // here we check if timer is ended up, when timer is over we finish matches and teleport everyone back
                        if (matchesTimeOut.IsActive() && !matchesTimeOut.IsTimeOut())
                        {
                            foreach (var match in matches)
                            {
                                if (match.Finished || match.Participant1?.Identity < 10_000_000 || match.Participant2?.Identity < 10_000_000)
                                {
                                    continue;
                                }

                                int rate = await NextAsync(100);
                                if (rate < 30)
                                {
                                    await EndMapAsync(match);
                                    await SubmitEventWindowAsync();
                                }
                            }
                        }

                        bool allMapsFinished = false;
                        if (matchesTimeOut.IsActive())
                        {
                            allMapsFinished = matches.All(x => x.Finished);
                        }

                        if ((matchesTimeOut.IsActive() && matchesTimeOut.IsTimeOut()) || allMapsFinished)
                        {
                            if (!allMapsFinished)
                            {
                                foreach (var match in matches)
                                {
                                    await EndMapAsync(match);
                                }
                            }

                            matchesTimeOut.Clear();
                            destroyMatchTimeOut.Startup(DESTROY_MAPS_TIMEOUT);
                            return;
                        }
                        
                        if (destroyMatchTimeOut.IsActive() && destroyMatchTimeOut.IsTimeOut())
                        {
                            foreach (var match in matches)
                            {
                                if (match is DoubleTournamentMatch<ElitePkParticipant, Character> doubleMatch)
                                {
                                    if (doubleMatch.CurrentlyFighting == DoubleTournamentMatch<ElitePkParticipant, Character>.CurrentMatch.First)
                                    {
                                        if (doubleMatch.Match1.MapStatus == SingleTournamentMatch<ElitePkParticipant, Character>.MatchMapStatus.DestroyEnable)
                                        {
                                            await DestroyMapAsync(doubleMatch.Match1);
                                        }
                                    }
                                    else
                                    {
                                        if (doubleMatch.Match2.MapStatus == SingleTournamentMatch<ElitePkParticipant, Character>.MatchMapStatus.DestroyEnable)
                                        {
                                            await DestroyMapAsync(doubleMatch.Match2);
                                        }
                                    }
                                }
                                else
                                {
                                    var singleMatch = match as SingleTournamentMatch<ElitePkParticipant, Character>;
                                    if (singleMatch.MapStatus == SingleTournamentMatch<ElitePkParticipant, Character>.MatchMapStatus.DestroyEnable)
                                    {
                                        await DestroyMapAsync(singleMatch);
                                    }
                                }
                            }

                            destroyMatchTimeOut.Clear();
                            matchStage = MatchStage.Finished;
                            return;
                        }
                        break;
                    }

                case MatchStage.Finished:
                    {
                        // step to next stage, if not final yet go back to wages and set wages timeout
                        Step();
                        if (GetStage() == TournamentStage.Final)
                        {
                            await SubmitEventWindowAsync();
                            return;
                        }

                        matchStage = MatchStage.Wages;
                        wagesTimeOut.Startup(WAGES_TIMEOUT);

                        foreach (var match in matches)
                        {
                            if (match is DoubleTournamentMatch<ElitePkParticipant, Character> doubleMatch)
                            {
                                if (firstStagePlayed && !doubleMatch.HasSecondMatch)
                                {
                                    continue;
                                }
                            }
                            await PrepareMapAsync(match);
                        }
                        await SubmitEventWindowAsync();
                        break;
                    }
            }
        }

        private async Task SetRankingAsync()
        {
            if (stage == InternalTournamentStage.RankingSet)
            {
                return;
            }

            int index = 0;
            foreach (var winner in tournamentParticipantReward.OrderByDescending(x => x.Rank))
            {
                await SetRankAsync(index++, winner.Participant);
            }
            await rankingInfo.SaveAsync();
            stage = InternalTournamentStage.RankingSet;
        }

        private async Task SetRankAsync(int position, ElitePkParticipant participant)
        {
            switch (position)
            {
                case 0:
                    {
                        rankingInfo.Id1 = participant?.Identity ?? 0;
                        rankingInfo.Name1 = participant?.Name ?? string.Empty;
                        rankingInfo.Lookface1 = participant?.Lookface ?? 0;

                        if (rankingInfo.Id1 > 1_000_000)
                        {
                            await Character.AddTitleAsync(rankingInfo.Id1, index == 3 ? ELITE_PK_CHAMPION_HIGH_TITLE : ELITE_PK_CHAMPION_LOW_TITLE);
                        }
                        break;
                    }
                case 1:
                    {
                        rankingInfo.Id2 = participant?.Identity ?? 0;
                        rankingInfo.Name2 = participant?.Name ?? string.Empty;
                        rankingInfo.Lookface2 = participant?.Lookface ?? 0;

                        if (rankingInfo.Id2 > 1_000_000)
                        {
                            await Character.AddTitleAsync(rankingInfo.Id2, index == 3 ? ELITE_PK_2ND_HIGH_TITLE : ELITE_PK_2ND_LOW_TITLE);
                        }
                        break;
                    }
                case 2:
                    {
                        rankingInfo.Id3 = participant?.Identity ?? 0;
                        rankingInfo.Name3 = participant?.Name ?? string.Empty;
                        rankingInfo.Lookface3 = participant?.Lookface ?? 0;

                        if (rankingInfo.Id3 > 1_000_000)
                        {
                            await Character.AddTitleAsync(rankingInfo.Id3, index == 3 ? ELITE_PK_3RD_HIGH_TITLE: ELITE_PK_3RD_LOW_TITLE);
                        }
                        break;
                    }
                case 3:
                    {
                        rankingInfo.Id4 = participant?.Identity ?? 0;
                        rankingInfo.Name4 = participant?.Name ?? string.Empty;
                        rankingInfo.Lookface4 = participant?.Lookface ?? 0;

                        if (rankingInfo.Id4 > 1_000_000)
                        {
                            await Character.AddTitleAsync(rankingInfo.Id4, index == 3 ? ELITE_PK_TOP8_HIGH_TITLE : ELITE_PK_TOP8_LOW_TITLE);
                        }
                        break;
                    }
                case 4:
                    {
                        rankingInfo.Id5 = participant?.Identity ?? 0;
                        rankingInfo.Name5 = participant?.Name ?? string.Empty;
                        rankingInfo.Lookface5 = participant?.Lookface ?? 0;

                        if (rankingInfo.Id5 > 1_000_000)
                        {
                            await Character.AddTitleAsync(rankingInfo.Id5, index == 3 ? ELITE_PK_TOP8_HIGH_TITLE : ELITE_PK_TOP8_LOW_TITLE);
                        }
                        break;
                    }
                case 5:
                    {
                        rankingInfo.Id6 = participant?.Identity ?? 0;
                        rankingInfo.Name6 = participant?.Name ?? string.Empty;
                        rankingInfo.Lookface6 = participant?.Lookface ?? 0;

                        if (rankingInfo.Id6 > 1_000_000)
                        {
                            await Character.AddTitleAsync(rankingInfo.Id6, index == 3 ? ELITE_PK_TOP8_HIGH_TITLE : ELITE_PK_TOP8_LOW_TITLE);
                        }
                        break;
                    }
                case 6:
                    {
                        rankingInfo.Id7 = participant?.Identity ?? 0;
                        rankingInfo.Name7 = participant?.Name ?? string.Empty;
                        rankingInfo.Lookface7 = participant?.Lookface ?? 0;

                        if (rankingInfo.Id7 > 1_000_000)
                        {
                            await Character.AddTitleAsync(rankingInfo.Id7, index == 3 ? ELITE_PK_TOP8_HIGH_TITLE : ELITE_PK_TOP8_LOW_TITLE);
                        }
                        break;
                    }
                case 7:
                    {
                        rankingInfo.Id8 = participant?.Identity ?? 0;
                        rankingInfo.Name8 = participant?.Name ?? string.Empty;
                        rankingInfo.Lookface8 = participant?.Lookface ?? 0;

                        if (rankingInfo.Id8 > 1_000_000)
                        {
                            await Character.AddTitleAsync(rankingInfo.Id8, index == 3 ? ELITE_PK_TOP8_HIGH_TITLE : ELITE_PK_TOP8_LOW_TITLE);
                        }
                        break;
                    }

            }
        }

        public override Task UnsubscribeAsync(ElitePkParticipant entity)
        {
            throw new NotImplementedException();
        }

        public override Task UnsubscribeAsync(uint identity)
        {
            throw new NotImplementedException();
        }

        private async Task PrepareMapAsync(BaseTournamentMatch<ElitePkParticipant, Character> match)
        {
            uint idMap = (uint)ElitePkTournament.ElitePkMap.GetNextIdentity;
            match.Map = new GameMap(new DbDynamap
            {
                Identity = idMap,
                Name = "ElitePKMap",
                Description = $"ElitePKMap",
                Type = (ulong)ElitePkTournament.BasePkMap.Type,
                OwnerIdentity = (uint)match.Identity,
                LinkMap = 1002,
                LinkX = 300,
                LinkY = 278,
                MapDoc = ElitePkTournament.BasePkMap.MapDoc,
                OwnerType = 1
            });

            if (!await match.Map.InitializeAsync())
            {
                logger.LogError($"Could not initialize map for elitepk!!!");
            }
            await MapManager.AddMapAsync(match.Map);

            match.MatchFlag = BaseTournamentMatch<ElitePkParticipant, Character>.MatchStatus.AcceptingWagers;
        }

        private async Task FlyToMapAsync(BaseTournamentMatch<ElitePkParticipant, Character> match)
        {
            if (match.Participant1?.Participant == null || match.Participant2?.Participant == null)
            {
                await EndMapAsync(match);
                match.MatchFlag = BaseTournamentMatch<ElitePkParticipant, Character>.MatchStatus.OK;
            }
            else
            {
                if (match is DoubleTournamentMatch<ElitePkParticipant, Character> doubleMatch)
                {
                    if (firstStagePlayed && !doubleMatch.HasSecondMatch)
                    {
                        return;
                    }

                    doubleMatch.SetFightFlag();
                    var fighters = doubleMatch.GetCurrentFighters();
                    await FlyUserAsync(match, fighters[0], fighters[1]);
                    await FlyUserAsync(match, fighters[1], fighters[0]);

                    if (doubleMatch.CurrentlyFighting == DoubleTournamentMatch<ElitePkParticipant, Character>.CurrentMatch.First && doubleMatch.Participant3 != null)
                    {
                        doubleMatch.Flag3 = BaseTournamentMatch<ElitePkParticipant, Character>.ContestantFlag.Waiting;
                    }
                }
                else
                {
                    await FlyUserAsync(match, match.Participant1.Participant, match.Participant2.Participant);
                    await FlyUserAsync(match, match.Participant2.Participant, match.Participant1.Participant);
                    match.Flag1 = BaseTournamentMatch<ElitePkParticipant, Character>.ContestantFlag.Fighting;
                    match.Flag2 = BaseTournamentMatch<ElitePkParticipant, Character>.ContestantFlag.Fighting;
                }

                match.MatchFlag = BaseTournamentMatch<ElitePkParticipant, Character>.MatchStatus.InFight;
            }
        }

        private async Task FlyUserAsync(BaseTournamentMatch<ElitePkParticipant, Character> match, Character user, Character target)
        {
            await user.DetachAllStatusAsync();
            await user.SetAttributesAsync(ClientUpdateType.Hitpoints, user.MaxLife);
            await user.SetAttributesAsync(ClientUpdateType.Mana, user.MaxMana);
            await user.SetAttributesAsync(ClientUpdateType.Stamina, user.MaxEnergy);

            int x = 32 + await NextAsync(37);
            int y = 32 + await NextAsync(37);

            await user.FlyMapAsync(match.Map.Identity, x, y);
            await user.SendAsync(new MsgElitePKArenic
            {
                Action = MsgElitePKArenic.ArenicAction.BeginMatch,
                Identity = target.Identity,
                Name = target.Name,
                TimeLeft = CalculateMatchTime()
            });
            await user.SetPkModeAsync(Character.PkModeType.FreePk);
        }

        private async Task EndMapAsync(BaseTournamentMatch<ElitePkParticipant, Character> match)
        {
            if (match.Finished)
            {
                return;
            }

            if (match.Participant1?.Identity == 1_000_001 || match.Participant2?.Identity == 1_000_001)
            {

            }

            ElitePkParticipant winner = null;
            ElitePkParticipant loser = null;

            // if no winner is set, then sort it
            if (match is DoubleTournamentMatch<ElitePkParticipant, Character> doubleMatch)
            {
                if (firstStagePlayed && !doubleMatch.HasSecondMatch)
                {
                    return;
                }

                SingleTournamentMatch<ElitePkParticipant, Character> currentMatch;
                if (doubleMatch.CurrentlyFighting == DoubleTournamentMatch<ElitePkParticipant, Character>.CurrentMatch.First)
                {
                    currentMatch = doubleMatch.Match1;
                }
                else
                {
                    currentMatch = doubleMatch.Match2;
                }

                if (currentMatch.Winner == null)
                {
                    currentMatch.AssumeWinner();
                    if (currentMatch.Winner == currentMatch.Participant1)
                    {
                        winner = currentMatch.Participant1;
                        loser = currentMatch.Participant2;
                    }
                    else
                    {
                        winner = currentMatch.Participant2;
                        loser = currentMatch.Participant1;
                    }
                }
            }
            else
            {
                doubleMatch = null;
                SingleTournamentMatch<ElitePkParticipant, Character> singleMatch = match as SingleTournamentMatch<ElitePkParticipant, Character>;
                if (singleMatch.Winner == null)
                {
                    singleMatch.AssumeWinner();
                    if (singleMatch.Winner == singleMatch.Participant1)
                    {
                        winner = singleMatch.Winner = singleMatch.Participant1;
                        loser = singleMatch.Participant2;
                    }
                    else
                    {
                        winner = singleMatch.Winner = singleMatch.Participant2;
                        loser = singleMatch.Participant1;
                    }
                }
            }

            match.MatchFlag = BaseTournamentMatch<ElitePkParticipant, Character>.MatchStatus.OK;

            if (winner?.Participant != null)
            {
                winner.Participant.HonorPoints += 1000;
                if (!winner.Participant.UserPackage.IsPackFull())
                {
                    await winner.Participant.UserPackage.AwardItemAsync(723912);
                } // TODO send via mail
                await winner.Participant.SendAsync(new MsgElitePKArenic
                {
                    Action = MsgElitePKArenic.ArenicAction.Effect,
                    Effect = MsgElitePKArenic.EffectType.Victory
                });
                await winner.Participant.SendAsync(new MsgElitePKArenic
                {
                    Action = MsgElitePKArenic.ArenicAction.EndMatch,
                    Effect = MsgElitePKArenic.EffectType.Victory
                });
            }

            if (loser?.Participant != null)
            {
                loser.Participant.HonorPoints += 1000;
                if (!loser.Participant.UserPackage.IsPackFull())
                {
                    await loser.Participant.UserPackage.AwardItemAsync(723912);
                } // TODO send via mail
                await loser.Participant.SendAsync(new MsgElitePKArenic
                {
                    Action = MsgElitePKArenic.ArenicAction.Effect,
                    Effect = MsgElitePKArenic.EffectType.Defeat
                });
                await loser.Participant.SendAsync(new MsgElitePKArenic
                {
                    Action = MsgElitePKArenic.ArenicAction.EndMatch,
                    Effect = MsgElitePKArenic.EffectType.Victory
                });
            }

            if (doubleMatch != null)
            {
                if (doubleMatch.IsSecondMatch)
                {
                    doubleMatch.SetSecondMatchWinner(winner?.Identity ?? loser?.Identity ?? 0);
                }
                else
                {
                    doubleMatch.SetFirstMatchWinner(winner?.Identity ?? loser?.Identity ?? 0);
                }
            }
            else if (winner != null)
            {
                if (winner.Identity == match.Participant1.Identity)
                {
                    match.Flag1 = BaseTournamentMatch<ElitePkParticipant, Character>.ContestantFlag.Qualified;
                    match.Flag2 = BaseTournamentMatch<ElitePkParticipant, Character>.ContestantFlag.Lost;
                }
                else
                {
                    match.Flag1 = BaseTournamentMatch<ElitePkParticipant, Character>.ContestantFlag.Lost;
                    match.Flag2 = BaseTournamentMatch<ElitePkParticipant, Character>.ContestantFlag.Qualified;
                }
            }

            if (winner != null && loser != null)
            {
                if (winner.Participant.SyndicateIdentity != 0 && loser.Participant.SyndicateIdentity != 0)
                {
                    await BroadcastWorldMsgAsync(string.Format(StrEliteCompeteWin0, winner.Participant.SyndicateName, winner.Participant.Name, name, loser.Participant.SyndicateName, loser.Participant.Name), TalkChannel.Qualifier);
                }
                else if (winner.Participant.SyndicateIdentity != 0)
                {
                    await BroadcastWorldMsgAsync(string.Format(StrEliteCompeteWin2, winner.Participant.Name, name, loser.Participant.SyndicateName, loser.Participant.Name), TalkChannel.Qualifier);
                }
                else if (loser.Participant.SyndicateIdentity != 0)
                {
                    await BroadcastWorldMsgAsync(string.Format(StrEliteCompeteWin1, winner.Participant.SyndicateName, winner.Participant.Name, name, loser.Participant.Name), TalkChannel.Qualifier);
                }
                else
                {
                    await BroadcastWorldMsgAsync(string.Format(StrEliteCompeteWin3, winner.Participant.Name, name, loser.Participant.Name), TalkChannel.Qualifier);
                }
            }

            match.Finished = true;
        }

        private async Task DestroyMapAsync(SingleTournamentMatch<ElitePkParticipant, Character> match)
        {
            if (!match.Finished)
            {
                return;
            }

            foreach (var user in match.Map.QueryPlayers())
            {
                if (user.Identity == match.Participant1.Identity || user.Identity == match.Participant2.Identity)
                {
                    if (!user.IsAlive)
                    {
                        // revive and go back to previous map
                        await user.RebornAsync(true);
                        continue;
                    }

                    var point = await match.Map.QueryRandomPositionAsync();
                    await user.FlyMapAsync(waitingMapId, point.X, point.Y);
                }
                else
                {
                    await user.FlyMapAsync(user.RecordMapIdentity, user.RecordMapX, user.RecordMapY);
                }
            }

            match.SetDestroyed();
            await MapManager.RemoveMapAsync(match.Map.Identity);
            ElitePkTournament.ElitePkMap.ReturnIdentity(match.Map.Identity);
        }

#if DEBUG
        public async Task DebugStepAsync()
        {
            foreach (var match in matches)
            {
                match.Score1 = await NextAsync(1000000);
                match.Score2 = await NextAsync(1000000);
                await EndMapAsync(match);
            }
            await SubmitEventWindowAsync();
        }
#endif

        private enum MatchStage
        {
            Wages,
            Running,
            DestroyMatches,
            Finished
        }

        public enum InternalTournamentStage
        {
            /// <summary>
            /// Event is doing nothing.
            /// </summary>
            Idle,
            /// <summary>
            /// Start time has been set, waiting for time.
            /// </summary>
            Awaiting,
            /// <summary>
            /// Event is currently running.
            /// </summary>
            Running,
            /// <summary>
            /// Ranking is set, awaiting for other groups to finish.
            /// </summary>
            RankingSet,

            Error = int.MaxValue
        }

        public class RankingInfo
        {
            private readonly DbPkInfo pkInfo;

            public RankingInfo(DbPkInfo pkInfo)
            {
                this.pkInfo = pkInfo;
            }

            public async Task InitializeAsync()
            {
                Lookface1 = (await CharacterRepository.FindAsync(Id1))?.Mesh ?? MsgTalk.SystemLookface;
                Lookface2 = (await CharacterRepository.FindAsync(Id2))?.Mesh ?? MsgTalk.SystemLookface;
                Lookface3 = (await CharacterRepository.FindAsync(Id3))?.Mesh ?? MsgTalk.SystemLookface;
                Lookface4 = (await CharacterRepository.FindAsync(Id4))?.Mesh ?? MsgTalk.SystemLookface;
                Lookface5 = (await CharacterRepository.FindAsync(Id5))?.Mesh ?? MsgTalk.SystemLookface;
                Lookface6 = (await CharacterRepository.FindAsync(Id6))?.Mesh ?? MsgTalk.SystemLookface;
                Lookface7 = (await CharacterRepository.FindAsync(Id7))?.Mesh ?? MsgTalk.SystemLookface;
                Lookface8 = (await CharacterRepository.FindAsync(Id8))?.Mesh ?? MsgTalk.SystemLookface;
            }

            public uint Id1 { get => pkInfo.Pk1; set => pkInfo.Pk1 = value; }
            public uint Lookface1 { get; set; }
            public string Name1 { get => pkInfo.Pk1Name; set => pkInfo.Pk1Name = value; }

            public uint Id2 { get => pkInfo.Pk2; set => pkInfo.Pk2 = value; }
            public uint Lookface2 { get; set; }
            public string Name2 { get => pkInfo.Pk2Name; set => pkInfo.Pk2Name = value; }

            public uint Id3 { get => pkInfo.Pk3; set => pkInfo.Pk3 = value; }
            public uint Lookface3 { get; set; }
            public string Name3 { get => pkInfo.Pk3Name; set => pkInfo.Pk3Name = value; }

            public uint Id4 { get => pkInfo.Pk4; set => pkInfo.Pk4 = value; }
            public uint Lookface4 { get; set; }
            public string Name4 { get => pkInfo.Pk4Name; set => pkInfo.Pk4Name = value; }

            public uint Id5 { get => pkInfo.Pk5; set => pkInfo.Pk5 = value; }
            public uint Lookface5 { get; set; }
            public string Name5 { get => pkInfo.Pk5Name; set => pkInfo.Pk5Name = value; }

            public uint Id6 { get => pkInfo.Pk6; set => pkInfo.Pk6 = value; }
            public uint Lookface6 { get; set; }
            public string Name6 { get => pkInfo.Pk6Name; set => pkInfo.Pk6Name = value; }

            public uint Id7 { get => pkInfo.Pk7; set => pkInfo.Pk7 = value; }
            public uint Lookface7 { get; set; }
            public string Name7 { get => pkInfo.Pk7Name; set => pkInfo.Pk7Name = value; }

            public uint Id8 { get => pkInfo.Pk8; set => pkInfo.Pk8 = value; }
            public uint Lookface8 { get; set; }
            public string Name8 { get => pkInfo.Pk8Name; set => pkInfo.Pk8Name = value; }

            public Task SaveAsync()
            {
                return ServerDbContext.SaveAsync(pkInfo);
            }
        }
    }
}
