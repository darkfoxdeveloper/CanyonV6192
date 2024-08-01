using Canyon.Game.Services.Managers;
using Canyon.Game.States;
using Canyon.Network.Packets.Ai;

namespace Canyon.Game.Sockets.Ai.Packets
{
    public sealed class MsgAiAction : MsgAiAction<AiClient>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MsgAiAction>();

        public override async Task ProcessAsync(AiClient client)
        {
            Role role = RoleManager.GetRole(Identity);
            if (role == null || role.Map == null)
            {
                return;
            }

            if (!role.HasGenerator)
            {
                logger.LogWarning($"{role.Identity} - {role.Name} AI Jump request not AI Generated");
                return;
            }

            switch (Action)
            {
                case AiActionType.Run:
                case AiActionType.Walk:
                    {
                        if (!role.IsAlive)
                        {
                            return;
                        }

                        role.QueueAction(() => role.MoveTowardAsync(Direction, (int)TargetIdentity, true));
                        break;
                    }

                case AiActionType.Jump:
                    {
                        if (!role.IsAlive)
                        {
                            return;
                        }

                        role.QueueAction(() => role.JumpPosAsync(X, Y, true));
                        break;
                    }

                default:
                    {
                        break;
                    }
            }
        }
    }
}
