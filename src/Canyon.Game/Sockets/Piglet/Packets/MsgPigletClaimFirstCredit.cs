using Canyon.Network.Packets.Piglet;

namespace Canyon.Game.Sockets.Piglet.Packets
{
    public sealed class MsgPigletClaimFirstCredit : MsgPigletClaimFirstCredit<PigletActor>
    {
        public MsgPigletClaimFirstCredit(uint accountId)
        {
            Data = new ClaimFirstCreditData
            {
                AccountId = accountId
            };
        }
    }
}
