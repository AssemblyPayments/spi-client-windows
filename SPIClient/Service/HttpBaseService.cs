using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using RestSharp;

namespace SPIClient.Service
{
    public class HttpBaseService
    {
        private static readonly ILog Log = LogManager.GetLogger("Http base service");

        public DataFormat DataFormat { get; set; }
        private string Url { get; }
        private IRestClient RestClient { get; set; }
        private readonly TimeSpan _timeOut = TimeSpan.FromSeconds(3);

        public HttpBaseService(string url)
        {
            Url = url;
            DataFormat = DataFormat.Json;
            RestClient = new RestClient(url);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        public async Task<IRestResponse<T>> SendRequest<T>(IRestRequest request) where T : new()
        {
            var cancellationTokenSource = new CancellationTokenSource(_timeOut);
            var response = await RestClient.ExecuteTaskAsync<T>(request, cancellationTokenSource.Token);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                Log.Error($"Status code {(int)response.StatusCode} received from {Url} - Exception {response.ErrorException}");                
            }
            else
            {
                Log.Info($"Response received from {Url} - {response.Content}");
            }
            
            return response;
        }
    }


}

