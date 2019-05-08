using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using SPIClient;
using SPIClient.Service;

namespace Test
{
    public class AutoAddressResolutionTest
    {
        [Fact]
        public void TestSetSerialNumber()
        {
            string serialnumber = "111-111-111";
            Spi spi = new Spi("", "", "", null);
            SpiClientTestUtils.SetInstanceField(spi, "_currentStatus", SpiStatus.Unpaired);
            spi.SetSerialNumber(serialnumber);
            Assert.Equal(serialnumber, SpiClientTestUtils.GetInstanceField(typeof(Spi), spi, "_serialNumber"));
        }

        [Fact]
        public void TestSetAutoAddressResolution()
        {
            bool autoAddressResolutionEnable = true;
            Spi spi = new Spi("", "", "", null);
            SpiClientTestUtils.SetInstanceField(spi, "_currentStatus", SpiStatus.Unpaired);
            spi.SetAutoAddressResolution(autoAddressResolutionEnable);
            Assert.Equal(autoAddressResolutionEnable, SpiClientTestUtils.GetInstanceField(typeof(Spi), spi, "_autoAddressResolutionEnabled"));
        }

        [Fact]
        public async void TestAutoResolveEftposAddressWithIncorectSerialNumberAsync()
        {
            string apiKey = "RamenPosDeviceAddressApiKey";
            string acquirerCode = "wbc";
            string serialNumber = "111-111-111";

            DeviceAddressService deviceService = new DeviceAddressService();
            DeviceAddressStatus addressResponse = await deviceService.RetrieveService(serialNumber, apiKey, acquirerCode, true);

            Assert.Null(addressResponse);
        }

        [Fact]
        public async Task testAutoResolveEftposAddressWithValidSerialNumberAsync()
        {
            string apiKey = "RamenPosDeviceAddressApiKey";
            string acquirerCode = "wbc";
            string serialNumber = "321-404-842";//should be valid serial number

            DeviceAddressService deviceService = new DeviceAddressService();
            DeviceAddressStatus addressResponse = await deviceService.RetrieveService(serialNumber, apiKey, acquirerCode, true);

            Assert.NotNull(addressResponse);
            Assert.NotNull(addressResponse.Address);
        }
    }
}
