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
        public void SetPosId_OnInvalidLength_IsSet()
        {
            // arrange
            const string posId = "12345678901234567";
            const string eftposAddress = "10.20.30.40";
            var spi = new Spi(posId, "", eftposAddress,null);

            // act
            spi.SetPosId(posId);
            var value = SpiClientTestUtils.GetInstanceField(typeof(Spi), spi, "_posId");

            // assert
            Assert.NotEqual("", value);
        }

        [Fact]
        public void SpiInitiate_OnInvalidLengthForPosId_IsSet()
        {
            // arrange
            const string posId = "12345678901234567";
            const string eftposAddress = "10.20.30.40";
            var spi = new Spi(posId, "", eftposAddress, null);

            // act            
            var value = SpiClientTestUtils.GetInstanceField(typeof(Spi), spi, "_posId");

            // assert
            Assert.NotEqual("", value);
        }

        [Fact]
        public void SetPosId_OnValidCharacters_IsSet()
        {
            // arrange
            const string posId = "RamenPos";
            const string eftposAddress = "10.20.30.40";
            var spi = new Spi(posId, "", eftposAddress, null);

            // act
            spi.SetPosId(posId);
            var value = SpiClientTestUtils.GetInstanceField(typeof(Spi), spi, "_posId");

            // assert
            Assert.Equal(posId, value);
        }

        [Fact]
        public void SpiInitiate_OnValidCharactersForPosId_IsSet()
        {
            // arrange
            const string posId = "RamenPos@";
            const string eftposAddress = "10.20.30.40";

            // act
            var spi = new Spi(posId, "", eftposAddress, null);
            var value = SpiClientTestUtils.GetInstanceField(typeof(Spi), spi, "_posId");

            // assert
            Assert.NotEqual("", value);
        }

        [Fact]
        public void SetEftposAddress_OnValidCharacters_IsSet()
        {
            // arrange
            const string eftposAddress = "10.20.30.40";
            var spi = new Spi();
            var conn = new Connection();
            SpiClientTestUtils.SetInstanceField(spi, "_currentStatus", SpiStatus.Unpaired);
            SpiClientTestUtils.SetInstanceField(spi, "_conn", conn);

            // act
            spi.SetEftposAddress(eftposAddress);
            var value = SpiClientTestUtils.GetInstanceField(typeof(Spi), spi, "_eftposAddress").ToString().Replace("ws://", "");

            // assert
            Assert.Equal(eftposAddress, value);
        }

        [Fact]
        public void SpiInitate_OnValidCharactersForEftposAddress_IsSet()
        {
            // arrange
            const string eftposAddress = "10.20.30.40";
            const string posId = "DummyPos";

            // act
            var spi = new Spi(posId, "", eftposAddress, null);
            var value = SpiClientTestUtils.GetInstanceField(typeof(Spi), spi, "_eftposAddress").ToString().Replace("ws://", ""); 

            // assert
            Assert.Equal(eftposAddress, value);
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
