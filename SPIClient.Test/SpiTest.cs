using SPIClient;
using Xunit;

namespace Test
{
    public class SpiTest
    {

        [Theory]
        [InlineData("123456", false, "Was not waiting for one.")]
        [InlineData("1234567", false, "Not a 6-digit code.")]
        public void TestSubmitAuthCode(string authCode, bool expectedValidFormat, string expectedMessage)
        {
            var spi = new Spi();
            var submitAuthCodeResult = spi.SubmitAuthCode(authCode);

            Assert.Equal(submitAuthCodeResult.ValidFormat, expectedValidFormat);
            Assert.Equal(submitAuthCodeResult.Message, expectedMessage);
        }
    }
}
