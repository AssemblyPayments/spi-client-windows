using SPIClient;
using System.Numerics;
using Xunit;

namespace Test
{
    public class PairingTests
    {
        [Fact]
        public void GenerateSecretsAndKeyResponse_OnValidResponse_ReturnObjects()
        {
            // arrange
            // We don't have secrets to start with.
            Secrets secrets = null;
            // We've just received a message from the server
            var incomingMessageJsonStr = IncomingKeyRequestJson();

            // act
            // Let's parse it
            var incomingMessage = Message.FromJson(incomingMessageJsonStr, secrets);

            // assert
            Assert.Equal("key_request", incomingMessage.EventName);

            // arrange
            // Incoming Message is a key_request.
            var keyRequest = new KeyRequest(incomingMessage);

            // act
            // Let's generate the Secrets and the KeyResponse
            var result = PairingHelper.GenerateSecretsAndKeyResponse(keyRequest);
            secrets = result.Secrets; // Save These. They are precious!

            // assert
            // Let's Assert KeyResponse values
            var keyResponse = result.KeyResponse;
            Assert.NotEmpty(keyResponse.Benc);
            Assert.NotEmpty(keyResponse.Bhmac);
            Assert.Equal("62", keyResponse.RequestId);

            //assert
            // Let's now prepare to send the key_response back to the server.
            var msgToSend = keyResponse.ToMessage();
            Assert.Equal("62", msgToSend.Id);
            Assert.NotEmpty((string)msgToSend.Data["enc"]["B"]);
            Assert.NotEmpty((string)msgToSend.Data["hmac"]["B"]);
        }

        [Fact]
        public void SpiAHexStringToBigInteger_OnValidResult_IsGenerated()
        {
            // arrange
            // This is a typical A value coming from the server, that could be wrongly interpreted as a negative value.
            var incomingA = "DE42490C8F4F6D4D94C6EF91FE14C88CC81EAF1E67B8F1F40AF0E7F820E9DA3C0A94B9ACC6A624BD119C3270910D925D351F097C859356A048E0FE9154C1AAB7CC69C125B55455C1E5B0A3790D2AA65A5AA3E2BB60CCF0F140E32ADEB5931245BECD361DE6070436EB54329972C86C01141EBCDB190B24789F05372D95219693DFC484F4FA04BDA808911835344145B5EC1AC277103FCC042DFFA19B081745C2A286ED378068165C289BCE4E66C25D959416F5CD493B37CD051D09505FC4166DB1B77253693F7671B5019945DF1DA561AB3514AAA2F665A6F80610ADC6B7E8B149FDC62E6A289A2E91708ECD68C98AC34A5138869CE387C0813B72DFC4013DCC";
            var expectedBigInt = BigInteger.Parse("28057590225756641307990478575115942434087786432814243319932776727076438406521076596140114483936714752816261267076418421365582967840892369393410969737840494345407097935234656169358022904124610557991122629465181790672063793938502648166037000156854900527078271865860807536279471604640419277958287835405482406786942622132480633555138157731306330505014921731704281742225329497728638630895408406470042417047793985972099848310146938210045502000965935203871114530585068895834412190170964682990662520608415632041868324548288922663465883289142775376591626822575267795629370129550324552253761158326240672561176761746423986339276");

            // act
            var calculatedBigInt = PairingHelper.SpiAHexStringToBigInteger(incomingA);

            // assert
            Assert.Equal(expectedBigInt, calculatedBigInt);
        }

        [Fact]
        public void SpiAHexStringToBigInteger_OnValidResult_WithLongChars()
        {
            // This is a typical A value coming from the server, 509 chars long
            var incomingA = "CA8A33B81963BAC7068FF4FA21193FD54445C47EE5D6EAC14B97C1DBD0AE21D683CB5BC355B16BE969B567536985177FCF446DD95B4A0B45A4D3FD497C6FEADB0126CD2383C0DA475B482007D4B941641EC82E177321292103B842660B1A9690E892A4E8D0664A4A50102B01F6562EBD480DC3B16D8BE5EE5ED076099CABB44330BA8124582669BEA1A3990754B9D9FB41F470539558B54C398628553B366ADDD5055C7EE784F916E44FFA8188E96C635F2314181BE662EE3125F0D119490132DD3F6C778199A11D80E182565C1CA05582988824ACF4E1CE84ECCE1F17FBE8BF9BB86D8C059A1E2114F96F163E0C45E6DEDA41DD27187A7CB696BD7F7032E";

            var expectedBigInt = BigInteger.Parse("6242257705828984036656266600403397573397224990025429675853955473612201444359563013179842155682812885831396293418099698228546742104443715887407249098046261687995142454490054830537910056555738078918996622954400366037842515342200987294449506875517728947106125309850920720564574130849595667906600652966151947593406017393637289292641140821958942411236880001806160003295142004163227450419959475352141829824638220928883338755038418821169677357987142679463603775052047306995489677114235996408400789487599802214581742956123668820445766837730477438131320464040402782216170369457291283600982299724185897733491544121861931822");

            var calculatedBigInt = PairingHelper.SpiAHexStringToBigInteger(incomingA);

            Assert.Equal(expectedBigInt, calculatedBigInt);
        }

        [Fact]
        public void DHSecretToSPISecret_OnValidRequest_IsSet()
        {
            // arrange
            var dhSecretBI = BigInteger.Parse("17574532284595554228770542578145458081719781058045063175688772743423924399411406200223997425795977226735712284391179978852253613346926080761628802664085045531796220784085311215093471160914442692274980632286568900367895454304533334450617380428362254473222831478193415222881689923861172428575632214297967550826460508634891791127942687630353829719246724903147169063379750256523005309264102997944008112551383251560153285483075803832550164760264165682355751637761390244202226339540318827287797180863284173748514677579269180126947721499144727772986832223499738071139796968492815538042908414723947769999062186130240163854083");
            var expectedSecret = "7D3895D92143692B46AEB66C66D7023C008093F2D8E272954898918DF12AAAD7";

            // act
            var calculatedSecret = PairingHelper.DHSecretToSPISecret(dhSecretBI);

            // assert
            Assert.Equal(expectedSecret, calculatedSecret);
        }

        [Fact]
        public void SecretToSPISecret_OnValidRequest_IsSet_2()
        {
            // arrange
            var dhSecretBI = BigInteger.Parse("17574532284595554228770542578145458081719781058045063175688772743423924399411406200223997425795977226735712284391179978852253613");
            var expectedSecret = "238A19795053605B1995E678C7785FB1E2137E6F49F13CCAFFAC0CB9773AF3B1";

            // act
            var calculatedSecret = PairingHelper.DHSecretToSPISecret(dhSecretBI);

            // assert
            Assert.Equal(expectedSecret, calculatedSecret);
        }

        [Fact]
        public void DropKeysRequest_OnValidRequest_IsSet()
        {
            // arrange
            var request = new DropKeysRequest();

            // act
            var msg = request.ToMessage();

            // assert
            Assert.Equal(msg.EventName, "drop_keys");
        }

        [Fact]
        public void PairRequest_OnValidRequest_IsSet()
        {
            // arrange
            var request = new PairRequest();

            // act
            var msg = request.ToMessage();

            // assert
            Assert.Equal(msg.EventName, "pair_request");
        }

        [Fact]
        public void KeyCheck_OnValidResponse_ReturnObjects()
        {
            // arrange
            var secrets = SpiClientTestUtils.SetTestSecrets("3DFC835E5A24C63F3DD04E0BCC54FDBB441339A4D26589D1558D2C10A18AEE4F", "F2AEAC7AA7776D7178F0AC4F0F6D4EF1B5F22630EA93C309620E44ABA5A82D32");
            const string keyResponseJsonStr = @"{""enc"":""6A45A1F6E746CCE3FA470BEAC479F79783C03331DE10A795A859F6D5564168C956557BCCD3EDA55BBEACF34916269537"",""hmac"":""75B56FE4978AC9A22B2D56DDF952983CE8BECE1B6CEC8A7EEEE6ABF9CA2CB471""}";

            // act
            var msg = Message.FromJson(keyResponseJsonStr, secrets);
            var request = new KeyCheck(msg);

            // assert
            Assert.Equal(msg.EventName, "key_check");
            Assert.Equal(request.ConfirmationCode, msg.IncomingHmac.Substring(0, 6));
        }

        [Fact]
        public void NewPairequest_OnValidRequest_IsSet()
        {
            // act
            var pairRequest = PairingHelper.NewPairequest();

            // assert
            Assert.Equal(pairRequest.ToMessage().EventName, "pair_request");
        }

        [Fact]
        public void PairResponse_OnValidResponse_ReturnObjects()
        {
            // arrange
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""data"":{""success"":true},""event"":""pair_response"",""id"":""pr1""}}";

            // act
            var msg = Message.FromJson(jsonStr, secrets);
            var pairResponse = new PairResponse(msg);

            // assert
            Assert.True(pairResponse.Success);
            Assert.Equal(msg.EventName, "pair_response");
            Assert.Equal(msg.Id, "pr1");
        }

        private string IncomingKeyRequestJson()
        {
            return @"
                {
                    ""message"": {
                        ""event"": ""key_request"",
                        ""id"": ""62"",
                        ""data"": {
                            ""enc"": {
                                ""A"": ""17E7BE43D53102647040FC090000C215810E28E5E0CBD4F47923E194AE72AB0CDADF922642B73C568AA94A84B61874A475549E1F95847BE2725462E3D635F019BE39B2064F1EFFBE6B80CE97FBB7C0913ADC06A2445980B57647778B127FFCCE8B28A44BADEDE0110A5AFB05FEF7AA3F54988AFB04310A113F713601683D8E30CA2BAFC2EC34879127019E3352D8CAB9603184283AE3C9359D40C12474500018B8640AF371DC8712A06A3A443DF41DA9C1C60FAD2ACB02564A6694382B18811AA30CE38A1FC251DE0669504CAB620C2BA4A84CCC8FBDCBB30BBB3EACA76008599F74C2FDF6231773DC0439969CB5F2904A71DDF57F7DF9394AA29CBE4856FC82""
                            },
                            ""hmac"": {
                                ""A"": ""89708531EADF129B4F67F00ECBF883C825A0EF3D766E32BC2BA13508B53FC3F5928316DE05CBE82FA1BBF4116E58A68C6F9C3C8FEF492051498188F4E80F82D5764FF50331B34E418E41480FAE0C794F20D9F7AE9819CB317AD2351B165783D57D12C39F95D9A5A292B89D3A26F9BBDE5C218EEC3FE63D910DCB0E1A0E6B570AF94BBD3025EB5E23FFBD9E8D58FE68403B3E50566DA8E2E54EED1A4D754689ECB7266B3D4804E39FB868F1741896757E7844C3389DA49F87D23FB2E9F6ADDBE9C14CC92F322CF3B471CE217E48D0762D5C963827AA6F4316B905F19E0262A35DC4B62E2FB95B7AAD5616C61F31C9A74008EE51BAB2CD6F646320FA30A6DDC4D7"" 
                            }
                        }                        
                    }
                }";
        }
    }
}