using Canyon.Network;
using Canyon.Network.Security;
using Canyon.Network.Sockets;
using System.Net.Sockets;

namespace Canyon.Game.Sockets.Piglet
{
    public sealed class PigletActor : TcpServerActor
    {
        private static readonly string AesKey = "sA%Bc#O(Hj&jibPbzD5W6&EeHsfbft!f";
        private static readonly string AesEncryptionIv = "l0h5*2RKy&Dop*PF";
        private static readonly string AesDecryptionIv = "l0h5*2RKy&Dop*PF";

        public PigletActor(Socket socket, Memory<byte> buffer, uint partition = 0)
            : base(socket, buffer, AesCipher.Create(AesKey, AesEncryptionIv, AesDecryptionIv), partition, NetworkDefinition.GM_TOOLS_FOOTER)
        {
            DiffieHellman = DiffieHellman.Create();
        }

        public DiffieHellman DiffieHellman { get; }

        public override Task SendAsync(byte[] packet)
        {
            PigletClient.Instance.Send(this, packet);
            return Task.CompletedTask;
        }

        public override Task SendAsync(byte[] packet, Func<Task> task)
        {
            PigletClient.Instance.Send(this, packet, task);
            return Task.CompletedTask;
        }
    }
}
