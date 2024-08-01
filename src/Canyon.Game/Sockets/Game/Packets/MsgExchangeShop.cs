using Canyon.Game.States.User;
using Canyon.Network.Packets;
using ProtoBuf;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgExchangeShop : MsgProtoBufBase<Client, MsgExchangeShop.ExchangeShopData>
    {
        public MsgExchangeShop() 
            : base(PacketType.MsgExchangeShop)
        {
        }

        [ProtoContract]
        public struct ExchangeShopData
        {
            [ProtoMember(1, IsRequired = true)]
            public uint NpcId { get; set; }
            [ProtoMember(2, IsRequired = true)]
            public uint Enabled { get; set; }
            [ProtoMember(3, IsRequired = true)]
            public uint Identity { get; set; }
            [ProtoMember(4, IsRequired = true)]
            public uint Timer { get; set; }
            [ProtoMember(5, IsRequired = true)]
            public Goods[] Items { get; set; }

            [ProtoContract]
            public class Goods
            {
                [ProtoMember(1, IsRequired = true)]
                public uint ID { get; set; }
                [ProtoMember(2, IsRequired = true)]
                public uint Cost { get; set; }
            }
        }
    }
}
