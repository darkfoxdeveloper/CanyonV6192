using Canyon.Network.Packets.Piglet;
using Canyon.Network.Security;
using Org.BouncyCastle.Math;
using System.Security.Cryptography;

namespace Canyon.GM.Server.Sockets.Panel.Packets
{
    public sealed class MsgPigletHandshake : MsgPigletHandshake<PanelActor>
    {
        private static readonly byte[] AesKey;

        static MsgPigletHandshake()
        {
            const string strKey = "91311a21e0285d46677aa75d44c727ac";
            AesKey = new byte[strKey.Length / 2];
            for (var index = 0; index < AesKey.Length; index++)
            {
                string byteValue = strKey.Substring(index * 2, 2);
                AesKey[index] = Convert.ToByte(byteValue, 16);
            }
        }

        public MsgPigletHandshake()
        {
        }

        public MsgPigletHandshake(BigInteger publicKey, BigInteger modulus, byte[] eIv, byte[] dIv)
            : base(publicKey, modulus, eIv ?? new byte[16], dIv ?? new byte[16])
        {
        }

        public override async Task ProcessAsync(PanelActor client)
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

                string username = AesCipherHelper.Decrypt(AesKey, Program.ServerConfiguration.Socket.UserName);
                string password = AesCipherHelper.Decrypt(AesKey, Program.ServerConfiguration.Socket.Password);
                await client.SendAsync(new MsgPigletLogin(username, password, Program.ServerConfiguration.RealmName));
            });
        }
    }
}
