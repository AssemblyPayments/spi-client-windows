using System;

namespace SPIClient
{
    public class DeviceIpAddressChangedEventArgs : EventArgs
    {
        public string DeviceIpAddress { get; set; }

        public DeviceIpAddressChangedEventArgs() {}
    }
}
