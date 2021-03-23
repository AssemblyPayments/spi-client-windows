using SPIClient;
using SPIClient.Service;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Test
{
    public class AutoAddressResolutionTest
    {
        [Fact]
        public void SetSerialNumber_ValidSerialNumber_IsSet()
        {
            // arrange
            const string serialNumber = "111-111-111";
            var spi = new Spi("", "", "", null);
            SpiClientTestUtils.SetInstanceField(spi, "_currentStatus", SpiStatus.Unpaired);

            // act
            spi.SetSerialNumber(serialNumber);

            // assert
            Assert.Equal(serialNumber, SpiClientTestUtils.GetInstanceField(typeof(Spi), spi, "_serialNumber"));
        }

        [Fact]
        public void SetAutoAddressResolution_TurnOnAutoAddress_Enabled()
        {
            // arrange
            const bool autoAddressResolutionEnable = true;
            var spi = new Spi("", "", "", null);
            SpiClientTestUtils.SetInstanceField(spi, "_currentStatus", SpiStatus.Unpaired);

            // act
            spi.SetAutoAddressResolution(autoAddressResolutionEnable);

            // assert
            Assert.Equal(autoAddressResolutionEnable, SpiClientTestUtils.GetInstanceField(typeof(Spi), spi, "_autoAddressResolutionEnabled"));
        }

        [Fact]
        public async void RetrieveDeviceAddress_SerialNumberNotRegistered_NotFound()
        {
            // arrange
            const string apiKey = "RamenPosDeviceAddressApiKey";
            const string tenantCode = "wbc";
            const string serialNumber = "111-111-111"; // invalid serial number
            var deviceService = new DeviceAddressService();

            // act
            var addressResponse = await deviceService.RetrieveDeviceAddress(serialNumber, apiKey, tenantCode, true);

            // assert
            Assert.NotNull(addressResponse);
            Assert.Equal(DeviceAddressResponseCode.SUCCESS, addressResponse.Data.DeviceAddressResponseCode);
            Assert.Equal(HttpStatusCode.NotFound, addressResponse.StatusCode);
            Assert.Equal("Not Found", addressResponse.StatusDescription);
        }

        [Fact]
        public async Task RetrieveDeviceAddress_SerialNumberRegistered_Found()
        {
            // arrange
            const string apiKey = "RamenPosDeviceAddressApiKey";
            const string tenantCode = "wbc";
            const string serialNumber = "321-404-842";
            var deviceService = new DeviceAddressService();

            // act
            var addressResponse = await deviceService.RetrieveDeviceAddress(serialNumber, apiKey, tenantCode, true);

            // assert
            Assert.NotNull(addressResponse);
            Assert.NotNull(addressResponse.Data.Address);
            Assert.Equal(DeviceAddressResponseCode.SUCCESS, addressResponse.Data.DeviceAddressResponseCode);
        }

        [Fact]
        public void GetTerminalAddress_OnRegisteredSerialNumber_ReturnAddress()
        {
            // arrange
            const string apiKey = "RamenPosDeviceAddressApiKey";
            const string tenantCode = "wbc";
            const string serialNumber = "328-513-254";
            var spi = new Spi("Pos1", serialNumber, "", null);
            SpiClientTestUtils.SetInstanceField(spi, "_currentStatus", SpiStatus.Unpaired);
            spi.SetTestMode(true);
            spi.SetDeviceApiKey(apiKey);
            spi.SetTenantCode(tenantCode);

            // act
            var response = spi.GetTerminalAddress();
            var ipAddress = response.Result;

            // assert
            Assert.NotNull(ipAddress);
        }

        [Fact]
        public void GetTerminalAddress_OnNotRgisteredSerialNumber_ReturnAddress()
        {
            // arrange
            const string apiKey = "RamenPosDeviceAddressApiKey";
            const string tenantCode = "gko";
            const string serialNumber = "123-456-789";
            var spi = new Spi("Pos1", serialNumber, "", null);
            SpiClientTestUtils.SetInstanceField(spi, "_currentStatus", SpiStatus.Unpaired);
            spi.SetTestMode(true);
            spi.SetDeviceApiKey(apiKey);
            spi.SetTenantCode(tenantCode);

            // act
            var response = spi.GetTerminalAddress();
            var ipAddress = response.Result;

            // assert
            Assert.Null(ipAddress);
        }
    }
}
