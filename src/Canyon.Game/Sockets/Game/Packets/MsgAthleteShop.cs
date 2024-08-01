using Canyon.Game.States.User;
using Canyon.Network.Packets;
using ProtoBuf;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgAthleteShop : MsgProtoBufBase<Client, MsgAthleteShop.AthleteShopData>
    {
        public MsgAthleteShop() 
            : base(PacketType.MsgAthleteShop)
        {
        }

        public MsgAthleteShop(uint athletePoint, uint historyPoints)
            : base(PacketType.MsgAthleteShop)
        {
            Data = new AthleteShopData
            {
                HonorPoints = athletePoint,
                HistoryHonorPoints = historyPoints
            };
        }

        [ProtoContract]
        public struct AthleteShopData
        {
            [ProtoMember(1)]
            public uint HonorPoints { get; set; }
            [ProtoMember(2)]
            public uint HistoryHonorPoints { get; set; }
        }

        public override Task ProcessAsync(Client client)
        {
            return client.SendAsync(new MsgAthleteShop(client.Character.HonorPoints, client.Character.HistoryHonorPoints));
        }
    }
}
