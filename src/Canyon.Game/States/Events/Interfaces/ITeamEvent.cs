using Canyon.Game.States.User;

namespace Canyon.Game.States.Events.Interfaces
{
    public interface ITeamEvent
    {
        bool AllowJoinTeam(Character leader, Character target);
        Task<bool> OnJoinTeamAsync(Character leader, Character target);
        Task<bool> OnLeaveTeamAsync(Character target);
    }
}
