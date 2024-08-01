using Canyon.Database.Entities;
using Canyon.Game.Database;
using Canyon.Game.Database.Repositories;
using Canyon.Game.Sockets.Piglet;
using Canyon.Game.Sockets.Piglet.Packets;
using Canyon.Game.States;
using Canyon.Game.States.NPCs;
using Canyon.Game.States.User;
using Canyon.Game.States.World;
using Canyon.Network.Packets.Login;
using Canyon.Network.Packets.Piglet;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Canyon.Game.Services.Managers
{
    public class RoleManager
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<RoleManager>();
        private static readonly ConcurrentDictionary<uint, Character> userSet = new();
        private static readonly ConcurrentDictionary<uint, Role> roleSet = new();
        private static readonly ConcurrentDictionary<uint, DbSuperman> superman = new();
        private static readonly ConcurrentDictionary<uint, DbMonstertype> monsterTypes = new();
        private static readonly ConcurrentDictionary<uint, DbMonsterTypeMagic> monsterMagics = new();

        private static bool isShutdown;
        private static bool isMaintenanceEntrance;
        private static bool isCooperatorMode;

        public static int OnlineUniquePlayers => userSet.Values.Select(x => x.Client.MacAddress).Distinct().Count();
        public static int OnlinePlayers => userSet.Count;
        public static int RolesCount => roleSet.Count;

        public static int MaxOnlinePlayers { get; private set; }

        public static async Task InitializeAsync()
        {
            logger.LogInformation("Starting Role Manager");

            var supermen = await SupermanRepository.GetAsync();
            foreach (var superman in supermen)
            {
                RoleManager.superman.TryAdd(superman.UserIdentity, superman);
            }

            foreach (DbMonstertype mob in await MonsterypeRepository.GetAsync())
            {
                monsterTypes.TryAdd(mob.Id, mob);
            }

            foreach (DbMonsterTypeMagic magic in await MonsterTypeMagicRepository.GetAsync())
            {
                monsterMagics.TryAdd(magic.Id, magic);
            }
        }

        public static async Task<bool> LoginUserAsync(Client user)
        {
            if (isShutdown)
            {
                await user.DisconnectWithMessageAsync(MsgConnectEx<Client>.RejectionCode.ServerDown);
                return false;
            }

            if (isMaintenanceEntrance)
            {
                await user.DisconnectWithMessageAsync(MsgConnectEx<Client>.RejectionCode.ServerLocked);
                return false;
            }

            if (isCooperatorMode)
            {
                await user.DisconnectWithMessageAsync(MsgConnectEx<Client>.RejectionCode.NonCooperatorAccount);
                return false;
            }

            if (userSet.TryGetValue(user.Character.Identity, out Character concurrent))
            {
                logger.LogInformation($"User {user.Character.Identity} {user.Character.Name} tried to login an already connected client.");

                string message;
                if (user.IpAddress != concurrent.Client.IpAddress)
                {
                    message = StrAnotherLoginSameIp;
                }
                else
                {
                    message = StrAnotherLoginOtherIp;
                }

                await concurrent.Client.DisconnectWithMessageAsync(message);
                user.Disconnect();
                return false;
            }

            if (user.Character.IsPm() && user.AccountIdentity < 10_000)
            {
                await user.DisconnectWithMessageAsync(MsgConnectEx<Client>.RejectionCode.AccountLocked);
                logger.LogInformation($"{user.Character.Name} no administration account ID.");
                return false;
            }

            if (userSet.Count > ServerConfiguration.Configuration.Realm.MaxOnlinePlayers && user.AuthorityLevel <= 1 && !user.Character.IsGm())
            {
                await user.DisconnectWithMessageAsync(MsgConnectEx<Client>.RejectionCode.ServerFull);
                logger.LogInformation($"{user.Character.Name} tried to login and server is full.");
                return false;
            }

            userSet.TryAdd(user.Character.Identity, user.Character);
            roleSet.TryAdd(user.Character.Identity, user.Character);

            await user.Character.SetLoginAsync();

            logger.LogInformation($"{user.Character.Name} has logged in.");

            await user.Character.CheckFirstCreditAsync();

            if (PigletClient.Instance?.Actor != null)
            {
                await PigletClient.Instance.Actor.SendAsync(new MsgPigletUserLogin()
                {
                    Data = new MsgPigletUserLogin<PigletActor>.UserLoginData
                    {
                        Users = new List<MsgPigletUserLogin<PigletActor>.UserData>
                        {
                            new MsgPigletUserLogin<PigletActor>.UserData
                            {
                                AccountId = user.AccountIdentity,
                                UserId = user.Character.Identity,
                                IsLogin = true
                            }
                        },
                        MaxPlayerOnline = MaxOnlinePlayers
                    }
                });
            }

            await EventManager.OnLoginAsync(user.Character);

            if (OnlinePlayers > MaxOnlinePlayers)
            {
                MaxOnlinePlayers = OnlinePlayers;
            }

            return true;
        }

        public static void ForceLogoutUser(uint idUser)
        {
            userSet.TryRemove(idUser, out _);
            roleSet.TryRemove(idUser, out _);
        }

        public static void SetMaintenanceStart()
        {
            isMaintenanceEntrance = true;
        }

        public static void ToggleCooperatorMode()
        {
            isCooperatorMode = !isCooperatorMode;
            if (isCooperatorMode)
            {
                logger.LogInformation("Cooperator mode has been enabled! Only cooperators accounts will be able to login.");
            }
            else
            {
                logger.LogInformation("Cooperator mode has been disabled! All accounts are enabled to login.");
            }
        }

        public static async Task KickOutByAccountAsync(uint accountId, string reason)
        {
            Character player = userSet.Values.FirstOrDefault(x => x.Client?.AccountIdentity == accountId);
            if (player != null)
            {
                if (!userSet.TryGetValue(player.Identity, out _))
                {
                    logger.LogError("Request to disconnect account [{}] not in user dictionary.", accountId);
                }
                await player.Client.DisconnectWithMessageAsync(string.Format(StrKickout, reason));
                logger.LogInformation($"User {player.Name} has been disconnected: {reason}");
            }
        }

        public static async Task KickOutAsync(uint idUser, string reason = "")
        {
            if (userSet.TryGetValue(idUser, out Character user))
            {
                if (!user.IsDeleted)
                {
                    await user.SaveAsync();
                }
                await user.Client.DisconnectWithMessageAsync(string.Format(StrKickout, reason));
                logger.LogInformation($"User {user.Name} has been kicked: {reason}");
            }
        }

        public static async Task KickOutAllAsync(string reason = "", bool isShutdown = false)
        {
            if (isShutdown)
            {
                RoleManager.isShutdown = true;
            }

            foreach (Character user in userSet.Values)
            {
                await user.Client.DisconnectWithMessageAsync(string.Format(StrKickout, reason));
                logger.LogInformation($"User {user.Name} has been kicked (kickoutall): {reason}");
            }
        }

        public static Character GetUserByAccount(uint idAccount)
        {
            return userSet.Values.FirstOrDefault(x => x.Client?.AccountIdentity == idAccount);
        }

        public static Character GetUser(uint idUser)
        {
            return userSet.TryGetValue(idUser, out Character client) ? client : null;
        }

        public static Character GetUser(string name)
        {
            return userSet.Values.FirstOrDefault(x => x.Name == name);
        }

        public static int CountUserByMacAddress(string macAddress)
        {
            return userSet.Values.Count(x => macAddress.Equals(x.Client?.MacAddress, StringComparison.InvariantCultureIgnoreCase));
        }

        public static List<T> QueryRoleByMap<T>(uint idMap) where T : Role
        {
            return roleSet.Values.Where(x => x.MapIdentity == idMap && x is T).Cast<T>().ToList();
        }

        public static List<T> QueryRoleByType<T>() where T : Role
        {
            return roleSet.Values.Where(x => x is T).Cast<T>().ToList();
        }

        public static List<Character> QueryUserSetByMap(uint idMap)
        {
            return userSet.Values.Where(x => x.MapIdentity == idMap).ToList();
        }

        public static List<Character> QueryUserSet()
        {
            return userSet.Values.ToList();
        }

        /// <summary>
        ///     Attention, DO NOT USE to add <see cref="Character" />.
        /// </summary>
        public static bool AddRole(Role role)
        {
            return roleSet.TryAdd(role.Identity, role);
        }

        public static Role GetRole(uint idRole)
        {
            return roleSet.TryGetValue(idRole, out Role role) ? role : null;
        }

        public static List<Role> QueryRoles(Func<Role, bool> predicate)
        {
            return roleSet.Values.Where(predicate).ToList();
        }

        public static T GetRole<T>(uint idRole) where T : Role
        {
            return roleSet.TryGetValue(idRole, out Role role) ? role as T : null;
        }

        public static T GetRole<T>(Func<T, bool> predicate) where T : Role
        {
            return roleSet.Values
                            .Where(x => x is T)
                            .Cast<T>()
                            .FirstOrDefault(x => predicate != null && predicate(x));
        }

        public static T FindRole<T>(uint idRole) where T : Role
        {
            foreach (GameMap map in MapManager.GameMaps.Values)
            {
                var result = map.QueryRole<T>(idRole);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        public static T FindRole<T>(Func<T, bool> predicate) where T : Role
        {
            foreach (GameMap map in MapManager.GameMaps.Values)
            {
                T result = map.QueryRole(predicate);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        public static List<T> FindRoles<T>(Func<T, bool> predicate) where T : Role
        {
            List<T> result = new();
            foreach (GameMap map in MapManager.GameMaps.Values)
            {
                T role = map.QueryRole(predicate);
                if (role != null)
                {
                    result.Add(role);
                }
            }
            return result;
        }

        /// <summary>
        ///     Attention, DO NOT USE to remove <see cref="Character" />.
        /// </summary>
        public static bool RemoveRole(uint idRole)
        {
            return roleSet.TryRemove(idRole, out _);
        }

        public static async Task AddOrUpdateSupermanAsync(uint idUser, int amount)
        {
            if (!superman.TryGetValue(idUser, out var sm))
            {
                superman.TryAdd(idUser, sm = new DbSuperman
                {
                    UserIdentity = idUser,
                    Amount = (uint)amount
                });
                await ServerDbContext.CreateAsync(sm);
            }
            else
            {
                sm.Amount = (uint)amount;
                await ServerDbContext.SaveAsync(sm);
            }
        }

        public static int GetSupermanPoints(uint idUser)
        {
            return (int)(superman.TryGetValue(idUser, out var value) ? value.Amount : 0);
        }

        public static int GetSupermanRank(uint idUser)
        {
            int result = 1;
            foreach (var super in superman.Values.OrderByDescending(x => x.Amount))
            {
                if (super.UserIdentity == idUser)
                {
                    return result;
                }

                result++;
            }
            return result;
        }

        public static DbMonstertype GetMonstertype(uint type)
        {
            return monsterTypes.TryGetValue(type, out DbMonstertype mob) ? mob : null;
        }

        public static List<DbMonsterTypeMagic> GetMonsterMagics(uint type)
        {
            return monsterMagics.Values.Where(x => x.MonsterType == type).ToList();
        }

        public static bool IsValidName(string szName)
        {
            if (long.TryParse(szName, out _))
            {
                return false;
            }

            foreach (var c in szName)
            {
                if (c < ' ')
                {
                    return false;
                }

                switch (c)
                {
                    case ' ':
                    case ';':
                    case ',':
                    case '/':
                    case '\\':
                    case '=':
                    case '%':
                    case '@':
                    case '\'':
                    case '"':
                    case '[':
                    case ']':
                    case '?':
                    case '{':
                    case '}':
                        return false;
                }
            }

            string lower = szName.ToLower();
            return InvalidNames.All(part => !lower.Contains(part));
        }

        private static readonly string[] InvalidNames =
        {
            "{", "}", "[", "]", "(", ")", "\"", "[gm]", "[pm]", "'", "´", "`", "admin", "helpdesk", " ",
            "bitch", "puta", "whore", "ass", "fuck", "cunt", "fdp", "porra", "poha", "caralho", "caraio",
            "system", "allusers", "none"
        };

        public static long RoleTimerTicks { get; private set; }
        public static int ProcessedRoles { get; private set; }

        public static async Task OnRoleTimerAsync()
        {
            int processedRoles = 0;
            Stopwatch sw = Stopwatch.StartNew();
            foreach (var role in roleSet.Values.Where(x => x is not Character && x is not BaseNpc))
            {
                await role.OnTimerAsync();
                processedRoles++;
            }
            sw.Stop();
            ProcessedRoles = processedRoles;
            RoleTimerTicks = sw.ElapsedTicks;
        }

#if DEBUG

        public const uint FAKE_PLAYER_MIN_ID = 10_000_000;
        private static uint fakePlayerIdx = 1;

        public static async Task<Character> FakePlayerLoginAsync(int level, int profession, int previousProfession, int firstProfession)
        {            
            string name = $"Test{fakePlayerIdx:0000}";
            uint playerId = FAKE_PLAYER_MIN_ID + fakePlayerIdx++;
            int rate = await NextAsync(100);
            uint mesh;
            if (rate < 25)
            {
                mesh = 11003;
            }
            else if (rate < 50)
            {
                mesh = 11004;
            }
            else if (rate < 75)
            {
                mesh = 2012001;
            }
            else
            {
                mesh = 2012002;
            }
            DbCharacter player = new DbCharacter
            {
                Identity = playerId,
                Name = name,
                Level = (byte)level,
                Profession = (byte)profession,
                PreviousProfession = (byte)previousProfession,
                FirstProfession = (byte)firstProfession,
                Mesh = mesh,
                AccountIdentity = fakePlayerIdx,
                Strength = 255,
                Agility = 255,
                Vitality = 255,
                Spirit = 255,
                QuizPoints = (uint)await NextAsync(100000),
                MapID = 1002,
                X = 300,
                Y = 278
            };
            Character user = new Character(player, null);
            await user.UserPackage.InitializeAsync();
            await user.MagicData.InitializeAsync();

            userSet.TryAdd(user.Identity, user);
            roleSet.TryAdd(user.Identity, user);

            await user.EnterMapAsync();
            return user;
        }

#endif
    }
}
