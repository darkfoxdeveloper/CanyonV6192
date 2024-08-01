using Canyon.Database.Entities;
using Canyon.Game.Database.Repositories;
using Canyon.Game.States.World;
using Canyon.Shared.Managers;
using System.Collections.Concurrent;
using System.Text;

namespace Canyon.Game.Services.Managers
{
    public class MapManager
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MapManager>();
        private static TimeOut instanceCheckTimer = new();

        public static ConcurrentDictionary<uint, GameMap> GameMaps { get; } = new();

        public static async Task InitializeAsync()
        {
            List<DbMap> maps = await MapsRepository.GetAsync();
            foreach (DbMap dbmap in maps)
            {
                var map = new GameMap(dbmap);
                if (await map.InitializeAsync())
                {
#if DEBUG
                    logger.LogDebug($"Map[{map.Identity:000000}] MapDoc[{map.MapDoc:000000}] {map.Name,-32} Partition: {map.Partition:00} loaded...");
#endif
                    GameMaps.TryAdd(map.Identity, map);
                }
                else
                {
                    logger.LogError("Could not load map {Identity} {Name}", dbmap.Identity, dbmap.Name);
                }
            }

            List<DbDynamap> dynaMaps = await MapsRepository.GetDynaAsync();
            foreach (DbDynamap dbmap in dynaMaps)
            {
                var map = new GameMap(dbmap);
                if (await map.InitializeAsync())
                {
#if DEBUG
                    logger.LogDebug($"Dynamic Map [{map.Identity:0000000}] MapDoc[{map.MapDoc:000000}] {map.Name,-32} Partition: {map.Partition:00} loaded...");
#endif
                    GameMaps.TryAdd(map.Identity, map);
                }
            }

            foreach (var map in GameMaps.Values)
            {
                await map.LoadTrapsAsync();
            }

#if DEBUG
            const string partitionLogFile = "MapPartition";
            string path = Path.Combine(Environment.CurrentDirectory, $"{partitionLogFile}.log");
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            using StreamWriter writer = new(path, false, Encoding.UTF8);
            foreach (var map in GameMaps.Values.OrderBy(x => x.Partition).ThenBy(x => x.Identity))
            {
                writer.WriteLine($"{map.Identity:0000000},{map.Name:-32},{map.Partition}");
            }
            writer.Flush();
#endif
        }

        public static GameMap GetMap(uint idMap)
        {
            if (GameMaps.TryGetValue(idMap, out GameMap value))
            {
                return value;
            }
            if (idMap > 1_000_000)
            {
                DbDynamap dynaMap = MapsRepository.GetDynaMap(idMap);
                if (dynaMap == null)
                {
                    return null;
                }
                var map = new GameMap(dynaMap);
                if (map.InitializeAsync().GetAwaiter().GetResult())
                {
#if DEBUG
                    logger.LogDebug($"Map[{map.Identity:000000}] MapDoc[{map.MapDoc:000000}] {map.Name,-32} Partition: {map.Partition:00} loaded...");
#endif
                    GameMaps.TryAdd(map.Identity, map);
                    return map;
                }
                else
                {
                    logger.LogError("Could not load map {Identity} {Name}", dynaMap.Identity, dynaMap.Name);
                }
            }
            return null;
        }

        public static InstanceMap FindInstanceByUser(uint instanceId, uint userId)
        {
            return GameMaps.Values
                .Where(x => x is InstanceMap)
                .Cast<InstanceMap>()
                .FirstOrDefault(x => x.InstanceType == instanceId && x.OwnerIdentity == userId);
        }

        public static async Task<bool> AddMapAsync(GameMap map)
        {
            if (GameMaps.TryAdd(map.Identity, map))
            {
                await map.SendAddToNpcServerAsync();
                return true;
            }

            return false;
        }

        public static async Task<bool> RemoveMapAsync(uint idMap)
        {
            if (GameMaps.TryRemove(idMap, out GameMap map))
            {
                await map.SendRemoveToNpcServerAsync();
            }
            return true;
        }

        public static async Task OnTimerAsync()
        {
            if (instanceCheckTimer.ToNextTime(1))
            {
                foreach (var map in GameMaps.Values
                    .Where(x => x is InstanceMap)
                    .Cast<InstanceMap>())
                {
                    if (map.HasExpired && map.PlayerCount > 0)
                    {
                        await map.OnTimeOverAsync();
                    }
                    else if (map.HasExpired)
                    {
                        await RemoveMapAsync(map.Identity);
                        IdentityManager.Instances.ReturnIdentity(map.Identity);
                    }
                }
            }
        }
    }
}