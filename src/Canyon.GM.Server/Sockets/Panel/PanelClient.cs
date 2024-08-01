using Canyon.GM.Server.Sockets.Game;
using Canyon.GM.Server.Sockets.Panel.Packets;
using Canyon.Network;
using Canyon.Network.Packets;
using Canyon.Network.Sockets;
using Canyon.Shared;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;

namespace Canyon.GM.Server.Sockets.Panel
{
    public sealed class PanelClient : TcpClientWrapper<PanelActor>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<PanelClient>();
        private readonly PacketProcessor<PanelActor> packetProcessor;

        public static PanelClient Instance { get; set; }
        public static ConnectionState ConnectionStage { get; set; }

        public PanelClient()
            : base(NetworkDefinition.GM_TOOLS_FOOTER.Length, true)
        {
            packetProcessor = new PacketProcessor<PanelActor>(ProcessAsync, 1);
            packetProcessor.StartAsync(CancellationToken.None).ConfigureAwait(false);

            ExchangeStartPosition = 0;
        }

        public PanelActor Actor { get; set; }

        protected override async Task<PanelActor> ConnectedAsync(Socket socket, Memory<byte> buffer)
        {
            Actor = new PanelActor(socket, buffer);
            if (socket.Connected)
            {
                ConnectionStage = ConnectionState.Exchanging;
                return Actor;
            }
            return null;
        }

        protected override async Task<bool> ExchangedAsync(PanelActor actor, Memory<byte> buffer)
        {
            try
            {
                MsgPigletHandshake handshake = new MsgPigletHandshake();
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

        protected override void Received(PanelActor actor, ReadOnlySpan<byte> packet)
        {
            packetProcessor.QueueRead(actor, packet.ToArray());
        }

        public override void Send(PanelActor actor, ReadOnlySpan<byte> packet)
        {
            packetProcessor.QueueWrite(actor, packet.ToArray());
        }

        public void Send(PanelActor actor, ReadOnlySpan<byte> packet, Func<Task> task)
        {
            packetProcessor.QueueWrite(actor, packet.ToArray(), task);
        }

        private async Task ProcessAsync(PanelActor actor, byte[] packet)
        {
            if (!actor.Socket.Connected)
                return;

            var length = BitConverter.ToUInt16(packet, 0);
            var type = (PacketType)BitConverter.ToUInt16(packet, 2);
            packet = packet.AsSpan()[..length].ToArray();

            try
            {
                MsgBase<PanelActor> msg;
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

                    case PacketType.MsgPigletRealmAction:
                        {
                            msg = new MsgPigletRealmAction();
                            break;
                        }

                    case PacketType.MsgPigletUserBan:
                    case PacketType.MsgPigletUserMail:
                    case PacketType.MsgPigletUserMassMail:
                    case PacketType.MsgPigletItemSuspicious:
                    case PacketType.MsgPigletUserCreditInfoEx:
                        {
                            if (GameServer.Instance?.Actor != null)
                            {
                                await GameServer.Instance.Actor.SendAsync(packet);
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
            catch (Exception ex)
            {
                logger.LogError(ex, "Error on processing packet!!!! {ex}", ex.Message);
            }
        }

        protected override void Disconnected(PanelActor actor)
        {
            ConnectionStage = ConnectionState.Disconnected;
            Instance.Actor = null;
            Instance = null;
            _ = StopAsync().ConfigureAwait(false);
            logger.LogInformation($"GM Server disconnected!!!");
        }

        public Task StopAsync()
        {
            try
            {
                return packetProcessor.StopAsync(CancellationToken.None);
            }
            catch
            {
                return Task.CompletedTask;
            }
        }

        public enum ConnectionState
        {
            Disconnected,
            Exchanging,
            Connected
        }
    }
}
