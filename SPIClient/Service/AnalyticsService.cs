using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Deserializers;

namespace SPIClient.Service
{
    public class TransactionReport
    {
        public string PosVendorId { get; set; }
        public string PosVersion { get; set; }
        public string LibraryLanguage { get; set; }
        public string LibraryVersion { get; set; }
        public string PosRefId { get; set; }
        public string SerialNumber { get; set; }
        public string Event{ get; set; }
        public string TxType { get; set; }
        public string TxResult { get; set; }
        public long TxStartTime { get; set; }
        public long TxEndTime { get; set; }
        public int DurationMs { get; set; }
        public string CurrentFlow { get; set; }
        public string CurrentTxFlowState { get; set; }
        public string CurrentStatus { get; set; }

        internal JObject ToMessage()
        {
            var message = new JObject(
                new JProperty("pos_vendor_id", PosVendorId),
                new JProperty("pos_version", PosVersion),
                new JProperty("library_language", LibraryLanguage),
                new JProperty("library_version", LibraryVersion),
                new JProperty("pos_ref_id", PosRefId),
                new JProperty("serial_number", SerialNumber),
                new JProperty("event", Event),
                new JProperty("tx_type", TxType),
                new JProperty("tx_result", TxResult),
                new JProperty("tx_start_ts_ms", TxStartTime),
                new JProperty("tx_end_ts_ms", TxEndTime),
                new JProperty("duration_ms", DurationMs),
                new JProperty("current_flow", CurrentFlow),
                new JProperty("current_tx_flow_state", CurrentTxFlowState),
                new JProperty("current_status", CurrentStatus));

            return message;
        }
    }

    public class AnalyticsService
    {
        private const string ApiKeyHeader = "ASM-MSP-DEVICE-ADDRESS-API-KEY";

        public Task<IRestResponse<TransactionReport>> ReportTransaction(TransactionReport transactionReport, string apiKey, string tenantCode, bool isTestMode)
        {
            var transactionServiceUri = isTestMode ? $"https://spi-analytics-api-sb.{tenantCode}.mspenv.io/v1/report-transaction" : $"https://spi-analytics-api.{tenantCode}.mspenv.io/v1/report-transaction";

#if DEBUG
            transactionServiceUri = "https://spi-analytics-api-qa.eng.mspenv.io/v1/report-transaction";
#endif
            var message = transactionReport.ToMessage();

            var analyticsService = new HttpBaseService(transactionServiceUri);
            var request = new RestRequest(Method.POST);
            request.AddParameter("application/json", JsonConvert.SerializeObject(message), ParameterType.RequestBody);
            request.AddHeader(ApiKeyHeader, apiKey);
            var serviceResponse = analyticsService.SendRequest<TransactionReport>(request);
            
            return serviceResponse;
        }

    }
} 