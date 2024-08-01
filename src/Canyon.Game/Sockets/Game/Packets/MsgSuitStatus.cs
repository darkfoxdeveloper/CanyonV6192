using Canyon.Game.Services.Managers;
using Canyon.Game.States.User;
using Canyon.Network.Packets;
using static Canyon.Game.Services.Managers.FlowerManager;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgSuitStatus : MsgBase<Client>
    {
        public int Action { get; set; }
        public int Unknown { get; set; }
        public int Data { get; set; }
        public int Param { get; set; }

        public override void Decode(byte[] bytes)
        {
            using var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Action = reader.ReadInt32();
            Unknown = reader.ReadInt32();
            Data = reader.ReadInt32();
            Param = reader.ReadInt32();
        }

        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgSuitStatus);
            writer.Write(Action); // 4
            writer.Write(Unknown); // 8
            writer.Write(Data); // 12
            writer.Write(Param); // 16
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            if (user.Gender != 2 || user.Transformation != null)
            {
                return;
            }

            Param = (int)user.Identity;
            if (Action == 2)
            {
                user.FairyType = 0;
                await user.BroadcastRoomMsgAsync(this, true);
                return;
            }

            List<FlowerRankingStruct> ranking;
            List<FlowerRankingStruct> rankingToday;

            switch (Data) // validate :]
            {
                case 1000: // RedRose
                    {
                        ranking = GetFlowerRanking(MsgFlower.FlowerType.RedRose, 0, 100);
                        rankingToday = GetFlowerRankingToday(MsgFlower.FlowerType.RedRose, 0, 100);
                        break;
                    }

                case 1002: // Orchids
                    {
                        ranking = GetFlowerRanking(MsgFlower.FlowerType.Orchid, 0, 100);
                        rankingToday = GetFlowerRankingToday(MsgFlower.FlowerType.Orchid, 0, 100);
                        break;
                    }

                case 1003: // Tulips
                    {
                        ranking = GetFlowerRanking(MsgFlower.FlowerType.Tulip, 0, 100);
                        rankingToday = GetFlowerRankingToday(MsgFlower.FlowerType.Tulip, 0, 100);
                        break;
                    }

                case 1001: // Lily
                    {
                        ranking = GetFlowerRanking(MsgFlower.FlowerType.WhiteRose, 0, 100);
                        rankingToday = GetFlowerRankingToday(MsgFlower.FlowerType.WhiteRose, 0, 100);
                        break;
                    }

                default:
                    {
                        return;
                    }
            }

            int myRank = ranking.FirstOrDefault(x => x.Identity == user.Identity).Position;
            int myRankToday = rankingToday.FirstOrDefault(x => x.Identity == user.Identity).Position;

            if ((myRank <= 0 || myRank > 100) && (myRankToday <= 0 || myRankToday > 100))
            {
                return; // not in top 100
            }

            // let's limit the amount of fairies (per type)
            int fairyCount = RoleManager.QueryUserSet().Count(x => x.FairyType == Data);
            if (fairyCount >= 3)
            {
                // message? na
                return;
            }

            if (user.FairyType != 0)
            {
                await user.BroadcastRoomMsgAsync(new MsgSuitStatus
                {
                    Action = 2,
                    Data = (int)user.FairyType,
                    Param = Param
                }, true);
            }

            user.FairyType = (uint)Data;
            await user.BroadcastRoomMsgAsync(this, true);
        }
    }
}
