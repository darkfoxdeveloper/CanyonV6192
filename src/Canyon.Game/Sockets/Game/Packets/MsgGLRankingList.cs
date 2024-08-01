using Canyon.Game.States.User;
using Canyon.Network.Packets;
using ProtoBuf;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgGLRankingList : MsgProtoBufBase<Client, MsgGLRankingList.GoldenLeagueRankingListData>
    {
        public MsgGLRankingList()
            : base(PacketType.MsgGLRankingList)
        {
        }

        public MsgGLRankingList(uint points, uint historyPoints)
            : base(PacketType.MsgGLRankingList)
        {
            Data = new GoldenLeagueRankingListData
            {
                Points = points,
                //HistoryPoints = points
            };
        }

        [ProtoContract]
        public struct GoldenLeagueRankingListData
        {
            [ProtoMember(1, IsRequired = true)]
            public uint Points { get; set; }
        }

        public override async Task ProcessAsync(Client client)
        {
            await client.SendAsync(new MsgGLRankingList(client.Character.GoldenLeaguePoints, 0));
        }
    }
}
