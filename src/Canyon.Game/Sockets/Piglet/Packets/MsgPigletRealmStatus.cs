using Canyon.Network.Packets.Piglet;

namespace Canyon.Game.Sockets.Piglet.Packets
{
    public class MsgPigletRealmStatus : MsgPigletRealmStatus<PigletActor>
    {
        public MsgPigletRealmStatus()
        {
            serializeWithHeaders = true;
        }
    }
}
