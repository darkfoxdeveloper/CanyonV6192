using Canyon.Database.Entities;

namespace Canyon.Game.Database.Repositories
{
    public static class WeaponSkillRepository
    {
        public static async Task<List<DbWeaponSkill>> GetAsync(uint idUser)
        {
            await using var db = new ServerDbContext();
            return db.WeaponSkills.Where(x => x.OwnerIdentity == idUser).ToList();
        }
    }
}
