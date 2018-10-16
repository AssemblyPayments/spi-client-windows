using System;

namespace SPIClient
{
    public class DeviceIpAddressChangedEventArgs : EventArgs
    {
        public string DeviceIpAddress { get; set; }

        public DeviceIpAddressChangedEventArgs() {}
    }

    public class DeviceIpAddressRequest
    {
        public string ApiKey { get; set; }
        public string SerialNumber { get; set; }
    }
}
