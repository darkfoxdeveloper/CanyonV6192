using Canyon.Game.Services.Managers;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States;
using Canyon.Network.Packets.Ai;

namespace Canyon.Game.Sockets.Ai.Packets
{
    public sealed class MsgAiInteract : MsgAiInteract<AiClient>
    {
        public override async Task ProcessAsync(AiClient client)
        {
            Role attacker = RoleManager.GetRole(Identity);
            if (attacker == null || !attacker.IsAlive)
            {
                return;
            }

            switch (Action)
            {
                case AiInteractAction.Attack:
                    {
                        attacker.BattleSystem.CreateBattle(TargetIdentity);
                        break;
                    }

                case AiInteractAction.MagicAttack:
                    {
                        if (attacker is Monster monster && monster.SpeciesType != 0)
                        {
                            await attacker.BroadcastRoomMsgAsync(new MsgInteract
                            {
                                SenderIdentity = attacker.Identity,
                                MagicType = MagicType,
                                Action = MsgInteract.MsgInteractType.AnnounceAttack,
                                PosX = attacker.X,
                                PosY = attacker.Y
                            }, false);
                        }

                        await attacker.ProcessMagicAttackAsync(MagicType, TargetIdentity, (ushort) X, (ushort)Y);
                        break;
                    }

                case AiInteractAction.MagicAttackWarning:
                    {
                        await attacker.BroadcastRoomMsgAsync(new MsgInteract
                        {
                            SenderIdentity = attacker.Identity,
                            TargetIdentity = TargetIdentity,
                            MagicType = MagicType,
                            Action = MsgInteract.MsgInteractType.AnnounceAttack,
                            PosX = attacker.X,
                            PosY = attacker.Y
                        }, false);
                        break;
                    }
            }
        }
    }
}
