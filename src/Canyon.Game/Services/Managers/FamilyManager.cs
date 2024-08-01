using Canyon.Database.Entities;
using Canyon.Game.Database.Repositories;
using Canyon.Game.States.Families;
using System.Collections.Concurrent;

namespace Canyon.Game.Services.Managers
{
    public static class FamilyManager
    {
        private static readonly ConcurrentDictionary<uint, Family> families = new();
        private static readonly ConcurrentDictionary<uint, DbFamilyBattleEffectShareLimit> familyBattlePowerLimit = new();

        public static async Task<bool> InitializeAsync()
        {
            List<DbFamily> dbFamilies = await FamilyRepository.GetAsync();
            foreach (DbFamily dbFamily in dbFamilies)
            {
                var family = await Family.CreateAsync(dbFamily);
                if (family != null)
                {
                    families.TryAdd(family.Identity, family);
                }
            }

            foreach (Family family in families.Values)
            {
                family.LoadRelations();
            }

            foreach (DbFamilyBattleEffectShareLimit limit in await FamilyBattleEffectShareLimitRepository.GetAsync())
            {
                if (!familyBattlePowerLimit.ContainsKey(limit.Identity))
                {
                    familyBattlePowerLimit.TryAdd(limit.Identity, limit);
                }
            }

            return true;
        }

        public static bool AddFamily(Family family)
        {
            return families.TryAdd(family.Identity, family);
        }

        public static Family GetFamily(uint idFamily)
        {
            return families.TryGetValue((ushort)idFamily, out Family family) ? family : null;
        }

        public static Family GetFamily(string name)
        {
            return families.Values.FirstOrDefault(
                x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }

        public static Family GetOccupyOwner(uint idNpc)
        {
            return families.Values.FirstOrDefault(x => x.Occupy == idNpc);
        }

        /// <summary>
        ///     Find the family a user is in.
        /// </summary>
        public static Family FindByUser(uint idUser)
        {
            return families.Values.FirstOrDefault(x => x.GetMember(idUser) != null);
        }

        public static List<Family> QueryFamilies(Func<Family, bool> predicate)
        {
            return families.Values.Where(predicate).ToList();
        }

        public static DbFamilyBattleEffectShareLimit GetSharedBattlePowerLimit(int level)
        {
            return familyBattlePowerLimit.Values.FirstOrDefault(x => x.Identity == level);
        }
    }
}
