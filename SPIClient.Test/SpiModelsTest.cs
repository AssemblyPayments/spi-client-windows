using SPIClient;
using Xunit;

namespace Test
{
    public class SpiModelsTest
    {
        [Fact]
        public void TestTransactionFlowState()
        {
            var stlEnqMsg = new SettlementEnquiryRequest(RequestIdHelper.Id("stlenq")).ToMessage();
            var transactionFlowState = new TransactionFlowState("1", TransactionType.SettlementEnquiry, 0, stlEnqMsg, $"Waiting for EFTPOS connection to make a settlement enquiry");

            Assert.Equal(transactionFlowState.PosRefId, "1");
            Assert.Equal(transactionFlowState.Id, "1");
            Assert.Equal(transactionFlowState.Type, TransactionType.SettlementEnquiry);
            Assert.Equal(transactionFlowState.AmountCents, 0);
            Assert.False(transactionFlowState.AwaitingSignatureCheck);
            Assert.False(transactionFlowState.RequestSent);
            Assert.False(transactionFlowState.Finished);
            Assert.Equal(transactionFlowState.Success, Message.SuccessState.Unknown);
            Assert.Equal(transactionFlowState.DisplayMessage, $"Waiting for EFTPOS connection to make a settlement enquiry");
        }

        [Fact]
        public void TestTransactionFlowStateSent()
        {
            var stlEnqMsg = new SettlementEnquiryRequest(RequestIdHelper.Id("stlenq")).ToMessage();
            var transactionFlowState = new TransactionFlowState("1", TransactionType.SettlementEnquiry, 0, stlEnqMsg, $"Waiting for EFTPOS connection to make a settlement enquiry");
            transactionFlowState.Sent("Sent");

            Assert.NotNull(transactionFlowState.RequestTime);
            Assert.NotNull(transactionFlowState.LastStateRequestTime);
            Assert.True(transactionFlowState.RequestSent);
            Assert.Equal(transactionFlowState.DisplayMessage, "Sent");
        }

        [Fact]
        public void TestTransactionFlowStateCancelling()
        {
            var stlEnqMsg = new SettlementEnquiryRequest(RequestIdHelper.Id("stlenq")).ToMessage();
            var transactionFlowState = new TransactionFlowState("1", TransactionType.SettlementEnquiry, 0, stlEnqMsg, $"Waiting for EFTPOS connection to make a settlement enquiry");
            transactionFlowState.Cancelling("Cancelling");

            Assert.True(transactionFlowState.AttemptingToCancel);
            Assert.Equal(transactionFlowState.DisplayMessage, "Cancelling");
        }

        [Fact]
        public void TestTransactionFlowStateCancelFailed()
        {
            var stlEnqMsg = new SettlementEnquiryRequest(RequestIdHelper.Id("stlenq")).ToMessage();
            var transactionFlowState = new TransactionFlowState("1", TransactionType.SettlementEnquiry, 0, stlEnqMsg, $"Waiting for EFTPOS connection to make a settlement enquiry");
            transactionFlowState.CancelFailed("CancelFailed");

            Assert.False(transactionFlowState.AttemptingToCancel);
            Assert.Equal(transactionFlowState.DisplayMessage, "CancelFailed");
        }

        [Fact]
        public void TestTransactionFlowStateCallingGlt()
        {
            var stlEnqMsg = new SettlementEnquiryRequest(RequestIdHelper.Id("stlenq")).ToMessage();
            var transactionFlowState = new TransactionFlowState("1", TransactionType.SettlementEnquiry, 0, stlEnqMsg, $"Waiting for EFTPOS connection to make a settlement enquiry");
            transactionFlowState.CallingGlt("25");

            Assert.True(transactionFlowState.AwaitingGltResponse);
            Assert.NotNull(transactionFlowState.LastStateRequestTime);
            Assert.Equal(transactionFlowState.LastGltRequestId, "25");
        }

        [Fact]
        public void TestTransactionFlowStateGotGltResponse()
        {
            var stlEnqMsg = new SettlementEnquiryRequest(RequestIdHelper.Id("stlenq")).ToMessage();
            var transactionFlowState = new TransactionFlowState("1", TransactionType.SettlementEnquiry, 0, stlEnqMsg, $"Waiting for EFTPOS connection to make a settlement enquiry");
            transactionFlowState.GotGltResponse();

            Assert.False(transactionFlowState.AwaitingGltResponse);
        }

        [Fact]
        public void TestTransactionFlowStateFailed()
        {
            var stlEnqMsg = new SettlementEnquiryRequest(RequestIdHelper.Id("stlenq")).ToMessage();
            var transactionFlowState = new TransactionFlowState("1", TransactionType.SettlementEnquiry, 0, stlEnqMsg, $"Waiting for EFTPOS connection to make a settlement enquiry");
            transactionFlowState.Failed(stlEnqMsg, "Failed");

            Assert.Equal(transactionFlowState.Response, stlEnqMsg);
            Assert.True(transactionFlowState.Finished);
            Assert.Equal(transactionFlowState.Success, Message.SuccessState.Failed);
            Assert.Equal(transactionFlowState.DisplayMessage, "Failed");
        }

        [Fact]
        public void TestTransactionFlowSignatureRequired()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""data"":{""merchant_receipt"": ""\nEFTPOS FROM WESTPAC\nVAAS Product 2\n275 Kent St\nSydney 2000\nAustralia\n\n\nMID         02447506\nTSP     100381990116\nTIME 26APR17   11:29\nRRN     170426000358\nTRAN 000358   CREDIT\nAmex               S\nCARD............4477\nAUTH          764167\n\nPURCHASE   AUD100.00\nTIP          AUD5.00\n\nTOTAL      AUD105.00\n\n\n (001) APPROVE WITH\n     SIGNATURE\n\n\n\n\n\n\nSIGN:_______________\n\n\n\n\n\n\n\n"",""pos_ref_id"":""prchs-06-06-2019-11-49-05""},""datetime"": ""2017-04-26T11:30:21.000"",""event"": ""signature_required"",""id"": ""24""}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            SignatureRequired response = new SignatureRequired(msg);

            var transactionFlowState = new TransactionFlowState("1", TransactionType.SettlementEnquiry, 0, msg, $"Waiting for EFTPOS connection to make a settlement enquiry");
            transactionFlowState.SignatureRequired(response, "SignatureRequired");

            Assert.Equal(transactionFlowState.SignatureRequiredMessage, response);
            Assert.True(transactionFlowState.AwaitingSignatureCheck);
            Assert.Equal(transactionFlowState.DisplayMessage, "SignatureRequired");
        }

        [Fact]
        public void TestTransactionFlowSignatureResponded()
        {
            var stlEnqMsg = new SettlementEnquiryRequest(RequestIdHelper.Id("stlenq")).ToMessage();
            var transactionFlowState = new TransactionFlowState("1", TransactionType.SettlementEnquiry, 0, stlEnqMsg, $"Waiting for EFTPOS connection to make a settlement enquiry");
            transactionFlowState.SignatureResponded("SignatureResponded");

            Assert.False(transactionFlowState.AwaitingSignatureCheck);
            Assert.Equal(transactionFlowState.DisplayMessage, "SignatureResponded");
        }

        [Fact]
        public void TestTransactionFlowPhoneForAuthRequired()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""event"":""authorisation_code_required"",""id"":""20"",""datetime"":""2017-11-01T06:09:33.918"",""data"":{""merchant_id"":""12345678"",""auth_centre_phone_number"":""1800999999"",""pos_ref_id"": ""xyz""}}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            PhoneForAuthRequired request = new PhoneForAuthRequired(msg);

            var transactionFlowState = new TransactionFlowState("1", TransactionType.SettlementEnquiry, 0, msg, $"Waiting for EFTPOS connection to make a settlement enquiry");
            transactionFlowState.PhoneForAuthRequired(request, "PhoneForAuthRequired");

            Assert.Equal(transactionFlowState.PhoneForAuthRequiredMessage, request);
            Assert.True(transactionFlowState.AwaitingPhoneForAuth);
            Assert.Equal(transactionFlowState.DisplayMessage, "PhoneForAuthRequired");
        }

        [Fact]
        public void TestTransactionFlowAuthCodeSent()
        {
            var stlEnqMsg = new SettlementEnquiryRequest(RequestIdHelper.Id("stlenq")).ToMessage();
            var transactionFlowState = new TransactionFlowState("1", TransactionType.SettlementEnquiry, 0, stlEnqMsg, $"Waiting for EFTPOS connection to make a settlement enquiry");
            transactionFlowState.AuthCodeSent("AuthCodeSent");

            Assert.False(transactionFlowState.AwaitingPhoneForAuth);
            Assert.Equal(transactionFlowState.DisplayMessage, "AuthCodeSent");
        }

        [Fact]
        public void TestTransactionFlowCompleted()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""data"":{""merchant_receipt"": ""\nEFTPOS FROM WESTPAC\nVAAS Product 2\n275 Kent St\nSydney 2000\nAustralia\n\n\nMID         02447506\nTSP     100381990116\nTIME 26APR17   11:29\nRRN     170426000358\nTRAN 000358   CREDIT\nAmex               S\nCARD............4477\nAUTH          764167\n\nPURCHASE   AUD100.00\nTIP          AUD5.00\n\nTOTAL      AUD105.00\n\n\n (001) APPROVE WITH\n     SIGNATURE\n\n\n\n\n\n\nSIGN:_______________\n\n\n\n\n\n\n\n"",""pos_ref_id"":""prchs-06-06-2019-11-49-05""},""datetime"": ""2017-04-26T11:30:21.000"",""event"": ""signature_required"",""id"": ""24""}}";

            Message msg = Message.FromJson(jsonStr, secrets);

            var transactionFlowState = new TransactionFlowState("1", TransactionType.SettlementEnquiry, 0, msg, $"Waiting for EFTPOS connection to make a settlement enquiry");
            transactionFlowState.Completed(Message.SuccessState.Success, msg, "Completed");

            Assert.Equal(transactionFlowState.Success, Message.SuccessState.Success);
            Assert.Equal(transactionFlowState.Response, msg);
            Assert.True(transactionFlowState.Finished);
            Assert.False(transactionFlowState.AttemptingToCancel);
            Assert.False(transactionFlowState.AwaitingGltResponse);
            Assert.False(transactionFlowState.AwaitingSignatureCheck);
            Assert.False(transactionFlowState.AwaitingPhoneForAuth);
            Assert.Equal(transactionFlowState.DisplayMessage, "Completed");
        }

        [Fact]
        public void TestTransactionFlowUnknownCompleted()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""data"":{""merchant_receipt"": ""\nEFTPOS FROM WESTPAC\nVAAS Product 2\n275 Kent St\nSydney 2000\nAustralia\n\n\nMID         02447506\nTSP     100381990116\nTIME 26APR17   11:29\nRRN     170426000358\nTRAN 000358   CREDIT\nAmex               S\nCARD............4477\nAUTH          764167\n\nPURCHASE   AUD100.00\nTIP          AUD5.00\n\nTOTAL      AUD105.00\n\n\n (001) APPROVE WITH\n     SIGNATURE\n\n\n\n\n\n\nSIGN:_______________\n\n\n\n\n\n\n\n"",""pos_ref_id"":""prchs-06-06-2019-11-49-05""},""datetime"": ""2017-04-26T11:30:21.000"",""event"": ""signature_required"",""id"": ""24""}}";

            Message msg = Message.FromJson(jsonStr, secrets);

            var transactionFlowState = new TransactionFlowState("1", TransactionType.SettlementEnquiry, 0, msg, $"Waiting for EFTPOS connection to make a settlement enquiry");
            transactionFlowState.UnknownCompleted("UnknownCompleted");

            Assert.Equal(transactionFlowState.Success, Message.SuccessState.Unknown);
            Assert.Null(transactionFlowState.Response);
            Assert.True(transactionFlowState.Finished);
            Assert.False(transactionFlowState.AttemptingToCancel);
            Assert.False(transactionFlowState.AwaitingGltResponse);
            Assert.False(transactionFlowState.AwaitingSignatureCheck);
            Assert.False(transactionFlowState.AwaitingPhoneForAuth);
            Assert.Equal(transactionFlowState.DisplayMessage, "UnknownCompleted");
        }
    }
}
