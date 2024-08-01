using Canyon.Network.Tests.BaseImpl;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Canyon.Network.Packets.Piglet.Tests
{
    [TestClass()]
    public class MsgPigletLoginTests
    {
        [TestMethod()]
        public void EncodeDecodeTest()
        {
            MsgPigletLogin msg = new MsgPigletLogin();
            msg.Data = new MsgPigletLogin<TcpActor>.PigletData
            {
                UserName = "test",
                Password = "test"
            };
            byte[] encoded = msg.Encode();

            MsgPigletLogin result = new MsgPigletLogin();
            result.Decode(encoded);
            Assert.AreEqual(msg.Data.UserName, result.Data.UserName);
            Assert.AreEqual(msg.Data.Password, result.Data.Password);
        }

        public class MsgPigletLogin : MsgPigletLogin<TcpActor>
        {

        }
    }
}