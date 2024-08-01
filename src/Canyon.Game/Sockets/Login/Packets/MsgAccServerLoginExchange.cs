using Canyon.Game.Services.Managers;
using Canyon.Game.States.Transfer;
using Canyon.Game.States.User;
using Canyon.Network.Packets.Login;
using System.Runtime.Caching;
using System.Security.Cryptography;

namespace Canyon.Game.Sockets.Login.Packets
{
    public sealed class MsgAccServerLoginExchange : MsgAccServerLoginExchange<LoginActor>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MsgAccServerLoginExchange>();

        public override async Task ProcessAsync(LoginActor client)
        {
            Character user = RoleManager.GetUserByAccount(Data.AccountId);
            if (user != null)
            {
                logger.LogWarning($"Login denied! User {Data.AccountId} already signed in.");
                await RoleManager.KickOutAsync(user.Identity, "Duplicated login.");
                await client.SendAsync(new MsgAccServerLoginExchangeEx
                {
                    Data = new MsgAccServerLoginExchangeEx<LoginActor>.LoginExchangeData
                    {
                        AccountId = Data.AccountId,
                        Request = Data.Request,
                        Response = MsgAccServerLoginExchangeEx.ALREADY_LOGGED_IN,
                        Token = 0
                    }
                });
                return;
            }

            // Generate the access token
            var bytes = new byte[8];
            var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            var token = BitConverter.ToUInt64(bytes);

            var args = new TransferAuthArgs
            {
                AccountID = Data.AccountId,
                AuthorityID = Data.AuthorityId,
                IPAddress = Data.IpAddress,
                VIPLevel = Data.VipLevel
            };
            // Store in the login cache with an absolute timeout
            var timeoutPolicy = new CacheItemPolicy { AbsoluteExpiration = DateTime.Now.AddSeconds(30) };
            Kernel.Logins.Set(token.ToString(), args, timeoutPolicy);

            await client.SendAsync(new MsgAccServerLoginExchangeEx
            {
                Data = new MsgAccServerLoginExchangeEx<LoginActor>.LoginExchangeData
                {
                    AccountId = Data.AccountId,
                    Request = Data.Request,
                    Response = MsgAccServerLoginExchangeEx.SUCCESS,
                    Token = token
                }
            });
        }
    }
}
