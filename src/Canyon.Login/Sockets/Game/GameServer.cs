using Canyon.Login.Managers;
using Canyon.Login.Sockets.Game.Packets;
using Canyon.Login.States;
using Canyon.Network;
using Canyon.Network.Packets;
using Canyon.Network.Sockets;
using Canyon.Shared;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;

namespace Canyon.Login.Sockets.Game
{
    public sealed class GameServer : TcpServerListener<Realm>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<GameServer>();
        private readonly PacketProcessor<Realm> packetProcessor;

        public static GameServer Instance { get; private set; }

        public GameServer()
            : base(10, 4096, false, true, NetworkDefinition.ACCOUNT_FOOTER.Length)
        {
            Instance = this;

            ExchangeStartPosition = 0;

            packetProcessor = new PacketProcessor<Realm>(ProcessAsync, 1);
            packetProcessor.StartAsync(CancellationToken.None);
        }

        protected override async Task<Realm> AcceptedAsync(Socket socket, Memory<byte> buffer)
        {
            var actor = new Realm(socket, buffer);
            await actor.SendAsync(new MsgAccServerHandshake(actor.DiffieHellman.PublicKey, actor.DiffieHellman.Modulus, null, null));
            return actor;
        }

        protected override bool Exchanged(Realm actor, ReadOnlySpan<byte> buffer)
        {
            try
            {
                MsgAccServerHandshake msg = new MsgAccServerHandshake();
                msg.Decode(buffer.ToArray());

                if (!actor.DiffieHellman.Initialize(msg.Data.PublicKey, msg.Data.Modulus))
                {
                    throw new Exception("Could not initialize Diffie-Helmman!!!");
                }

                actor.Cipher.GenerateKeys(new object[]
                {
                    actor.DiffieHellman.SharedKey.ToByteArrayUnsigned(),
                    msg.Data.EncryptIV,
                    msg.Data.DecryptIV
                });
                return true;
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Error when exchanging Account Server data. Error: {}", ex.Message);
                return false;
            }
        }

        protected override void Received(Realm actor, ReadOnlySpan<byte> packet)
        {
            packetProcessor.QueueRead(actor, packet.ToArray());
        }

        public override void Send(Realm actor, ReadOnlySpan<byte> packet)
        {
            packetProcessor.QueueWrite(actor, packet.ToArray());
        }

        public override void Send(Realm actor, ReadOnlySpan<byte> packet, Func<Task> task)
        {
            packetProcessor.QueueWrite(actor, packet.ToArray(), task);
        }

        private async Task ProcessAsync(Realm actor, byte[] packet)
        {
            if (!actor.Socket.Connected)
            {
                return;
            }

            var length = BitConverter.ToUInt16(packet, 0);
            var type = (PacketType)BitConverter.ToUInt16(packet, 2);

            try
            {
                MsgBase<Realm> msg;
                switch (type)
                {
                    case PacketType.MsgAccServerAuth:
                        {
                            msg = new MsgAccServerAuth();
                            break;
                        }

                    case PacketType.MsgAccServerLoginExchangeEx:
                        {
                            msg = new MsgAccServerLoginExchangeEx();
                            break;
                        }

                    case PacketType.MsgAccServerPing:
                        {
                            msg = new MsgAccServerPing();
                            break;
                        }

                    default:
                        {
                            logger.LogWarning($"Missing packet {type}, Length {length}\n{PacketDump.Hex(packet)}");
                            return;
                        }
                }
                // Decode packet bytes into the structure and process
                msg.Decode(packet);
                await msg.ProcessAsync(actor);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Error on process message! {}", ex.Message);
            }
        }

        protected override void Disconnected(Realm actor)
        {
            if (actor != null)
            {
                logger.LogInformation("Realm [{},{}] has disconnected", actor.RealmID, actor.Data?.RealmName);
                RealmManager.RemoveRealm(actor.RealmID);
            }
        }
    }
}
