using Canyon.Ai.Managers;
using Canyon.Ai.States;
using Canyon.Network.Packets.Ai;

namespace Canyon.Ai.Sockets.Packets
{
    public sealed class MsgAiRoleStatusFlag : MsgAiRoleStatusFlag<GameServer>
    {
        public override async Task ProcessAsync(GameServer client)
        {
            Role target = RoleManager.GetRole(Identity);
            if (target == null)
                return;

            Role sender = RoleManager.GetRole(Caster);
            if (Mode == 0)
                await target.AttachStatusAsync(sender, Flag, 0, Duration, Steps, 0);
            else
                await target.DetachStatusAsync(Flag);
        }
    }
}
