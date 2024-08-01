using System.Collections.Concurrent;

namespace Canyon.GM.Server.Managers
{
    public static class UserManager
    {
        private static ConcurrentDictionary<uint, UserData> users = new();

        public static int UserCount => users.Count;
        public static int MaxUserOnline { get; private set; }

        public static bool AddUser(uint userId, uint accountId)
        {
            MaxUserOnline = Math.Max(MaxUserOnline, users.Count + 1);
            return users.TryAdd(userId, new UserData()
            {
                Id = userId,
                AccountId = accountId
            });
        }

        public static void RemoveUser(uint userId)
        {
            users.TryRemove(userId, out _);
        }

        public static void SetMaxOnlinePlayer(int maxOnlinePlayers)
        {
            MaxUserOnline = maxOnlinePlayers;
        }

        public static void DisconnectionClear()
        {
            users.Clear();
            MaxUserOnline = 0;
        }

        public class UserData
        {
            public uint Id { get; init; }
            public uint AccountId { get; init; }
        }
    }
}
