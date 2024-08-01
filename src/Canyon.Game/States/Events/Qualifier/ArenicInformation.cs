using Canyon.Database.Entities;
using Canyon.Game.Database;
using Canyon.Game.Database.Repositories;
using Canyon.Game.States.User;
using System.Globalization;

namespace Canyon.Game.States.Events.Qualifier
{
    public sealed class ArenicInformation
    {
        private readonly DbArenic arenic;

        public ArenicInformation(DbArenic arenic)
        {
            this.arenic = arenic;
        }

        public ArenicInformation(Character user, byte type)
        {
            arenic = new DbArenic
            {
                UserId = user.Identity,
                Date = uint.Parse(DateTime.Now.ToString("yyyyMMdd")),
                Type = type
            };

            Name = user.Name;
            Mesh = user.Mesh;
            Level = user.Level;
            Profession = user.Profession;
        }

        public uint UserId => arenic.UserId;

        public string Name { get; private set; }
        public uint Mesh { get; private set; }
        public byte Level { get; private set; }
        public byte Profession { get; private set; }

        public DateTime Date => DateTime.ParseExact(arenic.Date.ToString(), "yyyyMMdd", CultureInfo.InvariantCulture);

        public uint AthletePoint 
        {
            get => arenic.AthletePoint; 
            set => arenic.AthletePoint = value;
        }

        public uint CurrentHonor 
        {
            get => arenic.CurrentHonor;
            set => arenic.CurrentHonor = value;
        }

        public uint HistoryHonor 
        {
            get => arenic.HistoryHonor;
            set => arenic.HistoryHonor = value;
        }

        public uint DayWins 
        {
            get => arenic.DayWins;
            set => arenic.DayWins = value;
        }

        public uint DayLoses 
        {
            get => arenic.DayLoses; 
            set => arenic.DayLoses = value;
        }

        public async Task<bool> InitializeAsync()
        {
            DbCharacter user = await CharacterRepository.FindByIdentityAsync(UserId);
            if (user == null)
            {
                return false;
            }

            Name = user.Name;
            Mesh = user.Mesh;
            Level = user.Level;
            Profession = user.Profession;
            return true;
        }

        public Task<bool> SaveAsync()
        {
            return ServerDbContext.SaveAsync(this.arenic);
        }

        public Task<bool> DeleteAsync()
        {
            return ServerDbContext.DeleteAsync(this.arenic);
        }
    }
}
