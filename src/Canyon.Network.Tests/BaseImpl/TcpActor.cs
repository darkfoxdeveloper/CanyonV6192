using Canyon.Network.Security;
using Canyon.Network.Sockets;
using System.Net.Sockets;

namespace Canyon.Network.Tests.BaseImpl
{
    public class TcpActor : TcpServerActor
    {
        public TcpActor(Socket socket, Memory<byte> buffer, ICipher cipher, uint partition = 0, string packetFooter = "")
            : base(socket, buffer, cipher, partition, packetFooter)
        {
        }

        public override Task SendAsync(byte[] packet)
        {
            throw new NotImplementedException();
        }

        public override Task SendAsync(byte[] packet, Func<Task> task)
        {
            throw new NotImplementedException();
        }
    }
}
