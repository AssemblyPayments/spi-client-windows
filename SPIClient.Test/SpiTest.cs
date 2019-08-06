using SPIClient;
using Xunit;

namespace Test
{
    public class SpiTest
    {

        [Theory]
        [InlineData("123456", false, "Was not waiting for one.")]
        [InlineData("1234567", false, "Not a 6-digit code.")]
        public void SubmitAuthCode_OnValidResponse_ReturnObjects(string authCode, bool expectedValidFormat, string expectedMessage)
        {
            // arrange
            var spi = new Spi();

            // act
            var submitAuthCodeResult = spi.SubmitAuthCode(authCode);

            // assert
            Assert.Equal(expectedValidFormat, submitAuthCodeResult.ValidFormat);
            Assert.Equal(expectedMessage, submitAuthCodeResult.Message);
        }

        [Fact]
        public void RetriesBeforeResolvingDeviceAddress_OnValidValue_Checked()
        {
            // arrange
            const int retriesBeforeResolvingDeviceAddress = 3;

            // act
            Spi spi = new Spi();

            // assert
            Assert.Equal(retriesBeforeResolvingDeviceAddress, SpiClientTestUtils.GetInstanceField(typeof(Spi), spi, "_retriesBeforeResolvingDeviceAddress"));
        }
    }
}
