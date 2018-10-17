using System.Net;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using RestSharp;

namespace SPIClient.Service
{
    public class HttpBaseService : IHttpBaseService
    {
        private static readonly ILog Log = LogManager.GetLogger("Http base service");

        public DataFormat DataFormat { get; set; }
        private string Url { get; }
        private IRestClient RestClient { get; set; }

        public HttpBaseService(string url)
        {
            Url = url;
            DataFormat = DataFormat.Json;
            RestClient = new RestClient(url);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12; // TODO: check this
        }

        public async Task<T> SendRequest<T>(IRestRequest request) where T : new()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var response = await RestClient.ExecuteTaskAsync<T>(request, cancellationTokenSource.Token);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                Log.Error($"Status code {(int)response.StatusCode} received from {Url} - Exception {response.ErrorException}");
                return default(T);
            }

            Log.Info($"Response received from {Url} - {response.Content}");
            return response.Data;
        }
    }


}

