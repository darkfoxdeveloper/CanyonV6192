using Canyon.Game.Services.Managers;
using Canyon.Game.States.Items;
using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgDataArray : MsgBase<Client>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MsgDataArray>();

        public List<uint> Items = new();

        public DataArrayMode Action { get; set; }
        public byte Count { get; set; }

        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Action = (DataArrayMode)reader.ReadByte();
            Count = reader.ReadByte();
            reader.ReadUInt16();
            for (var i = 0; i < Count; i++)
            {
                Items.Add(reader.ReadUInt32());
            }
        }

        public override byte[] Encode()
        {
            PacketWriter writer = new();
            writer.Write((ushort)PacketType.MsgDataArray);
            writer.Write((byte)Action);
            writer.Write((byte)Items.Count);
            writer.Write((ushort)0);
            foreach (uint item in Items)
            {
                writer.Write(item);
            }
            return writer.ToArray();
        }

        public enum DataArrayMode : byte
        {
            Composition = 0,
            CompositionSteedOriginal = 2,
            CompositionSteedNew = 3,
            QuickCompose = 4,
            QuickComposeMount = 5,
            UpgradeItemLevel = 6,
            UpgradeItemQuality = 7
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;
            Item target = user.UserPackage.FindByIdentity(Items[0]);
            if (target == null)
            {
                return;
            }

            if (target.Position != Item.ItemPosition.Inventory && !user.UserPackage.TryItem(target.Identity, target.Position))
            {
                return;
            }

            if (target.IsSuspicious())
            {
                return;
            }

            switch (Action)
            {
                case DataArrayMode.Composition:
                case DataArrayMode.CompositionSteedOriginal:
                case DataArrayMode.CompositionSteedNew:
                case DataArrayMode.QuickCompose:
                case DataArrayMode.QuickComposeMount:
                    {
                        break;
                    }
                default:
                    {
                        if (target.Durability / 100 != target.MaximumDurability / 100)
                        {
                            await user.SendAsync(StrItemErrRepairItem);
                            return;
                        }
                        break;
                    }
            }

            int oldAddition = target.Plus;
            switch (Action)
            {
                case DataArrayMode.Composition:
                    {
                        if (target.Plus >= 12 && !target.IsWing())
                        {
                            await user.SendAsync(StrComposeItemMaxComposition);
                            return;
                        }

                        for (var i = 1; i < Items.Count; i++)
                        {
                            Item source = user.UserPackage[Items[i]];
                            if (source == null)
                            {
                                continue;
                            }

                            if (source.Type is < Item.TYPE_STONE1 or > Item.TYPE_STONE8)
                            {
                                if (source.IsWeaponOneHand())
                                {
                                    if (!target.IsWeaponOneHand() && !target.IsWeaponProBased())
                                    {
                                        continue;
                                    }
                                }
                                else if (source.IsWeaponTwoHand())
                                {
                                    if (source.IsBow() && !target.IsBow())
                                    {
                                        continue;
                                    }

                                    if (!target.IsWeaponTwoHand())
                                    {
                                        continue;
                                    }
                                }
                                else
                                {
                                    if (target.GetItemSort() != source.GetItemSort())
                                    {
                                        continue;
                                    }
                                }

                                if (source.Plus == 0 || source.Plus > 8)
                                {
                                    continue;
                                }

                            }

                            target.CompositionProgress += PlusAddLevelExp(source.Plus, false);
                            while (target.CompositionProgress >= GetAddLevelExp(target.Plus, false) && target.Plus < 12)
                            {
                                if (target.Plus < 12)
                                {
                                    target.CompositionProgress -= GetAddLevelExp(target.Plus, false);
                                    if (!target.ChangeAddition())
                                    {
                                        if (target.Plus >= 12)
                                        {
                                            target.CompositionProgress = 0;
                                        }
                                        break;
                                    }
                                }
                                else if (!target.IsWing())
                                {
                                    target.CompositionProgress = 0;
                                    break;
                                }
                            }

                            await user.UserPackage.SpendItemAsync(source);
                        }

                        break;
                    }

                case DataArrayMode.CompositionSteedOriginal:
                case DataArrayMode.CompositionSteedNew:
                    {
                        if (!target.IsMount())
                        {
                            return;
                        }

                        for (var i = 1; i < Items.Count; i++)
                        {
                            Item source = user.UserPackage[Items[i]];
                            if (source == null)
                            {
                                continue;
                            }

                            target.CompositionProgress += PlusAddLevelExp(source.Plus, true);
                            while (target.CompositionProgress >= GetAddLevelExp(target.Plus, false) && target.Plus < 12)
                            {
                                if (target.Plus < 12)
                                {
                                    target.CompositionProgress -= GetAddLevelExp(target.Plus, false);
                                    target.ChangeAddition();
                                }
                            }

                            if (Action == DataArrayMode.CompositionSteedNew)
                            {
                                int newB = (int)Math.Floor(0.9 * target.Enchantment) + (int)Math.Floor(0.1 * source.Enchantment);
                                int newG = (int)Math.Floor(0.9 * target.AntiMonster) + (int)Math.Floor(0.1 * source.AntiMonster);
                                int newR = (int)Math.Floor(0.9 * target.ReduceDamage) + (int)Math.Floor(0.1 * source.ReduceDamage);
                                target.ReduceDamage = (byte)newR;
                                target.Enchantment = (byte)newB;
                                target.AntiMonster = (byte)newG;
                                target.SocketProgress = (uint)(newG | (newB << 8) | (newR << 16));
                            }

                            await user.UserPackage.SpendItemAsync(source);
                        }

                        break;
                    }

                case DataArrayMode.QuickCompose:
                case DataArrayMode.QuickComposeMount:
                    {
                        double percent = (double)(target.CompositionProgress / (double)GetAddLevelExp(target.Plus, Action == DataArrayMode.QuickComposeMount) * 100);
                        if (await ChanceCalcAsync(percent))
                        {
                            target.ChangeAddition();
                        }
                        target.CompositionProgress = 0;
                        break;
                    }

                case DataArrayMode.UpgradeItemLevel:
                    {
                        if (!IsItemUpgradable(target))
                        {
                            return;
                        }

                        var nextItemType = target.NextItemLevel();
                        if (nextItemType == null)
                        {
                            await user.SendAsync(new MsgItem
                            {
                                Identity = target.Identity,
                                Action = MsgItem.ItemActionType.EquipmentLevelUp
                            });
                            return;
                        }

                        if (nextItemType.ReqLevel > user.Level && target.Position != Item.ItemPosition.Inventory)
                        {
                            return;
                        }

                        bool success = false;
                        List<Item> usable = new();
                        int totalGems = 0;
                        if (IsMeteorLevelUpgrade(target))
                        {
                            for (int i = 1; i < Items.Count; i++)
                            {
                                if (Items[i] == 0)
                                {
                                    break;
                                }

                                Item minor = user.UserPackage.FindByIdentity(Items[i]);
                                if (minor == null || minor.Position != Item.ItemPosition.Inventory)
                                {
                                    continue;
                                }

                                switch (minor.Type)
                                {
                                    case Item.TYPE_METEOR:
                                    case Item.TYPE_METEORTEAR:
                                        totalGems += 1;
                                        break;
                                    case Item.TYPE_METEOR_SCROLL:
                                        totalGems += 10;
                                        break;
                                }

                                usable.Add(minor);
                            }

                            int reqGems = Math.Max(2, target.GetUpgradeGemAmount());
                            if (reqGems > totalGems)
                            {
                                foreach (var usage in usable)
                                {
                                    await user.UserPackage.RemoveFromInventoryAsync(usage, UserPackage.RemovalType.Delete);
                                }

                                double chance = totalGems / (double)reqGems * 100;

                                if (!await ChanceCalcAsync(chance))
                                {
                                    await user.SendAsync(new MsgItem
                                    {
                                        Identity = target.Identity,
                                        Action = MsgItem.ItemActionType.EquipmentLevelUp
                                    });

                                    if (user.IsLucky && await ChanceCalcAsync(2))
                                    {
                                        await user.SendEffectAsync("LuckyGuy", true);
                                        await user.SendAsync(StrLuckyGuyNoDuraDown);
                                    }
                                    else
                                    {
                                        target.Durability = (ushort)(target.MaximumDurability / 2);
                                    }
                                }
                                else
                                {
                                    target.MaximumDurability = nextItemType.AmountLimit;
                                    target.Durability = nextItemType.Amount;

                                    success = true;
                                }
                                await user.SendAsync($"Up by meteor[{target.Type}->{nextItemType.Type}]: Chance {chance:0.000}% Required Gems[{reqGems}] SentGems[{totalGems}]", TalkChannel.Talk);
                            }
                            else
                            {
                                int usage = 0;
                                foreach (var item in usable.OrderByDescending(x => x.Type))
                                {
                                    switch (item.Type)
                                    {
                                        case Item.TYPE_METEOR:
                                        case Item.TYPE_METEORTEAR:
                                            usage++;
                                            break;
                                        case Item.TYPE_METEOR_SCROLL:
                                            usage += 10;
                                            break;
                                        default:
                                            continue;
                                    }

                                    await user.UserPackage.RemoveFromInventoryAsync(item, UserPackage.RemovalType.Delete);
                                    if (usage >= reqGems)
                                    {
                                        break;
                                    }
                                }

                                int rebate = usage - reqGems;
                                if (rebate > 0)
                                {
                                    for (int i = 0; i < rebate; i++)
                                    {
                                        await user.UserPackage.AwardItemAsync(Item.TYPE_METEOR);
                                    }
                                }

                                target.MaximumDurability = nextItemType.AmountLimit;
                                target.Durability = nextItemType.Amount;

                                success = true;
                                await user.SendAsync($"Up by meteor[{target.Type}->{nextItemType.Type}]: Required Gems[{reqGems}] SentGems[{totalGems}]", TalkChannel.Talk);
                            }
                        }
                        else // Dragon Balls
                        {
                            if (Items.Count <= 1)
                            {
                                await user.SendAsync(new MsgItem
                                {
                                    Identity = target.Identity,
                                    Action = MsgItem.ItemActionType.EquipmentLevelUp
                                });
                                return;
                            }

                            Item minor = user.UserPackage.FindByIdentity(Items[2]);
                            if (minor == null)
                            {
                                await user.SendAsync(new MsgItem
                                {
                                    Identity = target.Identity,
                                    Action = MsgItem.ItemActionType.EquipmentLevelUp
                                });
                                return;
                            }

                            int backDb = 0;
                            if (minor.Type == Item.TYPE_DRAGONBALL_SCROLL)
                            {
                                if (!user.UserPackage.IsPackSpare(9))
                                {
                                    await user.SendAsync(new MsgItem
                                    {
                                        Identity = target.Identity,
                                        Action = MsgItem.ItemActionType.EquipmentLevelUp
                                    });
                                    return;
                                }

                                backDb = 9;
                            }

                            if (!await user.UserPackage.SpendItemAsync(minor))
                            {
                                await user.SendAsync(new MsgItem
                                {
                                    Identity = target.Identity,
                                    Action = MsgItem.ItemActionType.EquipmentLevelUp
                                });
                                return;
                            }

                            for (int i = 0; i < backDb; i++)
                            {
                                await user.UserPackage.AwardItemAsync(Item.TYPE_DRAGONBALL);
                            }

                            target.MaximumDurability = nextItemType.AmountLimit;
                            target.Durability = nextItemType.Amount;

                            success = true;
                        }

                        if (success)
                        {
                            await target.ChangeTypeAsync(nextItemType.Type);
                            await user.SendAsync(new MsgItem
                            {
                                Identity = target.Identity,
                                Command = 1,
                                Action = MsgItem.ItemActionType.EquipmentLevelUp
                            });
                        }
                        break;
                    }

                case DataArrayMode.UpgradeItemQuality:
                    {
                        if (target.Type % 10 == 0)
                        {
                            await user.SendAsync(StrItemErrUpgradeFixed);
                            return;
                        }

                        uint idNewType = 0;
                        double nChance = 0.00;

                        if (!target.GetUpEpQualityInfo(out nChance, out idNewType) || idNewType == 0)
                        {
                            await user.SendAsync(StrItemCannotImprove);
                            return;
                        }

                        var nextItemType = ItemManager.GetItemtype(idNewType);
                        if (nextItemType == null)
                        {
                            await user.SendAsync(StrItemCannotImprove);
                            return;
                        }

                        int gemCost = (int)Math.Ceiling(100d / nChance);
                        List<Item> usable = new();
                        int totalGems = 0;
                        for (int i = 1; i < Items.Count; i++)
                        {
                            if (Items[i] == 0)
                            {
                                break;
                            }

                            Item minor = user.UserPackage.FindByIdentity(Items[i]);
                            if (minor == null || minor.Position != Item.ItemPosition.Inventory)
                            {
                                continue;
                            }

                            switch (minor.Type)
                            {
                                case Item.TYPE_DRAGONBALL:
                                    totalGems += 1;
                                    break;
                                case Item.TYPE_DRAGONBALL_SCROLL:
                                    totalGems += 10;
                                    break;
                            }

                            usable.Add(minor);
                        }

                        bool success = false;
                        if (gemCost > totalGems)
                        {
                            if (target.Type % 10 < 6 && target.Type % 10 > 0)
                            {
                                nChance = 100.00;
                            }

                            nChance = Math.Min(100d, Math.Max(1d, nChance * totalGems));

                            if (user.IsLucky && await ChanceCalcAsync(10, 2000))
                            {
                                await user.SendEffectAsync("LuckyGuy", true);
                                await user.SendAsync(StrLuckyGuySuccessUpgrade);
                                nChance = 100.00;
                            }

                            foreach (var i in usable)
                            {
                                await user.UserPackage.SpendItemAsync(i);
                            }

                            if (!await ChanceCalcAsync(nChance))
                            {
                                if (user.IsLucky && await ChanceCalcAsync(2))
                                {
                                    await user.SendEffectAsync("LuckyGuy", true);
                                    await user.SendAsync(StrLuckyGuyNoDuraDown);
                                }
                                else
                                {
                                    target.Durability = (ushort)(target.MaximumDurability / 2);
                                }

                                await user.SendAsync(new MsgItem
                                {
                                    Identity = target.Identity,
                                    Action = MsgItem.ItemActionType.EquipmentImprove
                                });
                            }
                            else
                            {
                                success = true;
                            }

                            logger.LogDebug("Quality DragonBall: Chance[{Chance}] Required Gems[{Gems}] SentGems[{SentGems}]", nChance, gemCost, totalGems);
                        }
                        else
                        {
                            int usage = 0;
                            foreach (var item in usable.OrderByDescending(x => x.Type))
                            {
                                switch (item.Type)
                                {
                                    case Item.TYPE_DRAGONBALL:
                                        usage++;
                                        break;
                                    case Item.TYPE_DRAGONBALL_SCROLL:
                                        usage += 10;
                                        break;
                                    default:
                                        continue;
                                }

                                await user.UserPackage.RemoveFromInventoryAsync(item, UserPackage.RemovalType.Delete);
                                if (usage >= totalGems)
                                {
                                    break;
                                }
                            }

                            int rebate = usage - totalGems;
                            if (rebate > 0)
                            {
                                for (int i = 0; i < rebate; i++)
                                {
                                    await user.UserPackage.AwardItemAsync(Item.TYPE_DRAGONBALL);
                                }
                            }

                            logger.LogDebug("Quality DragonBall: Required Gems[{Gems}] SentGems[{SentGems}]", gemCost, totalGems);
                            success = true;
                        }

                        if (success)
                        {
                            await target.ChangeTypeAsync(nextItemType.Type);
                            await user.SendAsync(new MsgItem
                            {
                                Identity = target.Identity,
                                Command = 1,
                                Action = MsgItem.ItemActionType.EquipmentImprove
                            });
                        }
                        break;
                    }

                default:
                    logger.LogError($"Invalid MsgDataArray Action: {Action}." +
                                                            $"{user.Identity},{user.Name},{user.Level},{user.MapIdentity}[{user.Map.Name}],{user.X},{user.Y}");
                    return;
            }

            if (oldAddition < target.Plus && target.Plus >= 6)
            {
                if (user.Gender == 1)
                {
                    await BroadcastWorldMsgAsync(string.Format(StrComposeOverpowerMale, user.Name,
                                                                      target.Itemtype.Name, target.Plus),
                                                        TalkChannel.TopLeft);
                }
                else
                {
                    await BroadcastWorldMsgAsync(string.Format(StrComposeOverpowerFemale, user.Name,
                                                                      target.Itemtype.Name, target.Plus),
                                                        TalkChannel.TopLeft);
                }
            }

            await target.SaveAsync();
            await user.SendAsync(new MsgItemInfo(target, MsgItemInfo.ItemMode.Update));
        }

        private static ushort PlusAddLevelExp(uint plus, bool steed)
        {
            switch (plus)
            {
                case 0:
                    if (steed)
                    {
                        return 1;
                    }

                    return 0;
                case 1: return 10;
                case 2: return 40;
                case 3: return 120;
                case 4: return 360;
                case 5: return 1080;
                case 6: return 3240;
                case 7: return 9720;
                case 8: return 29160;
                default: return 0;
            }
        }

        public static ushort GetAddLevelExp(uint plus, bool steed)
        {
            switch (plus)
            {
                case 0: return 20;
                case 1: return 20;
                case 2:
                    if (steed)
                    {
                        return 90;
                    }

                    return 80;
                case 3: return 240;
                case 4: return 720;
                case 5: return 2160;
                case 6: return 6480;
                case 7: return 19440;
                case 8: return 58320;
                case 9: return 2700;
                case 10: return 5500;
                case 11: return 9000;
                default: return 0;
            }
        }

        private static bool IsMeteorLevelUpgrade(Item item)
        {
            switch (item.GetPosition())
            {
                case Item.ItemPosition.Headwear:
                    return item.GetLevel() < 9;
                case Item.ItemPosition.Necklace:
                    return item.GetLevel() < 22;
                case Item.ItemPosition.Ring:
                    if (item.IsBangle())
                    {
                        return item.GetLevel() < 22;
                    }

                    return item.GetLevel() < 22;
                case Item.ItemPosition.RightHand:
                    if (item.IsBow())
                    {
                        return item.GetLevel() < 21;
                    }

                    return item.GetLevel() < 22;
                case Item.ItemPosition.LeftHand:
                    if (item.IsShield())
                    {
                        return item.GetLevel() < 9;
                    }

                    if (item.IsPistol())
                    {
                        return item.GetLevel() < 22;
                    }

                    return false;
                case Item.ItemPosition.Armor:
                    return item.GetLevel() < 9;
                case Item.ItemPosition.Boots:
                    return item.GetLevel() < 22;
            }

            return false;
        }

        private static bool IsItemUpgradable(Item item)
        {
            switch (item.GetPosition())
            {
                case Item.ItemPosition.Headwear:
                    return item.GetLevel() < 30;
                case Item.ItemPosition.Necklace:
                    return item.GetLevel() < 26;
                case Item.ItemPosition.Ring:
                    if (item.IsBangle())
                    {
                        return item.GetLevel() < 27;
                    }

                    return item.GetLevel() < 26;
                case Item.ItemPosition.RightHand:
                    if (item.IsBow())
                    {
                        return item.GetLevel() < 42;
                    }

                    return item.GetLevel() < 43;
                case Item.ItemPosition.LeftHand:
                    if (item.IsShield())
                    {
                        return item.GetLevel() < 30;
                    }

                    if (item.IsPistol())
                    {
                        return item.GetLevel() < 42;
                    }

                    return false;
                case Item.ItemPosition.Armor:
                    return item.GetLevel() < 30;
                case Item.ItemPosition.Boots:
                    return item.GetLevel() < 24;
            }

            return false;
        }
    }
}
