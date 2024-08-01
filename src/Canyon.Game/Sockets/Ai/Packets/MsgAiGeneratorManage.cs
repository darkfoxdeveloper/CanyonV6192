using Canyon.Database.Entities;
using Canyon.Network.Packets.Ai;

namespace Canyon.Game.Sockets.Ai.Packets
{
    public sealed class MsgAiGeneratorManage : MsgAiGeneratorManage<AiClient>
    {
        public MsgAiGeneratorManage(DbGenerator gen)
        {
            MapId = gen.Mapid;
            BoundX = gen.BoundX;
            BoundY = gen.BoundY;
            BoundCx = gen.BoundCx;
            BoundCy = gen.BoundCy;
            MaxNpc = gen.MaxNpc;
            RestSecs = gen.RestSecs;
            MaxPerGen = gen.MaxPerGen;
            Npctype = gen.Npctype;
            TimerBegin = gen.TimerBegin;
            TimerEnd = gen.TimerEnd;
            BornX = gen.BornX;
            BornY = gen.BornY;
        }
    }
}
