using Canyon.Network.Packets.Piglet;
using Org.BouncyCastle.Math;
using System.Security.Cryptography;

namespace Canyon.Game.Sockets.Piglet.Packets
{
    public sealed class MsgPigletHandshake : MsgPigletHandshake<PigletActor>
    {
        public MsgPigletHandshake()
        {
        }

        public MsgPigletHandshake(BigInteger publicKey, BigInteger modulus, byte[] eIv, byte[] dIv)
            : base(publicKey, modulus, eIv ?? new byte[16], dIv ?? new byte[16])
        {
        }

        public override async Task ProcessAsync(PigletActor client)
        {
            if (!client.DiffieHellman.Initialize(Data.PublicKey, Data.Modulus))
            {
                throw new Exception("Could not initialize Diffie-Helmman!!!");
            }

            byte[] iv = RandomNumberGenerator.GetBytes(16);
            await client.SendAsync(new MsgPigletHandshake(client.DiffieHellman.PublicKey, client.DiffieHellman.Modulus, iv, iv), async () =>
            {
                client.Cipher.GenerateKeys(new object[]
                {
                    client.DiffieHellman.SharedKey.ToByteArrayUnsigned(),
                    iv,
                    iv
                });
                await client.SendAsync(new MsgPigletLogin(ServerConfiguration.Configuration.Piglet.Username,
                    ServerConfiguration.Configuration.Piglet.Password,
                    ServerConfiguration.Configuration.Realm.Name));
            });
        }
    }
}
