using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SPIClient
{
    public class TerminalStatusRequest
    {
        public TerminalStatusRequest() { }

        public Message ToMessage()
        {
            var data = new JObject();

            return new Message(RequestIdHelper.Id("trmnl"), Events.TerminalStatusRequest, data, true);
        }
    }

    /// <summary>
    /// These attributes work for COM interop.
    /// </summary>
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class TerminalStatusResponse
    {
        private bool _success;
        private Message _m;

        public TerminalStatusResponse() { }

        public TerminalStatusResponse(Message m)
        {
            _success = m.GetSuccessState() == Message.SuccessState.Success;
            _m = m;
        }

        public bool isSuccess()
        {
            return _success;
        }

        public string GetStatus()
        {
            return _m.GetDataStringValue("status");
        }

        public string GetBatteryLevel()
        {
            return _m.GetDataStringValue("battery_level");
        }

        public bool IsCharging()
        {
            return _m.GetDataBoolValue("charging", false);
        }
    }

    /// <summary>
    /// These attributes work for COM interop.
    /// </summary>

    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class TerminalBattery
    {
        public string BatteryLevel { get; }

        public TerminalBattery() { }

        public TerminalBattery(Message m)
        {
            BatteryLevel = m.GetDataStringValue("battery_level");
        }
    }

    public class TerminalConfigurationRequest
    {
        public TerminalConfigurationRequest() { }

        public Message ToMessage()
        {
            var data = new JObject();

            return new Message(RequestIdHelper.Id("trmnlcnfg"), Events.TerminalConfigurationRequest, data, true);
        }
    }

    /// <summary>
    /// These attributes work for COM interop.
    /// </summary>
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class TerminalConfigurationResponse
    {
        private bool _success;
        private Message _m;

        public TerminalConfigurationResponse() { }

        public TerminalConfigurationResponse(Message m)
        {
            _success = m.GetSuccessState() == Message.SuccessState.Success;
            _m = m;
        }

        public bool isSuccess()
        {
            return _success;
        }

        public string GetCommsSelected()
        {
            return _m.GetDataStringValue("comms_selected");
        }

        public string GetMerchantId()
        {
            return _m.GetDataStringValue("merchant_id");
        }

        public string GetPAVersion()
        {
            return _m.GetDataStringValue("pa_version");
        }

        public string GetPaymentInterfaceVersion()
        {
            return _m.GetDataStringValue("payment_interface_version");
        }

        public string GetPluginVersion()
        {
            return _m.GetDataStringValue("plugin_version");
        }

        public string GetSerialNumber()
        {
            return _m.GetDataStringValue("serial_number");
        }

        public string GetTerminalId()
        {
            return _m.GetDataStringValue("terminal_id");
        }

        public string GetTerminalModel()
        {
            return _m.GetDataStringValue("terminal_model");
        }
    }
}
