using Canyon.Game.States;
using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgTrainingVitalityProtect : MsgBase<Client>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MsgTrainingVitalityProtect>();

        public enum TrainingVitalityProtectMode : ushort
        {
            QueryInfo,
            RequestRetreat,
            Retreat,
            RequestRestore,
            Restore,
            RequestExtend,
            Extend,
            RequestPayoff,
            Payoff,
            RequestAbandon,
            Abandon,
            RequestUpdate,
            Update,
            RequestUpdate2,
            Update2
        }

        public TrainingVitalityProtectMode Mode { get; set; }
        public Fate.FateType FateType { get; set; }

        public override void Decode(byte[] bytes)
        {
            using var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Mode = (TrainingVitalityProtectMode)reader.ReadInt16();
            FateType = (Fate.FateType)reader.ReadByte();
        }

        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgTrainingVitalityProtect);
            writer.Write((ushort)Mode);
            writer.Write((byte)FateType);
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            const int retreatCost = 4000;

            Character user = client.Character;
            switch (Mode)
            {
                case TrainingVitalityProtectMode.QueryInfo:
                    {
                        await user.Fate.SendProtectInfoAsync();
                        break;

                    }
                case TrainingVitalityProtectMode.RequestRetreat:
                    {
                        if (user.ChiPoints < retreatCost)
                        {
                            return;
                        }

                        if (await user.Fate.ProtectAsync(FateType, false))
                        {
                            await user.SpendStrengthValueAsync(retreatCost);
                            Mode = TrainingVitalityProtectMode.Retreat;
                            await user.SendAsync(this);
                        }
                        break;
                    }

                case TrainingVitalityProtectMode.RequestRestore:
                    {
                        if (await user.Fate.RestoreProtectionAsync(FateType))
                        {
                            Mode = TrainingVitalityProtectMode.Restore;
                            await user.SendAsync(this);
                        }
                        break;
                    }

                case TrainingVitalityProtectMode.RequestUpdate:
                    {
                        if (user.Fate.IsValidProtection(FateType))
                        {
                            await user.Fate.ProtectAsync(FateType, true);
                            Mode = TrainingVitalityProtectMode.Update;
                            await user.SendAsync(this);
                        }
                        break;
                    }

                case TrainingVitalityProtectMode.RequestUpdate2:
                    {
                        if (user.ChiPoints < retreatCost)
                        {
                            return;
                        }

                        if (await user.Fate.ExtendAsync(FateType))
                        {
                            await user.SpendStrengthValueAsync(retreatCost);
                            Mode = TrainingVitalityProtectMode.Update2;
                            await user.SendAsync(this);
                        }
                        break;
                    }

                case TrainingVitalityProtectMode.RequestExtend:
                    {
                        int extendCost = user.Fate.GetRestorationCost(FateType);
                        if (extendCost < 1 || user.ChiPoints < extendCost)
                        {
                            return;
                        }

                        if (await user.Fate.ExtendAsync(FateType))
                        {
                            await user.SpendStrengthValueAsync(extendCost);
                            Mode = TrainingVitalityProtectMode.Extend;
                            await user.SendAsync(this);
                        }
                        break;
                    }

                case TrainingVitalityProtectMode.RequestPayoff:
                    {
                        if (user.ChiPoints < retreatCost)
                        {
                            return;
                        }

                        if (await user.Fate.RestoreProtectionAsync(FateType) && await user.Fate.ExtendAsync(FateType))
                        {
                            await user.SpendStrengthValueAsync(retreatCost);
                            Mode = TrainingVitalityProtectMode.Payoff;
                            await user.SendAsync(this);
                        }
                        break;
                    }

                case TrainingVitalityProtectMode.RequestAbandon:
                    {
                        if (await user.Fate.AbandonAsync(FateType))
                        {
                            Mode = TrainingVitalityProtectMode.Abandon;
                            await user.SendAsync(this);
                        }
                        break;
                    }
                    
                default:
                    {
                        logger.LogWarning($"Unhandled {Mode}\n{PacketDump.Hex(Encode())}");
                        break;
                    }
            }
        }
    }
}
