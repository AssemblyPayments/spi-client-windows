using SPIClient;
using Xunit;

namespace Test
{
    public class PrintingTest
    {
        [Fact]
        public void PrintingRequest_OnValidRequest_ReturnObjects()
        {
            // arrange
            const string key = "test";
            const string payload = "test";

            // act
            var request = new PrintingRequest(key, payload);
            var msg = request.ToMessage();

            // assert
            Assert.Equal("print", msg.EventName);
            Assert.Equal(key, msg.GetDataStringValue("key"));
            Assert.Equal(payload, msg.GetDataStringValue("payload"));
        }

        [Fact]
        public void PrintingResponse_OnValidResponse_ReturnObjects()
        {
            // arrange
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""data"":{""success"":true},""datetime"":""2019-06-14T18:51:00.948"",""event"":""print_response"",""id"":""C24.0""}}";
            var msg = Message.FromJson(jsonStr, secrets);

            // act
            var response = new PrintingResponse(msg);

            // assert
            Assert.Equal("print_response", msg.EventName);
            Assert.True(response.IsSuccess());
            Assert.Equal("C24.0", msg.Id);
            Assert.Equal("", response.GetErrorReason());
            Assert.Equal("", response.GetErrorDetail());
            Assert.Equal("", response.GetResponseValueWithAttribute("error_detail"));
        }
    }
}
