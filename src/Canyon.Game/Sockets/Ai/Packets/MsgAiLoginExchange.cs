using Canyon.Game.Services.Managers;
using Canyon.Game.States;
using Canyon.Network.Packets.Ai;

namespace Canyon.Game.Sockets.Ai.Packets
{
    public sealed class MsgAiLoginExchange : MsgAiLoginExchange<AiClient>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MsgAiLoginExchange>();

        public override async Task ProcessAsync(AiClient client)
        {
            logger.LogInformation($"Received auth data from {client.GUID}");
            if (client.Stage != AiClient.ConnectionStage.AwaitingAuth)
            {
                logger.LogWarning($"This Npc Server is already signed in!");
                await client.SendAsync(new MsgAiLoginExchangeEx
                {
                    Result = MsgAiLoginExchangeEx<AiClient>.AiLoginResult.AlreadySignedIn
                });
                return;
            }

            if (!UserName.Equals(ServerConfiguration.Configuration.Ai.Username) ||
                !Password.Equals(ServerConfiguration.Configuration.Ai.Password))
            {
                logger.LogError($"Invalid username or password!!!");
                await client.SendAsync(new MsgAiLoginExchangeEx
                {
                    Result = MsgAiLoginExchangeEx<AiClient>.AiLoginResult.AlreadySignedIn
                });
                return;
            }

            if (NpcServer.NpcClient != null && !NpcServer.NpcClient.GUID.Equals(client.GUID))
            {
                logger.LogWarning($"NPC Server is already connected...");
                await client.SendAsync(new MsgAiLoginExchangeEx
                {
                    Result = MsgAiLoginExchangeEx<AiClient>.AiLoginResult.AlreadyBound
                });
                return;
            }

            client.Stage = AiClient.ConnectionStage.Authenticated;
            logger.LogWarning($"NPC Server login OK...");

            Kernel.Services.Processor.Queue(0, () =>
            {
                foreach (var mob in RoleManager.QueryRoles(x => x is Monster monster && monster.GeneratorId != 0))
                {
                    mob.QueueAction(mob.LeaveMapAsync);
                }
                return Task.CompletedTask;
            });

            await client.SendAsync(new MsgAiLoginExchangeEx
            {
                Result = MsgAiLoginExchangeEx<AiClient>.AiLoginResult.Success
            });

            var players = RoleManager.QueryUserSet();
            foreach (var player in players)
            {
                await BroadcastNpcMsgAsync(new MsgAiPlayerLogin(player));
            }
        }
    }
}
