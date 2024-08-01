using Canyon.Game.States.Items;
using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgItemInfoEx : MsgBase<Client>
    {
        public MsgItemInfoEx(Item item, ViewMode mode)
        {
            Identity = item.Identity;
            TargetIdentity = item.PlayerIdentity;
            ItemType = item.Type;
            Amount = item.Durability;
            AmountLimit = item.MaximumDurability;
            Position = (ushort)item.Position;
            SocketOne = (byte)item.SocketOne;
            SocketTwo = (byte)item.SocketTwo;
            Effect = (byte)item.Effect;
            Addition = item.Plus;
            Blessing = (byte)item.Blessing;
            IsBound = item.IsBound;
            Enchantment = item.Enchantment;
            IsSuspicious = item.IsSuspicious();
            IsLocked = item.IsLocked();
            Color = (byte)item.Color;
            Mode = mode;
            SocketProgress = item.SocketProgress;
            CompositionProgress = item.CompositionProgress;
            if (item.RemainingSeconds != 0)
            {
                RemainingTime = item.RemainingSeconds;
            }
            else if (item.DeleteTime != 0)
            {
                SaveTime = item.DeleteTime;
            }
            StackAmount = (int)item.AccumulateNum;
            IsInscribed = item.SyndicateIdentity != 0;
            Purification = 0;
        }

        public MsgItemInfoEx(BoothItem item)
        {
            Identity = item.Identity;
            TargetIdentity = item.Item.PlayerIdentity;
            ItemType = item.Item.Type;
            Amount = item.Item.Durability;
            AmountLimit = item.Item.MaximumDurability;
            Position = (ushort)Item.ItemPosition.Inventory;
            SocketOne = (byte)item.Item.SocketOne;
            SocketTwo = (byte)item.Item.SocketTwo;
            Effect = (byte)item.Item.Effect;
            Addition = item.Item.Plus;
            Blessing = (byte)item.Item.Blessing;
            Enchantment = item.Item.Enchantment;
            Color = (byte)item.Item.Color;
            Mode = item.IsSilver ? ViewMode.Silvers : ViewMode.Emoney;
            Price = item.Value;
            SocketProgress = item.Item.SocketProgress;
            CompositionProgress = item.Item.CompositionProgress;
            if (item.Item.RemainingSeconds != 0)
            {
                RemainingTime = item.Item.RemainingSeconds;
            }
            else if (item.Item.DeleteTime != 0) 
            {
                SaveTime = item.Item.DeleteTime;
            }
        }

        public uint Identity { get; set; }
        public uint TargetIdentity { get; set; }
        public ulong Price { get; set; }
        public uint ItemType { get; set; }
        public ushort Amount { get; set; }
        public ushort AmountLimit { get; set; }
        public ViewMode Mode { get; set; }
        public ushort Position { get; set; }
        public uint SocketProgress { get; set; }
        public byte SocketOne { get; set; }
        public byte SocketTwo { get; set; }
        public byte Effect { get; set; }
        public byte Addition { get; set; }
        public byte Blessing { get; set; }
        public bool IsBound { get; set; }
        public byte Enchantment { get; set; }
        public bool IsSuspicious { get; set; }
        public bool IsLocked { get; set; }
        public uint CompositionProgress { get; set; }
        public bool IsInscribed { get; set; }
        public byte Color { get; set; }
        public int RemainingTime { get; set; }
        public int SaveTime { get; set; }
        public int StackAmount { get; set; }
        public uint Purification { get; set; }

        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgItemInfoEx);
            writer.Write(Identity);             // 4
            writer.Write(TargetIdentity);       // 8
            writer.Write(Price);                // 12
            writer.Write(ItemType);             // 20
            writer.Write(Amount);               // 24
            writer.Write(AmountLimit);          // 26
            writer.Write((ushort)Mode);         // 28
            writer.Write((byte)Position);       // 30
            writer.Write(SocketProgress);       // 31
            writer.Write(SocketOne);            // 35
            writer.Write(SocketTwo);            // 36
            writer.Write(Effect);               // 37
            writer.Write(new byte[4]);          // 38
            writer.Write(Addition);             // 42
            writer.Write(Blessing);             // 43
            writer.Write(IsBound);              // 44
            writer.Write(Enchantment);          // 45
            writer.Write(new byte[5]);          // 46
            writer.Write(IsSuspicious);         // 51
            writer.Write(IsLocked);             // 52
            writer.Write((byte)0);              // 53
            writer.Write((ushort)Color);        // 54
            writer.Write(CompositionProgress);  // 56
            writer.Write(IsInscribed ? 1 : 0);  // 60 Is Inscribed?
            writer.Write(RemainingTime);        // 64
            writer.Write(SaveTime);             // 68
            writer.Write(StackAmount);          // 72
            writer.Write(Purification);         // 76
            return writer.ToArray();
        }

        public enum ViewMode : ushort
        {
            None,
            Silvers,
            Unknown,
            Emoney,
            ViewEquipment
        }
    }
}
