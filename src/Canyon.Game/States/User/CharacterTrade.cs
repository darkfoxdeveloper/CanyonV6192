using Canyon.Database.Entities;
using Canyon.Game.Database;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States.Items;
using Canyon.Game.States.NPCs;
using Canyon.Game.States.World;
using System.Drawing;

namespace Canyon.Game.States.User
{
    public partial class Character
    {
        #region Booth

        public BoothNpc Booth { get; private set; }

        public async Task<bool> CreateBoothAsync()
        {
            if (Booth != null)
            {
                await Booth.LeaveMapAsync();
                Booth = null;
                return false;
            }

            if (Map?.IsBoothEnable() != true)
            {
                await SendAsync(Language.StrBoothRegionCantSetup);
                return false;
            }

            Booth = new BoothNpc(this);
            if (!await Booth.InitializeAsync())
            {
                return false;
            }

            await Booth.EnterMapAsync(); // remider: MUST BE executed in map queue
            return true;
        }

        public async Task<bool> DestroyBoothAsync()
        {
            if (Booth == null)
            {
                return false;
            }

            await Booth.LeaveMapAsync(); // remider: MUST BE executed in map queue
            Booth = null;
            return true;
        }

        public bool AddBoothItem(uint idItem, uint value, MsgItem.Moneytype type)
        {
            if (Booth == null)
            {
                return false;
            }

            if (!Booth.ValidateItem(idItem))
            {
                return false;
            }

            Item item = UserPackage[idItem];
            return Booth.AddItem(item, value, type);
        }

        public bool RemoveBoothItem(uint idItem)
        {
            if (Booth == null)
            {
                return false;
            }

            return Booth.RemoveItem(idItem);
        }

        public async Task<bool> SellBoothItemAsync(uint idItem, Character target)
        {
            if (Booth == null)
            {
                return false;
            }

            if (target.Identity == Identity)
            {
                return false;
            }

            if (!target.UserPackage.IsPackSpare(1))
            {
                return false;
            }

            if (GetDistance(target) > Screen.VIEW_SIZE)
            {
                return false;
            }

            if (!Booth.ValidateItem(idItem))
            {
                return false;
            }

            BoothItem item = Booth.QueryItem(idItem);
            var value = (int)item.Value;
            string moneyType = item.IsSilver ? Language.StrSilvers : Language.StrConquerPoints;
            if (item.IsSilver)
            {
                if (!await target.SpendMoneyAsync((int)item.Value, true))
                {
                    return false;
                }

                await AwardMoneyAsync(value);
            }
            else
            {
                if (!await target.SpendConquerPointsAsync((int)item.Value, true))
                {
                    return false;
                }

                await AwardConquerPointsAsync(value);
            }

            Booth.RemoveItem(idItem);

            await BroadcastRoomMsgAsync(new MsgItem(item.Identity, MsgItem.ItemActionType.BoothRemove)
            {
                Command = Booth.Identity
            }, true);
            await UserPackage.RemoveFromInventoryAsync(item.Item, UserPackage.RemovalType.RemoveAndDisappear);
            await target.UserPackage.AddItemAsync(item.Item);

            await SendAsync(string.Format(Language.StrBoothSold, target.Name, item.Item.Name, value, moneyType),
                            TalkChannel.Talk, Color.White);
            await target.SendAsync(string.Format(Language.StrBoothBought, item.Item.Name, value, moneyType),
                                   TalkChannel.Talk, Color.White);

            DbTrade trade = new()
            {
                Type = DbTrade.TradeType.Booth,
                UserIpAddress = Client.IpAddress,
                UserMacAddress = Client.MacAddress,
                TargetIpAddress = target.Client.IpAddress,
                TargetMacAddress = target.Client.MacAddress,
                MapIdentity = MapIdentity,
                TargetEmoney = item.IsSilver ? 0 : item.Value,
                TargetMoney = item.IsSilver ? item.Value : 0,
                UserEmoney = 0,
                UserMoney = 0,
                TargetIdentity = target.Identity,
                UserIdentity = Identity,
                TargetX = target.X,
                TargetY = target.Y,
                UserX = X,
                UserY = Y,
                Timestamp = DateTime.Now
            };

            if (!await ServerDbContext.SaveAsync(trade))
            {
                logger.LogWarning($"{item.Item.Identity},{item.Item.PlayerIdentity},{Identity},{item.Item.Type},{item.IsSilver},{item.Value},{item.Item.ToJson()}");
                return true;
            }

            DbTradeItem tradeItem = new()
            {
                TradeIdentity = trade.Identity,
                SenderIdentity = Identity,
                ItemIdentity = item.Identity,
                Itemtype = item.Item.Type,
                Chksum = (uint)item.Item.GetHashCode(),
                JsonData = item.Item.ToJson()
            };
            await ServerDbContext.SaveAsync(tradeItem);
            return true;
        }

        #endregion
    }
}
