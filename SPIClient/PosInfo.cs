using Newtonsoft.Json.Linq;
using SPIClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPIClient
{
    public class SetPosInfoRequest
    {
        private string _version;
        private string _vendorId;
        private string _libraryLanguage;
        private string _libraryVersion;
        private Dictionary<string, string> _otherInfo;

        public SetPosInfoRequest(string version, string vendorId, string libraryLanguage, string libraryVersion, Dictionary<string, string> otherInfo)
        {
            _version = version;
            _vendorId = vendorId;
            _libraryLanguage = libraryLanguage;
            _libraryVersion = libraryVersion;
            _otherInfo = otherInfo;
        }

        public Message toMessage()
        {
            var data = new JObject(
                new JProperty("pos_version", _version),
                new JProperty("pos_vendor_id", _vendorId),
                new JProperty("library_language", _libraryLanguage),
                new JProperty("library_version", _libraryVersion),
                new JProperty("other_info", _otherInfo.ToString())
                );

            return new Message(RequestIdHelper.Id("prav"), Events.SetPosInfoRequest, data, true);
        }
    }

    public class SetPosInfoResponse
    {
        private bool _success;
        private Message _m;

        public SetPosInfoResponse(Message m)
        {
            _success = m.GetSuccessState() == Message.SuccessState.Success;
            _m = m;
        }
        public bool isSuccess()
        {
            return _success;
        }
        public String getErrorReason()
        {
            return _m.GetDataStringValue("error_reason");
        }
        public String getErrorDetail()
        {
            return _m.GetDataStringValue("error_detail");
        }
        public String getResponseValueWithAttribute(String attribute)
        {
            return _m.GetDataStringValue(attribute);
        }
    }

    public class DeviceInfo
    {
        private DeviceInfo() { }

        public static Dictionary<string, string> GetAppDeviceInfo()
        {
            Dictionary<string, string> deviceInfo = new Dictionary<string, string>();
            deviceInfo.Add("device_system", Environment.OSVersion.Platform.ToString() + " " + Environment.OSVersion.Version.ToString());
            return deviceInfo;
        }
    }
}
