using SPIClient;
using Xunit;

namespace Test
{
    public class TerminalTest
    {
        [Fact]
        public void TestTerminalStatusRequest()
        {
            TerminalStatusRequest request = new TerminalStatusRequest();
            Message msg = request.ToMessage();

            Assert.Equal(msg.EventName, "get_terminal_status");
        }

        [Fact]
        public void TestTerminalStatusResponse()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""data"":{""battery_level"":""100"",""charging"":true,""status"":""IDLE"",""success"":true},""datetime"":""2019-06-18T13:00:38.820"",""event"":""terminal_status"",""id"":""trmnl4""}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            TerminalStatusResponse response = new TerminalStatusResponse(msg);

            Assert.Equal(msg.EventName, "terminal_status");
            Assert.True(response.isSuccess());
            Assert.Equal(response.GetBatteryLevel(), "100");
            Assert.Equal(response.GetStatus(), "IDLE");
            Assert.True(response.IsCharging());

            response = new TerminalStatusResponse();
            Assert.False(response.isSuccess());
        }

        [Fact]
        public void TestTerminalBattery()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""data"":{""battery_level"":""40""},""datetime"":""2019-06-18T13:02:41.777"",""event"":""battery_level_changed"",""id"":""C1.3""}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            TerminalBattery response = new TerminalBattery(msg);

            Assert.Equal(msg.EventName, "battery_level_changed");
            Assert.Equal(response.BatteryLevel, "40");

            response = new TerminalBattery();
            Assert.Null(response.BatteryLevel);
        }

        [Fact]
        public void TestTerminalConfigurationRequest()
        {
            TerminalConfigurationRequest request = new TerminalConfigurationRequest();
            Message msg = request.ToMessage();

            Assert.Equal(msg.EventName, "get_terminal_configuration");
        }

        [Fact]
        public void TestTerminalConfigurationResponse()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""data"":{""comms_selected"":""WIFI"",""merchant_id"":""22341842"",""pa_version"":""SoftPay03.16.03"",""payment_interface_version"":""02.02.00"",""plugin_version"":""v2.6.11"",""serial_number"":""321-404-842"",""success"":true,""terminal_id"":""12348842"",""terminal_model"":""VX690""},""datetime"":""2019-06-18T13:00:41.075"",""event"":""terminal_configuration"",""id"":""trmnlcnfg5""}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            TerminalConfigurationResponse response = new TerminalConfigurationResponse(msg);

            Assert.Equal(msg.EventName, "terminal_configuration");
            Assert.True(response.isSuccess());
            Assert.Equal(response.GetCommsSelected(), "WIFI");
            Assert.Equal(response.GetMerchantId(), "22341842");
            Assert.Equal(response.GetPAVersion(), "SoftPay03.16.03");
            Assert.Equal(response.GetPaymentInterfaceVersion(), "02.02.00");
            Assert.Equal(response.GetPluginVersion(), "v2.6.11");
            Assert.Equal(response.GetSerialNumber(), "321-404-842");
            Assert.Equal(response.GetTerminalId(), "12348842");
            Assert.Equal(response.GetTerminalModel(), "VX690");

            response = new TerminalConfigurationResponse();
            Assert.False(response.isSuccess());
        }
    }
}
