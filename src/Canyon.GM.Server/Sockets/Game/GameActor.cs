using Canyon.Network;
using Canyon.Network.Security;
using Canyon.Network.Sockets;
using System.Net.Sockets;

namespace Canyon.GM.Server.Sockets.Game
{
    public sealed class GameActor : TcpServerActor
    {
        private static readonly string AesKey = ("sA%Bc#O(Hj&jibPbzD5W6&EeHsfbft!f");
        private static readonly string AesIv = ("l0h5*2RKy&Dop*PF");

        public GameActor(Socket socket, Memory<byte> buffer, uint partition = 0)
            : base(socket, buffer, AesCipher.Create(AesKey, AesIv, AesIv), partition, NetworkDefinition.GM_TOOLS_FOOTER)
        {
            DiffieHellman = DiffieHellman.Create();
        }

        public Guid Guid { get; init; } = Guid.NewGuid();
        public DiffieHellman DiffieHellman { get; }

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