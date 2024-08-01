using Canyon.Game.Services.Managers;
using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgRank : MsgBase<Client>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MsgRank>();

        private const int RedRose = 30000002;
        private const int WhiteRose = 30000102;
        private const int Orchid = 30000202;
        private const int Tulip = 30000302;

        private const int DragonChi = 60000001;
        private const int PhoenixChi = 60000002;
        private const int TigerChi = 60000003;
        private const int TurtleChi = 60000004;

        public RequestType Mode { get; set; }
        public uint Identity { get; set; }
        public ushort Data1 { get; set; }
        public ushort PageNumber { get; set; }

        public List<string> Strings { get; set; } = new();
        public List<QueryStruct> Infos { get; set; } = new();

        public override void Decode(byte[] bytes)
        {
            using var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();             // 0
            Type = (PacketType)reader.ReadUInt16();  // 2
            Mode = (RequestType)reader.ReadUInt32(); // 4
            Identity = reader.ReadUInt32();           // 8
            Data1 = reader.ReadUInt16(); // 12
            PageNumber = reader.ReadUInt16();         // 14
        }

        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgRank);
            writer.Write((uint)Mode);     // 4
            writer.Write(Identity); // 8
            writer.Write(Data1); // 12
            writer.Write(PageNumber);      // 14
            if (Mode == RequestType.QueryInfo || Mode == RequestType.QueryIcon || Mode == RequestType.RequestRank)
            {
                writer.Write(Infos.Count);     // 16
                writer.Write(0);               // 20
                foreach (QueryStruct info in Infos)
                {
                    writer.Write(info.Type);     // 24
                    writer.Write(info.Amount);   // 32
                    writer.Write(info.Identity); // 40
                    writer.Write(info.Identity); // 44
                    writer.Write(info.Name, 16); // 48
                    writer.Write(info.Name, 16); // 64
                }
            }
            else
            {
                writer.Write(1);
                writer.Write(0);
            }
            return writer.ToArray();
        }

        public struct QueryStruct
        {
            public ulong Type;
            public ulong Amount;
            public uint Identity;
            public string Name;
        }

        public enum RankType : byte
        {
            Flower,
            ChiDragon,
            ChiPhoenix,
            ChiTiger,
            ChiTurtle
        }

        public enum RequestType
        {
            None,
            RequestRank,
            QueryInfo,
            QueryIcon = 5
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;
            switch (Mode)
            {
                case RequestType.RequestRank:
                    {
                        switch (Identity)
                        {
                            case RedRose:
                            case WhiteRose:
                            case Orchid:
                            case Tulip:
                            case RedRose + 400:
                            case WhiteRose + 400:
                            case Orchid + 400:
                            case Tulip + 400:
                                {
                                    await QueryFlowerRankingAsync(user, (int)Identity, PageNumber);
                                    break;
                                }
                            case DragonChi:
                            case PhoenixChi:
                            case TigerChi:
                            case TurtleChi:
                                {
                                    await FateManager.SendRankAsync(user, this, (RankType)(Identity % 10));
                                    break;
                                }
                            default:
                                {
                                    logger.LogWarning("{}", Identity);
                                    break;
                                }
                        }

                        break;
                    }

                case RequestType.QueryInfo:
                    {
                        FlowerManager.FlowerRankObject flowerToday = await FlowerManager.QueryFlowersAsync(user);
                        await user.SendAsync(new MsgFlower
                        {
                            Mode = user.Gender != 1 ? MsgFlower.RequestMode.QueryFlower : MsgFlower.RequestMode.QueryGift,
                            Identity = user.Identity,
                            RedRoses = user.FlowerRed,
                            RedRosesToday = flowerToday?.RedRoseToday ?? 0,
                            WhiteRoses = user.FlowerWhite,
                            WhiteRosesToday = flowerToday?.WhiteRoseToday ?? 0,
                            Orchids = user.FlowerOrchid,
                            OrchidsToday = flowerToday?.OrchidsToday ?? 0,
                            Tulips = user.FlowerTulip,
                            TulipsToday = flowerToday?.TulipsToday ?? 0
                        });

                        await user.SendAsync(new MsgRank
                        {
                            Mode = RequestType.QueryIcon,
                            Infos = new List<QueryStruct>()
                        });

                        List<FlowerManager.FlowerRankingStruct> roseRank;
                        List<FlowerManager.FlowerRankingStruct> lilyRank;
                        List<FlowerManager.FlowerRankingStruct> orchidRank;
                        List<FlowerManager.FlowerRankingStruct> tulipRank;

                        List<FlowerManager.FlowerRankingStruct> roseRankToday;
                        List<FlowerManager.FlowerRankingStruct> lilyRankToday;
                        List<FlowerManager.FlowerRankingStruct> orchidRankToday;
                        List<FlowerManager.FlowerRankingStruct> tulipRankToday;

                        if (user.Gender == 1)
                        {
                            roseRank = FlowerManager.GetFlowerRanking(MsgFlower.FlowerType.Kiss, 0, 100);
                            lilyRank = FlowerManager.GetFlowerRanking(MsgFlower.FlowerType.Love, 0, 100);
                            orchidRank = FlowerManager.GetFlowerRanking(MsgFlower.FlowerType.Tins, 0, 100);
                            tulipRank = FlowerManager.GetFlowerRanking(MsgFlower.FlowerType.Jade, 0, 100);

                            roseRankToday = FlowerManager.GetFlowerRankingToday(MsgFlower.FlowerType.Kiss, 0, 100);
                            lilyRankToday = FlowerManager.GetFlowerRankingToday(MsgFlower.FlowerType.Love, 0, 100);
                            orchidRankToday = FlowerManager.GetFlowerRankingToday(MsgFlower.FlowerType.Tins, 0, 100);
                            tulipRankToday = FlowerManager.GetFlowerRankingToday(MsgFlower.FlowerType.Jade, 0, 100);
                        }
                        else
                        {
                            roseRank = FlowerManager.GetFlowerRanking(MsgFlower.FlowerType.RedRose, 0, 100);
                            lilyRank = FlowerManager.GetFlowerRanking(MsgFlower.FlowerType.WhiteRose, 0, 100);
                            orchidRank = FlowerManager.GetFlowerRanking(MsgFlower.FlowerType.Orchid, 0, 100);
                            tulipRank = FlowerManager.GetFlowerRanking(MsgFlower.FlowerType.Tulip, 0, 100);

                            roseRankToday = FlowerManager.GetFlowerRankingToday(MsgFlower.FlowerType.RedRose, 0, 100);
                            lilyRankToday = FlowerManager.GetFlowerRankingToday(MsgFlower.FlowerType.WhiteRose, 0, 100);
                            orchidRankToday = FlowerManager.GetFlowerRankingToday(MsgFlower.FlowerType.Orchid, 0, 100);
                            tulipRankToday = FlowerManager.GetFlowerRankingToday(MsgFlower.FlowerType.Tulip, 0, 100);
                        }

                        int myRose = roseRank.FirstOrDefault(x => x.Identity == user.Identity).Position;
                        int myLily = lilyRank.FirstOrDefault(x => x.Identity == user.Identity).Position;
                        int myOrchid = orchidRank.FirstOrDefault(x => x.Identity == user.Identity).Position;
                        int myTulip = tulipRank.FirstOrDefault(x => x.Identity == user.Identity).Position;

                        int myRoseToday = roseRankToday.FirstOrDefault(x => x.Identity == user.Identity).Position;
                        int myLilyToday = lilyRankToday.FirstOrDefault(x => x.Identity == user.Identity).Position;
                        int myOrchidToday = orchidRankToday.FirstOrDefault(x => x.Identity == user.Identity).Position;
                        int myTulipToday = tulipRankToday.FirstOrDefault(x => x.Identity == user.Identity).Position;

                        uint rankType = 0;
                        uint amount = 0;
                        var rank = 0;

                        int sum = user.Gender == 1 ? 400 : 0;

                        var display = false;
                        if (myRoseToday < myRose && myRoseToday > 0 && myRoseToday <= 100)
                        {
                            rankType = (uint)(RedRose + sum);
                            amount = flowerToday?.RedRoseToday ?? 0;
                            rank = myRoseToday;
                            display = true;
                        }
                        else if (myRose > 0 && myRose <= 100)
                        {
                            rankType = (uint)(RedRose + sum);
                            amount = user.FlowerRed;
                            rank = myRose;
                            display = true;
                        }

                        MsgRank msg;
                        if (display)
                        {
                            msg = new MsgRank();
                            msg.Mode = RequestType.QueryInfo;
                            msg.Identity = rankType;
                            msg.Infos.Add(new QueryStruct
                            {
                                Type = 1,
                                Amount = amount,
                                Identity = user.Identity,
                                Name = user.Name
                            });
                            await user.SendAsync(msg);
                        }

                        display = false;
                        if (myLilyToday < myLily && myLilyToday > 0 && myLilyToday <= 100)
                        {
                            rankType = (uint)(WhiteRose + sum);
                            amount = flowerToday?.WhiteRoseToday ?? 0;
                            rank = myLilyToday;
                            display = true;
                        }
                        else if (myLily > 0 && myLily <= 100)
                        {
                            rankType = (uint)(WhiteRose + sum);
                            amount = user.FlowerWhite;
                            rank = myLily;
                            display = true;
                        }

                        if (display)
                        {
                            msg = new MsgRank();
                            msg.Mode = RequestType.QueryInfo;
                            msg.Identity = rankType;
                            msg.Infos.Add(new QueryStruct
                            {
                                Type = 1,
                                Amount = amount,
                                Identity = user.Identity,
                                Name = user.Name
                            });
                            await user.SendAsync(msg);
                        }

                        display = false;
                        if (myOrchidToday < myOrchid && myOrchidToday > 0 && myOrchidToday <= 100)
                        {
                            rankType = (uint)(Orchid + sum);
                            amount = flowerToday?.OrchidsToday ?? 0;
                            rank = myOrchidToday;
                            display = true;
                        }
                        else if (myOrchid > 0 && myOrchid <= 100)
                        {
                            rankType = (uint)(Orchid + sum);
                            amount = user.FlowerOrchid;
                            rank = myOrchid;
                            display = true;
                        }

                        if (display)
                        {
                            msg = new MsgRank();
                            msg.Mode = RequestType.QueryInfo;
                            msg.Identity = rankType;
                            msg.Infos.Add(new QueryStruct
                            {
                                Type = 1,
                                Amount = amount,
                                Identity = user.Identity,
                                Name = user.Name
                            });
                            await user.SendAsync(msg);
                        }

                        display = false;
                        if (myTulipToday < myTulip && myTulipToday > 0 && myTulipToday <= 100)
                        {
                            rankType = (uint)(Tulip + sum);
                            amount = flowerToday?.TulipsToday ?? 0;
                            rank = myTulipToday;
                            display = true;
                        }
                        else if (myTulip > 0 && myTulip <= 100)
                        {
                            rankType = (uint)(Tulip + sum);
                            amount = user.FlowerTulip;
                            rank = myTulip;
                            display = true;
                        }

                        if (display)
                        {
                            msg = new MsgRank();
                            msg.Mode = RequestType.QueryInfo;
                            msg.Identity = rankType;
                            msg.Infos.Add(new QueryStruct
                            {
                                Type = 1,
                                Amount = amount,
                                Identity = user.Identity,
                                Name = user.Name
                            });
                            await user.SendAsync(msg);
                        }

                        if (rankType != user.FlowerCharm)
                        {
                            user.FlowerCharm = rankType;
                            await user.Screen.SynchroScreenAsync();
                        }

                        await user.SendAsync(new MsgRank
                        {
                            Mode = RequestType.QueryIcon
                        });

                        if (user.Fate != null)
                        {
                            await user.Fate.SubmitRankAsync();
                        }
                        break;
                    }
                default:
                    {
                        logger.LogWarning($"MsgRank:{Mode} unhandled");
                        return;
                    }
            }
        }

        private async Task QueryFlowerRankingAsync(Character user, int flowerIdentity, int page)
        {
            int currentPosition = -1;
            List<FlowerManager.FlowerRankingStruct> ranking;
            switch (flowerIdentity)
            {
                case RedRose: // red rose
                    {
                        ranking = FlowerManager.GetFlowerRanking(MsgFlower.FlowerType.RedRose, 0, 100);
                        break;
                    }

                case WhiteRose: // white rose
                    {
                        ranking = FlowerManager.GetFlowerRanking(MsgFlower.FlowerType.WhiteRose, 0, 100);
                        break;
                    }

                case Orchid: // orchid
                    {
                        ranking = FlowerManager.GetFlowerRanking(MsgFlower.FlowerType.Orchid, 0, 100);
                        break;
                    }

                case Tulip: // tulip
                    {
                        ranking = FlowerManager.GetFlowerRanking(MsgFlower.FlowerType.Tulip, 0, 100);
                        break;
                    }

                case RedRose + 400:
                    {
                        ranking = FlowerManager.GetFlowerRanking(MsgFlower.FlowerType.Kiss, 0, 100);
                        break;
                    }

                case WhiteRose + 400:
                    {
                        ranking = FlowerManager.GetFlowerRanking(MsgFlower.FlowerType.Love, 0, 100);
                        break;
                    }

                case Orchid + 400:
                    {
                        ranking = FlowerManager.GetFlowerRanking(MsgFlower.FlowerType.Tins, 0, 100);
                        break;
                    }

                case Tulip + 400:
                    {
                        ranking = FlowerManager.GetFlowerRanking(MsgFlower.FlowerType.Jade, 0, 100);
                        break;
                    }

                default:
                    return;
            }

            currentPosition = ranking.FirstOrDefault(x => x.Identity == user.Identity).Position;
            if (currentPosition <= 0)
            {
                return;
            }

            const int maxPerPage = 10;
            int index = page * maxPerPage;
            var count = 0;

            if (index >= ranking.Count)
            {
                return;
            }

            Data1 = (ushort)ranking.Count;
            for (; index < ranking.Count && count < 10; index++, count++)
            {
                Infos.Add(new QueryStruct
                {
                    Type = (ulong)index + 1,
                    Amount = ranking[index].Value,
                    Identity = ranking[index].Identity,
                    Name = ranking[index].Name
                });
            }
            await user.SendAsync(this);
        }
    }
}
