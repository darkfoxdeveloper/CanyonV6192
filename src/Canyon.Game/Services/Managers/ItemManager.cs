using Canyon.Database.Entities;
using Canyon.Game.Database;
using Canyon.Game.Database.Repositories;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States.Items;
using Canyon.Game.States.NPCs;
using Canyon.Game.States.User;
using Canyon.Shared.Managers;
using System.Collections.Concurrent;
using System.Drawing;
using static Canyon.Game.Sockets.Game.Packets.MsgAction;
using static Canyon.Game.Sockets.Game.Packets.MsgMapItem;
using static Canyon.Game.States.Items.ItemQuench;

namespace Canyon.Game.Services.Managers
{
    public class ItemManager
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<ItemManager>();
        private static readonly ILogger detainLogger = LogFactory.CreateGmLogger("detain_item");

        public const int MAX_REDEEM_DAYS = 7;
        public const int MAX_REDEEM_SECONDS = 60 * 60 * 24 * MAX_REDEEM_DAYS;

        private static readonly ConcurrentDictionary<int, QuenchInfoData> refineryTypes = new(new Dictionary<int, QuenchInfoData>
        {
            { 301, new QuenchInfoData { Attribute1 = QuenchAttribute.Intensification } },
            { 302, new QuenchInfoData { Attribute1 = QuenchAttribute.FinalDamage } },
            { 303, new QuenchInfoData { Attribute1 = QuenchAttribute.FinalAttack } },
            { 304, new QuenchInfoData { Attribute1 = QuenchAttribute.Detoxication } },
            { 305, new QuenchInfoData { Attribute1 = QuenchAttribute.FinalMagicAttack } },
            { 306, new QuenchInfoData { Attribute1 = QuenchAttribute.FinalMagicDefense } },
            { 307, new QuenchInfoData { Attribute1 = QuenchAttribute.CriticalStrike } },
            { 308, new QuenchInfoData { Attribute1 = QuenchAttribute.SkillCriticalStrike } },
            { 309, new QuenchInfoData { Attribute1 = QuenchAttribute.Immunity } },
            { 310, new QuenchInfoData { Attribute1 = QuenchAttribute.Breakthrough } },
            { 311, new QuenchInfoData { Attribute1 = QuenchAttribute.Counteraction } },
            { 312, new QuenchInfoData { Attribute1 = QuenchAttribute.Penetration } },
            { 313, new QuenchInfoData { Attribute1 = QuenchAttribute.Block } },
            { 314, new QuenchInfoData { Attribute1 = QuenchAttribute.MetalResist } },
            { 315, new QuenchInfoData { Attribute1 = QuenchAttribute.WoodResist } },
            { 316, new QuenchInfoData { Attribute1 = QuenchAttribute.WaterResist } },
            { 317, new QuenchInfoData { Attribute1 = QuenchAttribute.FireResist } },
            { 318, new QuenchInfoData { Attribute1 = QuenchAttribute.WoodResist } },
            { 319, new QuenchInfoData { Attribute1 = QuenchAttribute.MagicDefense } }
        });

        private static ConcurrentDictionary<uint, DbItemtype> itemtypes;
        private static ConcurrentDictionary<ulong, DbItemAddition> itemAdditions;
        private static List<uint> validRefineryIds;
        public static BaseNpc Confiscator => RoleManager.FindRole<BaseNpc>(4450);

        public static async Task InitializeAsync()
        {
            logger.LogInformation("Starting Item Manager");

            itemtypes = new ConcurrentDictionary<uint, DbItemtype>();
            foreach (var item in await ItemtypeRepository.GetAsync())
            {
                itemtypes.TryAdd(item.Type, item);
            }

            logger.LogInformation("{Count} itemtypes loaded", itemtypes.Count);

            itemAdditions = new ConcurrentDictionary<ulong, DbItemAddition>();
            foreach (var addition in await ItemAdditionRepository.GetAsync())
            {
                itemAdditions.TryAdd(AdditionKey(addition.TypeId, addition.Level), addition);
            }

            logger.LogInformation("{Count} additions loaded", itemAdditions.Count);

            validRefineryIds = new List<uint>();
            using StreamReader quenchReader = new(Path.Combine(Environment.CurrentDirectory, "ini", "ItemQuench.ini"));
            string quenchLine;
            while ((quenchLine = quenchReader.ReadLine()) != null)
            {
                if (uint.TryParse(quenchLine, out uint quenchId) && validRefineryIds.All(x => x != quenchId))
                {
                    validRefineryIds.Add(quenchId);
                }
            }

            logger.LogInformation("{Count} valid refineries added", validRefineryIds.Count);
        }

        public static List<DbItemtype> GetByRange(int mobLevel, int tolerationMin, int tolerationMax, int maxLevel = 120)
        {
            return itemtypes.Values.Where(x =>
                x.ReqLevel >= mobLevel - tolerationMin && x.ReqLevel <= mobLevel + tolerationMax &&
                x.ReqLevel <= maxLevel).ToList();
        }

        public static DbItemtype GetItemtype(uint type)
        {
            return itemtypes.TryGetValue(type, out var item) ? item : null;
        }

        public static DbItemAddition GetItemAddition(uint type, byte level)
        {
            return itemAdditions.TryGetValue(AdditionKey(type, level), out var item) ? item : null;
        }

        private static ulong AdditionKey(uint type, byte level)
        {
            uint key = type;
            Item.ItemSort sort = Item.GetItemSort(type);
            if (sort == Item.ItemSort.ItemsortWeaponSingleHand && Item.GetItemSubType(type) != 421)
            {
                key = type / 100000 * 100000 + type % 1000 + 44000 - type % 10;
            }
            else if (sort == Item.ItemSort.ItemsortWeaponDoubleHand && !Item.IsBow(type))
            {
                key = type / 100000 * 100000 + type % 1000 + 55000 - type % 10;
            }
            else
            {
                key = type / 1000 * 1000 + (type % 1000 - type % 10);
            }

            return (key + ((ulong)level << 32));
        }

        public static bool IsValidRefinery(uint id)
        {
            return validRefineryIds.Any(x => x == id);
        }

        public static bool QuenchInfoData(int quenchType, out QuenchInfoData data)
        {
            return refineryTypes.TryGetValue(quenchType, out data);
        }

        public static async Task<bool> DetainItemAsync(Character discharger, Character detainer)
        {
            var items = new List<Item>();
            for (var pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
            {
                switch (pos)
                {
                    case Item.ItemPosition.Headwear:
                    case Item.ItemPosition.Necklace:
                    case Item.ItemPosition.Ring:
                    case Item.ItemPosition.RightHand:
                    case Item.ItemPosition.Armor:
                    case Item.ItemPosition.LeftHand:
                    case Item.ItemPosition.Boots:
                    case Item.ItemPosition.AttackTalisman:
                    case Item.ItemPosition.DefenceTalisman:
                    case Item.ItemPosition.Crop:
                        {
                            if (discharger.UserPackage[pos] == null)
                            {
                                continue;
                            }

                            if (discharger.UserPackage[pos].IsArrowSort())
                            {
                                continue;
                            }

                            if (discharger.UserPackage[pos].IsSuspicious())
                            {
                                continue;
                            }

                            items.Add(discharger.UserPackage[pos]);
                            continue;
                        }
                }
            }

            Item item = items[await NextAsync(items.Count) % items.Count];

            if (item == null)
            {
                return false;
            }

            if (item.IsArrowSort())
            {
                return false;
            }

            if (item.IsMount())
            {
                return false;
            }

            if (item.IsSuspicious())
            {
                return false;
            }

            if (item.PlayerIdentity != discharger.Identity) // item must be owned by the discharger
            {
                return false;
            }

            await discharger.UserPackage.UnEquipAsync(item.Position, UserPackage.RemovalType.RemoveAndDisappear);
            item.Position = Item.ItemPosition.Detained;
            await item.SaveAsync();

            detainLogger.LogInformation($"did:{discharger.Identity},dname:{discharger.Name},detid:{detainer.Identity},detname:{detainer.Name},itemid:{item.Identity},mapid:{discharger.MapIdentity}");

            var dbDetain = new DbDetainedItem
            {
                ItemIdentity = item.Identity,
                TargetIdentity = discharger.Identity,
                TargetName = discharger.Name,
                HunterIdentity = detainer.Identity,
                HunterName = detainer.Name,
                HuntTime = UnixTimestamp.Now,
                RedeemPrice = (ushort)GetDetainPrice(item)
            };
            if (!await ServerDbContext.SaveAsync(dbDetain))
            {
                return false;
            }

            await discharger.BroadcastRoomMsgAsync(new MsgAction
            {
                Identity = discharger.Identity,
                Data = dbDetain.Identity,
                X = discharger.X,
                Y = discharger.Y,
                Action = ActionType.ItemDetainedEx
            }, true);
            await discharger.SendAsync(new MsgAction
            {
                Identity = discharger.Identity,
                X = discharger.X,
                Y = discharger.Y,
                Action = ActionType.ItemDetained
            });
            long detainFloorId = IdentityManager.MapItem.GetNextIdentity;
            await discharger.BroadcastRoomMsgAsync(new MsgMapItem
            {
                Identity = (uint)detainFloorId,
                Itemtype = item.Type,
                MapX = (ushort)(discharger.X + 2),
                MapY = discharger.Y,
                Mode = DropType.DetainItem
            }, true);
            IdentityManager.MapItem.ReturnIdentity(detainFloorId);

            await discharger.SendAsync(new MsgDetainItemInfo(dbDetain, item, MsgDetainItemInfo.Mode.DetainPage));
            await detainer.SendAsync(new MsgDetainItemInfo(dbDetain, item, MsgDetainItemInfo.Mode.ClaimPage));

            if (Confiscator != null)
            {
                await discharger.SendAsync(string.Format(StrDropEquip, item.Name, detainer.Name, Confiscator.Name, Confiscator.X,
                                  Confiscator.Y), TalkChannel.Talk, Color.White);
                await detainer.SendAsync(string.Format(StrKillerEquip, discharger.Name), TalkChannel.Talk, Color.White);
            }

            return true;
        }

        /// <summary>
        ///     Claims an expired item or Conquer Points to the hunter.
        /// </summary>
        /// <returns>True if the item has been successfully claimed.</returns>
        public static async Task<bool> ClaimDetainRewardAsync(uint idDetain, Character user)
        {
            DbDetainedItem dbDetain = await DetainedItemRepository.GetByIdAsync(idDetain);
            if (dbDetain == null)
            {
                return false;
            }

            if (dbDetain.HunterIdentity != user.Identity)
            {
                return false;
            }

            if (dbDetain.ItemIdentity != 0 &&
                dbDetain.HuntTime + MsgDetainItemInfo.MAX_REDEEM_SECONDS > UnixTimestamp.Now)
            {
                return false;
            }

            if (!user.UserPackage.IsPackSpare(1))
            {
                await user.SendAsync(StrYourBagIsFull);
                return false;
            }

            if (dbDetain.ItemIdentity == 0) // Conquer Points
            {
                await user.AwardConquerPointsAsync(dbDetain.RedeemPrice);

                await BroadcastWorldMsgAsync(
                    string.Format(StrGetEmoneyBonus, dbDetain.HunterName, dbDetain.TargetName,
                                  dbDetain.RedeemPrice),
                    TalkChannel.Talk, Color.White);
            }
            else
            {
                DbItem dbItem = await ItemRepository.GetByIdAsync(dbDetain.ItemIdentity);
                if (dbItem == null)
                {
                    return false;
                }

                var item = new Item();
                if (!await item.CreateAsync(dbItem) || item.Position != Item.ItemPosition.Detained)
                {
                    return false;
                }

                item.PlayerIdentity = user.Identity;
                item.Position = Item.ItemPosition.Inventory;

                await user.UserPackage.AddItemAsync(item);

                await BroadcastWorldMsgAsync(
                    string.Format(StrGetEquipBonus, dbDetain.HunterName, dbDetain.TargetName, item.FullName),
                    TalkChannel.Talk, Color.White);
            }

            await ServerDbContext.DeleteAsync(dbDetain);
            return true;
        }

        /// <summary>
        ///     Claim a detained item back to it's owner.
        /// </summary>
        /// <returns>True if the item has been successfully detained.</returns>
        public static async Task<bool> ClaimDetainedItemAsync(uint idDetain, Character user)
        {
            DbDetainedItem dbDetain = await DetainedItemRepository.GetByIdAsync(idDetain);
            if (dbDetain == null)
            {
                return false;
            }

            if (dbDetain.TargetIdentity != user.Identity)
            {
                return false;
            }

            if (dbDetain.HuntTime + MsgDetainItemInfo.MAX_REDEEM_SECONDS < UnixTimestamp.Now)
            {
                return false;
            }

            if (!user.UserPackage.IsPackSpare(1))
            {
                await user.SendAsync(StrYourBagIsFull);
                return false;
            }

            if (dbDetain.ItemIdentity == 0)
            {
                return false;
            }

            DbItem dbItem = await ItemRepository.GetByIdAsync(dbDetain.ItemIdentity);
            if (dbItem == null)
            {
                return false;
            }

            var item = new Item();
            if (!await item.CreateAsync(dbItem) || item.Position != Item.ItemPosition.Detained)
            {
                return false;
            }

            if (!await user.SpendConquerPointsAsync(dbDetain.RedeemPrice))
            {
                await user.SendAsync(StrNotEnoughEmoney);
                return false;
            }

            item.Position = Item.ItemPosition.Inventory;
            await user.UserPackage.AddItemAsync(item);

            await BroadcastWorldMsgAsync(
                string.Format(StrRedeemEquip, user.Name, dbDetain.RedeemPrice, dbDetain.HunterName),
                TalkChannel.Talk, Color.White);

            dbDetain.ItemIdentity = 0;
            await ServerDbContext.SaveAsync(dbDetain);

            Character hunter = RoleManager.GetUser(dbDetain.HunterIdentity);
            if (hunter != null)
            {
                await hunter.SendAsync(new MsgItem
                {
                    Action = MsgItem.ItemActionType.RedeemEquipment,
                    Identity = dbDetain.Identity,
                    Command = dbDetain.TargetIdentity,
                    Argument2 = dbDetain.RedeemPrice
                });

                if (Confiscator != null)
                {
                    await hunter.SendAsync(string.Format(StrHasEmoneyBonus, dbDetain.TargetName, Confiscator.Name, Confiscator.X, Confiscator.Y), TalkChannel.Talk, Color.White);
                }
            }

            return true;
        }

        public static int GetDetainPrice(Item item)
        {
            var price = 10;

            if (item.GetQuality() == 9) // if super +500CPs
            {
                price += 50;
            }

            switch (item.Plus) // (+n)
            {
                case 1:
                    price += 1;
                    break;
                case 2:
                    price += 2;
                    break;
                case 3:
                    price += 5;
                    break;
                case 4:
                    price += 10;
                    break;
                case 5:
                    price += 30;
                    break;
                case 6:
                    price += 90;
                    break;
                case 7:
                    price += 270;
                    break;
                case 8:
                    price += 600;
                    break;
                case 9:
                case 10:
                case 11:
                case 12:
                    price += 1200;
                    break;
            }

            if (item.IsWeapon()) // if weapon
            {
                if (item.SocketTwo > Item.SocketGem.NoSocket)
                {
                    price += 100;
                }
                else if (item.SocketOne > Item.SocketGem.NoSocket)
                {
                    price += 10;
                }
            }
            else // if not
            {
                if (item.SocketTwo > Item.SocketGem.NoSocket)
                {
                    price += 150;
                }
                else if (item.SocketOne > Item.SocketGem.NoSocket)
                {
                    price += 500;
                }
            }

            if (item.Quench != null)
            {
                if (item.Quench.GetOriginalArtifact()?.IsPermanent == true)
                {
                    switch (item.Quench.GetOriginalArtifact().ItemStatus.Level)
                    {
                        case 1: price += 30; break;
                        case 2: price += 90; break;
                        case 3: price += 180; break;
                        case 4: price += 300; break;
                        case 5: price += 450; break;
                        case 6: price += 600; break;
                        case 7: price += 800; break;
                    }
                }

                if (item.Quench.GetOriginalRefinery()?.IsPermanent == true)
                {
                    switch (item.Quench.GetOriginalRefinery().ItemStatus.Level)
                    {
                        case 1: price += 30; break;
                        case 2: price += 90; break;
                        case 3: price += 200; break;
                        case 4: price += 400; break;
                        case 5: price += 600; break;
                    }
                }
            }

            return price * 5;
        }
    }
}
