using Canyon.Game.Services.Managers;
using Canyon.Game.States.Items;
using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgOwnKongfuBase : MsgBase<Client>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MsgOwnKongfuBase>();

        public enum KongfuBaseMode : byte
        {
            IconBar = 0,
            SetName = 1,
            UpdateTalent = 5,
            SendStatus = 7,
            QueryTargetInfo = 9,
            RestoreStar = 10,
            UpdateStar = 11,
            OpenStage = 12,
            UpdateTime = 13,
            SendInfo = 14,
            ProtectionPillUsage = 16,
            GatherTalentPoints = 17
        }

        public KongfuBaseMode Mode { get; set; }
        public List<string> Strings { get; set; } = new List<string>();

        public override void Decode(byte[] bytes)
        {
            using PacketReader reader = new(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Mode = (KongfuBaseMode)reader.ReadByte();
            Strings = reader.ReadStrings();
        }

        public override byte[] Encode()
        {
            using PacketWriter writer = new();
            writer.Write((ushort)PacketType.MsgOwnKongfuBase);
            writer.Write((byte)Mode);
            writer.Write(Strings);
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            switch (Mode)
            {
                case KongfuBaseMode.SetName:
                    {
                        if (Strings.Count < 1)
                        {
                            logger.LogError("Set name called without strings!! {}", user.Name);
                            return;
                        }

                        await user.JiangHu.CreateAsync(Strings[0]);
                        break;
                    }

                case KongfuBaseMode.UpdateTime:
                    {
                        await user.JiangHu.SendTimeAsync();
                        break;
                    }

                case KongfuBaseMode.QueryTargetInfo:
                    {
                        if (uint.TryParse(Strings[0], out var idUser))
                        {
                            Character target = RoleManager.GetUser(idUser);
                            if (target != null)
                            {
                                await target.JiangHu.SendInfoAsync(user);
                                await target.JiangHu.SendStarsAsync(user);
                                await target.JiangHu.SendStarAsync(user);
                                await target.JiangHu.SendTimeAsync(user);
                            }
                        }
                        break;
                    }

                case KongfuBaseMode.RestoreStar:
                    {
                        if (Strings.Count < 2)
                        {
                            return;
                        }

                        if (!byte.TryParse(Strings[0], out var powerLevel) || !byte.TryParse(Strings[1], out var star))
                        {
                            return;
                        }

                        if (!await user.SpendConquerPointsAsync(20, true, true))
                        {
                            return;
                        }

                        await user.JiangHu.RestoreAsync(powerLevel, star);
                        break;
                    }

                case KongfuBaseMode.ProtectionPillUsage:
                    {
                        if (Strings.Count < 2)
                        {
                            return;
                        }

                        if (!byte.TryParse(Strings[0], out var powerLevel) || !byte.TryParse(Strings[1], out var star))
                        {
                            return;
                        }

                        if (!await user.UserPackage.SpendItemAsync(Item.PROTECTION_PILL) 
                            && !await user.UserPackage.SpendItemAsync(Item.SUPER_PROTECTION_PILL))
                        {
                            return;
                        }

                        await user.JiangHu.RestoreAsync(powerLevel, star);
                        break;
                    }

                case KongfuBaseMode.GatherTalentPoints:
                    {
                        if (user.JiangHu?.HasJiangHu != true)
                        {
                            return;
                        }

                        // Talent 1 = 3 CPs
                        if (!await user.SpendConquerPointsAsync(3, true, true))
                        {
                            return;
                        }

                        await user.JiangHu.AwardTalentAsync(1);
                        break;
                    }
            }
        }
    }
}
