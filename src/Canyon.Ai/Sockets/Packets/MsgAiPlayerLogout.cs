using Canyon.Ai.Managers;
using Canyon.Ai.States;
using Canyon.Network.Packets.Ai;

namespace Canyon.Ai.Sockets.Packets
{
    public sealed class MsgAiPlayerLogout : MsgAiPlayerLogout<GameServer>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MsgAiPlayerLogout>();

        public override async Task ProcessAsync(GameServer client)
        {
            if (!RoleManager.LogoutUser(Id, out Character user))
                return;

            await user.LeaveMapAsync();

#if DEBUG
            logger.LogInformation($"User [{Id}]{user.Name} has signed out.");
#endif
        }
    }
}
