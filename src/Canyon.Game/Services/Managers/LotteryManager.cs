using Canyon.Database.Entities;
using Canyon.Game.Database.Repositories;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States.User;

namespace Canyon.Game.Services.Managers
{
    public class LotteryManager
    {
        public static readonly int MAX_ATTEMPTS = 10;

        private static readonly ILogger logger = LogFactory.CreateLogger<LotteryManager>();
        private static readonly ILogger gmLogger = LogFactory.CreateGmLogger("lottery");

        private static readonly List<DbConfig> config = new();

        public static async Task<bool> InitializeAsync()
        {
            for (var i = 0; i < 5; i++)
            {
                int minConfig = 11000 + i * 10;
                int maxConfig = 11009 + i * 10;

                config.AddRange(await ConfigRepository.GetAsync(x => x.Type >= minConfig && x.Type <= maxConfig));
            }
            return true;
        }

        public static int GetMaxAttempts(Character user) 
        {
            return (int)(MAX_ATTEMPTS + MAX_ATTEMPTS * user.VipLevel);
        }

        public static async Task<bool> QueryPrizeAsync(Character user, int pool, bool retry)
        {
            List<DbLottery> allItems = await LotteryRepository.GetAsync();
            List<DbConfig> lotteryConfiguration = config.Where(x => x.Data1 == pool).ToList();

            var ranks = new List<LotteryRankTempInfo>();
            var chance = 0;
            foreach (DbConfig config in lotteryConfiguration.OrderBy(x => x.Data2))
            {
                chance += config.Data2;
                ranks.Add(new LotteryRankTempInfo
                {
                    Chance = chance,
                    Rank = config.Type % 10
                });
            }

            LotteryRankTempInfo tempRank = default;
            int rand = await NextAsync(chance);
            foreach (LotteryRankTempInfo rank in ranks)
            {
                if (rand <= rank.Chance)
                {
                    tempRank = rank;
                    break;
                }
            }

            chance = 0;
            var infos = new List<LotteryItemTempInfo>();
            foreach (DbLottery item in allItems.Where(x => x.Rank == tempRank.Rank && x.Color == pool))
            {
                chance += (int)item.Chance;
                infos.Add(new LotteryItemTempInfo
                {
                    Chance = chance,
                    ItemIdentity = item.ItemIdentity,
                    ItemName = item.Itemname,
                    Plus = item.Plus,
                    Color = item.Color,
                    SocketNum = item.SocketNum
                });
            }

            DbItemtype itemType = null;
            LotteryItemTempInfo reward = default;

            for (int i = 0; i < 20; i++)
            {
                rand = await NextAsync(chance);
                foreach (LotteryItemTempInfo info in infos.OrderBy(x => x.Chance))
                {
                    if (rand <= info.Chance)
                    {
                        reward = info;
                        break;
                    }
                }

                if (reward.ItemIdentity == user.LotteryTemporaryItem?.Type)
                {
                    i--;
                    continue;
                }

                itemType = ItemManager.GetItemtype(reward.ItemIdentity);
                if (itemType == null)
                {
                    logger.LogError($"Lottery failed, invalid itemtype {reward.ItemIdentity} [{reward.ItemName}]");
                    continue;
                }
            }

            if (itemType == null)
            {
                logger.LogError($"Lottery failed, invalid itemtype {reward.ItemIdentity} [{reward.ItemName}] after 20 times");
                return false;
            }

            var lottoItem = new DbItem
            {
                Type = reward.ItemIdentity,
                Amount = itemType.Amount,
                AmountLimit = itemType.AmountLimit,
                Magic3 = reward.Plus > 0 ? reward.Plus : itemType.Magic3,
                Gem1 = (byte)(reward.SocketNum > 0 ? 255 : 0),
                Gem2 = (byte)(reward.SocketNum > 1 ? 255 : 0),
                Color = 3,
                PlayerId = user.Identity,
                AccumulateNum = 1
            };

            gmLogger.LogInformation($"{user.Identity},{user.Name},{tempRank.Rank},{reward.Color},{lottoItem.Type},{lottoItem.Magic3},{lottoItem.Gem1},{lottoItem.Gem2}");

            user.LotteryLastColor = (byte)pool;
            user.LotteryLastRank = (byte)tempRank.Rank;
            user.LotteryLastItemName = reward.ItemName;
            user.LotteryTemporaryItem = lottoItem;

            if (!retry)
            {
                await user.SendAsync(new MsgLottery
                {
                    Addition = lottoItem.Magic3,
                    SocketOne = lottoItem.Gem1,
                    SocketTwo = lottoItem.Gem2,
                    Color = (byte)lottoItem.Color,
                    ItemType = lottoItem.Type,
                    Action = MsgLottery.LotteryRequest.Show
                });

                await user.Statistic.AddOrUpdateAsync(22, 0, user.Statistic.GetValue(22) + 1, true);
            }
            else
            {
                await user.SendAsync(new MsgLottery
                {
                    Addition = lottoItem.Magic3,
                    SocketOne = lottoItem.Gem1,
                    SocketTwo = lottoItem.Gem2,
                    Color = (byte)lottoItem.Color,
                    UsedChances = (byte)(user.LotteryRetries + 1 > 2 ? 2 : 1),
                    ItemType = lottoItem.Type,
                    Action = MsgLottery.LotteryRequest.Show
                });
            }
            return true;
        }

        private struct LotteryRankTempInfo
        {
            public int Chance { get; init; }
            public int Rank { get; init; }
        }

        private struct LotteryItemTempInfo
        {
            public int Chance { get; init; }
            public string ItemName { get; init; }
            public uint ItemIdentity { get; init; }
            public byte Color { get; init; }
            public byte SocketNum { get; init; }
            public byte Plus { get; init; }
        }
    }
}
