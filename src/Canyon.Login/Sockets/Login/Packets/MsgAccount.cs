using Canyon.Login.Database.Entities;
using Canyon.Login.Managers;
using Canyon.Login.Repositories;
using Canyon.Login.Sockets.Game.Packets;
using Canyon.Login.States;
using Canyon.Login.States.Requests;
using Canyon.Login.States.Responses;
using Canyon.Network.Packets.Login;
using Canyon.Shared;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Canyon.Login.Sockets.Login.Packets
{
    public sealed class MsgAccount : MsgAccount<Client>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MsgAccount>();

        public async override Task ProcessAsync(Client client)
        {
            GameAccountLoginResponse response;
#if !USE_MYSQL_DB
            try
            {
                response = await Kernel.RestClient.PostAsync<GameAccountLoginResponse>($"{Kernel.ServerConfiguration.Authentication.Url}/api/auth/login", new GameAccountLoginRequest
                {
                    UserName = Username,
                    Password = DecryptPassword(Password, client.Seed),
                    ServerName = Realm
                });
            }
            catch (Exception ex)
            {
                await client.DisconnectWithRejectionCodeAsync(MsgConnectEx<Client>.RejectionCode.ServerTimedOut);
                logger.LogCritical(ex, "{}", ex.Message);
                return;
            }
#else
            response = new()
            {
                Success = true
            };
            GameAccount gameAccount;
            try
            {
                gameAccount = AccountRepository.GetByUsername(Username);
            }
            catch (Exception ex)
            {
                await client.DisconnectWithRejectionCodeAsync(MsgConnectEx<Client>.RejectionCode.ServerTimedOut);
                logger.LogCritical(ex, "{}", ex.Message);
                return;
            }
            if (gameAccount != null)
            {
                string password = Encoding.UTF8.GetString(Password);// DecryptPassword(Password, client.Seed);
                if (!GameAccount.HashPassword(password, gameAccount.Salt).Equals(gameAccount.Password))
                {
                    response.Success = false;
                }

                response.AccountAuthority = gameAccount.AuthorityId;
                // add ban logic here
                response.IsBanned = false;
                response.IsPermanentlyBanned = false;
                response.IsLocked = false;

                GameAccountVip gameAccountVip = VipRepository.GetAccountVip((uint)gameAccount.Id);
                if (gameAccountVip != null)
                {
                    response.VIPLevel = gameAccountVip.VipLevel;
                }
                response.AccountId = gameAccount.Id;
            }
#endif

            if (!response.Success)
            {
                logger.LogInformation("User [{}] attempt failed: invalid password", Username);
                await client.DisconnectWithRejectionCodeAsync(MsgConnectEx<Client>.RejectionCode.InvalidPassword);
                return;
            }

            if (response.IsPermanentlyBanned)
            {
                logger.LogInformation("User [{}] attempt failed: permanent ban", Username);
                await client.DisconnectWithRejectionCodeAsync(MsgConnectEx<Client>.RejectionCode.AccountBanned);
                return;
            }

            if (response.IsBanned)
            {
                logger.LogInformation("User [{}] attempt failed: banned", Username);
                await client.DisconnectWithRejectionCodeAsync(MsgConnectEx<Client>.RejectionCode.AccountBanned);
                return;
            }

            if (response.IsLocked)
            {
                logger.LogInformation("User [{}] attempt failed: locked", Username);
                await client.DisconnectWithRejectionCodeAsync(MsgConnectEx<Client>.RejectionCode.AccountLocked);
                return;
            }

#if DEBUG
            // only testing
            if (response.AccountAuthority == 1)
            {
                logger.LogInformation("User [{}] attempt failed: non cooperator account", Username);
                await client.DisconnectWithRejectionCodeAsync(MsgConnectEx<Client>.RejectionCode.NonCooperatorAccount);
                return;
            }
#endif

            client.AccountID = (uint)response.AccountId;
            client.Username = Username;

            var realm = RealmManager.GetRealm(Realm);
            if (realm == null)
            {
                logger.LogInformation("User [{}] realm '{}' not configured", Username, Realm);
                await client.DisconnectWithRejectionCodeAsync(MsgConnectEx<Client>.RejectionCode.ServerDown);
                return;
            }

            if (!realm.Data.Active)
            {
                logger.LogInformation("User [{}] realm '{}' not active", Username, realm.Data.RealmName);
                await client.DisconnectWithRejectionCodeAsync(MsgConnectEx<Client>.RejectionCode.ServerLocked);
                return;
            }

            client.Realm = realm;

            ClientManager.AddClient(client);

            await realm.SendAsync(new MsgAccServerLoginExchange
            {
                Data = new MsgAccServerLoginExchange<Realm>.LoginExchangeData
                {
                    AccountId = (uint)response.AccountId,
                    IpAddress = client.IpAddress,
                    Request = client.Guid.ToString(),
                    VipLevel = response.VIPLevel,
                    AuthorityId = (ushort)response.AccountAuthority
                }
            });

            logger.LogInformation("User [{}] is awaiting for game server approval", Username);
        }
    }
}
