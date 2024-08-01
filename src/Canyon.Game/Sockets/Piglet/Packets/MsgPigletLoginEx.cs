using Canyon.Game.Services.Managers;
using Canyon.Network.Packets.Piglet;

namespace Canyon.Game.Sockets.Piglet.Packets
{
    public sealed class MsgPigletLoginEx : MsgPigletLoginEx<PigletActor>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MsgPigletLoginEx>();

        public override async Task ProcessAsync(PigletActor client)
        {
            PigletClient.ConnectionStage = PigletClient.ConnectionState.Connected;
            PigletClient.Instance.Actor = client;
            logger.LogInformation($"GM Server connected!!!");

            await client.SendAsync(new MsgPigletRealmStatus
            {
                Data = new MsgPigletRealmStatus<PigletActor>.RealmStatusData
                {
                    Status = MaintenanceManager.ServerUp
                }
            });

            MsgPigletUserLogin userLogin = new MsgPigletUserLogin()
            {
                Data = new MsgPigletUserLogin<PigletActor>.UserLoginData()
                {
                    Users = new List<MsgPigletUserLogin<PigletActor>.UserData>(),
                    ServerSync = true
                },
            };
            foreach (var user in RoleManager.QueryUserSet())
            {
                if (userLogin.Data.Users.Count >= 50)
                {
                    await client.SendAsync(userLogin);
                    userLogin.Data.Users.Clear();
                }

                userLogin.Data.Users.Add(new MsgPigletUserLogin<PigletActor>.UserData
                {
                    AccountId = user.Client.AccountIdentity,
                    IsLogin = true,
                    UserId = user.Identity
                });
            }            
            if (userLogin.Data.Users.Count > 0)
            {
                await client.SendAsync(userLogin);
            }
        }
    }
}
