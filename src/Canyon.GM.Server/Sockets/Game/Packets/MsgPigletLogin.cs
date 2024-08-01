using Canyon.Network.Packets.Piglet;
using Canyon.Shared;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace Canyon.GM.Server.Sockets.Game.Packets
{
    public sealed class MsgPigletLogin : MsgPigletLogin<GameActor>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MsgPigletLogin>();
        private static readonly ServerConfiguration Configuration = new ServerConfiguration();

        public override async Task ProcessAsync(GameActor client)
        {
            if (!Configuration.Rpc.UserName.Equals(Data.UserName)
                || !Configuration.Rpc.Password.Equals(Data.Password))
            {
                logger.LogWarning("Connection from [{}] denied due to invalid username or password.", client.IpAddress);
                client.Disconnect();
                return;
            }

            GameServer.Instance.Actor = client;

            logger.LogInformation($"Connected with success with the game server");
            await client.SendAsync(new MsgPigletLoginEx
            {
                Data = new MsgPigletLoginEx<GameActor>.LoginExData
                {
                    Result = RandomNumberGenerator.GetInt32(int.MaxValue)
                }
            });
        }
    }
}
