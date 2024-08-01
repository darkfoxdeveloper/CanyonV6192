using Canyon.Database.Entities;
using Canyon.Game.Database.Repositories;
using Canyon.Game.Services.Managers;
using Canyon.Game.States;
using Canyon.Game.States.Transfer;
using Canyon.Game.States.User;
using Canyon.Network.Packets.Login;
using static Canyon.Game.Sockets.Game.Packets.MsgTalk;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgConnect : MsgConnect<Client>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MsgConnect>();
        private static readonly ILogger purchaseProcessorLogger = LogFactory.CreateLogger<MsgConnect>("credit_error");

        /// <summary>
        ///     Process can be invoked by a packet after decode has been called to structure
        ///     packet fields and properties. For the server implementations, this is called
        ///     in the packet handler after the message has been dequeued from the server's
        ///     <see cref="PacketProcessor{TClient}" />.
        /// </summary>
        /// <param name="client">Client requesting packet processing</param>
        public override async Task ProcessAsync(Client client)
        {
#if STRESS_TEST
            // bot stress test
            if ("BT".Equals(Language) && Version == 2)
            {
                uint idBotUser = (uint)Token;
                if (idBotUser is > 10_000_000 and <= 19_999_999)
                {
                    logger.LogInformation("Bot signin! Start connect of fake player... [{},{}]", Token, idBotUser);
                    DbCharacter currentBot = await CharacterRepository.FindAsync($"BOT[{idBotUser % 1000000}]");
                    if (currentBot == null)
                    {
                        currentBot = await MsgRegister.CreateBotAccountAsync(idBotUser);
                    }

                    client.Character = new Character(currentBot, client);

                    if (await RoleManager.LoginUserAsync(client))
                    {
                        client.Character.MateName = StrNone;
                        client.Character.VipLevel = 6;

                        await client.Character.UserPackage.InitializeAsync();
                        await client.Character.Statistic.InitializeAsync();
                        await client.Character.TaskDetail.InitializeAsync();

                        await client.SendAsync(LoginOk);

                        await client.SendAsync(new MsgServerInfo());
                        await client.SendAsync(new MsgUserInfo(client.Character));
                        await client.SendAsync(new MsgData(DateTime.Now));
                        await client.SendAsync(new MsgVipFunctionValidNotify() { Flags = (int)client.Character.UserVipFlag });
                    }
                }
                return;
            }
#endif

            var auth = Kernel.Logins.Get(Token.ToString()) as TransferAuthArgs;
            if (auth == null)
            {
                auth = Kernel.Logins.Get(((uint)Token).ToString()) as TransferAuthArgs;
                if (auth == null)
                {
                    await client.DisconnectWithMessageAsync(LoginInvalid);
                    logger.LogWarning("Invalid Login Token: {Token} from {IpAddress}", Token, client.IpAddress);
                    return;
                }
            }

            Kernel.Logins.Remove(Token.ToString());

            // Generate new keys and check for an existing character
            DbCharacter character = await CharacterRepository.FindAsync(auth.AccountID);
            client.AccountIdentity = auth.AccountID;
            client.AuthorityLevel = auth.AuthorityID;
            client.MacAddress = MacAddress;

#if DEBUG || TEST_SERVER
            if (client.AuthorityLevel < 2)
            {
                await client.DisconnectWithMessageAsync(MsgConnectEx<Client>.RejectionCode.NonCooperatorAccount);
                logger.LogWarning("{Identity} non cooperator account.", client.Identity);
                return;
            }
#endif

            if (character == null)
            {
                // Create a new character
                client.Creation = new Creation { AccountID = auth.AccountID, Token = (uint)Token };
                Kernel.Registration.Add(client.Creation.Token);
                await client.SendAsync(LoginNewRole);
            }
            else
            {
                if (RoleManager.CountUserByMacAddress(MacAddress) >= 3)
                {
                    await client.DisconnectWithMessageAsync(MsgConnectEx<Client>.RejectionCode.AccountMaxLoginAttempts);
                    logger.LogWarning($"User [{character.Name}] with Mac Address [{MacAddress}] and IP [{client.IpAddress}] exceding login limit.");
                    return;
                }

                // The character exists, so we will turn the timeout back.
                client.ReceiveTimeOutSeconds = 30; // 30 seconds or DC

                // Character already exists
                client.Character = new Character(character, client);
                if (await RoleManager.LoginUserAsync(client))
                {
                    client.Character.MateName = (await CharacterRepository.FindByIdentityAsync(client.Character.MateIdentity))?.Name ?? StrNone;
                    client.Character.VipLevel = (uint)auth.VIPLevel;

                    await client.Character.UserPackage.InitializeAsync();
                    await client.Character.Statistic.InitializeAsync();
                    await client.Character.TaskDetail.InitializeAsync();

                    await client.SendAsync(LoginOk);

                    await client.SendAsync(new MsgServerInfo());
                    await client.SendAsync(new MsgUserInfo(client.Character));
                    await client.SendAsync(new MsgData(DateTime.Now));
                    await client.SendAsync(new MsgVipFunctionValidNotify() { Flags = (int)client.Character.UserVipFlag });

#if DEBUG
                    await client.Character.SendAsync($"Server is running in DEBUG mode. Version: {Program.Version}", TalkChannel.Talk);
#endif
                }
            }
        }
    }
}
