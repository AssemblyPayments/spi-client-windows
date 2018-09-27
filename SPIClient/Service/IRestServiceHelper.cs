using RestSharp;

namespace SPIClient.Service
{
    public interface IRestServiceHelper
    {
        T SendRequest<T>(IRestRequest request) where T : new();
    }
}
