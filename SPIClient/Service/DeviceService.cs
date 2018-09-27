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
        public DeviceService RetrieveService(string serialNumber)
        {
            var deviceIpUrl = $"https://device-address-api-dev.nonprod-wbc.msp.assemblypayments.com/v1/{serialNumber}/ip";
            var ipService = new RestServiceHelper(deviceIpUrl);
            var request = new RestRequest(Method.GET);
            var response = ipService.SendRequest<DeviceService>(request);

            return response;
        }
    }
}
