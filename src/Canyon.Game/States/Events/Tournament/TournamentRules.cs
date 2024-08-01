using static Canyon.Game.Sockets.Game.Packets.MsgPeerage;

namespace Canyon.Game.States.Events.Tournament
{
    public sealed class TournamentRules
    {

        public int MinLevel { get; init; }
        public int MaxLevel { get; init; }
        
        public int Metempsychosis { get; init; }

        public NobilityRank MinNobility { get; init; }
        public NobilityRank MaxNobility { get; init; }

    }
}
