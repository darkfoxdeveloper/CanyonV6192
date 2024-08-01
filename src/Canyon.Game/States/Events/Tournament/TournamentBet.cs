using Canyon.Game.States.User;

namespace Canyon.Game.States.Events.Tournament
{
    public sealed class TournamentBet
    {
        private readonly Character user;

        public TournamentBet(Character user, uint targetId, long initialAmount)
        {
            this.user = user;
            TargetId = targetId;
            Amount = initialAmount;
        }

        public uint Identity => user.Identity;
        public string Name => user.Name;

        public uint TargetId { get; init; }
        public long Amount { get; private set; }

        public void SetBet(long amount)
        {
            Amount += amount;
        }
    }
}
