using log4net;
using RestSharp;
using System.Net;

namespace SPIClient.Service
{
    public class RestServiceHelper : IRestServiceHelper
    {
        private static readonly ILog log = LogManager.GetLogger("spi service");

        public DataFormat DataFormat { get; set; }
        private string Url { get; }
        private IRestClient RestClient { get; set; }

        public RestServiceHelper(string url)
        {
            Url = url;
            DataFormat = DataFormat.Json;
            RestClient = new RestClient(url);
        }

        public T SendRequest<T>(IRestRequest request) where T : new()
        {
            IRestResponse<T> response = RestClient.Execute<T>(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                log.Error($"Status code {(int)response.StatusCode} received from {Url} - Exception {response.ErrorException}");

                return default(T);
            }

            log.Info($"Response received from {Url} - {response.Content}");

            return response.Data;
        }
    }
}
