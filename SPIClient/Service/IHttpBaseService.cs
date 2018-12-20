using System.Threading.Tasks;
using RestSharp;

namespace SPIClient.Service
{
    public interface IHttpBaseService
    {
        Task<T> SendRequest<T>(IRestRequest request) where T : new();
    }
}
