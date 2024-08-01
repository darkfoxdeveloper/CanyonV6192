using Canyon.Game.States.Events.Interfaces;
using Canyon.Game.States.World;
using System.Collections.Concurrent;

namespace Canyon.Game.States.Events.Tournament
{
    public abstract class BaseTournamentMatch<TParticipant, TEntity>
        where TParticipant : ITournamentEventParticipant<TEntity>
    {
        protected readonly ConcurrentDictionary<uint, TournamentBet> bets = new();

        public BaseTournamentMatch(int identity, int index)
        {
            Identity = identity;
            Index = index;
        }

        public int Identity { get; }
        public int Index { get; }

        public GameMap Map { get; set; }

        public MatchStatus MatchFlag { get; set; }

        public TournamentStage Stage { get; set; }

        public int Score1 { get; set; }
        public int Supporters1 { get; set; }
        public TParticipant Participant1 { get; set; }
        public ContestantFlag Flag1 { get; set; }

        public int Score2 { get; set; }
        public int Supporters2 { get; set; }
        public TParticipant Participant2 { get; set; }
        public ContestantFlag Flag2 { get; set; }

        public TParticipant Winner { get; set; }
        public bool Finished { get; set; }

        public override string ToString()
        {
            return $"{Participant1?.Name ?? StrNone} vs {Participant2?.Name ?? StrNone}";
        }

        public enum MatchStatus : ushort
        {
            AcceptingWagers = 0,
            Watchable = 1,
            SwitchOut = 2,
            InFight = 3,
            OK = 4
        }

        public enum ContestantFlag : uint
        {
            None = 0,
            Fighting = 1,
            Lost = 2,
            Qualified = 3,
            Waiting = 4,
            Bye = 5,
            Inactive = 7,
            WonMatch = 8
        }
    }
}
