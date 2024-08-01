using Canyon.Ai.Managers;
using Canyon.Ai.States;
using Canyon.Ai.States.World;
using Canyon.Database.Entities;
using Canyon.Network.Packets.Ai;
using Canyon.Shared.Managers;
using Newtonsoft.Json;

namespace Canyon.Ai.Sockets.Packets
{
    public sealed class MsgAiSpawnNpc : MsgAiSpawnNpc<GameServer>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MsgAiSpawnNpc>();

        public override async Task ProcessAsync(GameServer client)
        {
            switch (Mode)
            {
                case AiSpawnNpcMode.Spawn:
                    {
                        // Probably a Guard or something spawned by an action (Must have an existing generator)
                        foreach (SpawnNpc target in List)
                        {
                            DbMonstertype monstertype = RoleManager.GetMonstertype(target.MonsterType);
                            if (monstertype == null)
                            {
                                logger.LogWarning($"Could not create monster for type {target.MonsterType}");
                                continue;
                            }

                            GameMap map = MapManager.GetMap(target.MapId);
                            if (map == null)
                            {
                                logger.LogWarning($"Could not create monster for map {target.MapId}");
                                continue;
                            }

                            Generator generator = GeneratorManager.GetGenerator(target.GeneratorId);
                            if (generator == null)
                            {
                                logger.LogWarning($"Could not create monster for generator (no gen) {target.GeneratorId}");
                                continue;
                            }

                            var monster = new Monster(monstertype, (uint)IdentityManager.Monster.GetNextIdentity,
                                                      generator);
                            if (!await monster.InitializeAsync(target.MapId, target.X, target.Y))
                            {
                                logger.LogWarning($"ExecuteActionEventCreatepet could not initialize monster: {JsonConvert.SerializeObject(target)}");
                                IdentityManager.Monster.ReturnIdentity(monster.Identity);
                                continue;
                            }

                            RoleManager.AddRole(monster);
                            generator.Add(monster);
                            await monster.EnterMapAsync();
                        }

                        break;
                    }

                case AiSpawnNpcMode.DestroyNpc:
                    {
                        // Seeks and remove an NPC. Request by Game Server, does not need to reply. Already remove server side
                        foreach (SpawnNpc starget in List)
                        {
                            Role target = RoleManager.GetRole(starget.Id);
                            if (target == null)
                                continue;

                            await target.LeaveMapAsync();
                            RoleManager.RemoveRole(target.Identity);
                        }

                        break;
                    }

                case AiSpawnNpcMode.DestroyGenerator:
                    {
                        // Seeks and destroy an generator. Does not remove the generator from the pool only clears the monsters
                        break;
                    }
            }
        }
    }
}
