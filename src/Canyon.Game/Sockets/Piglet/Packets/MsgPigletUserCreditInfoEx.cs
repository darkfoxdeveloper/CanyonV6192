using Canyon.Game.Services.Managers;
using Canyon.Game.States.User;
using Canyon.Network.Packets.Piglet;

namespace Canyon.Game.Sockets.Piglet.Packets
{
    public sealed class MsgPigletUserCreditInfoEx : MsgPigletUserCreditInfoEx<PigletActor>
    {
        public override async Task ProcessAsync(PigletActor client)
        {
            Character user = RoleManager.GetUserByAccount(Data.UserIdentity);
            if (user == null)
            {
                return;
            }

            if (Data.HasFirstCreditToClaim)
            {
                await user.SetFirstCreditAsync();
            }

            // TODO check what need to be updated when crediting is detected!!!
        }
    }
}
