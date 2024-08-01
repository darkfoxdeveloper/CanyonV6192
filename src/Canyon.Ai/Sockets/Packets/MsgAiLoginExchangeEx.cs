using Canyon.Ai.Managers;
using Canyon.Network.Packets.Ai;

namespace Canyon.Ai.Sockets.Packets
{
    public sealed class MsgAiLoginExchangeEx : MsgAiLoginExchangeEx<GameServer>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MsgAiLoginExchangeEx>();

        public override async Task ProcessAsync(GameServer client)
        {
            switch (Result)
            {
                case AiLoginResult.Success:
                    {
                        logger.LogInformation("Accepted on the game server!");
                        client.Stage = GameServer.ConnectionStage.Ready;

                        GeneratorManager.SynchroGenerators();
                        return;
                    }

                case AiLoginResult.AlreadySignedIn:
                    logger.LogError("Could not connect to the game server! Already signed in!");
                    break;
                case AiLoginResult.InvalidPassword:
                    logger.LogError("Could not connect to the game server! Invalid username or password!");
                    break;
                case AiLoginResult.InvalidAddress:
                    logger.LogError("Could not connect to the game server! Address not authorized!");
                    break;
                case AiLoginResult.AlreadyBound:
                    logger.LogError("Could not connect to the game server! Server is already bound!");
                    break;
                case AiLoginResult.UnknownError:
                    logger.LogError("Could not connect to the game server! Unknown error!");
                    break;
            }

            if (client.Socket.Connected)
            {
                client.Disconnect();
            }
        }
    }
}
