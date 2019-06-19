using Newtonsoft.Json.Linq;
using SPIClient;
using System;
using Xunit;

namespace Test
{
    public class MessagesTest
    {
        [Fact]
        public void TestIncomingMessageUnencrypted()
        {
            // Here's an incoming msg from the server
            var msgJsonStr = @"{""message"": {""event"": ""event_x"",""id"": ""62"",""data"": {""param1"": ""value1""}}}";

            // Let's parse it, I don't have secrets yet. I don't expect it to be encryted
            var m = Message.FromJson(msgJsonStr, null);

            // And test that it's what we expected
            Assert.Equal("event_x", m.EventName);
            Assert.Equal("value1", (string)m.Data["param1"]);
        }

        [Fact]
        public void TestIncomingMessageEncrypted()
        {
            // Here's an incoming encrypted msg
            var msgJsonStr = @"{""enc"": ""819A6FF34A7656DBE5274AC44A28A48DD6D723FCEF12570E4488410B83A1504084D79BA9DF05C3CE58B330C6626EA5E9EB6BAAB3BFE95345A8E9834F183A1AB2F6158E8CDC217B4970E6331B4BE0FCAA"",""hmac"": ""21FB2315E2FB5A22857F21E48D3EEC0969AD24C0E8A99C56A37B66B9E503E1EF""}";

            // Here are our secrets
            var secrets = new Secrets("11A1162B984FEF626ECC27C659A8B0EEAD5248CA867A6A87BEA72F8A8706109D", "40510175845988F13F6162ED8526F0B09F73384467FA855E1E79B44A56562A58");

            // Let's parse it
            var m = Message.FromJson(msgJsonStr, secrets);

            // And test that it's what we expected
            Assert.Equal("pong", m.EventName);
            Assert.Equal("2017-11-16T21:51:50.499", m.DateTimeStamp);
        }

        [Fact]
        public void TestIncomingMessageEncrypted_BadSig()
        {
            // Here's an incoming encrypted msg
            var msgJsonStr = @"{""enc"": ""819A6FF34A7656DBE5274AC44A28A48DD6D723FCEF12570E4488410B83A1504084D79BA9DF05C3CE58B330C6626EA5E9EB6BAAB3BFE95345A8E9834F183A1AB2F6158E8CDC217B4970E6331B4BE0FCAA"",""hmac"": ""21FB2315E2FB5A22857F21E48D3EEC0969AD24C0E8A99C56A37B66B9E503E1EA""}";

            // Here are our secrets
            var secrets = new Secrets("11A1162B984FEF626ECC27C659A8B0EEAD5248CA867A6A87BEA72F8A8706109D", "40510175845988F13F6162ED8526F0B09F73384467FA855E1E79B44A56562A58");

            // Let's parse it
            var m = Message.FromJson(msgJsonStr, secrets);
            Assert.Equal(Events.InvalidHmacSignature, m.EventName);
        }

        [Fact]
        public void TestOutgoingMessageUnencrypted()
        {
            // Create a message
            JObject data = new JObject(new JProperty("param1", "value1"));
            var m = new Message("77", "event_y", data, false);

            // Serialize it to Json
            var mJson = m.ToJson(new MessageStamp("BAR1", null, TimeSpan.Zero));

            // Let's assert Serialize Result by parsing it back.
            var revertedM = Message.FromJson(mJson, null);
            Assert.Equal("event_y", revertedM.EventName);
            Assert.Equal("value1", (string)revertedM.Data["param1"]);
        }

        [Fact]
        public void TestOutgoingMessageEncrypted()
        {
            // Create a message
            JObject data = new JObject(new JProperty("param1", "value1"));
            var m = new Message("2", "ping", data, true);

            // Here are our secrets
            var secrets = new Secrets("11A1162B984FEF626ECC27C659A8B0EEAD5248CA867A6A87BEA72F8A8706109D", "40510175845988F13F6162ED8526F0B09F73384467FA855E1E79B44A56562A58");

            // Serialize it to Json
            var stamp = new MessageStamp("BAR1", secrets, TimeSpan.Zero);
            var mJson = m.ToJson(stamp);

            // Let's assert Serialize Result by parsing it back.
            var revertedM = Message.FromJson(mJson, secrets);
            Assert.Equal("ping", revertedM.EventName);
            Assert.Equal("value1", (string)revertedM.Data["param1"]);
        }
    }
}