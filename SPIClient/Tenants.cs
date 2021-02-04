using RestSharp;
using System.Collections.Generic;
using SPIClient.Service;
using Newtonsoft.Json;

namespace SPIClient
{
    internal static class TenantsHelper
    {
        internal static Tenants GetAvailableTenants(IRestResponse<List<Tenants>> serviceResponse)
        {
            var tenants = JsonConvert.DeserializeObject<Tenants>(serviceResponse.Content);

            return tenants;
        }
    }
}
