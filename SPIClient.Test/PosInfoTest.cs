using SPIClient;
using System.Collections.Generic;
using Xunit;

namespace Test
{
    public class PosInfoTest
    {
        [Fact]
        public void TestSetPosInfoRequest()
        {
            string version = "2.6.0";
            string vendorId = "25";
            string libraryLanguage = ".Net";
            string libraryVersion = "2.6.0";

            SetPosInfoRequest request = new SetPosInfoRequest(version, vendorId, libraryLanguage, libraryVersion, new Dictionary<string, string>());
            Message msg = request.toMessage();

            Assert.Equal(msg.EventName, "set_pos_info");
            Assert.Equal(version, msg.GetDataStringValue("pos_version"));
            Assert.Equal(vendorId, msg.GetDataStringValue("pos_vendor_id"));
            Assert.Equal(libraryLanguage, msg.GetDataStringValue("library_language"));
            Assert.Equal(libraryVersion, msg.GetDataStringValue("library_version"));
        }

        [Fact]
        public void TestSetPosInfoResponse()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""data"":{""success"":true},""datetime"":""2019-06-07T10:53:31.517"",""event"":""set_pos_info_response"",""id"":""prav3""}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            SetPosInfoResponse response = new SetPosInfoResponse(msg);

            Assert.Equal(msg.EventName, "set_pos_info_response");
            Assert.True(response.isSuccess());
            Assert.Equal(response.getErrorReason(), "");
            Assert.Equal(response.getErrorDetail(), "");
            Assert.Equal(response.getResponseValueWithAttribute("error_detail"), "");
        }
    }
}
