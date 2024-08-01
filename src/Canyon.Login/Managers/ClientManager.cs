using Canyon.Login.States;
using System.Collections.Concurrent;

namespace Canyon.Login.Managers
{
    public static class ClientManager
    {
        private static object clientLock = new();
        private static ConcurrentDictionary<Guid, Client> Clients { get; } = new ();

        public static bool AddClient(Client client)
        {
            lock (clientLock)
            {
                return Clients.TryAdd(client.Guid, client);
            }
        }

        public static bool HasClient(Guid idClient)
        {
            lock (clientLock)
            {
                return Clients.ContainsKey(idClient);
            }
        }

        public static Client GetClient(Guid guid)
        {
            lock (clientLock)
            {
                return Clients.TryGetValue(guid, out var client) ? client : null;
            }
        }

        public static bool RemoveClient(Guid idClient)
        {
            lock (clientLock) 
            {
                return Clients.TryRemove(idClient, out _);
            }            
        }
    }
}
