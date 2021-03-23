using SPIClient;
using Xunit;

namespace Test
{
    public class TenantTest
    {
        [Fact]
        public void GetAvailableTenants_ValidRequest_ReturnObject()
        {
            // arrange
            const string posVendorId = "BurgerPos";
            const string apiKey = "api12345";
            const string countryCode = "AU";
            const string tenantName = "Gecko Demo Bank";
            const string tenantCode = "gko";

            // act
            var service = Spi.GetAvailableTenants(posVendorId, apiKey, countryCode);
            var result = service.Result;

            // assert
            Assert.Equal(tenantName, service.Result.Data.Find(x => x.Name.Equals(tenantName)).Name);
            Assert.Equal(tenantCode, service.Result.Data.Find(x => x.Code.Equals(tenantCode)).Code);
        }

    }
}
