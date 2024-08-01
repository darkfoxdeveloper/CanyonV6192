using Canyon.Database.Entities;
using Canyon.Game.Database.Repositories;
using System.Collections.Concurrent;

namespace Canyon.Game.Services.Managers
{
    public class MagicManager
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MagicManager>();
        private static readonly ConcurrentDictionary<uint, DbMagictype> magicTypes = new();

        public static async Task InitializeAsync()
        {
            logger.LogInformation("Initializing Magic Manager");

            foreach (DbMagictype magicType in await MagicTypeRepository.GetAsync())
            {
                magicTypes.TryAdd(magicType.Id, magicType);
            }
        }

        public static byte GetMaxLevel(uint idType)
        {
            return (byte)(magicTypes.Values.Where(x => x.Type == idType).OrderByDescending(x => x.Level)
                                      .FirstOrDefault()?.Level ?? 0);
        }

        public static DbMagictype GetMagictype(uint idType, ushort level)
        {
            return magicTypes.Values.FirstOrDefault(x => x.Type == idType && x.Level == level);
        }
    }
}
