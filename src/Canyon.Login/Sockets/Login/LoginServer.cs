using Canyon.Login.Managers;
using Canyon.Login.Sockets.Login.Packets;
using Canyon.Login.States;
using Canyon.Network.Packets;
using Canyon.Network.Security;
using Canyon.Network.Sockets;
using Canyon.Shared;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;

namespace Canyon.Login.Sockets.Login
{
    public sealed class LoginServer : TcpServerListener<Client>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<LoginServer>();

        private readonly PacketProcessor<Client> processor;

        public LoginServer(ServerConfiguration config)
            : base(config.Network.MaxConn)
        {
            processor = new PacketProcessor<Client>(ProcessAsync);
            processor.StartAsync(CancellationToken.None).ConfigureAwait(false);
        }

        protected override async Task<Client> AcceptedAsync(Socket socket, Memory<byte> buffer)
        {
            uint partition = processor.SelectPartition();
            Client client = new(socket, buffer, new TQCipher(), partition);
            client.Seed = (uint)(await Kernel.NextAsync(10000, int.MaxValue));

            await client.SendAsync(new MsgEncryptCode(client.Seed));
            return client;
        }

        protected override void Received(Client actor, ReadOnlySpan<byte> packet)
        {
            processor.QueueRead(actor, packet.ToArray());
        }

        public override void Send(Client actor, ReadOnlySpan<byte> packet)
        {
            processor.QueueWrite(actor, packet.ToArray());
        }

        public override void Send(Client actor, ReadOnlySpan<byte> packet, Func<Task> task)
        {
            processor.QueueWrite(actor, packet.ToArray(), task);
        }

        private async Task ProcessAsync(Client actor, byte[] packet)
        {
            // Validate connection
            if (!actor.Socket.Connected)
            {
                return;
            }

            // Read in TQ's binary header
            var length = BitConverter.ToUInt16(packet, 0);
            var type = (PacketType)BitConverter.ToUInt16(packet, 2);

            // Switch on the packet type
            MsgBase<Client> msg;
            switch (type)
            {
                case PacketType.MsgAccount:
                    msg = new MsgAccount();
                    break;

                case PacketType.MsgConnect:
                    msg = new MsgConnect();
                    break;

                case PacketType.MsgPCNum:
                    msg = new MsgPCNum();
                    break;

                default:
                    logger.LogWarning("Missing packet {0}, Length {1}\n{2}", type, length, PacketDump.Hex(packet));
                    return;
            }

            // Decode packet bytes into the structure and process
            msg.Decode(packet);
            await msg.ProcessAsync(actor);
        }

        protected override void Disconnected(Client actor)
        {
            if (actor.AccountID != 0)
            {
                ClientManager.RemoveClient(actor.Guid);
            }
        }
    }
}
