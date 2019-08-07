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

        [Fact]
        public void SetPosId_OnValidLength_IsSet()
        {
            // arrange
            const string posId = "12345678901234567";
            const int lengthOfPosId = 16;
            var spi = new Spi();
            var messageStamp = new MessageStamp("", null, new System.TimeSpan());
            SpiClientTestUtils.SetInstanceField(spi, "_currentStatus", SpiStatus.Unpaired);
            SpiClientTestUtils.SetInstanceField(spi, "_spiMessageStamp", messageStamp);

            // act
            spi.SetPosId(posId);
            var value = SpiClientTestUtils.GetInstanceField(typeof(Spi), spi, "_posId");

            // assert
            Assert.NotEqual(posId, value);
            Assert.Equal(lengthOfPosId, value.ToString().Length);
        }

        [Fact]
        public void SpiInitate_OnValidLength_IsSet()
        {
            // arrange
            const string posId = "12345678901234567";
            const int lengthOfPosId = 16;
            var spi = new Spi(posId, "", "", null);            
            SpiClientTestUtils.SetInstanceField(spi, "_currentStatus", SpiStatus.Unpaired);

            // act
            spi.SetPosId(posId);
            var value = SpiClientTestUtils.GetInstanceField(typeof(Spi), spi, "_posId");

            // assert
            Assert.NotEqual(posId, value);
            Assert.Equal(lengthOfPosId, value.ToString().Length);
        }

        [Fact]
        public void RetriesBeforePairing_OnValidValue_Checked()
        {
            // arrange
            const int retriesBeforePairing = 3;

            // act
            Spi spi = new Spi();

            // assert
            Assert.Equal(retriesBeforePairing, SpiClientTestUtils.GetInstanceField(typeof(Spi), spi, "_retriesBeforePairing"));
        }

        [Fact]
        public void SleepBeforeReconnectMs_OnValidValue_Checked()
        {
            // arrange
            const int sleepBeforeReconnectMs = 3000;

            // act
            Spi spi = new Spi();

            // assert
            Assert.Equal(sleepBeforeReconnectMs, SpiClientTestUtils.GetInstanceField(typeof(Spi), spi, "_sleepBeforeReconnectMs"));
        }
    }
}
