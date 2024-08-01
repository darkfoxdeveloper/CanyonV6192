using Canyon.Game.Services.Managers;
using Canyon.Game.Sockets.Ai.Packets;
using Canyon.Game.States;
using Canyon.Network;
using Canyon.Network.Packets;
using Canyon.Network.Sockets;
using System.Net.Sockets;

namespace Canyon.Game.Sockets.Ai
{
    public sealed class NpcServer : TcpServerListener<AiClient>
    {
        // Fields and Properties
        private readonly PacketProcessor<AiClient> processor;
        private static readonly ILogger logger = LogFactory.CreateLogger<NpcServer>();

        public static AiClient NpcClient { get; private set; }

        /// <summary>
        ///     Instantiates a new instance of <see cref="Server" /> by initializing the
        ///     <see cref="PacketProcessor" /> for processing packets from the players using
        ///     channels and worker threads. Initializes the TCP server listener.
        /// </summary>
        /// <param name="config">The server's read configuration file</param>
        public NpcServer()
            : base(1, exchange: false, footerLength: NetworkDefinition.NPC_FOOTER.Length)
        {
            processor = new PacketProcessor<AiClient>(ProcessAsync, 1);
            _ = processor.StartAsync(CancellationToken.None).ConfigureAwait(false);
            ExchangeStartPosition = 0;
        }

        protected override async Task<AiClient> AcceptedAsync(Socket socket, Memory<byte> buffer)
        {
            uint partition = processor.SelectPartition();
            var client = new AiClient(socket, buffer, partition);
            NpcClient = client;
            logger.LogInformation($"Accepting connection from npc server on [{client.IpAddress}].");
            return client;
        }

        /// <summary>
        ///     Invoked by the server listener's Receiving method to process a completed packet
        ///     from the actor's socket pipe. At this point, the packet has been assembled and
        ///     split off from the rest of the buffer.
        /// </summary>
        /// <param name="actor">Server actor that represents the remote client</param>
        /// <param name="packet">Packet bytes to be processed</param>
        protected override void Received(AiClient actor, ReadOnlySpan<byte> packet)
        {
            Kernel.NetworkMonitor.Receive(packet.Length);
            processor.QueueRead(actor, packet.ToArray());
        }

        public override void Send(AiClient actor, ReadOnlySpan<byte> packet)
        {
            processor.QueueWrite(actor, packet.ToArray());
        }

        public override void Send(AiClient actor, ReadOnlySpan<byte> packet, Func<Task> task)
        {
            processor.QueueWrite(actor, packet.ToArray(), task);
        }

        /// <summary>
        ///     Invoked by one of the server's packet processor worker threads to process a
        ///     single packet of work. Allows the server to process packets as individual
        ///     messages on a single channel.
        /// </summary>
        /// <param name="actor">Actor requesting packet processing</param>
        /// <param name="packet">An individual data packet to be processed</param>
        private async Task ProcessAsync(AiClient actor, byte[] packet)
        {
            // Read in TQ's binary header
            var length = BitConverter.ToUInt16(packet, 0);
            var type = (PacketType)BitConverter.ToUInt16(packet, 2);

            // Validate connection
            if (!actor.Socket.Connected)
            {
                return;
            }

            try
            {
                // Switch on the packet type
                MsgBase<AiClient> msg = null;
                switch (type)
                {
                    case PacketType.MsgAiLoginExchange:
                        msg = new MsgAiLoginExchange();
                        break;

                    case PacketType.MsgAiPing:
                        msg = new MsgAiPing();
                        break;

                    case PacketType.MsgAiAction:
                        msg = new MsgAiAction();
                        break;

                    case PacketType.MsgAiSpawnNpc:
                        msg = new MsgAiSpawnNpc();
                        break;

                    case PacketType.MsgAiInteract:
                        msg = new MsgAiInteract();
                        break;

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
            catch (Exception e)
            {
                logger.LogCritical(e, "{Message}", e.Message);
            }
        }

        /// <summary>
        ///     Invoked by the server listener's Disconnecting method to dispose of the actor's
        ///     resources. Gives the server an opportunity to cleanup references to the actor
        ///     from other actors and server collections.
        /// </summary>
        /// <param name="actor">Server actor that represents the remote client</param>
        protected override void Disconnected(AiClient actor)
        {
            if (actor == null)
            {
                logger.LogError(@"Disconnected with ai server null ???");
                return;
            }

            processor.DeselectPartition(actor.Partition);

            if (NpcClient?.GUID.Equals(actor.GUID) == true)
            {
                NpcClient = null;
            }

            logger.LogWarning($"NPC Server [{actor.GUID}] has disconnected!");
        }

        public new void Close()
        {
            base.Close();
        }
    }
}
