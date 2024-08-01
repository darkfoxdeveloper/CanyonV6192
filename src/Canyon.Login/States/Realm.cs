using Canyon.Login.Sockets.Game;
using Canyon.Login.States.Responses;
using Canyon.Network;
using Canyon.Network.Security;
using Canyon.Network.Sockets;
using System.Net.Sockets;
using System.Text;

namespace Canyon.Login.States
{
    public sealed class Realm : TcpServerActor
    {
        public Realm(Socket socket, Memory<byte> buffer) 
            : base(socket, 
                  buffer, 
                  AesCipher.Create(
                      Kernel.ServerConfiguration.Auth.SharedKey,
                      Kernel.ServerConfiguration.Auth.SharedIV,
                      Kernel.ServerConfiguration.Auth.SharedIV), 
                  0, 
                  NetworkDefinition.ACCOUNT_FOOTER)
        {
            DiffieHellman = DiffieHellman.Create();
        }

        public DiffieHellman DiffieHellman { get; }

        public Guid RealmID => Data?.RealmID ?? Guid.Empty;

        public RealmDataResponse Data { get; set; }

        public override Task SendAsync(byte[] packet)
        {
            GameServer.Instance.Send(this, packet);
            return Task.CompletedTask;
        }

        public override Task SendAsync(byte[] packet, Func<Task> task)
        {
            GameServer.Instance.Send(this, packet, task);
            return Task.CompletedTask;
        }
    }
}
