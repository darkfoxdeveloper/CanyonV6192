using Canyon.Database.Entities;
using Canyon.Game.Database;
using Canyon.Game.Database.Repositories;
using Canyon.Game.Services.Managers;
using Canyon.Game.States.User;

namespace Canyon.Game.States.Families
{
    public sealed class FamilyMember
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<FamilyMember>();

        private DbFamilyAttr familyUserAttribute;

        private FamilyMember()
        {

        }

        #region Static Creation

        public static async Task<FamilyMember> CreateAsync(Character player, Family family, Family.FamilyRank rank = Family.FamilyRank.Member, uint proffer = 0)
        {
            if (player == null || family == null || rank == Family.FamilyRank.None)
            {
                return null;
            }

            DbFamilyAttr attr = new()
            {
                FamilyIdentity = family.Identity,
                UserIdentity = player.Identity,
                Proffer = proffer,
                AutoExercise = 0,
                ExpDate = 0,
                JoinDate = (uint)DateTime.Now.ToUnixTimestamp(),
                Rank = (byte)rank
            };
            if (!await ServerDbContext.CreateAsync(attr))
            {
                return null;
            }

            FamilyMember member = new()
            {
                familyUserAttribute = attr,
                Name = player.Name,
                MateIdentity = player.MateIdentity,
                Level = player.Level,
                LookFace = player.Mesh,
                Profession = player.Profession,

                FamilyIdentity = family.Identity,
                FamilyName = family.Name
            };
            if (!await member.SaveAsync())
            {
                return null;
            }

            logger.LogInformation($"[{player.Identity}],[{player.Name}],[{family.Identity}],[{family.Name}],[Join]");
            return member;
        }

        public static async Task<FamilyMember> CreateAsync(DbFamilyAttr player, Family family)
        {
            DbCharacter dbUser = await CharacterRepository.FindByIdentityAsync(player.UserIdentity);
            if (dbUser == null)
            {
                return null;
            }

            FamilyMember member = new()
            {
                familyUserAttribute = player,
                Name = dbUser.Name,
                MateIdentity = dbUser.Mate,
                Level = dbUser.Level,
                LookFace = dbUser.Mesh,
                Profession = dbUser.Profession,

                FamilyIdentity = family.Identity,
                FamilyName = family.Name
            };

            return member;
        }

        #endregion

        #region Properties

        public uint Identity => familyUserAttribute.UserIdentity;
        public string Name { get; private set; }
        public byte Level { get; private set; }
        public uint MateIdentity { get; private set; }
        public uint LookFace { get; private set; }
        public ushort Profession { get; private set; }

        public Family.FamilyRank Rank
        {
            get => (Family.FamilyRank)familyUserAttribute.Rank;
            set => familyUserAttribute.Rank = (byte)value;
        }

        public DateTime JoinDate => UnixTimestamp.ToDateTime(familyUserAttribute.JoinDate);

        public uint Proffer
        {
            get => familyUserAttribute.Proffer;
            set => familyUserAttribute.Proffer = value;
        }

        public Character User => RoleManager.GetUser(Identity);

        public bool IsOnline => User != null;

        public void ChangeName(string name)
        {
            Name = name;
        }

        #endregion

        #region Family Properties

        public uint FamilyIdentity { get; private set; }
        public string FamilyName { get; private set; }

        #endregion

        #region Database

        public Task<bool> SaveAsync()
        {
            return ServerDbContext.SaveAsync(familyUserAttribute);
        }


        public Task<bool> DeleteAsync()
        {
            return ServerDbContext.DeleteAsync(familyUserAttribute);
        }

        #endregion
    }
}
