using Canyon.Network;
using Canyon.Network.Security;
using Canyon.Network.Sockets;
using System.Net.Sockets;
using System.Text;

namespace Canyon.Game.Sockets.Login
{
    public sealed class LoginActor : TcpServerActor
    {
        public LoginActor(Socket socket, Memory<byte> buffer) 
            : base(socket, buffer,
                AesCipher.Create(
                    ServerConfiguration.Configuration.Auth.SharedKey, 
                    ServerConfiguration.Configuration.Auth.SharedIV, 
                    ServerConfiguration.Configuration.Auth.SharedIV), 
                0, NetworkDefinition.ACCOUNT_FOOTER)
        {
            DiffieHellman = DiffieHellman.Create();
        }

        public DiffieHellman DiffieHellman { get; init; }

        public override Task SendAsync(byte[] packet)
        {
            LoginClient.Instance.Send(this, packet);
            return Task.CompletedTask;
        }

        public override Task SendAsync(byte[] packet, Func<Task> task)
        {
            LoginClient.Instance.Send(this, packet, task);
            return Task.CompletedTask;
        }
    }
}
