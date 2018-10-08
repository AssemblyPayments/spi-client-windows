using System;
using System.Threading.Tasks;
using RestSharp;

namespace SPIClient.Service
{
    public class DeviceService
    {
        public string Ip { get; set; }
        public string Last_updated { get; set; }
    }

    public class DeviceIpService
    {
        public async Task<DeviceService> RetrieveService(string serialNumber)
        {
            var deviceIpUrl =
                $"https://device-address-api-dev.nonprod-wbc.msp.assemblypayments.com/v1/{serialNumber}/ip";
            var ipService = new RestServiceHelper(deviceIpUrl);
            var request = new RestRequest(Method.GET);
            request.AddHeader("ASM-MSP-DEVICE-ADDRESS-API-KEY", "KebabPosRK");
            var response = await ipService.SendRequest<DeviceService>(request);

            return response;
        }
    }
}