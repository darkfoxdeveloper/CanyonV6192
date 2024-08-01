using Canyon.Network;
using Canyon.Network.Security;
using Canyon.Network.Sockets;
using System.Net.Sockets;
using System.Text;

namespace Canyon.GM.Server.Sockets.Panel
{
    public sealed class PanelActor : TcpServerActor
    {
        private static readonly string AesKey;
        private static readonly string AesIV;

        static PanelActor()
        {
            AesKey = Program.ServerConfiguration.Socket.AesKey;
            AesIV = Program.ServerConfiguration.Socket.AesIV;
        }

        public PanelActor(Socket socket, Memory<byte> buffer, uint partition = 0) 
            : base(socket, buffer,
                  AesCipher.Create(AesKey, AesIV, AesIV),
                  partition, NetworkDefinition.GM_TOOLS_FOOTER)
        {
            DiffieHellman = DiffieHellman.Create();
        }

        public DiffieHellman DiffieHellman { get; init; }

        public override Task SendAsync(byte[] packet)
        {
            PanelClient.Instance.Send(this, packet);
            return Task.CompletedTask;
        }

        public override Task SendAsync(byte[] packet, Func<Task> task)
        {
            PanelClient.Instance.Send(this, packet, task);
            return Task.CompletedTask;
        }
    }
}
