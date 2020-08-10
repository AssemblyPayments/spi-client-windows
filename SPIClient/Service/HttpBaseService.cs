using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
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

        public class ErrorResponse_Legacy
        {
            public string error { get; set; }
        }

        public class ErrorResponse
        {
            public Error Error { get; set; }
        }

        public class Error
        {
            public string Code { get; set; }
            public string Message { get; set; }
        }


        public async Task<IRestResponse<T>> SendRequest<T>(IRestRequest request) where T : new()
        {
            var cancellationTokenSource = new CancellationTokenSource(_timeOut);

            try
            {
                var response = await RestClient.ExecuteTaskAsync<T>(request, cancellationTokenSource.Token);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    string errorMessage = "";

                    try
                    {
                        var result = JsonConvert.DeserializeObject<ErrorResponse>(response.Content);

                        if (result != null)
                            errorMessage = result.Error.Message;
                    }
                    catch
                    {
                        var result = JsonConvert.DeserializeObject<ErrorResponse_Legacy>(response.Content);
                        errorMessage = result.error;
                    }
                    Log.Error($"Status code {(int)response.StatusCode} received from {Url} - Error {errorMessage}");
                }
                else
                {
                    Log.Information($"Response received from {Url} - {response.Content}");
                }

                return response;
            }
            catch (TaskCanceledException ex)
            {
                Log.Error($"Task cancelled {ex.CancellationToken.IsCancellationRequested.ToString()}");
                return null;
            }
        }
    }
}

