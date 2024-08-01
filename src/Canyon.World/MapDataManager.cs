using Canyon.Shared;
using Canyon.World.Map;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Canyon.World
{
    public class MapDataManager
    {
        private static readonly ILogger logger;

        static MapDataManager()
        {
            logger = LogFactory.CreateLogger<MapDataManager>();
        }

        public static async Task LoadDataAsync()
        {
            FileStream stream = File.OpenRead(Path.Combine(Environment.CurrentDirectory, "ini", "GameMap.dat"));
            var reader = new BinaryReader(stream);

            int mapDataCount = reader.ReadInt32();
            logger.LogDebug($"Loading {mapDataCount} maps...");

            for (var i = 0; i < mapDataCount; i++)
            {
                uint idMap = reader.ReadUInt32();
                int length = reader.ReadInt32();
                var name = new string(reader.ReadChars(length));
                uint puzzle = reader.ReadUInt32();

                m_mapData.TryAdd(idMap, new MapData
                {
                    ID = idMap,
                    Length = length,
                    Name = name,
                    Puzzle = puzzle
                });
            }

            reader.Close();
            stream.Close();
            reader.Dispose();
            await stream.DisposeAsync();
        }

        public static GameMapData GetMapData(uint idDoc)
        {
            if (!m_mapData.TryGetValue(idDoc, out MapData value))
            {
                return null;
            }

            GameMapData mapData = new(idDoc);
            if (mapData.Load(value.Name.Replace("\\", Path.DirectorySeparatorChar.ToString())))
            {
                return mapData;
            }

            return null;
        }

        private struct MapData
        {
            public uint ID;
            public int Length;
            public string Name;
            public uint Puzzle;
        }

        private static readonly ConcurrentDictionary<uint, MapData> m_mapData = new();
    }
}
