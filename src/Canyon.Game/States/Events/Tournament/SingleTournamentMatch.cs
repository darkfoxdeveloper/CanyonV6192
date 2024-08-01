using Canyon.Game.States.Events.Interfaces;
using Canyon.Game.States.User;

namespace Canyon.Game.States.Events.Tournament
{
    public class SingleTournamentMatch<TParticipant, TEntity>
        : BaseTournamentMatch<TParticipant, TEntity>
        where TParticipant : ITournamentEventParticipant<TEntity>
    {
        private TimeOut destroyTimeOut = new TimeOut();

        public SingleTournamentMatch(int identity, int index)
            : base(identity, index)
        {
        }

        public MatchMapStatus MapStatus { get; set; }

        public bool IsDestroyTime => MapStatus == MatchMapStatus.DestroyEnable && destroyTimeOut.IsActive() && destroyTimeOut.Clear();

        public void AssumeWinner()
        {
            if (Score1 == Score2)
            {
                if (Participant1 != null && Participant1.Participant != null && Participant2 != null && Participant2.Participant != null) 
                { 
                    if (typeof(TEntity) == typeof(Character))
                    {
                        Character p1 = Participant1.Participant as Character;
                        Character p2 = Participant2.Participant as Character;
                        if (p1.BattlePower == p2.BattlePower)
                        {
                            if (p1.Identity < p2.Identity) 
                            {
                                Winner = Participant1;
                            }
                            else
                            {
                                Winner = Participant2;
                            }
                        }
                        else if (p1.BattlePower > p2.BattlePower)
                        {
                            Winner = Participant1;
                        }
                        else
                        {
                            Winner = Participant2;
                        }
                    }
                    // TODO must add support to Teams
                }
                else if (Participant1 != null && Participant1.Participant != null)
                {
                    Winner = Participant1;
                }
                else if (Participant2 != null && Participant2.Participant != null)
                {
                    Winner = Participant2;
                }
            }
            else
            {
                if (Score1 > Score2)
                {
                    Winner = Participant1;
                }
                else
                {
                    Winner = Participant2;
                }
            }
        }

        public void SetDestructionTime()
        {
            destroyTimeOut.Startup(5);
            MapStatus = MatchMapStatus.DestroyEnable;
        }

        public void SetDestroyed()
        {
            destroyTimeOut.Clear();
            MapStatus = MatchMapStatus.Destroyed;
        }

        public enum MatchMapStatus
        {
            Ok,
            DestroyEnable,
            Destroyed
        }
    }
}
