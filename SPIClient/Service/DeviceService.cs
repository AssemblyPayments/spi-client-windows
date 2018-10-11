using System.Threading.Tasks;
using RestSharp;

namespace SPIClient.Service
{
    public class DeviceStatus
    {
        public string Ip { get; set; }
        public string Last_updated { get; set; }
    }

    public class DeviceIpService
    {
        private const string ApiKeyHeader = "ASM-MSP-DEVICE-ADDRESS-API-KEY";

        public async Task<DeviceStatus> RetrieveService(string serialNumber, string apiKey)
        {
            var deviceIpUrl =
                $"https://device-address-api-dev.nonprod-wbc.msp.assemblypayments.com/v1/{serialNumber}/ip";

            var ipService = new HttpBaseService(deviceIpUrl);
            var request = new RestRequest(Method.GET);
            request.AddHeader(ApiKeyHeader, apiKey);

            var response = await ipService.SendRequest<DeviceStatus>(request);
            return response;
        }
    }
}