using Canyon.Database.Entities;
using Canyon.Game.Database.Repositories;
using Canyon.Game.Services.Managers;
using Canyon.Game.States;
using Canyon.Game.States.Items;
using Canyon.Game.States.NPCs;
using Canyon.Game.States.User;
using Canyon.Game.States.World;
using Canyon.Network.Packets;
using System.Drawing;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgItem : MsgBase<Client>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MsgItem>();
        private static readonly ILogger shopPurchaseLogger = LogFactory.CreateGmLogger("shop_purchase");
        private static readonly ILogger shopSellLogger = LogFactory.CreateGmLogger("shop_sell");
        public MsgItem()
        {
        }

        public MsgItem(uint identity, ItemActionType action, uint cmd = 0, uint param = 0)
        {
            Identity = identity;
            Command = cmd;
            Action = action;
            Timestamp = (uint)Environment.TickCount;
            Argument = param;
        }

        // Packet Properties
        public int Padding { get; set; }
        public uint Identity { get; set; }
        public uint Command { get; set; }
        public uint Data { get; set; }
        public uint Timestamp { get; set; }
        public uint Argument { get; set; } // ??? Count
        public ItemActionType Action { get; set; }
        public uint Argument2 { get; set; }
        public byte MoneyType { get; set; }
        public uint Headgear { get; set; }
        public uint Necklace { get; set; }
        public uint Armor { get; set; }
        public uint RightHand { get; set; }
        public uint LeftHand { get; set; }
        public uint Ring { get; set; }
        public uint Talisman { get; set; }
        public uint Boots { get; set; }
        public uint Garment { get; set; }
        public uint RightAccessory { get; set; }
        public uint LeftAccessory { get; set; }
        public uint MountArmor { get; set; }
        public uint Crop { get; set; }
        public uint Wings { get; set; }
        public List<uint> Consumables { get; } = new List<uint>();

        /// <summary>
        ///     Decodes a byte packet into the packet structure defined by this message class.
        ///     Should be invoked to structure data from the client for processing. Decoding
        ///     follows TQ Digital's byte ordering rules for an all-binary protocol.
        /// </summary>
        /// <param name="bytes">Bytes from the packet processor or client socket</param>
        public override void Decode(byte[] bytes)
        {
            using var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Padding = reader.ReadInt32(); // 4
            Identity = reader.ReadUInt32(); // 8
            Command = reader.ReadUInt32(); // 12
            Data = reader.ReadUInt32(); // 16
            Action = (ItemActionType)reader.ReadUInt16(); // 20
            Timestamp = reader.ReadUInt32(); // 22
            Argument = reader.ReadUInt32(); // 26
            Argument2 = reader.ReadUInt32(); // 30
            MoneyType = reader.ReadByte(); // 34
            Headgear = reader.ReadUInt32(); // 35
            Necklace = reader.ReadUInt32(); // 39
            Armor = reader.ReadUInt32(); // 43
            RightHand = reader.ReadUInt32(); // 47
            LeftHand = reader.ReadUInt32(); // 51
            Ring = reader.ReadUInt32(); // 55
            Talisman = reader.ReadUInt32(); // 59
            Boots = reader.ReadUInt32(); // 63
            Garment = reader.ReadUInt32(); // 67
            RightAccessory = reader.ReadUInt32(); // 71
            LeftAccessory = reader.ReadUInt32(); // 75
            MountArmor = reader.ReadUInt32(); // 79
            Crop = reader.ReadUInt32(); // 83
            Wings = reader.ReadUInt32(); // 87
            switch (Action)
            {
                case ItemActionType.TalismanProgress:
                case ItemActionType.GemCompose:
                case ItemActionType.TortoiseCompose:
                case ItemActionType.SocketEquipment:
                    {
                        for (int i = 0; i < Argument; i++)
                        {
                            Consumables.Add(reader.ReadUInt32());
                        }
                        break;
                    }
            }
        }

        /// <summary>
        ///     Encodes the packet structure defined by this message class into a byte packet
        ///     that can be sent to the client. Invoked automatically by the client's send
        ///     method. Encodes using byte ordering rules interoperable with the game client.
        /// </summary>
        /// <returns>Returns a byte packet of the encoded packet.</returns>
        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgItem);
            writer.Write(Environment.TickCount);
            writer.Write(Identity); // 8
            writer.Write(Command); // 12
            writer.Write(Data); // 16
            writer.Write((ushort)Action); // 20
            writer.Write(Timestamp); // 22
            writer.Write(MoneyType); // 26
            writer.Write(Argument); // 27
            writer.Write(Argument2); // 31
            writer.Write(Headgear); // 35
            writer.Write(Necklace); // 39
            writer.Write(Armor); // 43
            writer.Write(RightHand); // 47
            writer.Write(LeftHand); // 51
            writer.Write(Ring); // 55
            writer.Write(Talisman); // 59
            writer.Write(Boots); // 63
            writer.Write(Garment); // 67
            writer.Write(RightAccessory); // 71
            writer.Write(LeftAccessory); // 75
            writer.Write(MountArmor); // 79
            writer.Write(Crop); // 83
            writer.Write(Wings); // 87
            return writer.ToArray();
        }

        /// <summary>
        ///     Enumeration type for defining item actions that may be requested by the user,
        ///     or given to by the server. Allows for action handling as a packet subtype.
        ///     Enums should be named by the action they provide to a system in the context
        ///     of the player item.
        /// </summary>
        public enum ItemActionType
        {
            ShopPurchase = 1,
            ShopSell,
            InventoryRemove,
            InventoryEquip,
            EquipmentWear,
            EquipmentRemove,
            EquipmentSplit,
            EquipmentCombine,
            BankQuery,
            BankDeposit,
            BankWithdraw,
            EquipmentRepair = 14,
            EquipmentRepairAll,
            EquipmentImprove = 19,
            EquipmentLevelUp,
            BoothQuery,
            BoothSell,
            BoothRemove,
            BoothPurchase,
            EquipmentAmount,
            Fireworks,
            ClientPing = 27,
            EquipmentEnchant,
            BoothSellPoints,
            RedeemEquipment = 32,
            DetainEquipment = 33,
            DetainRewardClose = 34,
            TalismanProgress = 35,
            TalismanProgressEmoney = 36,
            InventoryDropItem = 37,
            InventoryDropSilver = 38,
            GemCompose = 39,
            TortoiseCompose = 40,
            ActivateAccessory = 41,
            SocketEquipment = 43,
            AlternativeEquipment = 45,
            DisplayGears = 46,
            MergeItems = 48,
            SplitItems = 49,
            ComposeRefinedTortoiseGem = 51,
            RequestItemTooltip = 52,
            DegradeEquipment = 54,
            ForgingBuy = 55,
            MergeSash = 56
        }

        public enum Moneytype
        {
            Silver,
            ConquerPoints,

            /// <summary>
            ///     CPs(B)
            /// </summary>
            ConquerPointsMono
        }

        public void Append(Item.ItemPosition pos, uint id)
        {
            switch (pos)
            {
                case Item.ItemPosition.Headwear:
                    Headgear = id;
                    break;
                case Item.ItemPosition.Necklace:
                    Necklace = id;
                    break;
                case Item.ItemPosition.Ring:
                    Ring = id;
                    break;
                case Item.ItemPosition.RightHand:
                    RightHand = id;
                    break;
                case Item.ItemPosition.LeftHand:
                    LeftHand = id;
                    break;
                case Item.ItemPosition.Armor:
                    Armor = id;
                    break;
                case Item.ItemPosition.Boots:
                    Boots = id;
                    break;
                case Item.ItemPosition.SteedArmor:
                    MountArmor = id;
                    break;
                case Item.ItemPosition.Crop:
                    Crop = id;
                    break;
                case Item.ItemPosition.Gourd:
                    Talisman = id;
                    break;
                case Item.ItemPosition.RightHandAccessory:
                    RightAccessory = id;
                    break;
                case Item.ItemPosition.LeftHandAccessory:
                    LeftAccessory = id;
                    break;
                case Item.ItemPosition.Garment:
                    Garment = id;
                    break;
                case Item.ItemPosition.Wing:
                    Wings = id;
                    break;
            }
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;
            BaseNpc npc = null;
            Item item = null;

            switch (Action)
            {
                case ItemActionType.ShopPurchase:
                case ItemActionType.ShopSell:
                case ItemActionType.BankWithdraw:
                case ItemActionType.BoothSell:
                case ItemActionType.TalismanProgress:
                case ItemActionType.TalismanProgressEmoney:
                case ItemActionType.InventoryDropItem:
                case ItemActionType.InventoryDropSilver:
                    {
                        if (!user.IsUnlocked())
                        {
                            await user.SendSecondaryPasswordInterfaceAsync();
                            return;
                        }
                        break;
                    }
            }

            switch (Action)
            {
                case ItemActionType.ShopPurchase:
                case ItemActionType.ForgingBuy:
                    {
                        int[] remoteShopping =
                        {
                            2888, 6000, 6001, 6002, 6003
                        };

                        npc = user.Map.QueryRole<BaseNpc>(Identity);
                        if (npc == null)
                        {
                            npc = RoleManager.GetRole<BaseNpc>(Identity);
                            if (npc == null)
                            {
                                return;
                            }

                            if (npc.MapIdentity != 5000 && npc.MapIdentity != user.MapIdentity)
                            {
                                return;
                            }
                        }

                        if (npc.MapIdentity != 5000 && remoteShopping.All(x => x != npc.Identity) && npc.GetDistance(user) > Screen.VIEW_SIZE)
                        {
                            return;
                        }

                        DbGoods goods = npc.ShopGoods.FirstOrDefault(x => x.Itemtype == Command);
                        if (goods == null)
                        {
                            logger.LogWarning($"Invalid goods itemtype {Command} for Shop {Identity}");
                            return;
                        }

                        DbItemtype itemtype = ItemManager.GetItemtype(Command);
                        if (itemtype == null)
                        {
                            logger.LogWarning($"Invalid goods itemtype (not existent) {Command} for Shop {Identity}");
                            return;
                        }

                        var amount = (int)Math.Max(1, (int)Argument);
                        if (!user.UserPackage.IsPackSpare(amount, itemtype.Type))
                        {
                            await user.SendAsync(StrYourBagIsFull);
                            return;
                        }

                        int price;
                        string moneyTypeString = ((Moneytype)goods.Moneytype).ToString();
                        const byte MONOPOLY_NONE_B = 0;
                        const byte MONOPOLY_BOUND_B = Item.ITEM_MONOPOLY_MASK;
                        byte monopoly = MONOPOLY_NONE_B;
                        switch ((Moneytype)goods.Moneytype)
                        {
                            case Moneytype.Silver:
                                {
                                    if ((Moneytype)goods.Moneytype != Moneytype.Silver)
                                    {
                                        return;
                                    }

                                    if (goods.HonorPrice != 0)
                                    {
                                        price = (int)goods.HonorPrice;
                                        if (user.HonorPoints < goods.HonorPrice)
                                        {
                                            return;
                                        }

                                        monopoly = MONOPOLY_BOUND_B;
                                        user.HonorPoints -= goods.HonorPrice;
                                        await user.SendAsync(new MsgAthleteShop(user.HonorPoints, user.HistoryHonorPoints));
                                        moneyTypeString = "HonorPoints";
                                    }
                                    else if (goods.RidingPrice != 0)
                                    {
                                        price = (int)goods.RidingPrice;
                                        if (!await user.SpendHorseRacePointsAsync(price))
                                        {
                                            return;
                                        }
                                        moneyTypeString = "RidingPoints";
                                    }
                                    else if (goods.GoldenLeaguePrice != 0)
                                    {
                                        price = (int)goods.GoldenLeaguePrice;
                                        if (!await user.SpendGoldenLeaguePointsAsync(price))
                                        {
                                            return;
                                        }
                                        moneyTypeString = "GoldenLeaguePoints";
                                    }
                                    else
                                    {
                                        if (itemtype.Price == 0)
                                        {
                                            return;
                                        }

                                        price = (int)(itemtype.Price * amount);
                                        if (!await user.SpendMoneyAsync(price, true))
                                        {
                                            return;
                                        }
                                    }
                                    break;
                                }

                            case Moneytype.ConquerPoints:
                                {
                                    if ((Moneytype)goods.Moneytype != Moneytype.ConquerPoints)
                                    {
                                        return;
                                    }

                                    if (MoneyType == 1)
                                    {
                                        if (itemtype.EmoneyPrice == 0)
                                        {
                                            return;
                                        }

                                        price = (int)(itemtype.EmoneyPrice * amount);
                                        if (!await user.SpendConquerPointsAsync(price, true))
                                        {
                                            return;
                                        }
                                    }
                                    else if (MoneyType == 2)
                                    {
                                        if (itemtype.BoundEmoneyPrice == 0)
                                        {
                                            return;
                                        }

                                        price = (int)(itemtype.BoundEmoneyPrice * amount);
                                        if (!await user.SpendBoundConquerPointsAsync(price, true))
                                        {
                                            return;
                                        }

                                        monopoly = MONOPOLY_BOUND_B;
                                        moneyTypeString += "(B)";
                                    }
                                    else
                                    {
                                        logger.LogWarning("Invalid money type {Argument}", Argument2);
                                        return;
                                    }
                                    break;
                                }

                            default:
                                logger.LogWarning($"Invalid moneytype {(Moneytype)Argument}/{Identity}/{Command} - {user.Identity}({user.Name})");
                                return;
                        }

                        DbItem dbItem = Item.CreateEntity(itemtype.Type, monopoly != 0);
                        if (dbItem == null)
                        {
                            return;
                        }

                        dbItem.AccumulateNum = (uint)amount;

                        item = new Item(user);
                        if (!await item.CreateAsync(dbItem))
                        {
                            return;
                        }

                        await user.UserPackage.AddItemAsync(item);

                        shopPurchaseLogger.LogInformation($"Purchase,{user.Identity},{user.Name},{user.Level},{user.MapIdentity},{user.X},{user.Y},{goods.OwnerIdentity},{goods.Itemtype},{goods.Moneytype},{moneyTypeString},{amount},{price}");
                        break;
                    }

                case ItemActionType.ShopSell:
                    {
                        if (Identity == 2888)
                        {
                            return;
                        }

                        npc = user.Map.QueryRole<BaseNpc>(Identity);
                        if (npc == null)
                        {
                            return;
                        }

                        if (npc.MapIdentity != user.MapIdentity || npc.GetDistance(user) > Screen.VIEW_SIZE)
                        {
                            return;
                        }

                        item = user.UserPackage[Command];
                        if (item == null)
                        {
                            return;
                        }

                        if (item.IsLocked())
                        {
                            return;
                        }

                        int price = item.GetSellPrice();
                        if (!await user.UserPackage.SpendItemAsync(item))
                        {
                            return;
                        }

                        shopSellLogger.LogInformation($"{user.Identity},{user.Name},{user.Level},{user.MapIdentity},{user.X},{user.Y},{item.Identity},{item.FullName},{item.Type},{price}");

                        await user.AwardMoneyAsync(price);
                        break;
                    }

                case ItemActionType.InventoryDropItem:
                case ItemActionType.InventoryRemove:
                    {
                        await user.DropItemAsync(Identity, user.X, user.Y);
                        break;
                    }

                case ItemActionType.InventoryDropSilver:
                    {
                        await user.DropSilverAsync(Identity);
                        break;
                    }

                case ItemActionType.InventoryEquip:
                case ItemActionType.EquipmentWear:
                    {
                        if (!await user.UserPackage.UseItemAsync(Identity, (Item.ItemPosition)Command))
                        {
                            await user.SendAsync(StrUnableToUseItem, TalkChannel.TopLeft, Color.Red);
                        }
                        break;
                    }

                case ItemActionType.EquipmentRemove:
                    {
                        if (!await user.UserPackage.UnEquipAsync((Item.ItemPosition)Command))
                        {
                            await user.SendAsync(StrYourBagIsFull, TalkChannel.TopLeft, Color.Red);
                        }

                        break;
                    }

                case ItemActionType.EquipmentCombine:
                    {
                        item = user.UserPackage[Identity];
                        Item target = user.UserPackage[Command];
                        await user.UserPackage.CombineArrowAsync(item, target);
                        break;
                    }

                case ItemActionType.BankQuery:
                    {
                        Command = user.StorageMoney;
                        await user.SendAsync(this);
                        break;
                    }

                case ItemActionType.BankDeposit:
                    {
                        if (user.Silvers < Command)
                        {
                            return;
                        }

                        if (Command + user.StorageMoney > Role.MAX_STORAGE_MONEY)
                        {
                            await user.SendAsync(string.Format(StrSilversExceedAmount, int.MaxValue));
                            return;
                        }

                        if (!await user.SpendMoneyAsync((int)Command, true))
                        {
                            return;
                        }

                        user.StorageMoney += Command;

                        Action = ItemActionType.BankQuery;
                        Command = user.StorageMoney;
                        await user.SendAsync(this);
                        await user.SaveAsync();
                        break;
                    }

                case ItemActionType.BankWithdraw:
                    {
                        if (Command > user.StorageMoney)
                        {
                            return;
                        }

                        if (Command + user.Silvers > int.MaxValue)
                        {
                            await user.SendAsync(string.Format(StrSilversExceedAmount, int.MaxValue));
                            return;
                        }

                        user.StorageMoney -= Command;

                        await user.AwardMoneyAsync((int)Command);

                        Action = ItemActionType.BankQuery;
                        Command = user.StorageMoney;
                        await user.SendAsync(this);
                        await user.SaveAsync();
                        break;
                    }

                case ItemActionType.EquipmentRepair:
                    {
                        item = user.UserPackage[Identity];
                        if (item != null && item.Position == Item.ItemPosition.Inventory)
                        {
                            await item.RepairItemAsync();
                        }

                        break;
                    }

                case ItemActionType.EquipmentRepairAll:
                    {
                        if (user.VipLevel < 2)
                        {
                            return;
                        }

                        for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin;
                            pos <= Item.ItemPosition.EquipmentEnd;
                            pos++)
                        {
                            if (user.UserPackage[pos] != null && user.UserPackage.TryItem(user.UserPackage[pos].Identity, user.UserPackage[pos].Position))
                            {
                                await user.UserPackage[pos].RepairItemAsync();
                            }
                        }

                        break;
                    }

                case ItemActionType.EquipmentImprove:
                    {
                        item = user.UserPackage[Identity];
                        if (item == null || item.Position != Item.ItemPosition.Inventory)
                        {
                            return;
                        }

                        if (item.IsSuspicious())
                        {
                            return;
                        }

                        if (item.Durability / 100 != item.MaximumDurability / 100)
                        {
                            await user.SendAsync(StrItemErrRepairItem);
                            return;
                        }

                        if (item.Type % 10 == 0)
                        {
                            await user.SendAsync(StrItemErrUpgradeFixed);
                            return;
                        }

                        uint idNewType = 0;
                        double nChance = 0.00;

                        if (!item.GetUpEpQualityInfo(out nChance, out idNewType) || idNewType == 0)
                        {
                            await user.SendAsync(StrItemCannotImprove);
                            return;
                        }

                        if (item.Type % 10 < 6 && item.Type % 10 > 0)
                        {
                            nChance = 100.00;
                        }

                        if (!await user.UserPackage.SpendDragonBallsAsync(1, item.IsBound))
                        {
                            await user.SendAsync(StrItemErrNoDragonBall);
                            return;
                        }

                        if (user.IsLucky && await ChanceCalcAsync(10, 2000))
                        {
                            await user.SendEffectAsync("LuckyGuy", true);
                            await user.SendAsync(StrLuckyGuySuccessUpgrade);
                            nChance = 100.00;
                        }

                        if (await ChanceCalcAsync(nChance))
                        {
                            await item.ChangeTypeAsync(idNewType);
                        }
                        else
                        {
                            if (user.IsLucky && await ChanceCalcAsync(2))
                            {
                                await user.SendEffectAsync("LuckyGuy", true);
                                await user.SendAsync(StrLuckyGuyNoDuraDown);
                            }
                            else
                            {
                                item.Durability = (ushort)(item.MaximumDurability / 2);
                            }
                        }

                        if (item.SocketOne == Item.SocketGem.NoSocket && await ChanceCalcAsync(5, 1000))
                        {
                            item.SocketOne = Item.SocketGem.EmptySocket;
                            await user.SendAsync(StrUpgradeAwardSocket);
                        }

                        await item.SaveAsync();
                        await user.SendAsync(new MsgItemInfo(item, MsgItemInfo.ItemMode.Update));
                        logger.LogInformation($"{user.Identity},{user.Name};{item.Identity};{item.Type};{Item.TYPE_DRAGONBALL}");
                        break;
                    }

                case ItemActionType.EquipmentLevelUp:
                    {
                        item = user.UserPackage[Identity];
                        if (item == null || item.Position != Item.ItemPosition.Inventory)
                        {
                            return;
                        }

                        if (item.IsSuspicious())
                        {
                            return;
                        }

                        if (item.Durability / 100 != item.MaximumDurability / 100)
                        {
                            await user.SendAsync(StrItemErrRepairItem);
                            return;
                        }

                        if (item.Type % 10 == 0)
                        {
                            await user.SendAsync(StrItemErrUpgradeFixed);
                            return;
                        }

                        int idNewType = 0;
                        int nChance = 0;

                        if (!item.GetUpLevelChance(out nChance, out idNewType) || idNewType == 0)
                        {
                            await user.SendAsync(StrItemErrMaxLevel);
                            return;
                        }

                        DbItemtype dbNewType = ItemManager.GetItemtype((uint)idNewType);
                        if (dbNewType == null)
                        {
                            await user.SendAsync(StrItemErrMaxLevel);
                            return;
                        }

                        if (!await user.UserPackage.SpendMeteorsAsync(1))
                        {
                            await user.SendAsync(string.Format(StrItemErrNotEnoughMeteors, 1));
                            return;
                        }

                        if (user.IsLucky && await ChanceCalcAsync(10, 2000))
                        {
                            await user.SendEffectAsync("LuckyGuy", true);
                            await user.SendAsync(StrLuckyGuySuccessUplevel);
                            nChance = 100;
                        }

                        if (await ChanceCalcAsync(nChance))
                        {
                            await item.ChangeTypeAsync((uint)idNewType);
                        }
                        else
                        {
                            if (user.IsLucky && await ChanceCalcAsync(2))
                            {
                                await user.SendEffectAsync("LuckyGuy", true);
                                await user.SendAsync(StrLuckyGuyNoDuraDown);
                            }
                            else
                            {
                                item.Durability = (ushort)(item.MaximumDurability / 2);
                            }
                        }

                        if (item.SocketOne == Item.SocketGem.NoSocket && await ChanceCalcAsync(5, 1000))
                        {
                            item.SocketOne = Item.SocketGem.EmptySocket;
                            await user.SendAsync(StrUpgradeAwardSocket);
                            await item.SaveAsync();
                        }

                        await item.SaveAsync();
                        await user.SendAsync(new MsgItemInfo(item, MsgItemInfo.ItemMode.Update));
                        logger.LogInformation($"{user.Identity},{user.Name};{item.Identity};{item.Type};{Item.TYPE_METEOR}");
                        break;
                    }

                case ItemActionType.BoothQuery:
                    {
                        var targetNpc = user.Screen.Roles.Values.FirstOrDefault(x =>
                                                                                    x is Character targetUser &&
                                                                                    targetUser.Booth?.Identity ==
                                                                                    Identity) as Character;
                        if (targetNpc?.Booth == null)
                        {
                            return;
                        }

                        await targetNpc.Booth.QueryItemsAsync(user);
                        break;
                    }

                case ItemActionType.BoothSell:
                    {
                        if (user.AddBoothItem(Identity, Command, Moneytype.Silver))
                        {
                            await user.SendAsync(this);
                        }
                        break;
                    }

                case ItemActionType.BoothRemove:
                    {
                        if (user.RemoveBoothItem(Identity))
                        {
                            await user.SendAsync(this);
                        }
                        break;
                    }

                case ItemActionType.BoothPurchase:
                    {
                        var targetNpc = user.Screen.Roles.Values.FirstOrDefault(x =>
                                                                                    x is Character targetUser &&
                                                                                    targetUser.Booth?.Identity ==
                                                                                    Command) as Character;
                        if (targetNpc?.Booth == null)
                        {
                            return;
                        }

                        if (await targetNpc.SellBoothItemAsync(Identity, user))
                        {
                            Action = ItemActionType.BoothRemove;
                            await targetNpc.SendAsync(this);
                            await user.SendAsync(this);
                        }

                        break;
                    }

                case ItemActionType.BoothSellPoints:
                    {
                        if (user.AddBoothItem(Identity, Command, Moneytype.ConquerPoints))
                        {
                            await user.SendAsync(this);
                        }

                        break;
                    }

                case ItemActionType.ClientPing:
                    {
                        await user.SendAsync(this);
                        break;
                    }

                case ItemActionType.EquipmentEnchant:
                    {
                        item = user.UserPackage.FindByIdentity(Identity);
                        Item gem = user.UserPackage[Command];

                        if (item == null || gem == null)
                        {
                            return;
                        }

                        if (item.Durability / 100 != item.MaximumDurability / 100)
                        {
                            await user.SendAsync(StrItemErrRepairItem);
                            return;
                        }

                        if (item.IsSuspicious())
                        {
                            return;
                        }

                        if (item.Enchantment >= byte.MaxValue)
                        {
                            return;
                        }

                        if (!gem.IsGem())
                        {
                            return;
                        }

                        await user.UserPackage.SpendItemAsync(gem);

                        byte min, max;
                        switch ((Item.SocketGem)(gem.Type % 1000))
                        {
                            case Item.SocketGem.NormalPhoenixGem:
                            case Item.SocketGem.NormalDragonGem:
                            case Item.SocketGem.NormalFuryGem:
                            case Item.SocketGem.NormalKylinGem:
                            case Item.SocketGem.NormalMoonGem:
                            case Item.SocketGem.NormalTortoiseGem:
                            case Item.SocketGem.NormalVioletGem:
                                min = 1;
                                max = 59;
                                break;
                            case Item.SocketGem.RefinedPhoenixGem:
                            case Item.SocketGem.RefinedVioletGem:
                            case Item.SocketGem.RefinedMoonGem:
                                min = 60;
                                max = 109;
                                break;
                            case Item.SocketGem.RefinedFuryGem:
                            case Item.SocketGem.RefinedKylinGem:
                            case Item.SocketGem.RefinedTortoiseGem:
                                min = 40;
                                max = 89;
                                break;
                            case Item.SocketGem.RefinedDragonGem:
                                min = 100;
                                max = 159;
                                break;
                            case Item.SocketGem.RefinedRainbowGem:
                                min = 80;
                                max = 129;
                                break;
                            case Item.SocketGem.SuperPhoenixGem:
                            case Item.SocketGem.SuperTortoiseGem:
                            case Item.SocketGem.SuperRainbowGem:
                                min = 170;
                                max = 229;
                                break;
                            case Item.SocketGem.SuperVioletGem:
                            case Item.SocketGem.SuperMoonGem:
                                min = 140;
                                max = 199;
                                break;
                            case Item.SocketGem.SuperDragonGem:
                                min = 200;
                                max = 255;
                                break;
                            case Item.SocketGem.SuperFuryGem:
                                min = 90;
                                max = 149;
                                break;
                            case Item.SocketGem.SuperKylinGem:
                                min = 70;
                                max = 119;
                                break;
                            default:
                                return;
                        }

                        byte enchant = (byte)await NextAsync(min, max);
                        if (enchant > item.Enchantment)
                        {
                            item.Enchantment = enchant;
                            await item.SaveAsync();
                            logger.LogInformation($"User[{user.Identity}] Enchant[Gem: {gem.Type}|{gem.Identity}][Target: {item.Type}|{item.Identity}] with {enchant} points.");
                        }

                        Command = enchant;
                        await user.SendAsync(this);
                        await user.SendAsync(new MsgItemInfo(item, MsgItemInfo.ItemMode.Update));
                        break;
                    }

                case ItemActionType.RedeemEquipment:
                    {
                        if (await ItemManager.ClaimDetainedItemAsync(Identity, user))
                        {
                            Command = user.Identity;
                            await user.SendAsync(this);
                        }

                        break;
                    }

                case ItemActionType.DetainEquipment:
                    {
                        if (await ItemManager.ClaimDetainRewardAsync(Identity, user))
                        {
                            Command = user.Identity;
                            await user.SendAsync(this);
                        }

                        break;
                    }

                case ItemActionType.DetainRewardClose:
                    {
                        break;
                    }

                case ItemActionType.TalismanProgress:
                    {
                        item = user.UserPackage.FindByIdentity(Identity);

                        if (item == null)
                        {
                            return;
                        }

                        if (item.Durability / 100 != item.MaximumDurability / 100)
                        {
                            await user.SendAsync(StrItemErrRepairItem);
                            return;
                        }

                        if (!item.IsTalisman())
                        {
                            return;
                        }

                        foreach (var idItem in Consumables)
                        {
                            Item target = user.UserPackage[idItem];
                            if (target == null || target.Position != Item.ItemPosition.Inventory)
                            {
                                continue;
                            }

                            if (target.IsBound && !item.IsBound)
                            {
                                continue;
                            }

                            if (target.IsTalisman() || target.IsMount() || !target.IsEquipment())
                            {
                                continue;
                            }

                            if (target.GetQuality() < 6)
                            {
                                continue;
                            }

                            item.SocketProgress += target.CalculateSocketProgress();
                            await user.UserPackage.RemoveFromInventoryAsync(target, UserPackage.RemovalType.Delete);
                            if (item.SocketOne == Item.SocketGem.NoSocket && item.SocketProgress >= 8000)
                            {
                                item.SocketProgress = 0;
                                item.SocketOne = Item.SocketGem.EmptySocket;
                            }
                            else if (item.SocketOne != Item.SocketGem.NoSocket && item.SocketTwo == Item.SocketGem.NoSocket && item.SocketProgress >= 20000)
                            {
                                item.SocketProgress = 0;
                                item.SocketTwo = Item.SocketGem.EmptySocket;
                                break;
                            }
                        }

                        await item.SaveAsync();
                        await user.SendAsync(new MsgItemInfo(item, MsgItemInfo.ItemMode.Update));
                        await user.SendAsync(this);
                        break;
                    }

                case ItemActionType.TalismanProgressEmoney:
                    {
                        item = user.UserPackage.GetEquipmentById(Identity);

                        if (item == null)
                        {
                            return;
                        }

                        if (item.Durability / 100 != item.MaximumDurability / 100)
                        {
                            await user.SendAsync(StrItemErrRepairItem);
                            return;
                        }

                        if (item.SocketOne == Item.SocketGem.NoSocket)
                        {
                            if (item.SocketProgress < 2400)
                            {
                                return;
                            }

                            if (!await user.SpendConquerPointsAsync((int)(5600 * (1 - item.SocketProgress / 8000f)), true))
                            {
                                return;
                            }

                            item.SocketProgress = 0;
                            item.SocketOne = Item.SocketGem.EmptySocket;
                        }
                        else if (item.SocketOne != Item.SocketGem.NoSocket && item.SocketTwo == Item.SocketGem.NoSocket)
                        {
                            if (item.SocketProgress < 2400)
                            {
                                return;
                            }

                            if (!await user.SpendConquerPointsAsync((int)(14000 * (1 - item.SocketProgress / 20000f)), true))
                            {
                                return;
                            }

                            item.SocketProgress = 0;
                            item.SocketTwo = Item.SocketGem.EmptySocket;
                        }

                        await item.SaveAsync();
                        await user.SendAsync(new MsgItemInfo(item, MsgItemInfo.ItemMode.Update));
                        await user.SendAsync(this);
                        break;
                    }

                case ItemActionType.GemCompose:
                    {
                        const int necessaryGems = 15;
                        const int refinedPrice = 10_000;
                        const int superPrice = 800_000;
                        const int superTortoisePrice = 1_000_000;

                        List<Item> usable = new();
                        uint gemType = Identity;

                        user.UserPackage.MultiGetItem(gemType, gemType, necessaryGems, ref usable, true);

                        if (usable.Count < necessaryGems)
                        {
                            return;
                        }

                        int gemQuality = (int)(gemType % 10u);
                        if (gemQuality < 1 || gemQuality >= 3)
                        {
                            return;
                        }

                        int price;
                        if (gemQuality == 1)
                        {
                            price = refinedPrice;
                        }
                        else
                        {
                            if (gemType % 1000 / 10 == 7)
                            {
                                price = superTortoisePrice;
                            }
                            else
                            {
                                price = superPrice;
                            }
                        }

                        if (!await user.SpendMoneyAsync(price, true))
                        {
                            return;
                        }

                        foreach (var consumable in usable)
                        {
                            await user.UserPackage.SpendItemAsync(consumable);
                        }

                        gemType += 1;

                        await user.UserPackage.AwardItemAsync(gemType);
                        Command = 1;
                        await user.SendAsync(this);
                        break;
                    }

                case ItemActionType.TortoiseCompose:
                    {
                        item = user.UserPackage.FindByIdentity(Identity);

                        if (item == null)
                        {
                            await user.SendAsync(this);
                            return;
                        }

                        if (item.Durability / 100 != item.MaximumDurability / 100)
                        {
                            await user.SendAsync(this);
                            await user.SendAsync(StrItemErrRepairItem);
                            return;
                        }

                        bool isWeapon = item.IsWeapon();
                        bool isEquipment = item.IsHelmet() || item.IsNeck() || item.IsRing() || item.IsBangle() || item.IsArmor() || item.IsShoes() || item.IsShield();

                        if (!isWeapon && !isEquipment)
                        {
                            await user.SendAsync(this);
                            return;
                        }

                        string effect;
                        byte nextBless;
                        int consumeNum;
                        if (item.ReduceDamage < 1)
                        {
                            effect = "Aegis1";
                            consumeNum = 5;
                            nextBless = 1;
                        }
                        else if (item.ReduceDamage < 3)
                        {
                            effect = "Aegis2";
                            consumeNum = 1;
                            nextBless = 3;
                        }
                        else if (item.ReduceDamage < 5)
                        {
                            effect = "Aegis3";
                            consumeNum = 3;
                            nextBless = 5;
                        }
                        else if (item.ReduceDamage < 7)
                        {
                            effect = "Aegis4";
                            consumeNum = 5;
                            nextBless = 7;
                        }
                        else
                        {
                            await user.SendAsync(this);
                            return;
                        }

                        List<Item> usable = new();
                        foreach (var consumableId in Consumables)
                        {
                            Item consumable = user.UserPackage[consumableId];
                            if (consumable == null)
                            {
                                await user.SendAsync(this);
                                return;
                            }

                            usable.Add(consumable);
                            if (usable.Count >= consumeNum)
                            {
                                break;
                            }
                        }

                        if (consumeNum == 0 || usable.Count < consumeNum)
                        {
                            await user.SendAsync(this);
                            await user.SendAsync(StrEmbedNoRequiredItem);
                            return;
                        }

                        foreach (var consumable in usable)
                        {
                            await user.UserPackage.SpendItemAsync(consumable);
                        }

                        item.ReduceDamage = nextBless;

                        Command = 1;
                        await user.SendAsync(new MsgItemInfo(item, MsgItemInfo.ItemMode.Update));
                        await user.SendAsync(this);
                        await user.SendEffectAsync(effect, true);
                        await item.SaveAsync();
                        break;
                    }

                case ItemActionType.ActivateAccessory:
                    {
                        item = user.UserPackage[Identity];
                        if (item == null)
                        {
                            return;
                        }

                        if (!item.IsActivable()) // support for old items before update
                        {
                            DbItemtype itemType = ItemManager.GetItemtype(item.Type);
                            if (itemType == null)
                            {
                                return;
                            }

                            if (itemType.SaveTime == 0)
                            {
                                return;
                            }

                            item.SaveTime = (int)itemType.SaveTime;
                        }

                        await item.ActivateAsync();
                        await user.SendAsync(new MsgItemInfo(item, MsgItemInfo.ItemMode.Update));
                        await user.SendAsync(this);
                        break;
                    }

                case ItemActionType.SocketEquipment:
                    {
                        item = user.UserPackage.FindByIdentity(Identity);

                        if (item == null)
                        {
                            return;
                        }

                        if (item.Durability / 100 != item.MaximumDurability / 100)
                        {
                            await user.SendAsync(StrItemErrRepairItem);
                            return;
                        }

                        bool isWeapon = item.IsWeapon();
                        bool isEquipment = item.IsHelmet() || item.IsNeck() || item.IsRing() || item.IsBangle() || item.IsArmor() || item.IsShoes() || item.IsShield();

                        if (!isWeapon && !isEquipment)
                        {
                            return;
                        }

                        if (item.SocketTwo != Item.SocketGem.NoSocket)
                        {
                            return;
                        }

                        int consumeNum = 0;
                        if (isWeapon)
                        {
                            consumeNum = item.SocketOne == Item.SocketGem.NoSocket ? 1 : 5;
                        }

                        List<Item> usable = new();
                        foreach (var consumableId in Consumables)
                        {
                            Item consumable = user.UserPackage[consumableId];
                            if (isWeapon)
                            {
                                if (consumable.Type != Item.TYPE_DRAGONBALL)
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                if (item.SocketOne == Item.SocketGem.NoSocket)
                                {
                                    consumeNum = 12;
                                }
                                else
                                {
                                    if (consumeNum == 0 && Item.TYPE_STARDRILL == consumable.Type)
                                    {
                                        consumeNum = 7;
                                    }
                                    else if (consumeNum == 0 && Item.TYPE_TOUGHDRILL == consumable.Type)
                                    {
                                        consumeNum = 1;
                                    }
                                }
                            }

                            usable.Add(consumable);

                            if (usable.Count >= consumeNum)
                            {
                                break;
                            }
                        }

                        if (consumeNum == 0 || usable.Count < consumeNum)
                        {
                            await user.SendAsync(StrEmbedNoRequiredItem);
                            return;
                        }

                        foreach (var consumable in usable)
                        {
                            await user.UserPackage.SpendItemAsync(consumable);

                            if (consumable.Type == Item.TYPE_TOUGHDRILL)
                            {
                                if (await NextAsync(100) > 10)
                                {
                                    await user.UserPackage.AwardItemAsync(Item.TYPE_STARDRILL);
                                    await user.SendAsync(this);
                                    return;
                                }

                                await BroadcastWorldMsgAsync($"{user.Name} succeeded in using the ToughDrill to make a second socket on the item.", TalkChannel.Center);
                            }
                        }

                        if (item.SocketOne == Item.SocketGem.NoSocket)
                        {
                            item.SocketOne = Item.SocketGem.EmptySocket;
                        }
                        else if (item.SocketTwo == Item.SocketGem.NoSocket)
                        {
                            item.SocketTwo = Item.SocketGem.EmptySocket;
                        }

                        await user.SendAsync(new MsgItemInfo(item, MsgItemInfo.ItemMode.Update));
                        Command = 1;
                        await user.SendAsync(this);

                        await item.SaveAsync();
                        break;
                    }

                case ItemActionType.AlternativeEquipment:
                    {

                        break;
                    }


                case ItemActionType.MergeItems:
                    {
                        await user.UserPackage.CombineItemAsync(Identity, Command);
                        break;
                    }

                case ItemActionType.SplitItems:
                    {
                        await user.UserPackage.SplitItemAsync(Identity, (int)Command);
                        break;
                    }

                case ItemActionType.ComposeRefinedTortoiseGem:
                    {
                        uint[] necessaryGems =
                        {
                            700002,
                            700012,
                            700022,
                            700032,
                            700042,
                            700052,
                            700062,
                        };

                        List<Item> usable = new();
                        foreach (var necessary in necessaryGems)
                        {
                            Item gem = user.UserPackage.GetItemByType(necessary);
                            if (gem == null)
                            {
                                return;
                            }

                            usable.Add(gem);
                        }

                        if (usable.Count < necessaryGems.Length)
                        {
                            return;
                        }

                        if (!await user.SpendMoneyAsync(100_000, true))
                        {
                            return;
                        }

                        foreach (var consume in usable)
                        {
                            await user.UserPackage.SpendItemAsync(consume);
                        }

                        await user.UserPackage.AwardItemAsync(700_000u + (uint)Item.SocketGem.RefinedTortoiseGem);
                        Command = 1;
                        await user.SendAsync(this);
                        break;
                    }

                case ItemActionType.RequestItemTooltip:
                    {
                        DbItem dbItem = await ItemRepository.GetByIdAsync(Identity);
                        if (dbItem == null)
                        {
                            return;
                        }

                        item = new Item();
                        if (!await item.CreateAsync(dbItem))
                        {
                            return;
                        }

                        await user.SendAsync(new MsgItemInfo(item, (MsgItemInfo.ItemMode)9));
                        if (item.Quench != null)
                        {
                            await item.Quench.SendToAsync(user);
                        }
                        break;
                    }

                case ItemActionType.DegradeEquipment:
                    {
                        item = user.UserPackage[Identity];
                        if (item == null)
                        {
                            return;
                        }

                        if (!await user.SpendBoundConquerPointsAsync(54, true))
                        {
                            return;
                        }

                        await item.DegradeItemAsync();
                        await user.SendAsync(new MsgItemInfo(item));
                        Command = 1;
                        await user.SendAsync(this);
                        break;
                    }

                case ItemActionType.MergeSash:
                    {
                        item = user.UserPackage[Identity];
                        if (item == null)
                        {
                            return;
                        }

                        if (item.GetItemSort() != (Item.ItemSort?)11)
                        {
                            return;
                        }

                        await user.OpenSashSlotsAsync(item);
                        break;
                    }

                default:
                    {
                        logger.LogWarning("Action [{Action}] is not being handled.\n{Dump}", Action, PacketDump.Hex(Encode()));
                        break;
                    }
            }
        }
    }
}
