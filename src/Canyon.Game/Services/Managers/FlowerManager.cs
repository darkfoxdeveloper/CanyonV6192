using Canyon.Database.Entities;
using Canyon.Game.Database;
using Canyon.Game.Database.Repositories;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States.User;
using System.Collections.Concurrent;

namespace Canyon.Game.Services.Managers
{
    public class FlowerManager
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<FlowerManager>();
        private static readonly ConcurrentDictionary<uint, FlowerRankObject> flowers = new();

        public static async Task InitializeAsync()
        {
            logger.LogInformation("Initializating flower manager");
            foreach (DbFlower flower in await FlowerRepository.GetAsync())
            {
                DbCharacter user = await CharacterRepository.FindByIdentityAsync(flower.UserId);
                if (user == null)
                {
                    continue;
                }

                var obj = new FlowerRankObject(flower, user);
                flowers.TryAdd(flower.UserId, obj);
            }
        }

        public static List<FlowerRankingStruct> GetFlowerRanking(MsgFlower.FlowerType type, int from = 0, int limit = 10)
        {
            int gender = type >= MsgFlower.FlowerType.RedRose && type <= MsgFlower.FlowerType.Tulip ? 2 : 1;

            var position = 1;
            switch (type)
            {
                case MsgFlower.FlowerType.RedRose:
                case MsgFlower.FlowerType.Kiss:
                    return flowers.Values.Where(x => (x.Mesh % 10000 - x.Mesh % 10) / 1000 == gender && x.RedRose > 0)
                                       .OrderByDescending(x => x.RedRose).Skip(from)
                                       .Take(limit).Select(x => new FlowerRankingStruct
                                       {
                                           Identity = x.UserIdentity,
                                           Name = x.Name,
                                           Profession = (ushort)x.Profession,
                                           Value = x.RedRose,
                                           Position = position++
                                       }).ToList();

                case MsgFlower.FlowerType.WhiteRose:
                case MsgFlower.FlowerType.Love:
                    return flowers.Values.Where(x => (x.Mesh % 10000 - x.Mesh % 10) / 1000 == gender && x.WhiteRose > 0)
                                       .OrderByDescending(x => x.WhiteRose)
                                       .Skip(from).Take(limit).Select(x => new FlowerRankingStruct
                                       {
                                           Identity = x.UserIdentity,
                                           Name = x.Name,
                                           Profession = (ushort)x.Profession,
                                           Value = x.WhiteRose,
                                           Position = position++
                                       }).ToList();

                case MsgFlower.FlowerType.Orchid:
                case MsgFlower.FlowerType.Tins:
                    return flowers.Values.Where(x => (x.Mesh % 10000 - x.Mesh % 10) / 1000 == gender && x.Orchids > 0)
                                       .OrderByDescending(x => x.Orchids).Skip(from)
                                       .Take(limit).Select(x => new FlowerRankingStruct
                                       {
                                           Identity = x.UserIdentity,
                                           Name = x.Name,
                                           Profession = (ushort)x.Profession,
                                           Value = x.Orchids,
                                           Position = position++
                                       }).ToList();

                case MsgFlower.FlowerType.Tulip:
                case MsgFlower.FlowerType.Jade:
                    return flowers.Values.Where(x => (x.Mesh % 10000 - x.Mesh % 10) / 1000 == gender && x.Tulips > 0)
                                       .OrderByDescending(x => x.Tulips).Skip(from)
                                       .Take(limit).Select(x => new FlowerRankingStruct
                                       {
                                           Identity = x.UserIdentity,
                                           Name = x.Name,
                                           Profession = (ushort)x.Profession,
                                           Value = x.Tulips,
                                           Position = position++
                                       }).ToList();

            }

            return new List<FlowerRankingStruct>();
        }

        public static List<FlowerRankingStruct> GetFlowerRankingToday(MsgFlower.FlowerType type, int from = 0,
                                                                      int limit = 10)
        {
            var position = 1;
            switch (type)
            {
                case MsgFlower.FlowerType.RedRose:
                    return flowers
                           .Values.Where(x => (x.Mesh % 10000 - x.Mesh % 10) / 1000 == 2 && x.RedRoseToday > 0)
                           .OrderByDescending(x => x.RedRoseToday).Skip(from)
                           .Take(limit).Select(x => new FlowerRankingStruct
                           {
                               Identity = x.UserIdentity,
                               Name = x.Name,
                               Profession = (ushort)x.Profession,
                               Value = x.RedRoseToday,
                               Position = position++
                           }).ToList();

                case MsgFlower.FlowerType.WhiteRose:
                    return flowers
                           .Values.Where(x => (x.Mesh % 10000 - x.Mesh % 10) / 1000 == 2 && x.WhiteRoseToday > 0)
                           .OrderByDescending(x => x.WhiteRoseToday)
                           .Skip(from).Take(limit).Select(x => new FlowerRankingStruct
                           {
                               Identity = x.UserIdentity,
                               Name = x.Name,
                               Profession = (ushort)x.Profession,
                               Value = x.WhiteRoseToday,
                               Position = position++
                           }).ToList();

                case MsgFlower.FlowerType.Orchid:
                    return flowers
                           .Values.Where(x => (x.Mesh % 10000 - x.Mesh % 10) / 1000 == 2 && x.OrchidsToday > 0)
                           .OrderByDescending(x => x.OrchidsToday).Skip(from)
                           .Take(limit).Select(x => new FlowerRankingStruct
                           {
                               Identity = x.UserIdentity,
                               Name = x.Name,
                               Profession = (ushort)x.Profession,
                               Value = x.OrchidsToday,
                               Position = position++
                           }).ToList();

                case MsgFlower.FlowerType.Tulip:
                    return flowers
                           .Values.Where(x => (x.Mesh % 10000 - x.Mesh % 10) / 1000 == 2 && x.TulipsToday > 0)
                           .OrderByDescending(x => x.TulipsToday).Skip(from)
                           .Take(limit).Select(x => new FlowerRankingStruct
                           {
                               Identity = x.UserIdentity,
                               Name = x.Name,
                               Profession = (ushort)x.Profession,
                               Value = x.TulipsToday,
                               Position = position++
                           }).ToList();
            }

            return new List<FlowerRankingStruct>();
        }

        public static async Task<FlowerRankObject> QueryFlowersAsync(Character user)
        {
            if (flowers.TryGetValue(user.Identity, out FlowerRankObject value))
            {
                return value;
            }

            if (flowers.TryAdd(user.Identity, value = new FlowerRankObject(user)))
            {
                await ServerDbContext.SaveAsync(value.GetDatabaseObject());
                return value;
            }

            return null;
        }

        public static async Task DailyResetAsync()
        {
            foreach (var flower in flowers.Values)
            {
                flower.RedRoseToday = 0;
                flower.WhiteRoseToday = 0;
                flower.OrchidsToday = 0;
                flower.TulipsToday = 0;
            }
            await ServerDbContext.SaveRangeAsync(flowers.Values.Select(x => x.GetDatabaseObject()).ToList());
        }

        public class FlowerRankObject
        {
            private readonly DbFlower flower;

            public FlowerRankObject(DbFlower flower, DbCharacter user)
            {
                this.flower = flower;

                if (user == null)
                {
                    return;
                }

                Mesh = user.Mesh;
                Name = user.Name;
                Level = user.Level;
                Profession = user.Profession;
                Metempsychosis = user.Rebirths;

                RedRose = user.FlowerRed;
                WhiteRose = user.FlowerWhite;
                Orchids = user.FlowerOrchid;
                Tulips = user.FlowerTulip;
            }

            public FlowerRankObject(Character user)
            {
                flower = new DbFlower
                {
                    UserId = user.Identity
                };

                Mesh = user.Mesh;
                Name = user.Name;
                Level = user.Level;
                Metempsychosis = user.Metempsychosis;
                Profession = user.Profession;

                RedRose = user.FlowerRed;
                WhiteRose = user.FlowerWhite;
                Orchids = user.FlowerOrchid;
                Tulips = user.FlowerTulip;
            }

            public uint UserIdentity => flower.UserId;
            public uint Mesh { get; }
            public string Name { get; }
            public int Level { get; }
            public int Profession { get; }
            public int Metempsychosis { get; }

            public uint RedRose { get; set; }
            public uint WhiteRose { get; set; }
            public uint Orchids { get; set; }
            public uint Tulips { get; set; }

            public uint RedRoseToday
            {
                get => flower.RedRose;
                set => flower.RedRose = value;
            }

            public uint WhiteRoseToday
            {
                get => flower.WhiteRose;
                set => flower.WhiteRose = value;
            }

            public uint OrchidsToday
            {
                get => flower.Orchids;
                set => flower.Orchids = value;
            }

            public uint TulipsToday
            {
                get => flower.Tulips;
                set => flower.Tulips = value;
            }

            public Task SaveAsync()
            {
                return ServerDbContext.SaveAsync(flower);
            }

            public DbFlower GetDatabaseObject()
            {
                return flower;
            }
        }

        public struct FlowerRankingStruct
        {
            public int Position;
            public uint Identity;
            public string Name;
            public ushort Profession;
            public uint Value;
        }
    }
}
