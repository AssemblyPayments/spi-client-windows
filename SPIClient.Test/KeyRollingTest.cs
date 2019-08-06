using SPIClient;
using Xunit;

namespace Test
{
    public class KeyRollingTest
    {
        [Fact]
        public void PerformKeyRolling_OnValidResponse_ReturnObjects()
        {
            // arrange
            var krRequest = new Message("x", Events.KeyRollRequest, null, false);
            var oldSecerts = new Secrets("11A1162B984FEF626ECC27C659A8B0EEAD5248CA867A6A87BEA72F8A8706109D", "40510175845988F13F6162ED8526F0B09F73384467FA855E1E79B44A56562A58");

            // act
            var krResult = KeyRollingHelper.PerformKeyRolling(krRequest, oldSecerts);

            // assert
            Assert.Equal("0307C53DD0F119A1BC4CE61AA395882FB63BF8FCD0E0D27BBEB0D56AA9B24162", krResult.NewSecrets.EncKey);
            Assert.Equal("E4C3908437C14AC442C925FC8ED536C69FF67080D15FE007D69F8580D73FDF9D", krResult.NewSecrets.HmacKey);
            Assert.Equal("x", krResult.KeyRollingConfirmation.Id);
            Assert.Equal(Events.KeyRollResponse, krResult.KeyRollingConfirmation.EventName);
        }

    }
}