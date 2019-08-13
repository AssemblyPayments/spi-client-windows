using SPIClient;
using System.Text.RegularExpressions;
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
        public void SpiInitate_OnInvalidLengthForPosId_IsSet()
        {
            // arrange
            const string posId = "12345678901234567";
            const int lengthOfPosId = 16;
            var spi = new Spi(posId, "", "", null);
            SpiClientTestUtils.SetInstanceField(spi, "_currentStatus", SpiStatus.Unpaired);

            // act            
            var value = SpiClientTestUtils.GetInstanceField(typeof(Spi), spi, "_posId");

            // assert
            Assert.NotEqual(posId, value);
            Assert.Equal(lengthOfPosId, value.ToString().Length);
        }

        [Fact]
        public void SetPosId_OnValidCharacters_IsSet()
        {
            // arrange
            const string posId = "RamenPos@";
            var regexItemsForPosId = new Regex("^[a-zA-Z0-9 ]*$");
            var spi = new Spi();
            var messageStamp = new MessageStamp("", null, new System.TimeSpan());
            SpiClientTestUtils.SetInstanceField(spi, "_currentStatus", SpiStatus.Unpaired);
            SpiClientTestUtils.SetInstanceField(spi, "_spiMessageStamp", messageStamp);

            // act
            spi.SetPosId(posId);
            var value = SpiClientTestUtils.GetInstanceField(typeof(Spi), spi, "_posId");

            // assert
            Assert.Equal(posId, value);
            Assert.False(regexItemsForPosId.IsMatch(posId));
            Assert.Equal(regexItemsForPosId.IsMatch(posId), regexItemsForPosId.IsMatch(value.ToString()));
        }

        [Fact]
        public void SpiInitate_OnValidCharactersForPosId_IsSet()
        {
            // arrange
            const string posId = "RamenPos@";
            var regexItemsForPosId = new Regex("^[a-zA-Z0-9 ]*$");
            var spi = new Spi(posId, "", "", null);
            SpiClientTestUtils.SetInstanceField(spi, "_currentStatus", SpiStatus.Unpaired);

            // act
            var value = SpiClientTestUtils.GetInstanceField(typeof(Spi), spi, "_posId");

            // assert
            Assert.Equal(posId, value);
            Assert.False(regexItemsForPosId.IsMatch(posId));
            Assert.Equal(regexItemsForPosId.IsMatch(posId), regexItemsForPosId.IsMatch(value.ToString()));
        }

        [Fact]
        public void SetEftposAddress_OnValidCharacters_IsSet()
        {
            // arrange
            const string eftposAddress = "10.20";
            var regexItemsForEftposAddress = new Regex(@"^[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}$");
            var spi = new Spi();
            var conn = new Connection();
            SpiClientTestUtils.SetInstanceField(spi, "_currentStatus", SpiStatus.Unpaired);
            SpiClientTestUtils.SetInstanceField(spi, "_conn", conn);

            // act
            spi.SetEftposAddress(eftposAddress);
            var value = SpiClientTestUtils.GetInstanceField(typeof(Spi), spi, "_eftposAddress");
            value = value.ToString().Remove(0, 5);

            // assert
            Assert.False(regexItemsForEftposAddress.IsMatch(eftposAddress));
            Assert.Equal(eftposAddress, value);
            Assert.Equal(regexItemsForEftposAddress.IsMatch(eftposAddress), regexItemsForEftposAddress.IsMatch(value.ToString()));
        }

        [Fact]
        public void SpiInitate_OnValidCharactersForEftposAddress_IsSet()
        {
            // arrange
            const string eftposAddress = "10.20";
            var regexItemsForEftposAddress = new Regex(@"^[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}$");
            var spi = new Spi("", eftposAddress, "", null);
            SpiClientTestUtils.SetInstanceField(spi, "_currentStatus", SpiStatus.Unpaired);

            var value = SpiClientTestUtils.GetInstanceField(typeof(Spi), spi, "_eftposAddress");
            value = value.ToString().Remove(0, 5);

            // assert
            Assert.False(regexItemsForEftposAddress.IsMatch(eftposAddress));
            Assert.NotEqual(eftposAddress, value);
            Assert.Equal(regexItemsForEftposAddress.IsMatch(eftposAddress), regexItemsForEftposAddress.IsMatch(value.ToString()));
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
