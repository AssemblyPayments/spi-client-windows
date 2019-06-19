using SPIClient;
using Xunit;

namespace Test
{
    public class PrintingTest
    {
        [Fact]
        public void TestPrintingRequest()
        {
            string key = "test";
            string payload = "test";

            PrintingRequest request = new PrintingRequest(key, payload);
            Message msg = request.ToMessage();

            Assert.Equal(msg.EventName, "print");
            Assert.Equal(key, msg.GetDataStringValue("key"));
            Assert.Equal(payload, msg.GetDataStringValue("payload"));
        }

        [Fact]
        public void TestPrintingResponse()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""data"":{""success"":true},""datetime"":""2019-06-14T18:51:00.948"",""event"":""print_response"",""id"":""C24.0""}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            PrintingResponse response = new PrintingResponse(msg);

            Assert.Equal(msg.EventName, "print_response");
            Assert.True(response.IsSuccess());
            Assert.Equal(msg.Id, "C24.0");
            Assert.Equal(response.GetErrorReason(), "");
            Assert.Equal(response.GetErrorDetail(), "");
            Assert.Equal(response.GetResponseValueWithAttribute("error_detail"), "");
        }
    }
}
