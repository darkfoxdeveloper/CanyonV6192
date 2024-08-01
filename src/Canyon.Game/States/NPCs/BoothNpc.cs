using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States.Items;
using Canyon.Game.States.User;
using Canyon.Game.States.World;
using System.Collections.Concurrent;
using System.Drawing;

namespace Canyon.Game.States.NPCs
{
    public class BoothNpc : BaseNpc
    {
        private Npc ownerNpc;
        private Character owner;
        private readonly ConcurrentDictionary<uint, BoothItem> items = new();

        public BoothNpc(Character owner)
            : base(owner.Identity % 1000000 + owner.Identity / 1000000 * 100000)
        {
            this.owner = owner;
        }

        public override async Task<bool> InitializeAsync()
        {
            ownerNpc = owner.Screen.Roles.Values.FirstOrDefault(x => x is Npc && x.X == owner.X - 2 && x.Y == owner.Y) as Npc;
            if (ownerNpc == null)
            {
                return false;
            }

            idMap = owner.MapIdentity;
            currentX = (ushort)(owner.X + 1);
            currentY = owner.Y;

            Mesh = 406;
            Name = $"{owner.Name}`Shop";

            await owner.SetDirectionAsync(FacingDirection.SouthEast);
            await owner.SetActionAsync(EntityAction.Sit);

            return await base.InitializeAsync();
        }

        public override ushort Type => BOOTH_NPC;

        public string HawkMessage { get; set; }

        #region Items management

        public async Task QueryItemsAsync(Character requester)
        {
            if (GetDistance(requester) > Screen.VIEW_SIZE)
            {
                return;
            }

            foreach (BoothItem item in items.Values)
            {
                if (!ValidateItem(item.Identity))
                {
                    items.TryRemove(item.Identity, out _);
                    continue;
                }

                await requester.SendAsync(new MsgItemInfoEx(item) { TargetIdentity = Identity });
            }
        }

        public bool AddItem(Item item, uint value, MsgItem.Moneytype type)
        {
            var boothItem = new BoothItem();
            if (!boothItem.Create(item, Math.Min(value, int.MaxValue), type == MsgItem.Moneytype.Silver))
            {
                return false;
            }

            return items.TryAdd(boothItem.Identity, boothItem);
        }

        public BoothItem QueryItem(uint idItem)
        {
            return items.Values.FirstOrDefault(x => x.Identity == idItem);
        }

        public bool RemoveItem(uint idItem)
        {
            return items.TryRemove(idItem, out _);
        }

        public bool ValidateItem(uint id)
        {
            Item item = owner.UserPackage[id];
            if (item == null)
            {
                return false;
            }

            if (item.IsBound)
            {
                return false;
            }

            if (item.IsLocked())
            {
                return false;
            }

            if (item.IsSuspicious())
            {
                return false;
            }

            return true;
        }

        #endregion

        #region Enter and Leave Map

        public override Task EnterMapAsync()
        {
            return base.EnterMapAsync();
        }

        public override async Task LeaveMapAsync()
        {
            if (ownerNpc != null)
            {
                await owner.SetActionAsync(EntityAction.Stand);
                owner = null;
                ownerNpc = null;
            }

            items.Clear();
            await base.LeaveMapAsync();
        }

        #endregion

        #region Socket

        public override async Task SendSpawnToAsync(Character player)
        {
            await player.SendAsync(new MsgNpcInfoEx(this));

            if (!string.IsNullOrEmpty(HawkMessage))
            {
                await player.SendAsync(new MsgTalk(owner.Identity, TalkChannel.Vendor, Color.White,
                                                   HawkMessage));
            }
        }

        #endregion
    }
}
