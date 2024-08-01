using Canyon.Game.Database;
using Canyon.Game.Services.Managers;
using Canyon.Game.States.Items;
using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgFlower : MsgBase<Client>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MsgFlower>();

        public RequestMode Mode { get; set; }
        public uint Identity { get; set; }
        public uint ItemIdentity { get; set; }
        public uint FlowerIdentity { get; set; }
        public string SenderName { get; set; } = "";
        public uint Amount { get; set; }
        public FlowerType Flower { get; set; }
        public string ReceiverName { get; set; } = "";
        public uint SendAmount { get; set; }
        public FlowerType SendFlowerType { get; set; }
        public FlowerEffect SendFlowerEffect { get; set; }

        public uint RedRoses { get; set; }
        public uint RedRosesToday { get; set; }
        public uint WhiteRoses { get; set; }
        public uint WhiteRosesToday { get; set; }
        public uint Orchids { get; set; }
        public uint OrchidsToday { get; set; }
        public uint Tulips { get; set; }
        public uint TulipsToday { get; set; }

        public List<string> Strings { get; set; } = new();

        public override void Decode(byte[] bytes)
        {
            using var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();              // 0
            Type = (PacketType)reader.ReadUInt16();   // 2
            Mode = (RequestMode)reader.ReadUInt32();  // 4
            Identity = reader.ReadUInt32();            // 8
            ItemIdentity = reader.ReadUInt32();        // 12
            FlowerIdentity = reader.ReadUInt32();      // 16
            Amount = reader.ReadUInt32();              // 20
            Flower = (FlowerType)reader.ReadUInt32(); // 24
            Strings = reader.ReadStrings();
        }

        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgFlower);
            writer.Write((uint)Mode);
            writer.Write(Identity);
            writer.Write(ItemIdentity);
            if (Mode == RequestMode.QueryFlower
                || Mode == RequestMode.QueryGift)
            {
                writer.Write(RedRoses);
                writer.Write(RedRosesToday);
                writer.Write(WhiteRoses);
                writer.Write(WhiteRosesToday);
                writer.Write(Orchids);
                writer.Write(OrchidsToday);
                writer.Write(Tulips);
                writer.Write(TulipsToday);
            }
            else
            {
                writer.Write(SenderName, 16);
                writer.Write(ReceiverName, 16);
            }

            writer.Write(SendAmount);
            writer.Write((uint)SendFlowerType);
            writer.Write((uint)SendFlowerEffect);
            return writer.ToArray();
        }

        public enum FlowerEffect : uint
        {
            None = 0,

            RedRose,
            WhiteRose,
            Orchid,
            Tulip,

            Kiss = RedRose,
            Love = WhiteRose,
            Tins = Orchid,
            Jade = Tulip
        }

        public enum FlowerType
        {
            RedRose,
            WhiteRose,
            Orchid,
            Tulip,

            Kiss,
            Love,
            Tins,
            Jade
        }

        public enum RequestMode
        {
            SendFlower,
            SendGift,
            QueryFlower,
            QueryGift
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            switch (Mode)
            {
                case RequestMode.SendGift:
                case RequestMode.SendFlower:
                    {
                        uint idTarget = Identity;

                        Character target = RoleManager.GetUser(idTarget);

                        if (!user.IsAlive)
                        {
                            await user.SendAsync(StrFlowerSenderNotAlive);
                            return;
                        }

                        if (target == null)
                        {
                            await user.SendAsync(StrTargetNotOnline);
                            return;
                        }

                        if (user.Gender == target.Gender)
                        {
                            await user.SendAsync(StrGiftSameGender);
                            return;
                        }

                        if (user.Level < 50)
                        {
                            await user.SendAsync(StrFlowerLevelTooLow);
                            return;
                        }

                        string giftName = StrFlowerNameRed;
                        var type = SendFlowerType;
                        var effect = FlowerEffect.RedRose;

                        if (user.Gender == 2)
                        {
                            type += 4;
                        }

                        ushort amount;
                        if (ItemIdentity == 0) // daily flower
                        {
                            if (user.SendFlowerTime != 0
                                && user.SendFlowerTime >= int.Parse(DateTime.Now.ToString("yyyyMMdd")))
                            {
                                await user.SendAsync(StrFlowerHaveSentToday);
                                return;
                            }

                            switch (user.BaseVipLevel)
                            {
                                case 0:
                                    amount = 1;
                                    break;
                                case 1:
                                    amount = 2;
                                    break;
                                case 2:
                                    amount = 5;
                                    break;
                                case 3:
                                    amount = 7;
                                    break;
                                case 4:
                                    amount = 9;
                                    break;
                                case 5:
                                    amount = 12;
                                    break;
                                default:
                                    amount = 30;
                                    break;
                            }

                            user.SendFlowerTime = uint.Parse(DateTime.Now.ToString("yyyyMMdd"));
                            await user.SaveAsync();
                        }
                        else
                        {
                            Item flower = user.UserPackage[ItemIdentity];
                            if (flower == null)
                            {
                                return;
                            }

                            switch (flower.GetItemSubType())
                            {
                                case 751:
                                    type = FlowerType.RedRose;
                                    effect = FlowerEffect.RedRose;
                                    giftName = StrFlowerNameRed;
                                    break;
                                case 752:
                                    type = FlowerType.WhiteRose;
                                    effect = FlowerEffect.WhiteRose;
                                    giftName = StrFlowerNameWhite;
                                    break;
                                case 753:
                                    type = FlowerType.Orchid;
                                    effect = FlowerEffect.Orchid;
                                    giftName = StrFlowerNameLily;
                                    break;
                                case 754:
                                    type = FlowerType.Tulip;
                                    effect = FlowerEffect.Tulip;
                                    giftName = StrFlowerNameTulip;
                                    break;
                                case 755:
                                    type = FlowerType.Kiss;
                                    effect = FlowerEffect.Kiss;
                                    giftName = StrGiftKisses;
                                    break;
                                case 756:
                                    type = FlowerType.Love;
                                    effect = FlowerEffect.Love;
                                    giftName = StrGiftLoveLetters;
                                    break;
                                case 757:
                                    type = FlowerType.Tins;
                                    effect = FlowerEffect.Tins;
                                    giftName = StrGiftTinsOfBeer;
                                    break;
                                case 758:
                                    type = FlowerType.Jade;
                                    effect = FlowerEffect.Jade;
                                    giftName = StrGiftJades;
                                    break;
                            }

                            amount = flower.Durability;
                            await user.UserPackage.SpendItemAsync(flower);
                        }

                        FlowerManager.FlowerRankObject flowersToday = await FlowerManager.QueryFlowersAsync(target);
                        switch (type)
                        {
                            case FlowerType.RedRose:
                            case FlowerType.Kiss:
                                target.FlowerRed += amount;
                                flowersToday.RedRose += amount;
                                flowersToday.RedRoseToday += amount;
                                break;
                            case FlowerType.WhiteRose:
                            case FlowerType.Love:
                                target.FlowerWhite += amount;
                                flowersToday.WhiteRose += amount;
                                flowersToday.WhiteRoseToday += amount;
                                break;
                            case FlowerType.Orchid:
                            case FlowerType.Tins:
                                target.FlowerOrchid += amount;
                                flowersToday.Orchids += amount;
                                flowersToday.OrchidsToday += amount;
                                break;
                            case FlowerType.Tulip:
                            case FlowerType.Jade:
                                target.FlowerTulip += amount;
                                flowersToday.Tulips += amount;
                                flowersToday.TulipsToday += amount;
                                break;
                        }

                        if (user.Gender == 2)
                        {
                            await user.SendAsync(StrFlowerSendSuccess);
                        }
                        else
                        {
                            await user.SendAsync(StrGiftSendSuccess);
                        }

                        if (ItemIdentity != 0 && amount >= 99)
                        {
                            await BroadcastWorldMsgAsync(
                                string.Format(StrFlowerGmPromptAll, user.Name, amount, giftName, target.Name),
                                TalkChannel.Center);
                        }

                        await target.SendAsync(string.Format(StrFlowerReceiverPrompt, user.Name));
                        await user.BroadcastRoomMsgAsync(new MsgFlower
                        {
                            Identity = Identity,
                            ItemIdentity = ItemIdentity,
                            SenderName = user.Name,
                            ReceiverName = target.Name,
                            SendAmount = amount,
                            SendFlowerType = type,
                            SendFlowerEffect = effect
                        }, true);

                        var msg = new MsgFlower
                        {
                            SenderName = user.Name,
                            ReceiverName = target.Name,
                            SendAmount = amount
                        };
                        await user.SendAsync(msg);

                        await ServerDbContext.SaveAsync(flowersToday.GetDatabaseObject());
                        await user.UpdateTaskActivityAsync(ActivityManager.ActivityType.FlowerGifts);
                        break;
                    }
                default:
                    {
                        logger.LogWarning($"Unhandled MsgFlower:{Mode}");
                        return;
                    }
            }
        }
    }
}
