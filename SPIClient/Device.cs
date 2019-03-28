using System;

namespace SPIClient
{
    public class DeviceAddressChangedEventArgs : EventArgs
    {
        public string DeviceAddress { get; set; }

        public DeviceAddressChangedEventArgs() { }
    }
}
