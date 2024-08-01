using Canyon.GM.Server.Managers;
using Canyon.GM.Server.Sockets.Panel;
using Canyon.GM.Server.Sockets.Panel.Packets;
using Canyon.Network.Packets.Piglet;

namespace Canyon.GM.Server.Sockets.Game.Packets
{
    public sealed class MsgPigletUserLogin 
        : MsgPigletUserLogin<GameActor>
    {
        public override async Task ProcessAsync(GameActor client)
        {
            foreach (var user in Data.Users)
            {
                if (user.IsLogin)
                {
                    UserManager.AddUser(user.UserId, user.AccountId);
                }
                else
                {
                    UserManager.RemoveUser(user.UserId);
                }
            }

            UserManager.SetMaxOnlinePlayer(Data.MaxPlayerOnline);

            if (PanelClient.Instance?.Actor != null)
            {
                await PanelClient.Instance.Actor.SendAsync(new MsgPigletUserCount
                {
                    Data = new MsgPigletUserCount<PanelActor>.UserCountData
                    {
                        Current = UserManager.UserCount,
                        Max = UserManager.MaxUserOnline
                    }
                });
            }
        }
    }
}