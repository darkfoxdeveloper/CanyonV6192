using Canyon.Database.Entities;

namespace Canyon.Game.States
{
    public class FateData
    {
        private readonly DbFatePlayer fate;

        public FateData(DbFatePlayer fate)
        {
            if (fate == null)
            {
                throw new ArgumentNullException(nameof(fate));
            }
            this.fate = fate;
        }

        public uint Identity => fate?.PlayerId ?? 0;
        public uint Lookface { get; set; }
        public string Name { get; set; }
        public string Mate { get; set; }
        public int Level { get; set; }
        public int Profession { get; set; }
        public int PreviousProfession { get; set; }
        public int FirstProfession { get; set; }

        public DbFatePlayer GetDatabase() => this;

        public static implicit operator DbFatePlayer(FateData fate)
        {
            return fate?.fate;
        }

        public override string ToString()
        {
            return $"{Identity} {Name}";
        }
    }
}
