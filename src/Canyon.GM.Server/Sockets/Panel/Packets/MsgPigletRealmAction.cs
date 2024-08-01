using Canyon.GM.Server.Services;
using Canyon.Network.Packets.Piglet;
using Canyon.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Canyon.GM.Server.Sockets.Panel.Packets
{
    public sealed class MsgPigletRealmAction : MsgPigletRealmAction<PanelActor>
    {
        private static readonly ILogger logger  = LogFactory.CreateLogger<MsgPigletRealmAction>();

        public override async Task ProcessAsync(PanelActor client)
        {
            var realmService = ServiceProviderHelper.Instance.GetService<RealmService>();
            switch (Data.ActionType)
            {
                case ActionDataType.StartServer:
                    {
                        await realmService.StartServerAsync();
                        break;
                    }
                default:
                    {
                        
                        break;
                    }
            }
        }
    }
}
