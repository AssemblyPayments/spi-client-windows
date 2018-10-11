using System.Threading.Tasks;
using RestSharp;

namespace SPIClient.Service
{
    public interface IRestServiceHelper
    {
        Task<T> SendRequest<T>(IRestRequest request) where T : new();
    }
}
