using Canyon.Game.States.User;
using Canyon.Network.Packets;
using ProtoBuf;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgExchangeShopBuy : MsgProtoBufBase<Client, MsgExchangeShopBuy.ExchangeShopBuyData>
    {
        public MsgExchangeShopBuy() 
            : base(PacketType.MsgExchangeShopBuy)
        {
        }

        [ProtoContract]
        public class ExchangeShopBuyData
        {
            [ProtoMember(1, IsRequired = true)]
            public uint NpcId { get; set; }
            [ProtoMember(2, IsRequired = true)]
            public uint Type { get; set; }//1 and 2 for others ranks?
            [ProtoMember(3, IsRequired = true)]
            public uint Count { get; set; }
            [ProtoMember(4, IsRequired = true)]
            public uint Index { get; set; }

            [ProtoMember(5, IsRequired = true)]
            public Item[] Items { get; set; }

            [ProtoContract]
            public class Item
            {
                [ProtoMember(1, IsRequired = true)]
                public uint ItemType { get; set; }
                [ProtoMember(2, IsRequired = true)]
                public uint Cost { get; set; }

            }
        }
    }
}
