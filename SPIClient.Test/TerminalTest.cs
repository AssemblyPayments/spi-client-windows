using SPIClient;
using Xunit;

namespace Test
{
    public class TerminalTest
    {
        [Fact]
        public void TerminalStatusRequest_OnValidRequest_ReturnObjects()
        {
            // act
            var request = new TerminalStatusRequest();
            var msg = request.ToMessage();

            // assert
            Assert.Equal("get_terminal_status", msg.EventName);
        }

        [Fact]
        public void TerminalStatusResponse_OnValidResponse_ReturnObjects()
        {
            // arrange
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""data"":{""battery_level"":""100"",""charging"":true,""status"":""IDLE"",""success"":true},""datetime"":""2019-06-18T13:00:38.820"",""event"":""terminal_status"",""id"":""trmnl4""}}";

            // act
            var msg = Message.FromJson(jsonStr, secrets);
            var response = new TerminalStatusResponse(msg);

            // assert
            Assert.Equal("terminal_status", msg.EventName);
            Assert.True(response.isSuccess());
            Assert.Equal("100", response.GetBatteryLevel());
            Assert.Equal("IDLE", response.GetStatus());
            Assert.True(response.IsCharging());

            // act
            response = new TerminalStatusResponse();

            // assert
            Assert.False(response.isSuccess());
        }

        [Fact]
        public void TerminalBattery_OnValidResponse_ReturnObjects()
        {
            // arrange
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""data"":{""battery_level"":""40""},""datetime"":""2019-06-18T13:02:41.777"",""event"":""battery_level_changed"",""id"":""C1.3""}}";

            // act
            var msg = Message.FromJson(jsonStr, secrets);
            var response = new TerminalBattery(msg);

            // assert
            Assert.Equal("battery_level_changed", msg.EventName);
            Assert.Equal("40", response.BatteryLevel);

            // act
            response = new TerminalBattery();

            // assert
            Assert.Null(response.BatteryLevel);
        }

        [Fact]
        public void TerminalConfigurationRequest_OnValidRequest_ReturnObjects()
        {
            // act
            var request = new TerminalConfigurationRequest();
            var msg = request.ToMessage();

            // assert
            Assert.Equal("get_terminal_configuration", msg.EventName);
        }

        [Fact]
        public void TerminalConfigurationResponse_OnValidResponse_ReturnObjects()
        {
            // arrange
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""data"":{""comms_selected"":""WIFI"",""merchant_id"":""22341842"",""pa_version"":""SoftPay03.16.03"",""payment_interface_version"":""02.02.00"",""plugin_version"":""v2.6.11"",""serial_number"":""321-404-842"",""success"":true,""terminal_id"":""12348842"",""terminal_model"":""VX690""},""datetime"":""2019-06-18T13:00:41.075"",""event"":""terminal_configuration"",""id"":""trmnlcnfg5""}}";

            // act
            var msg = Message.FromJson(jsonStr, secrets);
            var response = new TerminalConfigurationResponse(msg);

            // assert
            Assert.Equal("terminal_configuration", msg.EventName);
            Assert.True(response.isSuccess());
            Assert.Equal("WIFI", response.GetCommsSelected());
            Assert.Equal("22341842", response.GetMerchantId());
            Assert.Equal("SoftPay03.16.03", response.GetPAVersion());
            Assert.Equal("02.02.00", response.GetPaymentInterfaceVersion());
            Assert.Equal("v2.6.11", response.GetPluginVersion());
            Assert.Equal("321-404-842", response.GetSerialNumber());
            Assert.Equal("12348842", response.GetTerminalId());
            Assert.Equal("VX690", response.GetTerminalModel());

            // act
            response = new TerminalConfigurationResponse();

            // assert
            Assert.False(response.isSuccess());
        }
    }
}
