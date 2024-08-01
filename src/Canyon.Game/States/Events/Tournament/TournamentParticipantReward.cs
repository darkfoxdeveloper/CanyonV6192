using Canyon.Game.States.Events.Interfaces;

namespace Canyon.Game.States.Events.Tournament
{
    public sealed class TournamentParticipantReward<TParticipant, TEntity>
        where TParticipant : ITournamentEventParticipant<TEntity>
    {
        public TournamentParticipantReward(TParticipant participant, TournamentParticipantRank rank)
        {
            Participant = participant;
            Rank = rank;
        }

        public TParticipant Participant { get; }
        public TournamentParticipantRank Rank { get; }
    }
}
