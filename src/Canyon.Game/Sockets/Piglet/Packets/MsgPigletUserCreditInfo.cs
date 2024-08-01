using Canyon.Network.Packets.Piglet;

namespace Canyon.Game.Sockets.Piglet.Packets
{
    public sealed class MsgPigletUserCreditInfo : MsgPigletUserCreditInfo<PigletActor>
    {
        public MsgPigletUserCreditInfo(uint userId)
        {
            Data = new FirstCreditData
            {
                UserIdentity = userId,
            };
        }
    }
}
