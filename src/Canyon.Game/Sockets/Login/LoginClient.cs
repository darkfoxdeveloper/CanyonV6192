using Canyon.Game.Sockets.Login.Packets;
using Canyon.Network;
using Canyon.Network.Packets;
using Canyon.Network.Sockets;
using System.Net.Sockets;

namespace Canyon.Game.Sockets.Login
{
    public sealed class LoginClient : TcpClientWrapper<LoginActor>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<LoginClient>();
        private readonly PacketProcessor<LoginActor> packetProcessor;

        public static LoginClient Instance { get; set; }
        public static ConnectionState ConnectionStage { get; set; }


        public LoginClient()
            : base(NetworkDefinition.ACCOUNT_FOOTER.Length, true)
        {
            packetProcessor = new PacketProcessor<LoginActor>(ProcessAsync, 1);
            packetProcessor.StartAsync(CancellationToken.None).ConfigureAwait(false);

            ExchangeStartPosition = 0;
        }

        public LoginActor Actor { get; set; }

        protected override Task<LoginActor> ConnectedAsync(Socket socket, Memory<byte> buffer)
        {
            Actor = new LoginActor(socket, buffer);
            ConnectionStage = ConnectionState.Exchanging;
            return Task.FromResult(Actor);
        }

        protected override async Task<bool> ExchangedAsync(LoginActor actor, Memory<byte> buffer)
        {
            try
            {
                MsgAccServerHandshake handshake = new MsgAccServerHandshake();
                handshake.Decode(buffer.ToArray());
                await handshake.ProcessAsync(actor);
                ConnectionStage = ConnectionState.Connected;
                return true;
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Error on exchange!!! {ex}", ex.Message);
                return false;
            }
        }

        protected override void Received(LoginActor actor, ReadOnlySpan<byte> packet)
        {
            packetProcessor.QueueRead(actor, packet.ToArray());
        }

        public override void Send(LoginActor actor, ReadOnlySpan<byte> packet)
        {
            packetProcessor.QueueWrite(actor, packet.ToArray());
        }

        public void Send(LoginActor actor, ReadOnlySpan<byte> packet, Func<Task> task)
        {
            packetProcessor.QueueWrite(actor, packet.ToArray(), task);
        }

        private async Task ProcessAsync(LoginActor actor, byte[] packet)
        {
            if (!actor.Socket.Connected)
                return;

            var length = BitConverter.ToUInt16(packet, 0);
            var type = (PacketType)BitConverter.ToUInt16(packet, 2);

            try
            {
                MsgBase<LoginActor> msg;
                switch (type)
                {
                    case PacketType.MsgAccServerAuthEx:
                        {
                            msg = new MsgAccServerAuthEx();
                            break;
                        }

                    case PacketType.MsgAccServerLoginExchange:
                        {
                            msg = new MsgAccServerLoginExchange();
                            break;
                        }

                    case PacketType.MsgAccServerPing:
                        {
                            msg = new MsgAccServerPing();
                            break;
                        }

                    default:
                        {
                            logger.LogWarning($"Missing packet {type}, Length {length}\n{PacketDump.Hex(packet)}");
                            return;
                        }
                }
                // Decode packet bytes into the structure and process
                msg.Decode(packet);
                await msg.ProcessAsync(actor);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Error on process message! {}", ex.Message);
            }
        }

        protected override void Disconnected(LoginActor actor)
        {
            if (actor != null && actor == Instance.Actor)
            {
                Instance.Actor = null;
                Instance = null;
                ConnectionStage = ConnectionState.Disconnected;
            }
        }

        public Task StopAsync()
        {
            return packetProcessor.StopAsync(CancellationToken.None);
        }

        public enum ConnectionState
        {
            Disconnected,
            Exchanging,
            Connected
        }
    }
}
