using Canyon.Ai.Managers;
using Canyon.Ai.States;
using Canyon.Network.Packets.Ai;
using static Canyon.Ai.States.Role;

namespace Canyon.Ai.Sockets.Packets
{
    public sealed class MsgAiAction : MsgAiAction<GameServer>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MsgAiAction>();

        public override async Task ProcessAsync(GameServer client)
        {
            switch (Action)
            {
                case AiActionType.LeaveMap:
                    {
                        Role target = RoleManager.GetRole(Identity);
                        if (target?.Map == null)
                            return;

                        target.QueueAction(async () =>
                        {
#if DEBUG
                            logger.LogDebug($"Target '{target.Name}' LeaveMap {target.MapIdentity},{target.X},{target.Y}");
#endif
                            await target.LeaveMapAsync();
                        });
                        break;
                    }

                case AiActionType.FlyMap:
                    {
                        Role target = RoleManager.GetRole(Identity);
                        if (target == null) return;
                        target.QueueAction(async () =>
                        {
#if DEBUG
                            logger.LogDebug($"Target '{target.Name}' FlyMap {target.MapIdentity},{target.X},{target.Y}=>{TargetIdentity},{X},{Y}");
#endif
                            await target.LeaveMapAsync(); // redundant? needed probably
                            target.MapIdentity = TargetIdentity;
                            target.X = X;
                            target.Y = Y;
                            await target.EnterMapAsync();
                        });
                        break;
                    }

                case AiActionType.Walk:
                case AiActionType.Run:
                case AiActionType.Jump:
                case AiActionType.SynchroPosition:
                    {
                        Role target = RoleManager.GetRole(Identity);
                        if (target?.Map == null)
                            return;

                        target.QueueAction(() =>
                        {
                            if (Action == AiActionType.Walk
                                || Action == AiActionType.Run)
                            {
                                target.MoveToward(Direction, 0);
                            }
                            else
                            {
                                target.JumpPos(X, Y);
                            }

                            if (Action != AiActionType.SynchroPosition && target is Character user)
                            {
                                user.ClearProtection();
                            }
                            return Task.CompletedTask;
                        });
                        break;
                    }

                case AiActionType.SetProtection:
                    {
                        Role target = RoleManager.GetRole((uint)Identity);
                        if (target?.Map == null)
                            return;

                        if (target is Character user)
                        {
                            user.SetProtection();
                        }
                        break;
                    }

                case AiActionType.ClearProtection:
                    {
                        Role target = RoleManager.GetRole((uint)Identity);
                        if (target?.Map == null)
                            return;

                        if (target is Character user)
                        {
                            user.ClearProtection();
                        }
                        break;
                    }

                case AiActionType.Shutdown:
                    {
                        logger.LogInformation($"Closing server due to game server shutdown request!!!");
                        logger.LogInformation($"Closing server due to game server shutdown request!!!");
                        logger.LogInformation($"Closing server due to game server shutdown request!!!");

                        await Kernel.StopAsync();
                        Environment.Exit(0);
                        break;
                    }
            }
        }
    }
}
