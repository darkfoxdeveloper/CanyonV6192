using Canyon.Game.States.Events.Interfaces;

namespace Canyon.Game.States.Events.Tournament
{
    public class DoubleTournamentMatch<TParticipant, TEntity>
        : BaseTournamentMatch<TParticipant, TEntity>
        where TParticipant : ITournamentEventParticipant<TEntity>
    {
        private SingleTournamentMatch<TParticipant, TEntity> match1;
        private SingleTournamentMatch<TParticipant, TEntity> match2;

        public DoubleTournamentMatch(int identity, int index)
            : base(identity, index)
        {
            match1 = new SingleTournamentMatch<TParticipant, TEntity>(identity, index);
            match2 = new SingleTournamentMatch<TParticipant, TEntity>(identity, index);

            Flag1 = ContestantFlag.Waiting;
            Flag2 = ContestantFlag.Waiting;
            Flag3 = ContestantFlag.Waiting;
        }

        public TParticipant Participant3 { get; private set; }
        public ContestantFlag Flag3 { get; set; }

        public ContestantFlag GetParticipantFlag(uint participantId)
        {
            if (participantId == Participant1?.Identity)
            {
                return Flag1;
            }
            if (participantId == Participant2?.Identity)
            {
                return Flag2;
            }
            if (participantId == Participant3?.Identity)
            {
                return Flag3;
            }
            return ContestantFlag.None;
        }

        public SingleTournamentMatch<TParticipant, TEntity> Match1 => match1;
        public SingleTournamentMatch<TParticipant, TEntity> Match2 => match2;

        public CurrentMatch CurrentlyFighting { get; set; } = CurrentMatch.First;

        public bool IsSecondMatch => HasSecondMatch && CurrentlyFighting == CurrentMatch.Second;
        public bool HasSecondMatch => Participant3 != null;

        public void SetFightFlag()
        {
            if (CurrentlyFighting == CurrentMatch.First)
            {
                MatchFlag = MatchStatus.InFight;
                Flag1 = ContestantFlag.Fighting;
                Flag2 = ContestantFlag.Fighting;
            }
            else if (IsSecondMatch)
            {
                MatchFlag = MatchStatus.InFight;
                if (Match2.Participant1.Identity == Participant1.Identity
                    || Match2.Participant2.Identity == Participant1.Identity)
                {
                    Flag1 = ContestantFlag.Fighting;
                }
                if (Match2.Participant1.Identity == Participant2.Identity
                    || Match2.Participant2.Identity == Participant2.Identity)
                {
                    Flag2 = ContestantFlag.Fighting;
                }
                if (Match2.Participant1.Identity == Participant3.Identity
                    || Match2.Participant2.Identity == Participant3.Identity)
                {
                    Flag3 = ContestantFlag.Fighting;
                }
            }
        }

        public List<TEntity> GetCurrentFighters()
        {
            if (IsSecondMatch)
            {
                return new List<TEntity>
                {
                    Match2.Participant1.Participant,
                    Match2.Participant2.Participant
                };
            }
            return new List<TEntity>
            {
                Participant1.Participant,
                Participant2.Participant
            };
        }

        public void SetFirstMatch()
        {
            match1 = new SingleTournamentMatch<TParticipant, TEntity>(Identity, Index)
            {
                Participant1 = Participant1,
                Participant2 = Participant2
            };
        }

        public void SetSecondMatch(TParticipant participant3)
        {
            Participant3 = participant3;
            Flag3 = ContestantFlag.Inactive;
            match2 = new SingleTournamentMatch<TParticipant, TEntity>(Identity, Index)
            {
                Participant2 = Participant3
            };
        }

        public void SetFirstMatchWinner(uint idWinner)
        {
            if (idWinner == 0 || Match1 == null)
            {
                return;
            }

            if (idWinner == Match1.Participant1.Identity)
            {
                Match1.Winner = Participant1;
                if (Participant3 == null)
                {
                    Flag1 = ContestantFlag.Qualified;
                }
                else if (HasSecondMatch)
                {
                    Flag1 = ContestantFlag.Waiting;
                }
                Flag2 = ContestantFlag.Lost;

                if (HasSecondMatch)
                {
                    Match2.Participant1 = Participant1;
                }
            }
            else
            {
                Match1.Winner = Participant2;
                if (Participant3 == null)
                {
                    Flag2 = ContestantFlag.Qualified;
                }
                else if (HasSecondMatch)
                {
                    Flag2 = ContestantFlag.Waiting;
                }
                Flag1 = ContestantFlag.Lost;

                if (HasSecondMatch)
                {
                    Match2.Participant1 = Participant2;
                }
            }

            Match1.SetDestructionTime();
        }

        public void SetSecondMatchWinner(uint idWinner)
        {
            if (idWinner == 0 || Match2 == null)
            {
                return;
            }

            if (idWinner == Participant1.Identity)
            {
                Winner = Participant1;
                Flag1 = ContestantFlag.Qualified;
                Flag2 = Flag3 = ContestantFlag.Lost;
            }
            else if (idWinner == Participant2.Identity)
            {
                Winner = Participant2;
                Flag2 = ContestantFlag.Qualified;
                Flag1 = Flag3 = ContestantFlag.Lost;
            }
            else if (idWinner == Participant3.Identity)
            {
                Winner = Participant3;
                Flag3 = ContestantFlag.Qualified;
                Flag1 = Flag2 = ContestantFlag.Lost;
            }

            if (idWinner == Match2.Participant1.Identity)
            {
                Match2.Winner = Match2.Participant1;
            }
            else
            {
                Match2.Winner = Match2.Participant2;
            }

            Match2.SetDestructionTime();
        }

        public override string ToString()
        {
            return $"{Participant1?.Name ?? StrNone} vs {Participant2?.Name ?? StrNone} vs {Participant3?.Name ?? StrNone}";
        }

        public enum CurrentMatch
        {
            First,
            Second
        }
    }
}
