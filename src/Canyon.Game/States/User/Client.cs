using Canyon.Game.Sockets.Game.Packets;
using Canyon.Network.Packets;
using Canyon.Network.Packets.Login;
using Canyon.Network.Security;
using Canyon.Network.Sockets;
using System.Drawing;
using System.Net.Sockets;

namespace Canyon.Game.States.User
{
    /// <summary>
    ///     Client encapsules the accepted client socket's actor and game server state.
    ///     The class should be initialized by the server's Accepted method and returned
    ///     to be passed along to the Receive loop and kept alive. Contains all world
    ///     interactions with the player.
    /// </summary>
    public sealed class Client : TcpServerActor
    {
        public Character Character { get; set; } = null;
        public Creation Creation { get; set; } = null;
        public NDDiffieHellman NdDiffieHellman { get; set; }

        /// <summary>
        ///     Instantiates a new instance of <see cref="Client" /> using the Accepted event's
        ///     resulting socket and pre-allocated buffer. Initializes all account server
        ///     states, such as the cipher used to decrypt and encrypt data.
        /// </summary>
        /// <param name="socket">Accepted remote client socket</param>
        /// <param name="buffer">pre-allocated buffer from the server listener</param>
        /// <param name="partition">Packet processing partition</param>
        public Client(Socket socket, Memory<byte> buffer, uint partition)
            : base(socket, buffer, new Cast5Cipher(), partition, "TQServer")
        {
            NdDiffieHellman = new NDDiffieHellman();

            GUID = Guid.NewGuid().ToString();
        }

        // Client unique identifier
        public uint Identity => Character?.Identity ?? 0;
        public uint AccountIdentity { get; set; }
        public ushort AuthorityLevel { get; set; }
        public string MacAddress { get; set; } = "Unknown";
        public int LastLogin { get; set; }
        public string GUID { get; }

        public override Task SendAsync(byte[] packet)
        {
            Kernel.NetworkMonitor.Send(packet.Length);
            Kernel.Sockets.GameServer.Send(this, packet);
            return Task.CompletedTask;
        }

        public override Task SendAsync(byte[] packet, Func<Task> task)
        {
            Kernel.NetworkMonitor.Send(packet.Length);
            Kernel.Sockets.GameServer.Send(this, packet, task);
            return Task.CompletedTask;
        }

        public Task DisconnectWithMessageAsync(MsgConnectEx<Client>.RejectionCode rejectionCode)
        {
            return SendAsync(new MsgConnectEx(rejectionCode), () =>
            {
                Disconnect();
                return Task.CompletedTask;
            });
        }

        public Task DisconnectWithMessageAsync(string message)
        {
            return SendAsync(new MsgTalk(Identity, TalkChannel.Talk, Color.White, message), () =>
            {
                Disconnect();
                return Task.CompletedTask;
            });
        }

        public Task DisconnectWithMessageAsync(IPacket msg)
        {
            return SendAsync(msg, () =>
            {
                Disconnect();
                return Task.CompletedTask;
            });
        }
    }
}
