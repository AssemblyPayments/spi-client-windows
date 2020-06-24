using Newtonsoft.Json.Linq;

namespace SPIClient
{
    public class ReversalRequest
    {
        public string PosRefId { get; }

        public ReversalRequest(string posRefId)
        {
            PosRefId = posRefId;
        }

        public Message ToMessage()
        {
            var data = new JObject(
                new JProperty("pos_ref_id", PosRefId));

            return new Message(RequestIdHelper.Id("rev"), Events.ReversalRequest, data, true);
        }
    }

    public class ReversalResponse
    {
        public bool Success { get; }
        public string PosRefId { get; }

        private readonly Message _m;

        public ReversalResponse() { }

        public ReversalResponse(Message m)
        {
            _m = m;
            PosRefId = _m.GetDataStringValue("pos_ref_id");
            Success = m.GetSuccessState() == Message.SuccessState.Success;
        }

        public string GetErrorReason()
        {
            return _m.GetDataStringValue("error_reason");
        }

        public string GetErrorDetail()
        {
            return _m.GetDataStringValue("error_detail");
        }
    }
}