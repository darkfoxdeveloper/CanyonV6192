using Canyon.Ai.Database;
using Canyon.Ai.Managers;
using Canyon.Ai.States.World;
using Canyon.Database.Entities;
using Canyon.Network.Packets.Ai;

namespace Canyon.Ai.Sockets.Packets
{
    public sealed class MsgAiGeneratorManage : MsgAiGeneratorManage<GameServer>
    {
        public override async Task ProcessAsync(GameServer client)
        {
            var dbGen = new DbGenerator
            {
                Mapid = MapId,
                BoundX = BoundX,
                BoundY = BoundY,
                BoundCx = BoundCx,
                BoundCy = BoundCy,
                MaxNpc = MaxNpc,
                MaxPerGen = MaxPerGen,
                Npctype = Npctype,
                RestSecs = RestSecs,
                TimerBegin = TimerBegin,
                TimerEnd = TimerEnd,
                BornX = BornX,
                BornY = BornY,
            };

            if (await ServerDbContext.SaveAsync(dbGen))
            {
                Generator generator = new Generator(dbGen);
                if (generator.Ready)
                {
                    await GeneratorManager.AddGeneratorAsync(generator);
                }
            }
        }
    }
}
