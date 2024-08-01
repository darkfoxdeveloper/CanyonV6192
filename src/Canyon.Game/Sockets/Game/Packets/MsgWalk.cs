using Canyon.Game.Services.Managers;
using Canyon.Game.Sockets.Ai.Packets;
using Canyon.Game.States;
using Canyon.Game.States.User;
using Canyon.Network.Packets;
using Canyon.Network.Packets.Ai;
using ProtoBuf;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgWalk : MsgProtoBufBase<Client, MsgWalk.WalkData>
    {
        public MsgWalk()
            : base(PacketType.MsgWalk)
        {
        }

        [ProtoContract]
        public struct WalkData
        {
            [ProtoMember(1, IsRequired = true)]
            public uint Direction { get; set; }
            [ProtoMember(2, IsRequired = true)]
            public uint Identity { get; set; }
            [ProtoMember(3, IsRequired = true)]
            public uint Mode { get; set; }
            [ProtoMember(4, IsRequired = true)]
            public uint Timestamp { get; set; }
            [ProtoMember(5, IsRequired = true)]
            public uint Map { get; set; }
        }

        public enum RoleMoveMode
        {
            Walk = 0,

            // PathMove()
            Run,
            Shift,

            // to server only
            Jump,
            Trans,
            Chgmap,
            JumpMagicAttack,
            Collide,
            Synchro,

            // to server only
            Track,

            RunDir0 = 20,

            RunDir7 = 27
        }

        /// <summary>
        ///     Process can be invoked by a packet after decode has been called to structure
        ///     packet fields and properties. For the server implementations, this is called
        ///     in the packet handler after the message has been dequeued from the server's
        ///     <see cref="PacketProcessor{TClient}" />.
        /// </summary>
        /// <param name="client">Client requesting packet processing</param>
        public override async Task ProcessAsync(Client client)
        {
            if (client != null && Data.Identity == client.Character.Identity)
            {
                Character user = client.Character;
                await user.ProcessOnMoveAsync();

                bool moved = await user.MoveTowardAsync((int)Data.Direction, (int)Data.Mode);
                Character couple;
                if (moved
                    && user.HasCoupleInteraction()
                    && user.HasCoupleInteractionStarted()
                    && (couple = user.GetCoupleInteractionTarget()) != null)
                {
                    await couple.ProcessOnMoveAsync();

                    couple.X = user.X;
                    couple.Y = user.Y;

                    await couple.ProcessAfterMoveAsync();

                    MsgSyncAction msg = new()
                    {
                        Action = SyncAction.Walk,
                        X = user.X,
                        Y = user.Y
                    };
                    msg.Targets.Add(user.Identity);
                    msg.Targets.Add(couple.Identity);

                    await user.SendAsync(this);
                    await user.ProcessAfterMoveAsync();
                    await BroadcastNpcMsgAsync(new MsgAiAction
                    {
                        Action = AiActionType.Walk,
                        Identity = user.Identity,
                        X = user.X,
                        Y = user.Y,
                        Direction = (int)user.Direction
                    });

                    //Data.Identity = couple.Identity;
                    Data = new WalkData
                    {
                        Identity = couple.Identity,
                        Direction = Data.Direction,
                        Mode = Data.Mode,
                        Timestamp = Data.Timestamp
                    };
                    await couple.SendAsync(this);
                    await couple.ProcessAfterMoveAsync();
                    await BroadcastNpcMsgAsync(new MsgAiAction
                    {
                        Action = AiActionType.Jump,
                        Identity = couple.Identity,
                        X = couple.X,
                        Y = couple.Y,
                        Direction = (int)couple.Direction
                    });

                    await user.SendAsync(msg);
                    await user.Screen.UpdateAsync(msg);
                    await couple.Screen.UpdateAsync();
                }
                else if (moved)
                {
                    await user.SendAsync(this);
                    await user.Screen.UpdateAsync(this);
                    await user.ProcessAfterMoveAsync();
                    await BroadcastNpcMsgAsync(new MsgAiAction
                    {
                        Action = AiActionType.Jump,
                        Identity = user.Identity,
                        X = user.X,
                        Y = user.Y,
                        Direction = (int)user.Direction
                    });
                }
                return;
            }

            Role target = RoleManager.GetRole(Data.Identity);
            if (target == null)
            {
                return;
            }

            await target.ProcessOnMoveAsync();
            await target.MoveTowardAsync((int)Data.Direction, (int)Data.Mode);
            if (target is Character targetUser)
            {
                await targetUser.Screen.UpdateAsync(this);
            }
            else
            {
                await target.BroadcastRoomMsgAsync(this, false);
            }
            await target.ProcessAfterMoveAsync();
            await BroadcastNpcMsgAsync(new MsgAiAction
            {
                Action = AiActionType.Jump,
                Identity = target.Identity,
                X = target.X,
                Y = target.Y,
                Direction = (int)target.Direction
            });
        }
    }
}
