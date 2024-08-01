using Canyon.Game.Services.Managers;
using Canyon.Network.Packets.Piglet;

namespace Canyon.Game.Sockets.Piglet.Packets
{
    public sealed class MsgPigletUserBan : MsgPigletUserBan<PigletActor>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MsgPigletUserBan>();

        public override async Task ProcessAsync(PigletActor client)
        {
            var user = RoleManager.GetUser(Data.UserId);
            if (user == null)
            {
                user = RoleManager.GetUserByAccount(Data.UserId);
                if (user == null)
                {
                    return;
                }
            }

            logger.LogInformation($"GM[{Data.GameMaster}] has banned [{user.Identity},{user.Name}]. Reason: {Data.Reason}");
            await RoleManager.KickOutAsync(user.Identity, StrYouHaveBeenBanned);
        }
    }
}