using Canyon.Network.Packets.Piglet;

namespace Canyon.Game.Sockets.Piglet.Packets
{
    public sealed class MsgPigletLogin : MsgPigletLogin<PigletActor>
    {
        public MsgPigletLogin(string userName, string password, string realmName)
            : base(userName, password, realmName)
        {            
        }
    }
}
