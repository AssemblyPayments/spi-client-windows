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
        public async void RetrieveService_SerialNumberNotRegistered_NotFound()
        {
            // arrange
            const string apiKey = "RamenPosDeviceAddressApiKey";
            const string acquirerCode = "wbc";
            const string serialNumber = "111-111-111"; // invalid serial number
            var deviceService = new DeviceAddressService();

            // act
            var addressResponse = await deviceService.RetrieveService(serialNumber, apiKey, acquirerCode, true);

            // assert
            Assert.NotNull(addressResponse);
            Assert.Equal(DeviceAddressResponseCode.SUCCESS, addressResponse.Data.DeviceAddressResponseCode);
            Assert.Equal(HttpStatusCode.NotFound, addressResponse.StatusCode);
            Assert.Equal("Not Found", addressResponse.StatusDescription);
        }

        [Fact]
        public async Task RetrieveService_SerialNumberRegistered_Found()
        {
            // arrange
            const string apiKey = "RamenPosDeviceAddressApiKey";
            const string acquirerCode = "wbc";
            const string serialNumber = "321-404-842";
            var deviceService = new DeviceAddressService();

            // act
            var addressResponse = await deviceService.RetrieveService(serialNumber, apiKey, acquirerCode, true);

            // assert
            Assert.NotNull(addressResponse);
            Assert.NotNull(addressResponse.Data.Address);
            Assert.Equal(DeviceAddressResponseCode.SUCCESS, addressResponse.Data.DeviceAddressResponseCode);
        }
    }
}
