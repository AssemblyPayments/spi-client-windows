using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SPIClient
{
    public class PrintingRequest
    {
        private string _key;
        private string _payload;

        public PrintingRequest(string key, string payload)
        {
            _key = key;
            _payload = payload;
        }

        public Message ToMessage()
        {
            var data = new JObject(
                new JProperty("key", _key),
                new JProperty("payload", _payload)
                );

            return new Message(RequestIdHelper.Id("print"), Events.PrintingRequest, data, true);
        }
    }

    /// <summary>
    /// These attributes work for COM interop.
    /// </summary>
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class PrintingResponse
    {
        private bool _success;
        private Message _m;

        public PrintingResponse() { }

        public PrintingResponse(Message m)
        {
            _success = m.GetSuccessState() == Message.SuccessState.Success;
            _m = m;
        }
        public bool IsSuccess()
        {
            return _success;
        }
        public String GetErrorReason()
        {
            return _m.GetDataStringValue("error_reason");
        }
        public String GetErrorDetail()
        {
            return _m.GetDataStringValue("error_detail");
        }
        public String GetResponseValueWithAttribute(String attribute)
        {
            return _m.GetDataStringValue(attribute);
        }
    }
}
