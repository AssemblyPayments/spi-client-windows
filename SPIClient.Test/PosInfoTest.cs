using SPIClient;
using System.Collections.Generic;
using Xunit;

namespace Test
{
    public class PosInfoTest
    {
        [Fact]
        public void SetPosInfoRequest_ValidRequest_IsSet()
        {
            // arrange
            const string version = "2.6.0";
            const string vendorId = "25";
            const string libraryLanguage = ".Net";
            const string libraryVersion = "2.6.0";
            const string eventName = Events.SetPosInfoRequest;
            var request = new SetPosInfoRequest(version, vendorId, libraryLanguage, libraryVersion, new Dictionary<string, string>());

            // act
            var msg = request.toMessage();

            // assert
            Assert.Equal(eventName, msg.EventName);
            Assert.Equal(version, msg.GetDataStringValue("pos_version"));
            Assert.Equal(vendorId, msg.GetDataStringValue("pos_vendor_id"));
            Assert.Equal(libraryLanguage, msg.GetDataStringValue("library_language"));
            Assert.Equal(libraryVersion, msg.GetDataStringValue("library_version"));
        }

        [Fact]
        public void SetPosInfoResponse_ValidResponse_IsSet()
        {
            // arrange
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""data"":{""success"":true},""datetime"":""2019-06-07T10:53:31.517"",""event"":""set_pos_info_response"",""id"":""prav3""}}";
            var msg = Message.FromJson(jsonStr, secrets);

            // act
            var response = new SetPosInfoResponse(msg);

            // assert
            Assert.Equal("set_pos_info_response", msg.EventName);
            Assert.True(response.isSuccess());
            Assert.Equal("", response.getErrorReason());
            Assert.Equal("", response.getErrorDetail());
            Assert.Equal("", response.getResponseValueWithAttribute("error_detail"));
        }
    }
}
