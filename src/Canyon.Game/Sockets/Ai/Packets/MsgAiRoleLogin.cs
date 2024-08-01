using Canyon.Game.States;
using Canyon.Network.Packets.Ai;

namespace Canyon.Game.Sockets.Ai.Packets
{
    public sealed class MsgAiRoleLogin : MsgAiRoleLogin<AiClient>
    {
        public MsgAiRoleLogin()
        {
        }

        public MsgAiRoleLogin(Monster monster)
        {
            NpcType = monster.IsCallPet() ? RoleLoginNpcType.CallPet : RoleLoginNpcType.Monster;
            Generator = (int)(monster.IsCallPet() ? 0 : monster.GeneratorId);
            Identity = monster.Identity;
            Name = monster.Name;
            LookFace = (int)monster.Type;
            MapId = monster.MapIdentity;
            MapX = monster.X;
            MapY = monster.Y;
        }
    }
}
