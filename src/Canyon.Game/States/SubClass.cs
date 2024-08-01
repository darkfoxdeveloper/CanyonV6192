using Canyon.Database.Entities;
using Canyon.Game.Database;
using Canyon.Game.Database.Repositories;
using Canyon.Game.Services.Managers;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States.User;
using System.Collections.Concurrent;

namespace Canyon.Game.States
{
    public sealed class SubClass
    {
        private readonly ConcurrentDictionary<AstProfType, AstProf> astProfs = new();

        private readonly Character user;

        public SubClass(Character owner)
        {
            user = owner;
        }

        public int Count => astProfs.Count;

        public async Task<bool> InitializeAsync()
        {
            var astProfs = await AstProfLevelRepository.GetAsync(user.Identity);
            foreach (var astProf in astProfs)
            {
                var type = (AstProfType)astProf.AstProf;
                if (!this.astProfs.ContainsKey(type))
                {
                    var prof = new AstProf(astProf);
                    this.astProfs.TryAdd(type, prof);
                }
            }

            foreach (var prof in this.astProfs.Values)
            {
                await user.SendAsync(new MsgSubPro
                {
                    Action = AstProfAction.Learn,
                    Class = prof.Class,
                    Level = prof.Level
                });

                await user.SendAsync(new MsgSubPro
                {
                    Action = AstProfAction.MartialPromoted,
                    Class = prof.Class,
                    Level = AstProfManager.GetRank(user, prof.Class)
                });
            }
            return true;
        }

        public async Task<bool> LearnAsync(AstProfType type, bool actionOnly = false)
        {
            if (astProfs.ContainsKey(type))
            {
                return false;
            }

            if (!actionOnly)
            {
                if (!await AstProfManager.CanInaugurateAsync(user, type))
                {
                    return false;
                }
            }

            var profession = new AstProf(type, user.Identity);
            if (!astProfs.TryAdd(type, profession))
            {
                return false;
            }

            await profession.SaveAsync();

            AstProfManager.SetRank(user, type, 1);
            await user.SaveAsync();

            await user.SendAsync(new MsgSubPro
            {
                Action = AstProfAction.Learn,
                Class = type
            });

            await SendAsync();

            if (astProfs.Count == 1)
            {
                await ActivateAsync(type);
            }

            return true;
        }

        public async Task<bool> UpLevAsync(AstProfType type)
        {
            if (!astProfs.ContainsKey(type))
            {
                return false;
            }

            var astProf = astProfs[type];
            if (!await AstProfManager.CanUpLevAsync(user, type, astProf.Level))
            {
                return false;
            }

            astProf.Level += 1;
            await astProf.SaveAsync();
            await user.SendAsync(new MsgSubPro
            {
                Action = AstProfAction.MartialUplev,
                Class = type
            });
            return true;
        }

        public async Task<bool> PromoteAsync(AstProfType type)
        {
            if (!astProfs.TryGetValue(type, out var profession) || AstProfManager.GetRank(user, type) >= profession.Level)
            {
                return false;
            }

            if (!AstProfManager.CanPromote(user, type, profession.Level))
            {
                return false;
            }

            int nextRank = AstProfManager.GetRank(user, type) + 1;
            if (nextRank > 9)
            {
                return false;
            }

            AstProfManager.SetRank(user, type, (byte)nextRank);
            await user.SaveAsync();

            await user.SendAsync(new MsgSubPro
            {
                Action = AstProfAction.MartialPromoted,
                Class = type,
                Level = (byte)nextRank
            });

            await SendAsync();
            return true;
        }

        public async Task<bool> PromoteAsync(AstProfType type, int rank)
        {
            if (!astProfs.TryGetValue(type, out var profession) || AstProfManager.GetRank(user, type) >= profession.Level)
            {
                return false;
            }

            if (!AstProfManager.CanPromote(user, type, profession.Level))
            {
                return false;
            }

            if (rank > 9)
            {
                return false;
            }

            AstProfManager.SetRank(user, type, (byte)rank);
            await user.SaveAsync();

            await user.SendAsync(new MsgSubPro
            {
                Action = AstProfAction.MartialPromoted,
                Class = type,
                Level = (byte)rank
            });

            await SendAsync();
            return true;
        }

        public async Task<bool> ActivateAsync(AstProfType type)
        {
            if (type != AstProfType.None && !astProfs.ContainsKey(type))
            {
                return false;
            }

            user.AstProfType = type;
            await user.SendAsync(new MsgSubPro
            {
                Action = AstProfAction.Activate,
                Class = type
            });
            return true;
        }

        public int GetPower(AstProfType type)
        {
            return AstProfManager.GetPower(type, AstProfManager.GetRank(user, type));
        }

        public int GetLevel(AstProfType type)
        {
            return astProfs.TryGetValue(type, out var value) ? value.Level : 0;
        }

        public int GetPromotion(AstProfType type)
        {
            return AstProfManager.GetRank(user, type);
        }

        public Task SendAsync()
        {
            MsgSubPro msg = new();
            msg.Action = AstProfAction.ShowGui;
            msg.Points = user.StudyPoints;
            foreach (var prof in astProfs.Values)
            {
                msg.Professions.Add(new AstProfStruct
                {
                    Class = prof.Class,
                    Rank = prof.Level,
                    Level = AstProfManager.GetRank(user, prof.Class)
                });
            }
            return user.SendAsync(msg);
        }

        public Task SendStudyAsync()
        {
            return user.SendAsync(new MsgSubPro
            {
                Action = AstProfAction.UpdateStudy,
                Study = user.StudyPoints
            });
        }

        public class AstProf
        {
            public AstProf(AstProfType type, uint owner)
            {
                database = new DbAstProfLevel
                {
                    AstProf = (byte)type,
                    Level = 1,
                    UserIdentity = owner
                };
            }

            public AstProf(DbAstProfLevel db)
            {
                database = db;
            }

            public AstProfType Class { get => (AstProfType)database.AstProf; }
            public byte Level { get => database.Level; set => database.Level = value; }

            private readonly DbAstProfLevel database;

            public Task SaveAsync() => ServerDbContext.SaveAsync(database);
            public Task DeleteAsync() => ServerDbContext.DeleteAsync(database);
        }
    }
}
