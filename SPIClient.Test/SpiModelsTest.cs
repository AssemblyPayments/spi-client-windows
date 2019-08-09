using SPIClient;
using Xunit;

namespace Test
{
    public class SpiModelsTest
    {
        [Fact]
        public void TransactionFlowState_OnValidState_ReturnObjects()
        {
            // arrange
            var stlEnqMsg = new SettlementEnquiryRequest(RequestIdHelper.Id("stlenq")).ToMessage();

            // act
            var transactionFlowState = new TransactionFlowState("1", TransactionType.SettlementEnquiry, 0, stlEnqMsg, $"Waiting for EFTPOS connection to make a settlement enquiry");

            // assert
            Assert.Equal("1", transactionFlowState.PosRefId);
            Assert.Equal("1", transactionFlowState.Id);
            Assert.Equal(TransactionType.SettlementEnquiry, transactionFlowState.Type);
            Assert.Equal(0, transactionFlowState.AmountCents);
            Assert.False(transactionFlowState.AwaitingSignatureCheck);
            Assert.False(transactionFlowState.RequestSent);
            Assert.False(transactionFlowState.Finished);
            Assert.Equal(Message.SuccessState.Unknown, transactionFlowState.Success);
            Assert.Equal($"Waiting for EFTPOS connection to make a settlement enquiry", transactionFlowState.DisplayMessage);
        }

        [Fact]
        public void TransactionFlowState_OnValidRequestSent_ReturnObjects()
        {
            // arrange
            var stlEnqMsg = new SettlementEnquiryRequest(RequestIdHelper.Id("stlenq")).ToMessage();
            var transactionFlowState = new TransactionFlowState("1", TransactionType.SettlementEnquiry, 0, stlEnqMsg, $"Waiting for EFTPOS connection to make a settlement enquiry");

            // act
            transactionFlowState.Sent("Sent");

            // assert
            Assert.NotNull(transactionFlowState.RequestTime);
            Assert.NotNull(transactionFlowState.LastStateRequestTime);
            Assert.True(transactionFlowState.RequestSent);
            Assert.Equal("Sent", transactionFlowState.DisplayMessage);
        }

        [Fact]
        public void TransactionFlowState_OnValidRequestCancelling_ReturnObjects()
        {
            // arrange
            var stlEnqMsg = new SettlementEnquiryRequest(RequestIdHelper.Id("stlenq")).ToMessage();
            var transactionFlowState = new TransactionFlowState("1", TransactionType.SettlementEnquiry, 0, stlEnqMsg, $"Waiting for EFTPOS connection to make a settlement enquiry");

            // act
            transactionFlowState.Cancelling("Cancelling");

            // assert
            Assert.True(transactionFlowState.AttemptingToCancel);
            Assert.Equal("Cancelling", transactionFlowState.DisplayMessage);
        }

        [Fact]
        public void TransactionFlowState_OnValidRequestCancelFailed_ReturnObjects()
        {
            // arrange
            var stlEnqMsg = new SettlementEnquiryRequest(RequestIdHelper.Id("stlenq")).ToMessage();
            var transactionFlowState = new TransactionFlowState("1", TransactionType.SettlementEnquiry, 0, stlEnqMsg, $"Waiting for EFTPOS connection to make a settlement enquiry");

            // act
            transactionFlowState.CancelFailed("CancelFailed");

            // assert
            Assert.False(transactionFlowState.AttemptingToCancel);
            Assert.Equal("CancelFailed", transactionFlowState.DisplayMessage);
        }

        [Fact]
        public void TransactionFlowState_OnValidRequestCallingGlt_ReturnObjects()
        {
            // arrange
            var stlEnqMsg = new SettlementEnquiryRequest(RequestIdHelper.Id("stlenq")).ToMessage();
            var transactionFlowState = new TransactionFlowState("1", TransactionType.SettlementEnquiry, 0, stlEnqMsg, $"Waiting for EFTPOS connection to make a settlement enquiry");

            // act
            transactionFlowState.CallingGlt("25");

            // assert
            Assert.True(transactionFlowState.AwaitingGltResponse);
            Assert.NotNull(transactionFlowState.LastStateRequestTime);
            Assert.Equal("25", transactionFlowState.LastGltRequestId);
        }

        [Fact]
        public void TransactionFlowState_OnValidRequestGotGltResponse_ReturnObject()
        {
            // arrange
            var stlEnqMsg = new SettlementEnquiryRequest(RequestIdHelper.Id("stlenq")).ToMessage();
            var transactionFlowState = new TransactionFlowState("1", TransactionType.SettlementEnquiry, 0, stlEnqMsg, $"Waiting for EFTPOS connection to make a settlement enquiry");

            // act
            transactionFlowState.GotGltResponse();

            // assert
            Assert.False(transactionFlowState.AwaitingGltResponse);
        }

        [Fact]
        public void TransactionFlowState_OnValidRequestFailed_ReturnObjects()
        {
            // arrange
            var stlEnqMsg = new SettlementEnquiryRequest(RequestIdHelper.Id("stlenq")).ToMessage();
            var transactionFlowState = new TransactionFlowState("1", TransactionType.SettlementEnquiry, 0, stlEnqMsg, $"Waiting for EFTPOS connection to make a settlement enquiry");

            // act
            transactionFlowState.Failed(stlEnqMsg, "Failed");

            // assert
            Assert.Equal(stlEnqMsg, transactionFlowState.Response);
            Assert.True(transactionFlowState.Finished);
            Assert.Equal(Message.SuccessState.Failed, transactionFlowState.Success);
            Assert.Equal("Failed", transactionFlowState.DisplayMessage);
        }

        [Fact]
        public void TransactionFlowState_OnValidResponseSignatureRequired_ReturnObjects()
        {
            // arrange
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""data"":{""merchant_receipt"": ""\nEFTPOS FROM WESTPAC\nVAAS Product 2\n275 Kent St\nSydney 2000\nAustralia\n\n\nMID         02447506\nTSP     100381990116\nTIME 26APR17   11:29\nRRN     170426000358\nTRAN 000358   CREDIT\nAmex               S\nCARD............4477\nAUTH          764167\n\nPURCHASE   AUD100.00\nTIP          AUD5.00\n\nTOTAL      AUD105.00\n\n\n (001) APPROVE WITH\n     SIGNATURE\n\n\n\n\n\n\nSIGN:_______________\n\n\n\n\n\n\n\n"",""pos_ref_id"":""prchs-06-06-2019-11-49-05""},""datetime"": ""2017-04-26T11:30:21.000"",""event"": ""signature_required"",""id"": ""24""}}";

            // act
            var msg = Message.FromJson(jsonStr, secrets);
            var response = new SignatureRequired(msg);
            var transactionFlowState = new TransactionFlowState("1", TransactionType.SettlementEnquiry, 0, msg, $"Waiting for EFTPOS connection to make a settlement enquiry");
            transactionFlowState.SignatureRequired(response, "SignatureRequired");

            // assert
            Assert.Equal(response, transactionFlowState.SignatureRequiredMessage);
            Assert.True(transactionFlowState.AwaitingSignatureCheck);
            Assert.Equal("SignatureRequired", transactionFlowState.DisplayMessage);
        }

        [Fact]
        public void TransactionFlowState_OnValidResponseSignatureResponded_ReturnObjects()
        {
            // arrange
            var stlEnqMsg = new SettlementEnquiryRequest(RequestIdHelper.Id("stlenq")).ToMessage();
            var transactionFlowState = new TransactionFlowState("1", TransactionType.SettlementEnquiry, 0, stlEnqMsg, $"Waiting for EFTPOS connection to make a settlement enquiry");

            // act
            transactionFlowState.SignatureResponded("SignatureResponded");

            // assert
            Assert.False(transactionFlowState.AwaitingSignatureCheck);
            Assert.Equal("SignatureResponded", transactionFlowState.DisplayMessage);
        }

        [Fact]
        public void TransactionFlowState_OnValidRequestPhoneForAuthRequired_ReturnObjects()
        {
            // arrnge
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""event"":""authorisation_code_required"",""id"":""20"",""datetime"":""2017-11-01T06:09:33.918"",""data"":{""merchant_id"":""12345678"",""auth_centre_phone_number"":""1800999999"",""pos_ref_id"": ""xyz""}}}";
            var msg = Message.FromJson(jsonStr, secrets);
            var request = new PhoneForAuthRequired(msg);
            var transactionFlowState = new TransactionFlowState("1", TransactionType.SettlementEnquiry, 0, msg, $"Waiting for EFTPOS connection to make a settlement enquiry");

            // act
            transactionFlowState.PhoneForAuthRequired(request, "PhoneForAuthRequired");

            // assert
            Assert.Equal(request, transactionFlowState.PhoneForAuthRequiredMessage);
            Assert.True(transactionFlowState.AwaitingPhoneForAuth);
            Assert.Equal("PhoneForAuthRequired", transactionFlowState.DisplayMessage);
        }

        [Fact]
        public void TransactionFlowState_OnValidRequestAuthCodeSent_ReturnObjects()
        {
            // arrange
            var stlEnqMsg = new SettlementEnquiryRequest(RequestIdHelper.Id("stlenq")).ToMessage();
            var transactionFlowState = new TransactionFlowState("1", TransactionType.SettlementEnquiry, 0, stlEnqMsg, $"Waiting for EFTPOS connection to make a settlement enquiry");

            // act
            transactionFlowState.AuthCodeSent("AuthCodeSent");

            // assert
            Assert.False(transactionFlowState.AwaitingPhoneForAuth);
            Assert.Equal("AuthCodeSent", transactionFlowState.DisplayMessage);
        }

        [Fact]
        public void TransactionFlowState_OnValidResponseCompleted_ReturnObjects()
        {
            // arrange
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""data"":{""merchant_receipt"": ""\nEFTPOS FROM WESTPAC\nVAAS Product 2\n275 Kent St\nSydney 2000\nAustralia\n\n\nMID         02447506\nTSP     100381990116\nTIME 26APR17   11:29\nRRN     170426000358\nTRAN 000358   CREDIT\nAmex               S\nCARD............4477\nAUTH          764167\n\nPURCHASE   AUD100.00\nTIP          AUD5.00\n\nTOTAL      AUD105.00\n\n\n (001) APPROVE WITH\n     SIGNATURE\n\n\n\n\n\n\nSIGN:_______________\n\n\n\n\n\n\n\n"",""pos_ref_id"":""prchs-06-06-2019-11-49-05""},""datetime"": ""2017-04-26T11:30:21.000"",""event"": ""signature_required"",""id"": ""24""}}";
            var msg = Message.FromJson(jsonStr, secrets);
            var transactionFlowState = new TransactionFlowState("1", TransactionType.SettlementEnquiry, 0, msg, $"Waiting for EFTPOS connection to make a settlement enquiry");

            // act
            transactionFlowState.Completed(Message.SuccessState.Success, msg, "Completed");

            // assert
            Assert.Equal(Message.SuccessState.Success, transactionFlowState.Success);
            Assert.Equal(msg, transactionFlowState.Response);
            Assert.True(transactionFlowState.Finished);
            Assert.False(transactionFlowState.AttemptingToCancel);
            Assert.False(transactionFlowState.AwaitingGltResponse);
            Assert.False(transactionFlowState.AwaitingSignatureCheck);
            Assert.False(transactionFlowState.AwaitingPhoneForAuth);
            Assert.Equal("Completed", transactionFlowState.DisplayMessage);
        }

        [Fact]
        public void TransactionFlowState_OnValidResponseUnknownCompleted_ReturnObjects()
        {
            // arrange
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""data"":{""merchant_receipt"": ""\nEFTPOS FROM WESTPAC\nVAAS Product 2\n275 Kent St\nSydney 2000\nAustralia\n\n\nMID         02447506\nTSP     100381990116\nTIME 26APR17   11:29\nRRN     170426000358\nTRAN 000358   CREDIT\nAmex               S\nCARD............4477\nAUTH          764167\n\nPURCHASE   AUD100.00\nTIP          AUD5.00\n\nTOTAL      AUD105.00\n\n\n (001) APPROVE WITH\n     SIGNATURE\n\n\n\n\n\n\nSIGN:_______________\n\n\n\n\n\n\n\n"",""pos_ref_id"":""prchs-06-06-2019-11-49-05""},""datetime"": ""2017-04-26T11:30:21.000"",""event"": ""signature_required"",""id"": ""24""}}";
            var msg = Message.FromJson(jsonStr, secrets);
            var transactionFlowState = new TransactionFlowState("1", TransactionType.SettlementEnquiry, 0, msg, $"Waiting for EFTPOS connection to make a settlement enquiry");

            // act
            transactionFlowState.UnknownCompleted("UnknownCompleted");

            // assert
            Assert.Equal(Message.SuccessState.Unknown, transactionFlowState.Success);
            Assert.Null(transactionFlowState.Response);
            Assert.True(transactionFlowState.Finished);
            Assert.False(transactionFlowState.AttemptingToCancel);
            Assert.False(transactionFlowState.AwaitingGltResponse);
            Assert.False(transactionFlowState.AwaitingSignatureCheck);
            Assert.False(transactionFlowState.AwaitingPhoneForAuth);
            Assert.Equal("UnknownCompleted", transactionFlowState.DisplayMessage);
        }
    }
}
