﻿using SPIClient;
using SPIClient.Service;
using System.Threading.Tasks;
using Xunit;

namespace Test
{
    public class AutoAddressResolutionTest
    {
        [Fact]
        public void TestSetSerialNumber()
        {
            string serialNumber = "111-111-111";
            Spi spi = new Spi("", "", "", null);
            SpiClientTestUtils.SetInstanceField(spi, "_currentStatus", SpiStatus.Unpaired);
            spi.SetSerialNumber(serialNumber);
            Assert.Equal(serialNumber, SpiClientTestUtils.GetInstanceField(typeof(Spi), spi, "_serialNumber"));
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
            var addressResponse = await deviceService.RetrieveService(serialNumber, apiKey, acquirerCode, true);

            Assert.NotNull(addressResponse);
            Assert.Equal(addressResponse.StatusDescription, "Not Found");
        }

        [Fact]
        public async Task TestAutoResolveEftposAddressWithValidSerialNumberAsync()
        {
            string apiKey = "RamenPosDeviceAddressApiKey";
            string acquirerCode = "wbc";
            string serialNumber = "321-404-842";//should be valid serial number

            DeviceAddressService deviceService = new DeviceAddressService();
            var addressResponse = await deviceService.RetrieveService(serialNumber, apiKey, acquirerCode, true);

            Assert.NotNull(addressResponse);
            Assert.NotNull(addressResponse.Data.Address);
            Assert.Equal(addressResponse.Data.DeviceAddressResponseCode, DeviceAddressResponseCode.SUCCESS);
            Assert.Null(addressResponse.Data.LastUpdated);
            Assert.Null(addressResponse.Data.ResponseMessage);
            Assert.Null(addressResponse.Data.ResponseStatusDescription);
        }
    }
}
