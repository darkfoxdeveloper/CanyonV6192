using Canyon.Database.Entities;
using Canyon.Game.Database;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States.Items;
using Canyon.Game.States.User;
using System.Collections.Concurrent;

namespace Canyon.Game.States
{
    public sealed class Trade
    {
        private const int MaxTradeItems = 20;
        private const int MaxTradeMoney = 1000000000;
        private const int MaxTradeEmoney = 1000000000;

        private readonly ConcurrentDictionary<uint, Item> itemsPlayer1 = new();
        private readonly ConcurrentDictionary<uint, Item> itemsPlayer2 = new();

        private uint money1, money2;
        private uint emoney1, emoney2;

        private bool accept1, accept2;

        public Trade(Character p1, Character p2)
        {
            User1 = p1;
            User2 = p2;

            User1.Trade = this;
            User2.Trade = this;
        }

        public Character User1 { get; }
        public Character User2 { get; }

        public bool Accepted => accept1 && accept2;

        public bool ContainsItem(uint idItem)
        {
            return itemsPlayer1.ContainsKey(idItem) || itemsPlayer2.ContainsKey(idItem);
        }

        public async Task<bool> AddItemAsync(uint idItem, Character sender)
        {
            if (sender.Identity != User1.Identity
                && sender.Identity != User2.Identity)
            {
                return false;
            }

            Character target = sender.Identity == User1.Identity ? User2 : User1;
            ConcurrentDictionary<uint, Item> items = User1.Identity == sender.Identity ? itemsPlayer1 : itemsPlayer2;

            Item item = sender.UserPackage[idItem];
            if (item == null)
            {
                await sender.SendAsync(StrNotToTrade);
                await sender.SendAsync(RemoveMsg(idItem));
                return false;
            }

            if (items.ContainsKey(idItem))
            {
                await sender.SendAsync(RemoveMsg(idItem));
                return false;
            }

            if (!sender.IsPm() && !target.IsPm())
            {
                if (item.IsMonopoly() || item.IsBound)
                {
                    await sender.SendAsync(StrNotToTrade);
                    await sender.SendAsync(RemoveMsg(idItem));
                    return false;
                }

                if (item.IsSuspicious())
                {
                    await sender.SendAsync(StrNotToTrade);
                    await sender.SendAsync(RemoveMsg(idItem));
                    return false;
                }
            }

            if (!sender.IsGm() && !target.IsGm())
            {
                if (item.IsLocked() && !sender.IsValidTradePartner(target.Identity))
                {
                    await sender.SendAsync(StrNotToTrade);
                    await sender.SendAsync(RemoveMsg(idItem));
                    return false;
                }
            }

            if (item.SyndicateIdentity != 0)
            {
                await sender.SendAsync(StrNotToTrade);
                await sender.SendAsync(RemoveMsg(idItem));
                return false;
            }

            if (sender.Booth?.QueryItem(item.Identity) != null)
            {
                await sender.SendAsync(StrNotToTrade);
                await sender.SendAsync(RemoveMsg(idItem));
                return false;
            }

            if (items.Count >= MaxTradeItems)
            {
                await sender.SendAsync(StrTradeSashFull);
                await sender.SendAsync(RemoveMsg(idItem));
                return false;
            }

            if (!target.UserPackage.IsPackSpare(items.Count + 1))
            {
                await target.SendAsync(StrTradeYourBagIsFull);
                await sender.SendAsync(StrTradeTargetBagIsFull);
                await sender.SendAsync(RemoveMsg(idItem));
                return false;
            }

            items.TryAdd(item.Identity, item);
            await target.SendAsync(new MsgItemInfo(item, MsgItemInfo.ItemMode.Trade));
            if (item.Quench != null)
            {
                await item.Quench.SendToAsync(target);
            }
            return true;
        }

        public async Task<bool> AddMoneyAsync(uint amount, Character sender)
        {
            if (sender.Identity != User1.Identity
                && sender.Identity != User2.Identity)
            {
                return false;
            }

            Character target = sender.Identity == User1.Identity ? User2 : User1;
            if (amount > MaxTradeMoney)
            {
                await sender.SendAsync(string.Format(StrTradeMuchMoney, MaxTradeMoney));
                await SendCloseAsync();
                return false;
            }

            if (sender.Silvers < amount)
            {
                await sender.SendAsync(StrNotEnoughMoney);
                await SendCloseAsync();
                return false;
            }

            if (sender.Identity == User1.Identity)
            {
                money1 = amount;
            }
            else
            {
                money2 = amount;
            }

            await target.SendAsync(new MsgTrade
            {
                Data = amount,
                Action = MsgTrade.TradeAction.ShowMoney
            });
            return true;
        }

        public async Task<bool> AddEmoneyAsync(uint amount, Character sender)
        {
            if (sender.Identity != User1.Identity
                && sender.Identity != User2.Identity)
            {
                return false;
            }

            Character target = sender.Identity == User1.Identity ? User2 : User1;

            if (amount > MaxTradeEmoney)
            {
                await sender.SendAsync(string.Format(StrTradeMuchEmoney, MaxTradeEmoney));
                await SendCloseAsync();
                return false;
            }

            if (sender.ConquerPoints < amount)
            {
                await sender.SendAsync(StrNotEnoughMoney);
                await SendCloseAsync();
                return false;
            }

            if (sender.Identity == User1.Identity)
            {
                emoney1 = amount;
            }
            else
            {
                emoney2 = amount;
            }

            await target.SendAsync(new MsgTrade
            {
                Data = amount,
                Action = MsgTrade.TradeAction.ShowConquerPoints
            });
            return true;
        }

        public async Task AcceptAsync(uint acceptId)
        {
            if (acceptId == User1.Identity)
            {
                accept1 = true;
                await User2.SendAsync(new MsgTrade
                {
                    Action = MsgTrade.TradeAction.Accept,
                    Data = acceptId
                });
            }
            else if (acceptId == User2.Identity)
            {
                accept2 = true;
                await User1.SendAsync(new MsgTrade
                {
                    Action = MsgTrade.TradeAction.Accept,
                    Data = acceptId
                });
            }

            if (!Accepted)
            {
                return;
            }

            bool success1 = itemsPlayer1.Values.All(x => User1.UserPackage[x.Identity] != null && !x.IsBound && !x.IsMonopoly());
            bool success2 = itemsPlayer2.Values.All(x => User2.UserPackage[x.Identity] != null && !x.IsBound && !x.IsMonopoly());

            bool success = success1 && success2;

            if (!User1.UserPackage.IsPackSpare(itemsPlayer2.Count))
            {
                success = false;
            }

            if (!User2.UserPackage.IsPackSpare(itemsPlayer1.Count))
            {
                success = false;
            }

            if (money1 > User1.Silvers || emoney1 > User1.ConquerPoints)
            {
                success = false;
            }

            if (money2 > User2.Silvers || emoney2 > User2.ConquerPoints)
            {
                success = false;
            }

            if (!success)
            {
                await SendCloseAsync();
                return;
            }

            var dbTrade = new DbTrade
            {
                Type = DbTrade.TradeType.Trade,
                UserIpAddress = User1.Client.IpAddress,
                UserMacAddress = User1.Client.MacAddress,
                TargetIpAddress = User2.Client.IpAddress,
                TargetMacAddress = User2.Client.MacAddress,
                MapIdentity = User1.MapIdentity,
                TargetEmoney = emoney2,
                TargetMoney = money2,
                UserEmoney = emoney1,
                UserMoney = money1,
                TargetIdentity = User2.Identity,
                UserIdentity = User1.Identity,
                TargetX = User2.X,
                TargetY = User2.Y,
                UserX = User1.X,
                UserY = User1.Y,
                Timestamp = DateTime.Now
            };
            await ServerDbContext.SaveAsync(dbTrade);

            await SendCloseAsync();

            await User1.SpendMoneyAsync((int)money1);
            await User2.AwardMoneyAsync((int)money1);

            await User2.SpendMoneyAsync((int)money2);
            await User1.AwardMoneyAsync((int)money2);

            await User1.SpendConquerPointsAsync((int)emoney1);
            await User2.AwardConquerPointsAsync((int)emoney1);

            await User2.SpendConquerPointsAsync((int)emoney2);
            await User1.AwardConquerPointsAsync((int)emoney2);

            var dbItemsRecordTrack = new List<DbTradeItem>(41);
            foreach (Item item in itemsPlayer1.Values)
            {
                if (item.IsMonopoly() || item.IsBound)
                {
                    continue;
                }

                if (item.IsSuperFlag())
                {
                    await item.ClearSuperFlagAsync();
                }

                await User1.UserPackage.RemoveFromInventoryAsync(item, UserPackage.RemovalType.RemoveAndDisappear);
                await User2.UserPackage.AddItemAsync(item);

                dbItemsRecordTrack.Add(new DbTradeItem
                {
                    TradeIdentity = dbTrade.Identity,
                    SenderIdentity = User1.Identity,
                    ItemIdentity = item.Identity,
                    Itemtype = item.Type,
                    Chksum = (uint)item.ToJson().GetHashCode(),
                    JsonData = item.ToJson()
                });
            }

            foreach (Item item in itemsPlayer2.Values)
            {
                if (item.IsMonopoly() || item.IsBound)
                {
                    continue;
                }

                if (item.IsSuperFlag())
                {
                    await item.ClearSuperFlagAsync();
                }

                await User2.UserPackage.RemoveFromInventoryAsync(item, UserPackage.RemovalType.RemoveAndDisappear);
                await User1.UserPackage.AddItemAsync(item);

                dbItemsRecordTrack.Add(new DbTradeItem
                {
                    TradeIdentity = dbTrade.Identity,
                    SenderIdentity = User2.Identity,
                    ItemIdentity = item.Identity,
                    Itemtype = item.Type,
                    Chksum = (uint)item.ToJson().GetHashCode(),
                    JsonData = item.ToJson()
                });
            }

            await ServerDbContext.SaveRangeAsync(dbItemsRecordTrack);

            await User1.SendAsync(StrTradeSuccess);
            await User2.SendAsync(StrTradeSuccess);
        }

        public async Task SendCloseAsync()
        {
            User1.Trade = null;
            User2.Trade = null;

            await User1.SendAsync(new MsgTrade
            {
                Action = MsgTrade.TradeAction.Fail,
                Data = User2.Identity
            });

            await User2.SendAsync(new MsgTrade
            {
                Action = MsgTrade.TradeAction.Fail,
                Data = User1.Identity
            });
        }

        private MsgTrade RemoveMsg(uint id)
        {
            return new MsgTrade
            {
                Action = MsgTrade.TradeAction.AddItemFail,
                Data = id
            };
        }
    }
}
