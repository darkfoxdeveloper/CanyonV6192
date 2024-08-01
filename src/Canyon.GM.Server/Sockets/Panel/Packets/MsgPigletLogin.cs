using Canyon.Network.Packets.Piglet;

namespace Canyon.GM.Server.Sockets.Panel.Packets
{
    public sealed class MsgPigletLogin : MsgPigletLogin<PanelActor>
    {
        public MsgPigletLogin(string userName, string password, string realmName)
            : base(userName, password, realmName) 
        {
        }
    }
}
