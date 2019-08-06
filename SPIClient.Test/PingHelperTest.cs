using SPIClient;
using Xunit;

namespace Test
{
    public class PingHelperTest
    {
        [Fact]
        public void GeneratePingRequest_OnValidRequest_IsSet()
        {
            // act
            var msg = PingHelper.GeneratePingRequest();

            // assert
            Assert.Equal(msg.EventName, "ping");
        }

        [Fact]
        public void GeneratePongResponse_OnValidResponse_ReturnObjects()
        {
            // arrange
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""datetime"":""2019-06-14T18:47:55.411"",""event"":""pong"",""id"":""ping563""}}";

            // act
            var msg = Message.FromJson(jsonStr, secrets);
            var message = PongHelper.GeneratePongRessponse(msg);

            // assert
            Assert.Equal(msg.EventName, "pong");
        }
    }
}
