using Canyon.Game.States.User;
using Canyon.Game.States.World;

namespace Canyon.Game.States.Events
{
    public sealed class GuildContest : SyndicateGameEvent
    {
        public GuildContest() 
            : base("Guild Contest")
        {
        }

        public override EventType Identity => EventType.GuildContest;

        public override Task<bool> CreateAsync()
        {
            return base.CreateAsync();
        }

        public override bool IsAllowedToJoin(Role sender)
        {
            return base.IsAllowedToJoin(sender);
        }

        public override Task OnEnterMapAsync(Character sender)
        {
            return base.OnEnterMapAsync(sender);
        }

        public override Task OnExitMapAsync(Character sender, GameMap currentMap)
        {
            return base.OnExitMapAsync(sender, currentMap);
        }

        public override Task OnExitSyndicate(Character user, uint syndicateId)
        {
            return base.OnExitSyndicate(user, syndicateId);
        }

        public override Task OnTimerAsync()
        {
            return base.OnTimerAsync();
        }
    }
}
