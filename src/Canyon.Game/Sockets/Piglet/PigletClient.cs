using Canyon.Game.Sockets.Piglet.Packets;
using Canyon.Network;
using Canyon.Network.Packets;
using Canyon.Network.Sockets;
using System.Net.Sockets;

namespace Canyon.Game.Sockets.Piglet
{
    public sealed class PigletClient : TcpClientWrapper<PigletActor>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<PigletClient>();
        private readonly PacketProcessor<PigletActor> packetProcessor;

        public static PigletClient Instance { get; set; }
        public static ConnectionState ConnectionStage { get; set; }

        public PigletClient()
            : base(NetworkDefinition.GM_TOOLS_FOOTER.Length, true)
        {
            packetProcessor = new PacketProcessor<PigletActor>(ProcessAsync, 1);
            packetProcessor.StartAsync(CancellationToken.None).ConfigureAwait(false);
        }

        public PigletActor Actor { get; set; }

        protected override async Task<PigletActor> ConnectedAsync(Socket socket, Memory<byte> buffer)
        {
            PigletActor actor = new PigletActor(socket, buffer);
            if (socket.Connected)
            {
                ConnectionStage = ConnectionState.Exchanging;
                return actor;
            }
            return null;
        }

        protected override async Task<bool> ExchangedAsync(PigletActor actor, Memory<byte> buffer)
        {
            try
            {
                MsgPigletHandshake handshake = new MsgPigletHandshake();
                handshake.Decode(buffer.ToArray());
                await handshake.ProcessAsync(actor);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Error on exchange!!! {ex}", ex.Message);
                return false;
            }
        }

        protected override void Received(PigletActor actor, ReadOnlySpan<byte> packet)
        {
            packetProcessor.QueueRead(actor, packet.ToArray());
        }

        public override void Send(PigletActor actor, ReadOnlySpan<byte> packet)
        {
            packetProcessor.QueueWrite(actor, packet.ToArray());
        }

        public void Send(PigletActor actor, ReadOnlySpan<byte> packet, Func<Task> task)
        {
            packetProcessor.QueueWrite(actor, packet.ToArray(), task);
        }

        private async Task ProcessAsync(PigletActor actor, byte[] packet)
        {
            if (!actor.Socket.Connected)
                return;

            var length = BitConverter.ToUInt16(packet, 0);
            var type = (PacketType)BitConverter.ToUInt16(packet, 2);
            packet = packet.AsSpan()[..length].ToArray();

            try
            {
                MsgBase<PigletActor> msg;
                switch (type)
                {   
                    case PacketType.MsgPigletLoginEx:
                        {
                            msg = new MsgPigletLoginEx();
                            break;
                        }

                    case PacketType.MsgPigletPing:
                        {
                            msg = new MsgPigletPing();
                            break;
                        }

                    case PacketType.MsgPigletUserBan:
                        {
                            msg = new MsgPigletUserBan();
                            break;
                        }

                    case PacketType.MsgPigletUserMail:
                        {
                            msg = new MsgPigletUserMail();
                            break;
                        }

                    case PacketType.MsgPigletUserMassMail:
                        {
                            msg = new MsgPigletUserMassMail();
                            break;
                        }

                    case PacketType.MsgPigletUserCreditInfoEx:
                        {
                            msg = new MsgPigletUserCreditInfoEx();
                            break;
                        }

                    case PacketType.MsgPigletRealmAnnounceMaintenance:
                        {
                            msg = new MsgPigletRealmAnnounceMaintenance();
                            break;
                        }

                    case PacketType.MsgPigletShutdown:
                        {
                            msg = new MsgPigletShutdown();
                            break;
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
            catch (Exception ex)
            {
                logger.LogError(ex, "Error on processing packet!!!! {ex}", ex.Message);
            }
        }

        protected override void Disconnected(PigletActor actor)
        {
            ConnectionStage = ConnectionState.Disconnected;
            logger.LogInformation($"GM Server disconnected!!!");
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
