using Canyon.Network.Packets.Login;

namespace Canyon.Game.Sockets.Login.Packets
{
    public sealed class MsgAccServerAuthEx : MsgAccServerAuthEx<LoginActor>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MsgAccServerAuthEx>();

        public override async Task ProcessAsync(LoginActor client)
        {
            if (Data.ResponseStatus != SUCCESS)
            {
                switch (Data.ResponseStatus)
                {
                    case INVALID_USERNAME_PASSWORD:
                        {
                            logger.LogWarning("Authentication failed: Invalid username or password!");
                            break;
                        }
                    case UNAUTHORIZED_IP_ADDRESS:
                        {
                            logger.LogWarning("Authentication failed: This host is not allowed to connect!");
                            break;
                        }
                    case REALM_DOES_NOT_EXIST:
                        {
                            logger.LogWarning("Authentication failed: Realm ID does not exist or is not authorized.");
                            break;
                        }
                    case DEBUG_MODE:
                        {
                            logger.LogWarning("Authentication failed: Login is in DEBUG mode and only allows non productive realms.");
                            break;
                        }
                    case DUPLICATED_LOGIN:
                        {
                            logger.LogWarning("Authentication failed: Same server ID is already connected.");
                            break;
                        }
                    default:
                        {
                            logger.LogWarning("Authentication failed with error {}", Data.ResponseStatus);
                            break;
                        }
                }
                return;
            }

            logger.LogInformation("Account server connected!");
        }
    }
}
