using Canyon.Game.States;
using Canyon.Game.States.User;
using Canyon.Shared.Managers;
using System.Collections.Concurrent;

namespace Canyon.Game.Services.Managers
{
    public class TeamManager
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<TeamManager>();

        private static IdentityManager Identity = new(1_000_000_000, 1_000_100_000);
        private static ConcurrentDictionary<uint, Team> teams = new();

        public static Team Create(Character leader)
        {
            if (leader.Team != null)
                return null;

            Team team = new(leader);
            return team.Create((uint)Identity.GetNextIdentity) ? team : null;
        }

        public static void Disband(uint idTeam)
        {
            if (teams.TryRemove(idTeam, out _))
            {
                Identity.ReturnIdentity(idTeam);
            }
        }

        public static Team FindTeamById(uint idTeam)
        {
            return teams.TryGetValue(idTeam, out var team) ? team : null;
        }

        public static Team FindTeamByUserId(uint idUser)
        {
            foreach (var team in teams.Values)
            {
                if (team.IsMember(idUser))
                    return team;
            }
            return null;
        }
    }
}
