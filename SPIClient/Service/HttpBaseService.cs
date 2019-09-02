using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;
using Serilog;

namespace SPIClient.Service
{
    public class HttpBaseService
    {
        public DataFormat DataFormat { get; set; }
        private string Url { get; }
        private IRestClient RestClient { get; set; }
        private readonly TimeSpan _timeOut = TimeSpan.FromSeconds(8);

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
                Log.Information($"Response received from {Url} - {response.Content}");
            }
            
            return response;
        }
    }
    

}

