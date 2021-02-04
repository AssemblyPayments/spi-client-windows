using System.Collections.Generic;
using System.Threading.Tasks;
using RestSharp;

namespace SPIClient.Service
{
    public class Tenants
    {
        public List<TenantDetails> Data { get; set; }
    }

    public class TenantDetails
    {
        public string Code { get; set; }
        public string Name { get; set; }
    }

    public class TenantsService
    {
        internal async Task<IRestResponse<List<Tenants>>> RetrieveTenantsList(string posVendorId, string apiKey, string countryCode)
        {
            var tenantServiceUri = $"https://spi.integration.mspenv.io/tenants?country-code={countryCode}&pos-vendor-id={posVendorId}&api-key={apiKey}";

            var tenantsService = new HttpBaseService(tenantServiceUri);
            var request = new RestRequest(Method.GET);

            var serviceResponse = await tenantsService.SendRequest<List<Tenants>>(request);

            return serviceResponse;
        }
    }
}
