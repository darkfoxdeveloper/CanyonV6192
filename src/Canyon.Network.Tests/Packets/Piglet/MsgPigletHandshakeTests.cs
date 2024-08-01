using Microsoft.VisualStudio.TestTools.UnitTesting;
using Canyon.Network.Tests.BaseImpl;

namespace Canyon.Network.Packets.Piglet.Tests
{
    [TestClass()]
    public class MsgPigletHandshakeTests
    {
        [TestMethod()]
        public void MsgPigletHandshakeTest()
        {
            MsgPigletHandshake encodeTest = new MsgPigletHandshake();
            encodeTest.Data = new MsgPigletHandshake<TcpActor>.HandshakeData
            {
                StartPadding = new byte[8],
                EncryptIV = new byte[16],
                MiddlePadding = new byte[8],
                DecryptIV = new byte[16],
                Modulus = new byte[8],
                PublicKey = new byte[128],
                FinalTrash = new byte[8],
                FinalPadding = new byte[8]
            };

            byte[] encoded = encodeTest.Encode();

            MsgPigletHandshake decodeTest = new MsgPigletHandshake();
            decodeTest.Decode(encoded);

            Assert.AreEqual(encodeTest.Data.StartPadding, decodeTest.Data.StartPadding);
            Assert.AreEqual(encodeTest.Data.EncryptIV, decodeTest.Data.EncryptIV);
            Assert.AreEqual(encodeTest.Data.MiddlePadding, decodeTest.Data.MiddlePadding);
            Assert.AreEqual(encodeTest.Data.DecryptIV, decodeTest.Data.DecryptIV);
            Assert.AreEqual(encodeTest.Data.Modulus, decodeTest.Data.Modulus);
            Assert.AreEqual(encodeTest.Data.PublicKey, decodeTest.Data.PublicKey);
            Assert.AreEqual(encodeTest.Data.FinalTrash, decodeTest.Data. FinalTrash);
            Assert.AreEqual(encodeTest.Data.FinalPadding, decodeTest.Data.FinalPadding);
        }

        public class MsgPigletHandshake : MsgPigletHandshake<TcpActor>
        {
        }        
    }
}