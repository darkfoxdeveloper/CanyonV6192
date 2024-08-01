using Canyon.Login.Managers;
using Canyon.Login.Repositories;
using Canyon.Login.States;
using Canyon.Network.Packets.Login;
using Canyon.Shared;
using Microsoft.Extensions.Logging;

namespace Canyon.Login.Sockets.Game.Packets
{
    public sealed class MsgAccServerAuth : MsgAccServerAuth<Realm>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MsgAccServerAuth>();

        public override async Task ProcessAsync(Realm client)
        {
            var realmData = await RealmsRepository.FindByIdAsync(Data.RealmId.ToString());
            if (realmData == null)
            {
                logger.LogWarning("Realm [{}] does not exist", Data.RealmId);
                await DenyWithMessageAsync(client, MsgAccServerAuthEx.REALM_DOES_NOT_EXIST);
                return;
            }

            if (!await RealmsRepository.ValidateRealmAsync(Guid.Parse(Data.RealmId), Data.Username, Data.Password))
            {
                logger.LogWarning("[{}] Invalid username or password", realmData.RealmName);
                await DenyWithMessageAsync(client, MsgAccServerAuthEx.INVALID_USERNAME_PASSWORD);
                return;
            }

#if DEBUG
            if (!realmData.GameIPAddress.StartsWith("192.168.")
                && !realmData.GameIPAddress.StartsWith("127."))
            {
                logger.LogDebug("DEBUG mode, do not connect to external addressess.");
                await DenyWithMessageAsync(client, MsgAccServerAuthEx.DEBUG_MODE);
                return;
            }
#else
            // TODO: Investigate why this is needed
            // if (!realmData.GameIPAddress.Equals(client.IpAddress))
            // {
            //     logger.LogWarning("[{}] realm IPAddress [{}] do not match target IPAddress [{}]", realmData.RealmName, client.IpAddress, realmData.GameIPAddress);
            //     await DenyWithMessageAsync(client, MsgAccServerAuthEx.UNAUTHORIZED_IP_ADDRESS);
            //     return;
            // }
#endif

            var oldRealm = RealmManager.GetRealm(realmData.RealmID);
            if (oldRealm != null)
            {
                oldRealm.Disconnect();
                logger.LogWarning("Duplicated login for realm [{}] from [{}]", realmData.RealmName, client.IpAddress);
            }

            client.Data = realmData;
            logger.LogInformation("Realm [{}] has connected", realmData.RealmName);
            RealmManager.AddRealm(client);
            await client.SendAsync(new MsgAccServerAuthEx
            {
                Data = new MsgAccServerAuthEx<Realm>.AuthExData
                {
                    RealmId = Data.RealmId,
                    ResponseStatus = MsgAccServerAuthEx.SUCCESS
                }
            });
        }

        private Task DenyWithMessageAsync(Realm actor, int message)
        {
            return actor.SendAsync(new MsgAccServerAuthEx
            {
                Data = new MsgAccServerAuthEx<Realm>.AuthExData
                {
                    RealmId = Data.RealmId,
                    ResponseStatus = message
                }
            },
            () =>
            {
                actor.Disconnect();
                return Task.CompletedTask;
            });
        }
    }
}