using SPIClient;
using Xunit;

namespace Test
{
    public class PingHelperTest
    {
        [Fact]
        public void TestGeneratePingRequest()
        {
            Message msg = PingHelper.GeneratePingRequest();

            Assert.Equal(msg.EventName, "ping");
        }

        [Fact]
        public void TestGeneratePongResponse()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""datetime"":""2019-06-14T18:47:55.411"",""event"":""pong"",""id"":""ping563""}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            var message = PongHelper.GeneratePongRessponse(msg);
            Assert.Equal(msg.EventName, "pong");
        }
    }
}
