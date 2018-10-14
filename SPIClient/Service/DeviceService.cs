using System.Threading.Tasks;
using RestSharp;

namespace SPIClient.Service
{
    public class DeviceIpAddressStatus
    {
        public string Ip { get; set; }
        public string Last_updated { get; set; }
    }

    public class DeviceIpAddressService
    {
        private const string ApiKeyHeader = "ASM-MSP-DEVICE-ADDRESS-API-KEY";

        public async Task<DeviceIpAddressStatus> RetrieveService(string serialNumber, string apiKey)
        {
            var deviceIpUrl =
                $"https://device-address-api-dev.nonprod-wbc.msp.assemblypayments.com/v1/{serialNumber}/ip";

            var ipService = new HttpBaseService(deviceIpUrl);
            var request = new RestRequest(Method.GET);
            request.AddHeader(ApiKeyHeader, apiKey);

            var response = await ipService.SendRequest<DeviceIpAddressStatus>(request);
            return response;
        }
    }
}