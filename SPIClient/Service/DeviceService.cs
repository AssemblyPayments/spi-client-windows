using System.Threading.Tasks;
using RestSharp;

namespace SPIClient.Service
{
    public class DeviceAddressStatus
    {
        public string Ip { get; set; }
        public string Last_updated { get; set; }
    }

    public class DeviceAddressService
    {
        private const string ApiKeyHeader = "ASM-MSP-DEVICE-ADDRESS-API-KEY";

        public async Task<DeviceAddressStatus> RetrieveService(string serialNumber, string apiKey, bool isTestMode)
        {
            // TODO: Replace with sandbox and prod urls
            var deviceAddressUri = isTestMode ? $"https://device-address-api-dev.nonprod-wbc.msp.assemblypayments.com/v1/{serialNumber}/ip" : $"https://device-address-api-dev.nonprod-wbc.msp.assemblypayments.com/v1/{serialNumber}/ip";

            var addressService = new HttpBaseService(deviceAddressUri);
            var request = new RestRequest(Method.GET);
            request.AddHeader(ApiKeyHeader, apiKey);

            var response = await addressService.SendRequest<DeviceAddressStatus>(request);
            return response;
        }
    }
}