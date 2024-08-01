#define DEBUG_TOURNAMENT

using Canyon.Game.Services.Managers;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States.Events.Interfaces;
using Canyon.Game.States.User;

namespace Canyon.Game.States.Events.Tournament
{
    public abstract class GameTournamentEvent<TParticipant, TEntity>
        : ITournamentEvent<TParticipant, TEntity>
        where TParticipant : ITournamentEventParticipant<TEntity>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<GameTournamentEvent<TParticipant, TEntity>>();

        private static readonly Random random = new(); // not using the async one here

        protected readonly TournamentRules tournamentRules;

        protected bool firstStagePlayed = false;
        protected bool singleStageOnly = false;

        protected List<TournamentParticipantReward<TParticipant, TEntity>> tournamentParticipantReward = new();
        protected List<TParticipant> participants = new();

        protected List<BaseTournamentMatch<TParticipant, TEntity>> matches = new();
        protected List<BaseTournamentMatch<TParticipant, TEntity>> previousMatches = new();

        protected SingleTournamentMatch<TParticipant, TEntity> thirdPlace;
        protected SingleTournamentMatch<TParticipant, TEntity> firstPlace;

        public GameTournamentEvent()
        {
            tournamentRules = new TournamentRules
            {
                MinLevel = 1,
                MaxLevel = ExperienceManager.GetLevelLimit(),
                MinNobility = MsgPeerage.NobilityRank.Serf,
                MaxNobility = MsgPeerage.NobilityRank.King,
                Metempsychosis = 0
            };
        }

        public GameTournamentEvent(TournamentRules tournamentRules)
        {
            this.tournamentRules = tournamentRules;
        }

        public TournamentRules Rules => tournamentRules;

        public int CurrentMatchIdentity { get; protected set; }
        public int CurrentMatchIndex { get; protected set; }

        public abstract Task<bool> InscribeAsync(TParticipant entity);
        public abstract Task SubmitEventWindowAsync(Character target, int page = 0);
        public abstract Task UnsubscribeAsync(TParticipant entity);
        public abstract Task UnsubscribeAsync(uint identity);
        public abstract bool IsContestant(uint identity); // user

        public abstract bool IsParticipantAllowedToJoin(TEntity participant);

        public void Start()
        {
            GenerateMatches();
        }

        public void Step()
        {
            bool generateBrackets = true;
            TournamentStage currentStage = GetStage();
            if (matches.All(x => x is SingleTournamentMatch<TParticipant, TEntity>))
            {
                bool cleanUpRound = currentStage == TournamentStage.QuarterFinals || currentStage == TournamentStage.SemiFinals;
                if (cleanUpRound)
                {
                    // in order to keep the matches order, clean up and re-add the winners in match order
                    participants.Clear();
                }

                foreach (var singleMatch in matches.Cast<SingleTournamentMatch<TParticipant, TEntity>>())
                {
                    TParticipant loser;
                    if (!singleMatch.Finished)
                    {
                        singleMatch.AssumeWinner();
                    }

                    if (singleMatch.Participant1?.Identity == singleMatch.Winner?.Identity)
                    {
                        singleMatch.Flag1 = BaseTournamentMatch<TParticipant, TEntity>.ContestantFlag.Qualified;
                        loser = singleMatch.Participant2;
                    }
                    else
                    {
                        singleMatch.Flag2 = BaseTournamentMatch<TParticipant, TEntity>.ContestantFlag.Qualified;
                        loser = singleMatch.Participant1;
                    }

                    if (cleanUpRound)
                    {
                        participants.Add(singleMatch.Winner);
                    }
                    else
                    {
                        participants.RemoveAll(x => x == null);
                        participants.RemoveAll(x => x.Identity == loser?.Identity);
                    }

                    if (singleMatch.Stage == TournamentStage.SemiFinals)
                    {
                        if (firstPlace == null)
                        {
                            firstPlace = new SingleTournamentMatch<TParticipant, TEntity>(CurrentMatchIdentity++, CurrentMatchIndex++)
                            {
                                Participant1 = singleMatch.Winner,
                                Stage = TournamentStage.Finals
                            };
                        }
                        else
                        {
                            firstPlace.Participant2 = singleMatch.Winner;
                        }

                        if (thirdPlace == null)
                        {
                            thirdPlace = new SingleTournamentMatch<TParticipant, TEntity>(CurrentMatchIdentity++, CurrentMatchIndex++)
                            {
                                Participant1 = loser,
                                Stage = TournamentStage.ThirdPlace
                            };
                        }
                        else
                        {
                            thirdPlace.Participant2 = loser;
                        }
                    }
                    else if (singleMatch.Stage == TournamentStage.ThirdPlace)
                    {
                        tournamentParticipantReward.Add(new TournamentParticipantReward<TParticipant, TEntity>(thirdPlace.Winner, TournamentParticipantRank.Top3));
                        tournamentParticipantReward.Add(new TournamentParticipantReward<TParticipant, TEntity>(loser, TournamentParticipantRank.Top8));
                        thirdPlace = null;
                    }
                    else if (singleMatch.Stage == TournamentStage.Finals)
                    {
                        tournamentParticipantReward.Add(new TournamentParticipantReward<TParticipant, TEntity>(firstPlace.Winner, TournamentParticipantRank.Champion));
                        tournamentParticipantReward.Add(new TournamentParticipantReward<TParticipant, TEntity>(loser, TournamentParticipantRank.Top2));
                        firstPlace = null;
                    }
                    else
                    {
                        tournamentParticipantReward.Add(new TournamentParticipantReward<TParticipant, TEntity>(loser, TournamentParticipantRank.Top8));
                    }

                    singleMatch.MatchFlag = BaseTournamentMatch<TParticipant, TEntity>.MatchStatus.OK;
                    singleMatch.Finished = true;
                    previousMatches.Add(singleMatch);
                }
            }
            else if (matches.All(x => x is DoubleTournamentMatch<TParticipant, TEntity>))
            {
                if (!firstStagePlayed)
                {
                    firstStagePlayed = true;
                    foreach (var doubleMatch in matches.Cast<DoubleTournamentMatch<TParticipant, TEntity>>())
                    {
                        TParticipant loser;
                        // winner MUST be defined, but let's assume it's not
                        if (doubleMatch.Match1.Winner == null)
                        {
                            doubleMatch.Match1.AssumeWinner();
                            doubleMatch.SetFirstMatchWinner(doubleMatch.Match1.Winner.Identity);
                            if (doubleMatch.Match1.Winner.Identity == doubleMatch.Match1.Participant1.Identity)
                            {
                                loser = doubleMatch.Match1.Participant2;
                            }
                            else
                            {
                                loser = doubleMatch.Match1.Participant1;
                            }
                        }
                        else
                        {
                            doubleMatch.SetFirstMatchWinner(doubleMatch.Match1.Winner.Identity);
                            if (doubleMatch.Match1.Winner.Identity == doubleMatch.Match1.Participant1.Identity)
                            {
                                loser = doubleMatch.Match1.Participant2;
                            }
                            else
                            {
                                loser = doubleMatch.Match1.Participant1;
                            }
                        }

                        participants.RemoveAll(x => x.Identity == loser?.Identity);
                        doubleMatch.Match1.Finished = true;
                        doubleMatch.CurrentlyFighting = DoubleTournamentMatch<TParticipant, TEntity>.CurrentMatch.Second;
                        doubleMatch.Finished = !doubleMatch.HasSecondMatch;
                        previousMatches.Add(doubleMatch.Match1);
                    }
                    generateBrackets = singleStageOnly;
                }
                else
                {
                    foreach (var doubleMatch in matches.Cast<DoubleTournamentMatch<TParticipant, TEntity>>())
                    {
                        if (!doubleMatch.HasSecondMatch)
                        {
                            continue;
                        }

                        TParticipant loser;
                        // winner MUST be defined, but let's assume it's not
                        if (doubleMatch.Match2.Winner == null)
                        {
                            doubleMatch.Match2.AssumeWinner();
                            doubleMatch.SetSecondMatchWinner(doubleMatch.Match2.Winner.Identity);
                            if (doubleMatch.Match2.Score1 > doubleMatch.Match2.Score2)
                            {                                
                                loser = doubleMatch.Match2.Participant2;
                            }
                            else
                            {
                                loser = doubleMatch.Match2.Participant1;
                            }
                        }
                        else
                        {
                            if (doubleMatch.Match2.Winner.Identity == doubleMatch.Match2.Participant1.Identity)
                            {
                                loser = doubleMatch.Match2.Participant2;
                            }
                            else
                            {
                                loser = doubleMatch.Match2.Participant1;
                            }
                        }

                        participants.RemoveAll(x => x.Identity == loser?.Identity);
                        doubleMatch.Match2.Finished = true;
                        previousMatches.Add(doubleMatch.Match2);
                    }
                }
            }
            else
            {
                logger.LogCritical("ERROR ON MATCHMAKING!!! MATCHES ARE NOT OF SAME TYPE");
            }

            if (generateBrackets)
            {
                matches.Clear();
            }

            currentStage = GetStage();
            if (currentStage == TournamentStage.QuarterFinals)
            {
                var tempParticipants = new List<TParticipant>(participants);
                participants.Clear();

                do
                {
                    int idx = random.Next(tempParticipants.Count) % tempParticipants.Count;
                    participants.Add(tempParticipants[idx]);
                    tempParticipants.RemoveAt(idx);
                }
                while (tempParticipants.Count > 0);
            }

            if (generateBrackets || singleStageOnly)
            {
                GenerateMatches();
            }
        }

        public void GenerateMatches()
        {
            TournamentStage currentStage = GetStage();
            logger.LogDebug($"Generating matches for stage: {currentStage}");
            List<TParticipant> participants = this.participants.ToList();
            if (!IsDoubleMatchStage())
            {
                if (thirdPlace != null)
                {
                    matches.Add(thirdPlace);
                }
                else if (firstPlace != null)
                {
                    matches.Add(firstPlace);
                }
                else
                {
                    if (participants.Count > 8)
                    {
                        do
                        {
                            int idx = random.Next(participants.Count) % participants.Count;
                            TParticipant p1 = participants[idx];
                            participants.RemoveAt(idx);

                            TParticipant p2 = default;
                            if (participants.Count > 0)
                            {
                                idx = random.Next(participants.Count) % participants.Count;
                                p2 = participants[idx];
                                participants.RemoveAt(idx);
                            }

                            SingleTournamentMatch<TParticipant, TEntity> gameMatch = new SingleTournamentMatch<TParticipant, TEntity>(CurrentMatchIdentity++, CurrentMatchIndex++);
                            gameMatch.Participant1 = p1;
                            gameMatch.Participant2 = p2;
                            gameMatch.Stage = currentStage;
                            matches.Add(gameMatch);
                        }
                        while (participants.Count > 0);
                    }
                    else
                    {
                        int matchAmount = 4;
                        if (currentStage == TournamentStage.SemiFinals)
                        {
                            matchAmount = 2;
                        }

                        int idx = 0;
                        do
                        {
                            int[] quarterFinals =
                            {
                                0, 2, 1, 3
                            };
                            SingleTournamentMatch<TParticipant, TEntity> match;
                            if (matches.Count >= (idx % matchAmount) + 1)
                            {
                                int matchIdx = currentStage == TournamentStage.QuarterFinals ? quarterFinals[idx % matchAmount] : idx % matchAmount;
                                match = matches[matchIdx] as SingleTournamentMatch<TParticipant, TEntity>;
                            }
                            else
                            {
                                match = new SingleTournamentMatch<TParticipant, TEntity>(CurrentMatchIdentity++, CurrentMatchIndex++)
                                {
                                    Stage = currentStage
                                };
                                matches.Add(match);
                            }

                            if (match.Participant1 == null)
                            {
                                match.Participant1 = participants[0];
                            }
                            else
                            {
                                match.Participant2 = participants[0];
                            }

                            participants.RemoveAt(0);
                            idx++;
                        }
                        while (participants.Count > 0);
                    }
                }
            }
            else
            {
                if (this.participants.Count <= 16)
                {
                    singleStageOnly = true;
                }

                int idx = 0;
                do
                {
                    int rand = random.Next(participants.Count) % participants.Count;
                    DoubleTournamentMatch<TParticipant, TEntity> doubleMatch;
                    if (matches.Count >= (idx % 8) + 1)
                    {
                        doubleMatch = matches[idx % 8] as DoubleTournamentMatch<TParticipant, TEntity>;
                    }
                    else
                    {
                        doubleMatch = new DoubleTournamentMatch<TParticipant, TEntity>(CurrentMatchIdentity++, CurrentMatchIndex++);
                        doubleMatch.Stage = currentStage;
                        matches.Add(doubleMatch);
                    }

                    TParticipant participant = participants[rand];
                    if (doubleMatch.Participant1 == null)
                    {
                        doubleMatch.Participant1 = participant;
                        doubleMatch.SetFirstMatch();

                    }
                    else if (doubleMatch.Participant2 == null)
                    {
                        doubleMatch.Participant2 = participant;
                        doubleMatch.SetFirstMatch();
                    }
                    else
                    {
                        doubleMatch.SetSecondMatch(participant);
                    }

                    participants.RemoveAt(rand);
                    idx++;
                }
                while (participants.Count > 0);
            }

#if DEBUG_TOURNAMENT
            DisplayBrackets();
#endif
        }

        #region DEBUG_TOURNAMENT

        public void DisplayBrackets()
        {
            int m = 1;
            logger.LogInformation($"Displaying matches for: {GetStage()}");
            if (!IsDoubleMatchStage())
            {
                if (GetStage() == TournamentStage.Final)
                {
                    if (participants.Count > 0)
                    {
                        logger.LogInformation($"Champion: {participants.FirstOrDefault().Name}!!");
                    }
                }
                foreach (var match in matches)
                {
                    logger.LogInformation($"[{match.Stage,-16}] {match.Participant1?.Name ?? "Bye",16} vs {match.Participant2?.Name ?? "Bye",16}");
                }
            }
            else
            {
                foreach (var doubleMatch in matches.Where(x => x is DoubleTournamentMatch<TParticipant, TEntity>).Cast<DoubleTournamentMatch<TParticipant, TEntity>>())
                {
                    if (doubleMatch.Match1.Finished)
                    {
                        if (doubleMatch.Match2.Finished)
                        {
                            SingleTournamentMatch<TParticipant, TEntity> match = doubleMatch.Match2;
                            logger.LogInformation($"[{doubleMatch.Stage,-16}] {match.Participant1?.Name ?? "Bye",16} vs {match.Participant2?.Name ?? "Bye",16}");
                        }
                        else
                        {
                            SingleTournamentMatch<TParticipant, TEntity> match = doubleMatch.Match1;
                            logger.LogInformation($"[{doubleMatch.Stage,-16}] {match.Participant1?.Name ?? "Bye",16} vs {match.Participant2?.Name ?? "Bye",16}");
                        }
                    }
                    else
                    {
                        logger.LogInformation($"[{doubleMatch.Stage,-16}] {doubleMatch.Participant1?.Name ?? "Bye",16} vs {doubleMatch.Participant2?.Name ?? "Bye",-16} - {doubleMatch.Participant3?.Name ?? "Bye",-16}");
                    }
                }
            }
        }

        #endregion

        public bool IsDoubleMatchStage()
        {
            return GetStage() == TournamentStage.Knockout8;
        }

        public TournamentStage GetStage()
        {
            if (participants.Count == 0)
            {
                return TournamentStage.None;
            }
            if (participants.Count > 24)
            {
                return TournamentStage.Knockout;
            }
            if (participants.Count > 8)
            {
                return TournamentStage.Knockout8;
            }
            if (participants.Count > 4)
            {
                return TournamentStage.QuarterFinals;
            }
            if (participants.Count > 2)
            {
                return TournamentStage.SemiFinals;
            }
            if (participants.Count > 1)
            {
                if (thirdPlace != null && !thirdPlace.Finished)
                {
                    return TournamentStage.ThirdPlace;
                }
                if (firstPlace != null)
                {
                    return TournamentStage.Finals;
                }
            }
            return TournamentStage.Final;
        }
    }
}
