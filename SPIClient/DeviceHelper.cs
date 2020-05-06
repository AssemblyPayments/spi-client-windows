using RestSharp;
using SPIClient.Service;
using System.Net;

namespace SPIClient
{
    internal static class DeviceHelper
    {
        internal static DeviceAddressStatus GenerateDeviceAddressStatus(IRestResponse<DeviceAddressStatus> serviceResponse, string currentEftposAddress)
        {
            DeviceAddressStatus deviceAddressStatus = new DeviceAddressStatus();

            if (serviceResponse.StatusCode == HttpStatusCode.NotFound)
            {
                deviceAddressStatus.DeviceAddressResponseCode = DeviceAddressResponseCode.INVALID_SERIAL_NUMBER;
                return deviceAddressStatus;
            }

            if (serviceResponse?.Data == null || serviceResponse.StatusCode != HttpStatusCode.OK)
            {
                deviceAddressStatus.DeviceAddressResponseCode = DeviceAddressResponseCode.DEVICE_SERVICE_ERROR;
                return deviceAddressStatus;
            }

            if (serviceResponse.Data.Address == currentEftposAddress.Replace("ws://", ""))
            {
                deviceAddressStatus.DeviceAddressResponseCode = DeviceAddressResponseCode.ADDRESS_NOT_CHANGED;
                return deviceAddressStatus;
            }

            deviceAddressStatus.DeviceAddressResponseCode = DeviceAddressResponseCode.SUCCESS;
            deviceAddressStatus.ResponseStatusDescription = serviceResponse.StatusDescription;
            deviceAddressStatus.Address = serviceResponse.Data.Address;
            deviceAddressStatus.LastUpdated = serviceResponse.Data.LastUpdated;

            return deviceAddressStatus;
        }
    }
}
