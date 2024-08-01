using Canyon.Game.Services.Managers;
using Canyon.Game.States.Events.Interfaces;
using Canyon.Game.States.User;

namespace Canyon.Game.States.Events.Elite
{
    public sealed class ElitePkParticipant : ITournamentEventParticipant<Character>
    {
        private readonly uint user;

        public ElitePkParticipant(Character user)
        {
            this.user = user.Identity;
            Name = user.Name;
            Lookface = user.Mesh;
        }

        public uint Identity => user;
        public string Name { get; }
        public uint Lookface { get; }

        public Character Participant
        {
            get
            {
                if (user == 0) // lets not spend resources looking for user
                    return null;
                return RoleManager.GetUser(user);
            }
        }

        public bool Bye => Participant == null;

        public override string ToString()
        {
            return $"[{Identity}] {Name}";
        }
    }
}
