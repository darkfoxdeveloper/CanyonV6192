using Canyon.Login.Managers;
using Canyon.Login.Sockets.Login.Packets;
using Canyon.Login.States;
using Canyon.Network.Packets.Login;
using Canyon.Shared;
using Microsoft.Extensions.Logging;

namespace Canyon.Login.Sockets.Game.Packets
{
    public sealed class MsgAccServerLoginExchangeEx : MsgAccServerLoginExchangeEx<Realm>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MsgAccServerLoginExchangeEx>();

        public override async Task ProcessAsync(Realm client)
        {
            var actor = ClientManager.GetClient(Guid.Parse(Data.Request));
            if (actor == null)
            {
                logger.LogInformation("{} was not found on client dictionary!", Data.Request);
                return;
            }

            if (Data.Response != SUCCESS)
            {
                logger.LogInformation("{} has failed auth with reply {}.", Data.Request, Data.Response);
                await actor.DisconnectWithRejectionCodeAsync(MsgConnectEx<Client>.RejectionCode.PleaseTryAgainLater);
                return;
            }

            await actor.SendAsync(new MsgConnectEx(client.Data.GameIPAddress, client.Data.GamePort, Data.Token));
            logger.LogInformation("User [{}] has been redirected to game server", actor.Username);
        }
    }
}
