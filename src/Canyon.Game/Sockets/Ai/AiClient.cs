using Canyon.Network;
using Canyon.Network.Security;
using Canyon.Network.Sockets;
using System.Net.Sockets;

namespace Canyon.Game.Sockets.Ai
{
    public sealed class AiClient : TcpServerActor
    {
        public AiClient(Socket socket, Memory<byte> buffer, uint partition)
            : base(socket, buffer, null, partition, NetworkDefinition.NPC_FOOTER)
        {
            GUID = Guid.NewGuid().ToString();
            DiffieHellman = DiffieHellman.Create();
        }

        public DiffieHellman DiffieHellman { get; }

        public ConnectionStage Stage { get; set; }
        public string GUID { get; }

        public override Task SendAsync(byte[] packet)
        {
            Kernel.NetworkMonitor.Send(packet.Length);
            Kernel.Sockets.NpcServer.Send(this, packet);
            return Task.CompletedTask;
        }

        public override Task SendAsync(byte[] packet, Func<Task> task)
        {
            Kernel.NetworkMonitor.Send(packet.Length);
            Kernel.Sockets.NpcServer.Send(this, packet, task);
            return Task.CompletedTask;
        }

        public enum ConnectionStage
        {
            AwaitingAuth,
            Authenticated
        }
    }
}

