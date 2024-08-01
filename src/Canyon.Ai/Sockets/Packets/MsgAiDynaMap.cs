using Canyon.Ai.Managers;
using Canyon.Ai.States.World;
using Canyon.Database.Entities;
using Canyon.Network.Packets.Ai;

namespace Canyon.Ai.Sockets.Packets
{
    public sealed class MsgAiDynaMap : MsgAiDynaMap<GameServer>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MsgAiDynaMap>();

        public override async Task ProcessAsync(GameServer client)
        {
            if (Mode == 0) // add
            {
                var dynaMap = new DbDynamap
                {
                    Identity = Identity,
                    Name = Name,
                    Description = Description,
                    Type = MapType,
                    LinkMap = LinkMap,
                    LinkX = LinkX,
                    LinkY = LinkY,
                    MapDoc = MapDoc,
                    MapGroup = MapGroup,
                    OwnerIdentity = OwnerIdentity,
                    OwnerType = OwnerType,
                    PortalX = PortalX,
                    PortalY = PortalY,
                    RebornMap = RebornMap,
                    RebornPortal = RebornPortal,
                    ResourceLevel = ResourceLevel,
                    ServerIndex = ServerIndex,
                    Weather = Weather,
                    BackgroundMusic = BackgroundMusic,
                    BackgroundMusicShow = BackgroundMusicShow,
                    Color = Color
                };

                var map = new GameMap(dynaMap);
                if (!await map.InitializeAsync())
                    return;

                MapManager.AddMap(map);

                if (map.IsInstanceMap)
                {
                    var generators = GeneratorManager.GetByMapId(InstanceMapId);
                    foreach (var generator in generators)
                    {
                        await GeneratorManager.AddGeneratorAsync(new Generator(Identity, generator.Npctype, generator.BoundX, generator.BoundY, generator.BoundCx, generator.BoundCy));
                    }
                }

#if DEBUG
                logger.LogDebug($"Map {map.Identity} {map.Name} {Description} has been added to the pool.");
#endif
            }
            else
            {
                GameMap map = MapManager.GetMap(Identity);
                if (map != null)
                {
                    if (map.IsInstanceMap)
                    {
                        var generators = GeneratorManager.GetGenerators(map.Identity);
                        foreach (var generator in generators)
                        {
                            GeneratorManager.RemoveGenerator(generator.Identity);
                        }
                    }
                    MapManager.RemoveMap(Identity);
                }               

#if DEBUG
                logger.LogDebug($"Map {Identity} has been removed from the pool.");
#endif
            }
        }
    }
}
