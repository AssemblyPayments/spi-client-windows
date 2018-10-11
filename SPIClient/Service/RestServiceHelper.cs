//using log4net;
//using RestSharp;
//using System.Net;

using System.Net;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using RestSharp;
using RestSharp.Extensions;

namespace SPIClient.Service
{
    public class RestServiceHelper : IRestServiceHelper
    {
        private static readonly ILog Log = LogManager.GetLogger("spi service");

        public DataFormat DataFormat { get; set; }
        private string Url { get; }
        private IRestClient RestClient { get; set; }

        public RestServiceHelper(string url)
        {
            Url = url;
            DataFormat = DataFormat.Json;
            RestClient = new RestClient(url);
        }

        //public T SendRequest<T>(IRestRequest request) where T : new()
        //{
        //    IRestResponse<T> response = RestClient.Execute<T>(request);

        //    if (response.StatusCode != HttpStatusCode.OK)
        //    {
        //        Log.Error($"Status code {(int)response.StatusCode} received from {Url} - Exception {response.ErrorException}");

        //        return default(T);
        //    }

        //    Log.Info($"Response received from {Url} - {response.Content}");

        //    return response.Data;
        //}
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

