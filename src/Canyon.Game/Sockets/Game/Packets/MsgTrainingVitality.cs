using Canyon.Game.Services.Managers;
using Canyon.Game.States;
using Canyon.Game.States.User;
using Canyon.Network.Packets;
using Org.BouncyCastle.Asn1.X509;
using static Canyon.Game.States.Fate;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgTrainingVitality : MsgBase<Client>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MsgTrainingVitality>();

        public uint Identity { get; set; }
        public TrainingAction Action { get; set; }
        public byte Mode { get; set; }
        public uint Param { get; set; }

        public override void Decode(byte[] bytes)
        {
            using PacketReader reader = new(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Identity = reader.ReadUInt32(); // 4
            Action = (TrainingAction)reader.ReadUInt16(); // 8
            Mode = reader.ReadByte(); // 10
            Param = reader.ReadUInt32(); // 
        }

        public override byte[] Encode()
        {
            using PacketWriter writer = new();
            writer.Write((ushort)PacketType.MsgTrainingVitality);
            writer.Write(Identity);
            writer.Write((ushort)Action);
            writer.Write(Mode);
            writer.Write(Param);
            return writer.ToArray();
        }

        public enum TrainingAction
        {
            TrainingtypeUnlock = 0,
            TrainingtypeQueryinfo = 1,
            TrainingtypeStudy = 2,
            TrainingtypeBuyStrength = 3,
            TrainingtypeBuyStrength2 = 6
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;
            switch (Action)
            {
                case TrainingAction.TrainingtypeUnlock:
                    {
                        await user.Fate.UnlockAsync((FateType)Mode);
                        return;
                    }
                case TrainingAction.TrainingtypeQueryinfo:
                    {
                        if (Identity == user.Identity)
                        {
                            await user.Fate.SendAsync(false);
                            return;
                        }

                        Character target = RoleManager.GetUser(Identity);
                        if (target == null)
                        {
                            return;
                        }

                        if (target.Fate == null)
                        {
                            return;
                        }

                        await target.Fate.SendAsync(false, user);
                        return;
                    }
                case TrainingAction.TrainingtypeStudy:
                    {
                        if (!user.IsUnlocked())
                        {
                            await user.SendSecondaryPasswordInterfaceAsync();
                        }

                        if (user.Fate == null)
                        {
                            return;
                        }

                        await user.Fate.GenerateAsync((FateType)Mode, (TrainingSave)Param);
                        return;
                    }
                case TrainingAction.TrainingtypeBuyStrength:
                    {
                        const int FILL_PRICE = 1000;
                        const int MAX_STRENGTH = 4000;
                        if (user.Strength >= MAX_STRENGTH)
                        {
                            return;
                        }

                        int deltaStrength = ((int)(MAX_STRENGTH - user.ChiPoints));
                        int deltaStrengthPrice = deltaStrength / 4;
                        if (!await user.SpendConquerPointsAsync(deltaStrengthPrice, true, true))
                        {
                            return;
                        }

                        await user.AwardStrengthValueAsync(deltaStrength);
                        await user.Fate.SendAsync(false);
                        break;
                    }
                case TrainingAction.TrainingtypeBuyStrength2:
                    {
                        // 100 CPs = 200 Points
                        if (!await user.SpendConquerPointsAsync(100, true, true))
                        {
                            return;
                        }

                        await user.AwardStrengthValueAsync(200);
                        await user.Fate.SendAsync(false);
                        break;
                    }
                default:
                    {
                        logger.LogWarning($"{Type}::{Action} unhandled");
                        break;
                    }
            }
        }
    }
}
