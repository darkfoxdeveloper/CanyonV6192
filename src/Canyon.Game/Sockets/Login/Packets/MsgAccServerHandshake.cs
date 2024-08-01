using Canyon.Network.Packets.Login;
using Org.BouncyCastle.Math;
using System.Security.Cryptography;

namespace Canyon.Game.Sockets.Login.Packets
{
    public sealed class MsgAccServerHandshake : MsgAccServerHandshake<LoginActor>
    {
        public MsgAccServerHandshake()
        {
        }

        public MsgAccServerHandshake(BigInteger publicKey, BigInteger modulus, byte[] eIv, byte[] dIv)
            : base(publicKey, modulus, eIv ?? new byte[16], dIv ?? new byte[16])
        {
        }

        public override async Task ProcessAsync(LoginActor client)
        {
            if (!client.DiffieHellman.Initialize(Data.PublicKey, Data.Modulus))
            {
                throw new Exception("Could not initialize Diffie-Helmman!!!");
            }

            byte[] iv = RandomNumberGenerator.GetBytes(16);
            await client.SendAsync(new MsgAccServerHandshake(client.DiffieHellman.PublicKey, client.DiffieHellman.Modulus, iv, iv), async () =>
            {
                client.Cipher.GenerateKeys(new object[]
                {
                    client.DiffieHellman.SharedKey.ToByteArrayUnsigned(),
                    iv,
                    iv
                });
                await client.SendAsync(new MsgAccServerAuth
                {
                    Data = new MsgAccServerAuth<LoginActor>.ServerAuthData
                    {
                        RealmId = ServerConfiguration.Configuration.Realm.ServerId.ToString(),
                        Username = ServerConfiguration.Configuration.Realm.Username,
                        Password = ServerConfiguration.Configuration.Realm.Password
                    }
                });
            });
        }
    }
}
