using Canyon.GM.Server.Managers;
using Canyon.GM.Server.Sockets.Game.Packets;
using Canyon.GM.Server.Sockets.Panel;
using Canyon.Network;
using Canyon.Network.Packets;
using Canyon.Network.Sockets;
using Canyon.Shared;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;

namespace Canyon.GM.Server.Sockets.Game
{
    public sealed class GameServer : TcpServerListener<GameActor>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<GameServer>();
        private readonly PacketProcessor<GameActor> packetProcessor;

        public static GameServer Instance { get; private set; }

        public GameServer()
            : base(10, 4096, false, true, NetworkDefinition.GM_TOOLS_FOOTER.Length)
        {
            Instance = this;

            ExchangeStartPosition = 0;

            packetProcessor = new PacketProcessor<GameActor>(ProcessAsync, 1);
            packetProcessor.StartAsync(CancellationToken.None);
        }

        public GameActor Actor { get; set; }
        public bool IsConnected => Actor?.Socket.Connected == true;
        public int ServerStatus { get; set; }

        protected override async Task<GameActor> AcceptedAsync(Socket socket, Memory<byte> buffer)
        {
            var actor = new GameActor(socket, buffer);
            if (socket.Connected)
            {
                await actor.SendAsync(new MsgPigletHandshake(actor.DiffieHellman.PublicKey, actor.DiffieHellman.Modulus, null, null));
                return actor;
            }
            return null;
        }

        protected override bool Exchanged(GameActor actor, ReadOnlySpan<byte> buffer)
        {
            try
            {
                MsgPigletHandshake msg = new MsgPigletHandshake();
                msg.Decode(buffer.ToArray());

                if (!actor.DiffieHellman.Initialize(msg.Data.PublicKey, msg.Data.Modulus))
                {
                    throw new Exception("Could not initialize Diffie-Helmman!!!");
                }

                actor.Cipher.GenerateKeys(new object[]
                {
                    actor.DiffieHellman.SharedKey.ToByteArrayUnsigned(),
                    msg.Data.EncryptIV,
                    msg.Data.DecryptIV
                });
                return true;
            }
            catch (Exception ex) 
            {
                logger.LogCritical(ex, "Error when exchanging GM data. Error: {}", ex.Message);
                return false;
            }
        }

        protected override void Received(GameActor actor, ReadOnlySpan<byte> packet)
        {
            packetProcessor.QueueRead(actor, packet.ToArray());
        }

        public override void Send(GameActor actor, ReadOnlySpan<byte> packet)
        {
            packetProcessor.QueueWrite(actor, packet.ToArray());
        }

        public override void Send(GameActor actor, ReadOnlySpan<byte> packet, Func<Task> task)
        {
            packetProcessor.QueueWrite(actor, packet.ToArray(), task);
        }

        private async Task ProcessAsync(GameActor actor, byte[] packet)
        {
            // Validate connection
            if (!actor.Socket.Connected)
            {
                return;
            }

            var length = BitConverter.ToUInt16(packet, 0);
            var type = (PacketType)BitConverter.ToUInt16(packet, 2);

            try
            {
                MsgBase<GameActor> msg;
                switch (type)
                {
                    case PacketType.MsgPigletLogin:
                        {
                            msg = new MsgPigletLogin();
                            break;
                        }

                    case PacketType.MsgPigletPing:
                        {
                            msg = new MsgPigletPing();
                            break;
                        }

                    case PacketType.MsgPigletUserLogin:
                        {
                            msg = new MsgPigletUserLogin();
                            break;
                        }

                    case PacketType.MsgPigletRealmStatus:
                        {
                            msg = new MsgPigletRealmStatus();
                            break;
                        }

                    case PacketType.MsgPigletUserCreditInfo:
                    case PacketType.MsgPigletClaimFirstCredit:
                        {
                            if (PanelClient.Instance?.Actor != null)
                            {
                                await PanelClient.Instance.Actor.SendAsync(packet);
                            }
                            return;
                        }

                    default:
                        {
                            logger.LogWarning($"Missing packet {type}, Length {length}\n{PacketDump.Hex(packet)}");
                            return;
                        }
                }

                msg.Decode(packet.ToArray());
                await msg.ProcessAsync(actor);
            }
            catch 
            {
            }
        }

        protected override void Disconnected(GameActor actor)
        {
            if (actor.Guid == Actor.Guid)
            {
                Actor = null;
                logger.LogInformation($"Game server has disconnected!!!");
                UserManager.DisconnectionClear();
                ServerStatus = 0;

                if (PanelClient.Instance.Actor != null)
                {
                    PanelClient.Instance.Actor.SendAsync(new MsgPigletRealmStatus
                    {
                        Data = new Network.Packets.Piglet.MsgPigletRealmStatus<GameActor>.RealmStatusData
                        {
                            Status = 0
                        }
                    }).GetAwaiter().GetResult();
                }
            }
        }
    }
}
