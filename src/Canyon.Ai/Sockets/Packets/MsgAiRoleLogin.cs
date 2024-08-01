using Canyon.Ai.Managers;
using Canyon.Ai.States;
using Canyon.Ai.States.World;
using Canyon.Database.Entities;
using Canyon.Network.Packets.Ai;

namespace Canyon.Ai.Sockets.Packets
{
    public sealed class MsgAiRoleLogin : MsgAiRoleLogin<GameServer>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MsgAiRoleLogin>();

        public override async Task ProcessAsync(GameServer client)
        {
            switch (NpcType)
            {
                case RoleLoginNpcType.Monster:
                    {
                        // must not use
                        break;
                    }

                case RoleLoginNpcType.CallPet:
                    {
                        DbMonstertype monsterType = RoleManager.GetMonstertype((uint)LookFace);
                        if (monsterType == null)
                        {
                            logger.LogWarning($"Could not create monster for type {LookFace}");
                            return;
                        }

                        GameMap map = MapManager.GetMap(MapId);
                        if (map == null)
                        {
                            logger.LogWarning($"Could not create monster for map {MapId}");
                            return;
                        }

                        Monster pet = new Monster(monsterType, Identity, new Generator(MapId, (uint)LookFace, MapX, MapY, 1, 1));
                        if (!await pet.InitializeAsync(MapId, MapX, MapY))
                        {
                            return;
                        }

                        await pet.EnterMapAsync(false);
                        RoleManager.AddRole(pet);
                        break;
                    }
            }
        }
    }
}
