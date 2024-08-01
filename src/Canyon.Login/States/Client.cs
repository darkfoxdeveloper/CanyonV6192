using Canyon.Login.Sockets.Login.Packets;
using Canyon.Network.Packets;
using Canyon.Network.Packets.Login;
using Canyon.Network.Security;
using Canyon.Network.Sockets;
using System.Net.Sockets;

namespace Canyon.Login.States
{
    public sealed class Client : TcpServerActor
    {
        public Client(Socket socket, Memory<byte> buffer, ICipher cipher, uint partition = 0)
            : base(socket, buffer, cipher, partition)
        {
            Guid = Guid.NewGuid();
        }

        public Guid Guid { get; }
        public Realm Realm { get; set; }
        public uint Seed { get; set; }

        public uint AccountID { get; set; }
        public string Username { get; set; }

        public override Task SendAsync(IPacket packet)
        {
            return SendAsync(packet.Encode());
        }

        public override Task SendAsync(byte[] packet)
        {
            Kernel.Sockets.LoginServer.Send(this, packet);
            return Task.CompletedTask;
        }

        public override Task SendAsync(byte[] packet, Func<Task> task)
        {
            Kernel.Sockets.LoginServer.Send(this, packet, task);
            return Task.CompletedTask;
        }

        public Task DisconnectWithRejectionCodeAsync(MsgConnectEx<Client>.RejectionCode rejectionCode)
        {
            return SendAsync(new MsgConnectEx(rejectionCode), () =>
            {
                Disconnect();
                return Task.CompletedTask;
            });
        }
    }
}
