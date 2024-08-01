using Canyon.Ai.Managers;
using Canyon.Ai.States;
using Canyon.Network.Packets.Ai;

namespace Canyon.Ai.Sockets.Packets
{
    public sealed class MsgAiPlayerLogin : MsgAiPlayerLogin<GameServer>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MsgAiPlayerLogin>();

        public override async Task ProcessAsync(GameServer client)
        {
            Character user = RoleManager.GetUser(Id);
            if (user != null)
            {
                logger.LogWarning($"User [{Id}]{Name} is already signed in. Invalid Call (FlyMap??)");
                return;
            }

            user = new Character(Id);
            if (!await user.InitializeAsync(this))
            {
                logger.LogWarning($"User [{Id}]{Name} could not be initialized!");
                return;
            }

            RoleManager.LoginUser(user);
#if DEBUG
            logger.LogDebug($"User [{Id}]{Name} has signed in.");
#endif
        }
    }
}
