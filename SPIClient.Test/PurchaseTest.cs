using SPIClient;
using System;
using System.Globalization;
using Xunit;

namespace Test
{
    public class PurchaseTest
    {
        [Fact]
        public void TestPurchaseRequest()
        {
            int purchaseAmount = 1000;
            string posRefId = "test";

            PurchaseRequest request = new PurchaseRequest(purchaseAmount, posRefId);
            Message msg = request.ToMessage();

            Assert.Equal(msg.EventName, "purchase");
            Assert.Equal(purchaseAmount, msg.GetDataIntValue("purchase_amount"));
            Assert.Equal(posRefId, msg.GetDataStringValue("pos_ref_id"));
            Assert.Equal(request.AmountCents, purchaseAmount);
            Assert.NotNull(request.Id);
            Assert.Equal(request.AmountSummary(), "Purchase: $10.00; Tip: $.00; Cashout: $.00;");
        }

        [Fact]
        public void TestPurchaseRequestWithFull()
        {
            int purchaseAmount = 1000;
            string posRefId = "test";
            int surchargeAmount = 100;
            int tipAmount = 200;
            bool promptForCashout = true;
            int cashoutAmount = 200;

            PurchaseRequest request = new PurchaseRequest(purchaseAmount, posRefId);
            request.TipAmount = tipAmount;
            request.SurchargeAmount = surchargeAmount;
            request.PromptForCashout = promptForCashout;
            request.CashoutAmount = cashoutAmount;
            Message msg = request.ToMessage();

            Assert.Equal(purchaseAmount, msg.GetDataIntValue("purchase_amount"));
            Assert.Equal(posRefId, msg.GetDataStringValue("pos_ref_id"));
            Assert.Equal(surchargeAmount, msg.GetDataIntValue("surcharge_amount"));
            Assert.Equal(cashoutAmount, msg.GetDataIntValue("cash_amount"));
            Assert.Equal(promptForCashout, msg.GetDataBoolValue("prompt_for_cashout", false));
        }

        [Fact]
        public void TestPurchaseRequestWithConfig()
        {
            int purchaseAmount = 1000;
            string posRefId = "test";

            SpiConfig config = new SpiConfig();
            config.PrintMerchantCopy = true;
            config.PromptForCustomerCopyOnEftpos = false;
            config.SignatureFlowOnEftpos = true;

            PurchaseRequest request = new PurchaseRequest(purchaseAmount, posRefId);
            request.Config = config;

            Message msg = request.ToMessage();

            Assert.Equal(config.PrintMerchantCopy, msg.GetDataBoolValue("print_merchant_copy", false));
            Assert.Equal(config.PromptForCustomerCopyOnEftpos, msg.GetDataBoolValue("prompt_for_customer_copy", false));
            Assert.Equal(config.SignatureFlowOnEftpos, msg.GetDataBoolValue("print_for_signature_required_transactions", false));
        }

        [Fact]
        public void TestPurchaseRequestWithOptions()
        {
            int purchaseAmount = 1000;
            string posRefId = "test";
            string merchantReceiptHeader = "";
            string merchantReceiptFooter = "merchantfooter";
            string customerReceiptHeader = "customerheader";
            string customerReceiptFooter = "";

            TransactionOptions options = new TransactionOptions();
            options.SetMerchantReceiptFooter(merchantReceiptFooter);
            options.SetCustomerReceiptHeader(customerReceiptHeader);

            PurchaseRequest request = new PurchaseRequest(purchaseAmount, posRefId);
            request.Options = options;
            Message msg = request.ToMessage();

            Assert.Equal(merchantReceiptHeader, msg.GetDataStringValue("merchant_receipt_header"));
            Assert.Equal(merchantReceiptFooter, msg.GetDataStringValue("merchant_receipt_footer"));
            Assert.Equal(customerReceiptHeader, msg.GetDataStringValue("customer_receipt_header"));
            Assert.Equal(customerReceiptFooter, msg.GetDataStringValue("customer_receipt_footer"));
        }

        [Fact]
        public void TestPurchaseRequestWithOptions_None()
        {
            int purchaseAmount = 1000;
            string posRefId = "test";

            PurchaseRequest request = new PurchaseRequest(purchaseAmount, posRefId);
            Message msg = request.ToMessage();

            Assert.Equal("", msg.GetDataStringValue("merchant_receipt_header"));
            Assert.Equal("", msg.GetDataStringValue("merchant_receipt_footer"));
            Assert.Equal("", msg.GetDataStringValue("customer_receipt_header"));
            Assert.Equal("", msg.GetDataStringValue("customer_receipt_footer"));
        }

        [Fact]
        public void TestPurchaseResponse()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""data"":{""account_type"":""SAVINGS"",""auth_code"":""278045"",""bank_cash_amount"":200,""bank_date"":""06062019"",""bank_noncash_amount"":1200,""bank_settlement_date"":""06062019"",""bank_time"":""110750"",""card_entry"":""MAG_STRIPE"",""cash_amount"":200,""currency"":""AUD"",""customer_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\nTIME 06JUN19   11:07\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190606001102\r\nDebit(S)         SAV\r\nCARD............5581\r\nAUTH          278045\r\n\r\nPURCHASE    AUD10.00\r\nCASH         AUD2.00\r\nSURCHARGE    AUD2.00\r\nTOTAL       AUD14.00\r\n\r\n   (000) APPROVED\r\n\r\n  *CUSTOMER COPY*\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""customer_receipt_printed"":false,""expiry_date"":""0822"",""host_response_code"":""000"",""host_response_text"":""APPROVED"",""informative_text"":""                "",""masked_pan"":""............5581"",""merchant_acquirer"":""EFTPOS FROM BANK SA"",""merchant_addr"":""213 Miller Street"",""merchant_city"":""Sydney"",""merchant_country"":""Australia"",""merchant_id"":""22341842"",""merchant_name"":""Merchant4"",""merchant_postcode"":""2060"",""merchant_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\nTIME 06JUN19   11:07\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190606001102\r\nDebit(S)         SAV\r\nCARD............5581\r\nAUTH          278045\r\n\r\nPURCHASE    AUD10.00\r\nCASH         AUD2.00\r\nSURCHARGE    AUD2.00\r\nTOTAL       AUD14.00\r\n\r\n   (000) APPROVED\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""merchant_receipt_printed"":false,""online_indicator"":""Y"",""pos_ref_id"":""prchs-06-06-2019-11-07-50"",""purchase_amount"":1000,""rrn"":""190606001102"",""scheme_name"":""Debit"",""stan"":""001102"",""success"":true,""surcharge_amount"":200,""terminal_id"":""100612348842"",""terminal_ref_id"":""12348842_06062019110812"",""transaction_type"":""PURCHASE""},""datetime"":""2019-06-06T11:08:12.946"",""event"":""purchase_response"",""id"":""prchs5""}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            PurchaseResponse response = new PurchaseResponse(msg);

            Assert.Equal(msg.EventName, "purchase_response");
            Assert.True(response.Success);
            Assert.Equal(response.RequestId, "prchs5");
            Assert.Equal(response.PosRefId, "prchs-06-06-2019-11-07-50");
            Assert.Equal(response.SchemeName, "Debit");
            Assert.Equal(response.GetRRN(), "190606001102");
            Assert.Equal(response.GetPurchaseAmount(), 1000);
            Assert.Equal(response.GetCashoutAmount(), 200);
            Assert.Equal(response.GetTipAmount(), 0);
            Assert.Equal(response.GetSurchargeAmount(), 200);
            Assert.Equal(response.GetBankNonCashAmount(), 1200);
            Assert.Equal(response.GetBankCashAmount(), 200);
            Assert.NotNull(response.GetCustomerReceipt());
            Assert.NotNull(response.GetMerchantReceipt());
            Assert.Equal(response.GetResponseText(), "APPROVED");
            Assert.Equal(response.GetResponseCode(), "000");
            Assert.Equal(response.GetTerminalReferenceId(), "12348842_06062019110812");
            Assert.Equal(response.GetCardEntry(), "MAG_STRIPE");
            Assert.Equal(response.GetAccountType(), "SAVINGS");
            Assert.Equal(response.GetAuthCode(), "278045");
            Assert.Equal(response.GetBankDate(), "06062019");
            Assert.Equal(response.GetBankTime(), "110750");
            Assert.Equal(response.GetMaskedPan(), "............5581");
            Assert.Equal(response.GetTerminalId(), "100612348842");
            Assert.False(response.WasCustomerReceiptPrinted());
            Assert.False(response.WasMerchantReceiptPrinted());
            Assert.Equal(response.GetSettlementDate(), DateTime.ParseExact(msg.GetDataStringValue("bank_settlement_date"), "ddMMyyyy", CultureInfo.InvariantCulture).Date);
            Assert.Equal(response.GetResponseValue("pos_ref_id"), response.PosRefId);

            response = new PurchaseResponse();
            Assert.Null(SpiClientTestUtils.GetInstanceField(response.GetType(), response, "_m"));
            Assert.Null(response.PosRefId);
        }

        [Fact]
        public void TestCancelTransactionRequest()
        {
            CancelTransactionRequest request = new CancelTransactionRequest();
            Message msg = request.ToMessage();

            Assert.NotNull(msg);
            Assert.Equal(msg.EventName, "cancel_transaction");
        }

        [Fact]
        public void TestCancelTransactionResponse()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"": {""event"": ""cancel_response"", ""id"": ""0"", ""datetime"": ""2018-02-06T15:16:44.094"", ""data"": {""pos_ref_id"": ""123456abc"", ""success"": false, ""error_reason"": ""txn_past_point_of_no_return"", ""error_detail"":""Too late to cancel transaction"" }}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            CancelTransactionResponse response = new CancelTransactionResponse(msg);

            Assert.Equal(msg.EventName, "cancel_response");
            Assert.False(response.Success);
            Assert.Equal(response.PosRefId, "123456abc");
            Assert.Equal(response.GetErrorReason(), "txn_past_point_of_no_return");
            Assert.NotNull(response.GetErrorDetail());
            Assert.Equal(response.GetResponseValueWithAttribute("pos_ref_id"), response.PosRefId);


            response = new CancelTransactionResponse();
            Assert.Null(SpiClientTestUtils.GetInstanceField(response.GetType(), response, "_m"));
            Assert.Null(response.PosRefId);
        }

        [Fact]
        public void TestGetLastTransactionRequest()
        {
            GetLastTransactionRequest request = new GetLastTransactionRequest();
            Message msg = request.ToMessage();

            Assert.NotNull(msg);
            Assert.Equal(msg.EventName, "get_last_transaction");
        }

        [Fact]
        public void TestGetLastTransactionResponse()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""data"":{""account_type"":""CREDIT"",""auth_code"":""139059"",""bank_date"":""14062019"",""bank_noncash_amount"":1000,""bank_settlement_date"":""14062019"",""bank_time"":""153747"",""card_entry"":""EMV_CTLS"",""currency"":""AUD"",""customer_receipt"":"""",""customer_receipt_printed"":false,""emv_actioncode"":""ARP"",""emv_actioncode_values"":""9BDDE227547B41F43030"",""emv_pix"":""1010"",""emv_rid"":""A000000003"",""emv_tsi"":""0000"",""emv_tvr"":""0000000000"",""expiry_date"":""1122"",""host_response_code"":""000"",""host_response_text"":""APPROVED"",""informative_text"":""                "",""masked_pan"":""............3952"",""merchant_acquirer"":""EFTPOS FROM BANK SA"",""merchant_addr"":""213 Miller Street"",""merchant_city"":""Sydney"",""merchant_country"":""Australia"",""merchant_id"":""22341842"",""merchant_name"":""Merchant4"",""merchant_postcode"":""2060"",""merchant_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\nTIME 14JUN19   15:37\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190614001137\r\nVisa Credit     \r\nVisa(C)           CR\r\nCARD............3952\r\nAID   A0000000031010\r\nTVR       0000000000\r\nAUTH          139059\r\n\r\nPURCHASE    AUD10.00\r\n\r\n   (000) APPROVED\r\n\r\n*DUPLICATE  RECEIPT*\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""merchant_receipt_printed"":false,""online_indicator"":""Y"",""pos_ref_id"":""prchs-14-06-2019-15-37-49"",""purchase_amount"":1000,""rrn"":""190614001137"",""scheme_app_name"":""Visa Credit"",""scheme_name"":""Visa"",""stan"":""001137"",""success"":true,""terminal_id"":""100612348842"",""terminal_ref_id"":""12348842_14062019153831"",""transaction_type"":""PURCHASE""},""datetime"":""2019-06-14T15:38:31.620"",""event"":""last_transaction"",""id"":""glt10""}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            GetLastTransactionResponse response = new GetLastTransactionResponse(msg);

            Assert.Equal(msg.EventName, "last_transaction");
            Assert.True(response.WasRetrievedSuccessfully());
            Assert.Equal(response.GetSuccessState(), Message.SuccessState.Success);
            Assert.True(response.WasSuccessfulTx());
            Assert.Equal(response.GetTxType(), "PURCHASE");
            Assert.Equal(response.GetPosRefId(), "prchs-14-06-2019-15-37-49");
            Assert.Equal(response.GetBankNonCashAmount(), 1000);
            Assert.Equal(response.GetSchemeName(), "Visa");
            Assert.Equal(response.GetSchemeApp(), "Visa");
            Assert.Equal(response.GetAmount(), 0);
            Assert.Equal(response.GetTransactionAmount(), 0);
            Assert.Equal(response.GetBankDateTimeString(), "14062019153747");
            Assert.Equal(response.GetRRN(), "190614001137");
            Assert.Equal(response.GetResponseText(), "APPROVED");
            Assert.Equal(response.GetResponseCode(), "000");

            response.CopyMerchantReceiptToCustomerReceipt();
            Assert.Equal(msg.GetDataStringValue("customer_receipt"), msg.GetDataStringValue("merchant_receipt"));

            response = new GetLastTransactionResponse();
            Assert.Null(SpiClientTestUtils.GetInstanceField(response.GetType(), response, "_m"));
        }

        [Fact]
        public void TestGetLastTransactionResponse_TimeOutOfSyncError()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""data"":{""account_type"":""NOT-SET"",""bank_date"":""07062019"",""bank_settlement_date"":""06062019"",""bank_time"":""143821"",""card_entry"":""NOT-SET"",""error_detail"":""see 'host_response_text' for details"",""error_reason"":""TIME_OUT_OF_SYNC"",""host_response_code"":""511"",""host_response_text"":""TRANS CANCELLED"",""pos_ref_id"":""prchs-07-06-2019-14-38-20"",""rrn"":""190606000000"",""scheme_name"":""TOTAL"",""stan"":""000000"",""success"":false,""terminal_ref_id"":""12348842_07062019144136""},""datetime"":""2019-06-07T14:41:36.857"",""event"":""last_transaction"",""id"":""glt18""}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            GetLastTransactionResponse response = new GetLastTransactionResponse(msg);

            Assert.Equal(msg.EventName, "last_transaction");
            Assert.Equal(msg.GetErrorDetail(), "see 'host_response_text' for details");
            Assert.True(response.WasTimeOutOfSyncError());
            Assert.True(response.WasRetrievedSuccessfully());
            Assert.Equal(response.GetSuccessState(), Message.SuccessState.Failed);
            Assert.False(response.WasSuccessfulTx());
            Assert.Equal(response.GetPosRefId(), "prchs-07-06-2019-14-38-20");
            Assert.Equal(response.GetBankNonCashAmount(), 0);
            Assert.Equal(response.GetSchemeName(), "TOTAL");
            Assert.Equal(response.GetBankDateTimeString(), "07062019143821");
            Assert.Equal(response.GetRRN(), "190606000000");
            Assert.Equal(response.GetResponseText(), "TRANS CANCELLED");
            Assert.Equal(response.GetResponseCode(), "511");
        }

        [Fact]
        public void TestGetLastTransactionResponse_OperationInProgressError()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""data"":{""account_type"":""NOT-SET"",""bank_date"":""07062019"",""bank_settlement_date"":""06062019"",""bank_time"":""143821"",""card_entry"":""NOT-SET"",""error_detail"":""see 'host_response_text' for details"",""error_reason"":""OPERATION_IN_PROGRESS"",""host_response_code"":""511"",""host_response_text"":""TRANS CANCELLED"",""pos_ref_id"":""prchs-07-06-2019-14-38-20"",""rrn"":""190606000000"",""scheme_name"":""TOTAL"",""stan"":""000000"",""success"":false,""terminal_ref_id"":""12348842_07062019144136""},""datetime"":""2019-06-07T14:41:36.857"",""event"":""last_transaction"",""id"":""glt18""}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            GetLastTransactionResponse response = new GetLastTransactionResponse(msg);

            Assert.Equal(msg.EventName, "last_transaction");
            Assert.True(response.WasOperationInProgressError());
            Assert.True(response.WasRetrievedSuccessfully());
            Assert.Equal(response.GetSuccessState(), Message.SuccessState.Failed);
            Assert.False(response.WasSuccessfulTx());
            Assert.Equal(response.GetPosRefId(), "prchs-07-06-2019-14-38-20");
            Assert.Equal(response.GetBankNonCashAmount(), 0);
            Assert.Equal(response.GetSchemeName(), "TOTAL");
            Assert.Equal(response.GetBankDateTimeString(), "07062019143821");
            Assert.Equal(response.GetRRN(), "190606000000");
            Assert.Equal(response.GetResponseText(), "TRANS CANCELLED");
            Assert.Equal(response.GetResponseCode(), "511");
        }

        [Fact]
        public void TestGetLastTransactionResponse_WaitingForSignatureResponse()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""data"":{""account_type"":""NOT-SET"",""bank_date"":""07062019"",""bank_settlement_date"":""06062019"",""bank_time"":""143821"",""card_entry"":""NOT-SET"",""error_detail"":""see 'host_response_text' for details"",""error_reason"":""OPERATION_IN_PROGRESS_AWAITING_SIGNATURE"",""host_response_code"":""511"",""host_response_text"":""TRANS CANCELLED"",""pos_ref_id"":""prchs-07-06-2019-14-38-20"",""rrn"":""190606000000"",""scheme_name"":""TOTAL"",""stan"":""000000"",""success"":false,""terminal_ref_id"":""12348842_07062019144136""},""datetime"":""2019-06-07T14:41:36.857"",""event"":""last_transaction"",""id"":""glt18""}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            GetLastTransactionResponse response = new GetLastTransactionResponse(msg);

            Assert.Equal(msg.EventName, "last_transaction");
            Assert.True(response.IsWaitingForSignatureResponse());
            Assert.True(response.WasRetrievedSuccessfully());
            Assert.Equal(response.GetSuccessState(), Message.SuccessState.Failed);
            Assert.False(response.WasSuccessfulTx());
            Assert.Equal(response.GetPosRefId(), "prchs-07-06-2019-14-38-20");
            Assert.Equal(response.GetBankNonCashAmount(), 0);
            Assert.Equal(response.GetSchemeName(), "TOTAL");
            Assert.Equal(response.GetBankDateTimeString(), "07062019143821");
            Assert.Equal(response.GetRRN(), "190606000000");
            Assert.Equal(response.GetResponseText(), "TRANS CANCELLED");
            Assert.Equal(response.GetResponseCode(), "511");
        }

        [Fact]
        public void TestGetLastTransactionResponse_WaitingForAuthCode()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""data"":{""account_type"":""NOT-SET"",""bank_date"":""07062019"",""bank_settlement_date"":""06062019"",""bank_time"":""143821"",""card_entry"":""NOT-SET"",""error_detail"":""see 'host_response_text' for details"",""error_reason"":""OPERATION_IN_PROGRESS_AWAITING_PHONE_AUTH_CODE"",""host_response_code"":""511"",""host_response_text"":""TRANS CANCELLED"",""pos_ref_id"":""prchs-07-06-2019-14-38-20"",""rrn"":""190606000000"",""scheme_name"":""TOTAL"",""stan"":""000000"",""success"":false,""terminal_ref_id"":""12348842_07062019144136""},""datetime"":""2019-06-07T14:41:36.857"",""event"":""last_transaction"",""id"":""glt18""}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            GetLastTransactionResponse response = new GetLastTransactionResponse(msg);

            Assert.Equal(msg.EventName, "last_transaction");
            Assert.True(response.IsWaitingForAuthCode());
            Assert.True(response.WasRetrievedSuccessfully());
            Assert.Equal(response.GetSuccessState(), Message.SuccessState.Failed);
            Assert.False(response.WasSuccessfulTx());
            Assert.Equal(response.GetPosRefId(), "prchs-07-06-2019-14-38-20");
            Assert.Equal(response.GetBankNonCashAmount(), 0);
            Assert.Equal(response.GetSchemeName(), "TOTAL");
            Assert.Equal(response.GetBankDateTimeString(), "07062019143821");
            Assert.Equal(response.GetRRN(), "190606000000");
            Assert.Equal(response.GetResponseText(), "TRANS CANCELLED");
            Assert.Equal(response.GetResponseCode(), "511");
        }

        [Fact]
        public void TestGetLastTransactionResponse_StillInProgress()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""data"":{""account_type"":""NOT-SET"",""bank_date"":""07062019"",""bank_settlement_date"":""06062019"",""bank_time"":""143821"",""card_entry"":""NOT-SET"",""error_detail"":""see 'host_response_text' for details"",""error_reason"":""OPERATION_IN_PROGRESS"",""host_response_code"":""511"",""host_response_text"":""TRANS CANCELLED"",""pos_ref_id"":""prchs-07-06-2019-14-38-20"",""rrn"":""190606000000"",""scheme_name"":""TOTAL"",""stan"":""000000"",""success"":false,""terminal_ref_id"":""12348842_07062019144136""},""datetime"":""2019-06-07T14:41:36.857"",""event"":""last_transaction"",""id"":""glt18""}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            GetLastTransactionResponse response = new GetLastTransactionResponse(msg);

            Assert.Equal(msg.EventName, "last_transaction");
            Assert.True(response.IsStillInProgress("prchs-07-06-2019-14-38-20"));
            Assert.True(response.WasRetrievedSuccessfully());
            Assert.Equal(response.GetSuccessState(), Message.SuccessState.Failed);
            Assert.False(response.WasSuccessfulTx());
            Assert.Equal(response.GetPosRefId(), "prchs-07-06-2019-14-38-20");
            Assert.Equal(response.GetBankNonCashAmount(), 0);
            Assert.Equal(response.GetSchemeName(), "TOTAL");
            Assert.Equal(response.GetBankDateTimeString(), "07062019143821");
            Assert.Equal(response.GetRRN(), "190606000000");
            Assert.Equal(response.GetResponseText(), "TRANS CANCELLED");
            Assert.Equal(response.GetResponseCode(), "511");
        }

        [Fact]
        public void TestRefundRequest()
        {
            int refundAmount = 1000;
            string posRefId = "test";
            bool suppressMerchantPassword = true;

            RefundRequest request = new RefundRequest(refundAmount, posRefId, suppressMerchantPassword);
            Message msg = request.ToMessage();

            Assert.Equal(msg.EventName, "refund");
            Assert.Equal(refundAmount, msg.GetDataIntValue("refund_amount"));
            Assert.Equal(posRefId, msg.GetDataStringValue("pos_ref_id"));
            Assert.Equal(suppressMerchantPassword, msg.GetDataBoolValue("suppress_merchant_password", false));
            Assert.NotNull(request.Id);
        }

        [Fact]
        public void TestRefundRequestWithConfig()
        {
            int refundAmount = 1000;
            string posRefId = "test";
            bool suppressMerchantPassword = true;

            SpiConfig config = new SpiConfig();
            config.PrintMerchantCopy = true;
            config.PromptForCustomerCopyOnEftpos = false;
            config.SignatureFlowOnEftpos = true;

            RefundRequest request = new RefundRequest(refundAmount, posRefId, suppressMerchantPassword);
            request.Config = config;

            Message msg = request.ToMessage();

            Assert.Equal(config.PrintMerchantCopy, msg.GetDataBoolValue("print_merchant_copy", false));
            Assert.Equal(config.PromptForCustomerCopyOnEftpos, msg.GetDataBoolValue("prompt_for_customer_copy", false));
            Assert.Equal(config.SignatureFlowOnEftpos, msg.GetDataBoolValue("print_for_signature_required_transactions", false));
        }

        [Fact]
        public void TestRefundRequestWithOptions()
        {
            int refundAmount = 1000;
            string posRefId = "test";
            bool suppressMerchantPassword = true;
            string merchantReceiptHeader = "";
            string merchantReceiptFooter = "merchantfooter";
            string customerReceiptHeader = "customerheader";
            string customerReceiptFooter = "";

            TransactionOptions options = new TransactionOptions();
            options.SetMerchantReceiptFooter(merchantReceiptFooter);
            options.SetCustomerReceiptHeader(customerReceiptHeader);

            RefundRequest request = new RefundRequest(refundAmount, posRefId, suppressMerchantPassword);
            request.Options = options;
            Message msg = request.ToMessage();

            Assert.Equal(merchantReceiptHeader, msg.GetDataStringValue("merchant_receipt_header"));
            Assert.Equal(merchantReceiptFooter, msg.GetDataStringValue("merchant_receipt_footer"));
            Assert.Equal(customerReceiptHeader, msg.GetDataStringValue("customer_receipt_header"));
            Assert.Equal(customerReceiptFooter, msg.GetDataStringValue("customer_receipt_footer"));
        }

        [Fact]
        public void TestRefundRequestWithOptions_none()
        {
            int refundAmount = 1000;
            string posRefId = "test";
            bool suppressMerchantPassword = true;

            RefundRequest request = new RefundRequest(refundAmount, posRefId, suppressMerchantPassword);
            Message msg = request.ToMessage();

            Assert.Equal("", msg.GetDataStringValue("merchant_receipt_header"));
            Assert.Equal("", msg.GetDataStringValue("merchant_receipt_footer"));
            Assert.Equal("", msg.GetDataStringValue("customer_receipt_header"));
            Assert.Equal("", msg.GetDataStringValue("customer_receipt_footer"));
        }

        [Fact]
        public void TestRefundResponse()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""data"":{""account_type"":""CREDIT"",""auth_code"":""067849"",""bank_date"":""06062019"",""bank_noncash_amount"":1000,""bank_settlement_date"":""06062019"",""bank_time"":""114905"",""card_entry"":""EMV_CTLS"",""currency"":""AUD"",""customer_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\nTIME 06JUN19   11:49\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190606001105\r\nVisa(C)           CR\r\nCARD............5581\r\nAUTH          067849\r\n\r\nREFUND      AUD10.00\r\n\r\n   (000) APPROVED\r\n\r\n  *CUSTOMER COPY*\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""customer_receipt_printed"":false,""emv_actioncode"":""ARQ"",""emv_actioncode_values"":""67031BCC5AD15818"",""expiry_date"":""0822"",""host_response_code"":""000"",""host_response_text"":""APPROVED"",""informative_text"":""                "",""masked_pan"":""............5581"",""merchant_acquirer"":""EFTPOS FROM BANK SA"",""merchant_addr"":""213 Miller Street"",""merchant_city"":""Sydney"",""merchant_country"":""Australia"",""merchant_id"":""22341842"",""merchant_name"":""Merchant4"",""merchant_postcode"":""2060"",""merchant_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\nTIME 06JUN19   11:49\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190606001105\r\nVisa(C)           CR\r\nCARD............5581\r\nAUTH          067849\r\n\r\nREFUND      AUD10.00\r\n\r\n   (000) APPROVED\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""merchant_receipt_printed"":false,""online_indicator"":""Y"",""pos_ref_id"":""rfnd-06-06-2019-11-49-05"",""refund_amount"":1000,""rrn"":""190606001105"",""scheme_name"":""Visa"",""stan"":""001105"",""success"":true,""terminal_id"":""100612348842"",""terminal_ref_id"":""12348842_06062019114915"",""transaction_type"":""REFUND""},""datetime"":""2019-06-06T11:49:15.038"",""event"":""refund_response"",""id"":""refund150""}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            RefundResponse response = new RefundResponse(msg);

            Assert.Equal(msg.EventName, "refund_response");
            Assert.True(response.Success);
            Assert.Equal(response.RequestId, "refund150");
            Assert.Equal(response.PosRefId, "rfnd-06-06-2019-11-49-05");
            Assert.Equal(response.SchemeName, "Visa");
            Assert.Equal(response.SchemeAppName, "Visa");
            Assert.Equal(response.GetRRN(), "190606001105");
            Assert.Equal(response.GetRefundAmount(), 1000);
            Assert.NotNull(response.GetCustomerReceipt());
            Assert.NotNull(response.GetMerchantReceipt());
            Assert.Equal(response.GetResponseText(), "APPROVED");
            Assert.Equal(response.GetResponseCode(), "000");
            Assert.Equal(response.GetTerminalReferenceId(), "12348842_06062019114915");
            Assert.Equal(response.GetCardEntry(), "EMV_CTLS");
            Assert.Equal(response.GetAccountType(), "CREDIT");
            Assert.Equal(response.GetAuthCode(), "067849");
            Assert.Equal(response.GetBankDate(), "06062019");
            Assert.Equal(response.GetBankTime(), "114905");
            Assert.Equal(response.GetMaskedPan(), "............5581");
            Assert.Equal(response.GetTerminalId(), "100612348842");
            Assert.False(response.WasCustomerReceiptPrinted());
            Assert.False(response.WasMerchantReceiptPrinted());
            Assert.Equal(response.GetSettlementDate(), DateTime.ParseExact(msg.GetDataStringValue("bank_settlement_date"), "ddMMyyyy", CultureInfo.InvariantCulture).Date);
            Assert.Equal(response.GetResponseValue("pos_ref_id"), response.PosRefId);

            response = new RefundResponse();
            Assert.Null(SpiClientTestUtils.GetInstanceField(response.GetType(), response, "_m"));
            Assert.Null(response.PosRefId);
        }

        [Fact]
        public void TestSignatureRequired()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""data"":{""merchant_receipt"": ""\nEFTPOS FROM WESTPAC\nVAAS Product 2\n275 Kent St\nSydney 2000\nAustralia\n\n\nMID         02447506\nTSP     100381990116\nTIME 26APR17   11:29\nRRN     170426000358\nTRAN 000358   CREDIT\nAmex               S\nCARD............4477\nAUTH          764167\n\nPURCHASE   AUD100.00\nTIP          AUD5.00\n\nTOTAL      AUD105.00\n\n\n (001) APPROVE WITH\n     SIGNATURE\n\n\n\n\n\n\nSIGN:_______________\n\n\n\n\n\n\n\n"",""pos_ref_id"":""prchs-06-06-2019-11-49-05""},""datetime"": ""2017-04-26T11:30:21.000"",""event"": ""signature_required"",""id"": ""24""}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            SignatureRequired response = new SignatureRequired(msg);

            Assert.Equal(msg.EventName, "signature_required");
            Assert.Equal(response.RequestId, "24");
            Assert.Equal(response.PosRefId, "prchs-06-06-2019-11-49-05");
            Assert.NotNull(response.GetMerchantReceipt());
        }

        [Fact]
        public void TestSignatureRequired_MissingReceipt()
        {
            string posRefId = "test";
            string requestId = "12";
            string receiptToSign = "MISSING RECEIPT\n DECLINE AND TRY AGAIN.";
            SignatureRequired response = new SignatureRequired(posRefId, requestId, receiptToSign);

            Assert.Equal(response.GetMerchantReceipt(), receiptToSign);
        }

        [Fact]
        public void TestSignatureDecline()
        {
            string posRefId = "test";
            SignatureDecline request = new SignatureDecline(posRefId);
            Message msg = request.ToMessage();

            Assert.Equal(msg.EventName, "signature_decline");
            Assert.Equal(posRefId, msg.GetDataStringValue("pos_ref_id"));
        }

        [Fact]
        public void TestSignatureAccept()
        {
            string posRefId = "test";
            SignatureAccept request = new SignatureAccept(posRefId);
            Message msg = request.ToMessage();

            Assert.Equal(msg.EventName, "signature_accept");
            Assert.Equal(posRefId, msg.GetDataStringValue("pos_ref_id"));
        }

        [Fact]
        public void TestMotoPurchaseRequest()
        {
            string posRefId = "test";
            int purchaseAmount = 1000;
            int surchargeAmount = 200;
            bool suppressMerchantPassword = true;

            MotoPurchaseRequest request = new MotoPurchaseRequest(purchaseAmount, posRefId);
            request.SurchargeAmount = surchargeAmount;
            request.SuppressMerchantPassword = suppressMerchantPassword;
            Message msg = request.ToMessage();

            Assert.Equal(msg.EventName, "moto_purchase");
            Assert.Equal(posRefId, msg.GetDataStringValue("pos_ref_id"));
            Assert.Equal(surchargeAmount, msg.GetDataIntValue("surcharge_amount"));
            Assert.Equal(purchaseAmount, msg.GetDataIntValue("purchase_amount"));
            Assert.Equal(suppressMerchantPassword, msg.GetDataBoolValue("suppress_merchant_password", false));
        }

        [Fact]
        public void TestMotoPurchaseRequestWithConfig()
        {
            int purchaseAmount = 1000;
            string posRefId = "test";

            SpiConfig config = new SpiConfig();
            config.PrintMerchantCopy = true;
            config.PromptForCustomerCopyOnEftpos = false;
            config.SignatureFlowOnEftpos = true;

            MotoPurchaseRequest request = new MotoPurchaseRequest(purchaseAmount, posRefId);
            request.Config = config;

            Message msg = request.ToMessage();

            Assert.Equal(config.PrintMerchantCopy, msg.GetDataBoolValue("print_merchant_copy", false));
            Assert.Equal(config.PromptForCustomerCopyOnEftpos, msg.GetDataBoolValue("prompt_for_customer_copy", false));
            Assert.Equal(config.SignatureFlowOnEftpos, msg.GetDataBoolValue("print_for_signature_required_transactions", false));
        }

        [Fact]
        public void TestMotoPurchaseRequestWithOptions()
        {
            int purchaseAmount = 1000;
            string posRefId = "test";
            string merchantReceiptHeader = "";
            string merchantReceiptFooter = "merchantfooter";
            string customerReceiptHeader = "customerheader";
            string customerReceiptFooter = "";

            TransactionOptions options = new TransactionOptions();
            options.SetMerchantReceiptFooter(merchantReceiptFooter);
            options.SetCustomerReceiptHeader(customerReceiptHeader);

            MotoPurchaseRequest request = new MotoPurchaseRequest(purchaseAmount, posRefId);
            request.Options = options;
            Message msg = request.ToMessage();

            Assert.Equal(merchantReceiptHeader, msg.GetDataStringValue("merchant_receipt_header"));
            Assert.Equal(merchantReceiptFooter, msg.GetDataStringValue("merchant_receipt_footer"));
            Assert.Equal(customerReceiptHeader, msg.GetDataStringValue("customer_receipt_header"));
            Assert.Equal(customerReceiptFooter, msg.GetDataStringValue("customer_receipt_footer"));
        }

        [Fact]
        public void TestMotoPurchaseRequesteWithOptions_none()
        {
            int purchaseAmount = 1000;
            string posRefId = "test";

            MotoPurchaseRequest request = new MotoPurchaseRequest(purchaseAmount, posRefId);
            Message msg = request.ToMessage();

            Assert.Equal("", msg.GetDataStringValue("merchant_receipt_header"));
            Assert.Equal("", msg.GetDataStringValue("merchant_receipt_footer"));
            Assert.Equal("", msg.GetDataStringValue("customer_receipt_header"));
            Assert.Equal("", msg.GetDataStringValue("customer_receipt_footer"));
        }

        [Fact]
        public void TestMotoPurchaseResponse()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""data"":{""pos_ref_id"":""a-zA-Z0-9"",""account_type"": ""CREDIT"",""purchase_amount"": 1000,""surcharge_amount"": 200,""bank_noncash_amount"": 1200,""bank_cash_amount"": 200,""auth_code"": ""653230"",""bank_date"": ""07092017"",""bank_time"": ""152137"",""bank_settlement_date"": ""21102017"",""currency"": ""AUD"",""emv_actioncode"": """",""emv_actioncode_values"": """",""emv_pix"": """",""emv_rid"": """",""emv_tsi"": """",""emv_tvr"": """",""expiry_date"": ""1117"",""host_response_code"": ""000"",""host_response_text"": ""APPROVED"",""informative_text"": ""                "",""masked_pan"": ""............0794"",""merchant_acquirer"": ""EFTPOS FROM WESTPAC"",""merchant_addr"": ""275 Kent St"",""merchant_city"": ""Sydney"",""merchant_country"": ""Australia"",""merchant_id"": ""02447508"",""merchant_name"": ""VAAS Product 4"",""merchant_postcode"": ""2000"",""online_indicator"": ""Y"",""scheme_app_name"": """",""scheme_name"": """",""stan"": ""000212"",""rrn"": ""1517890741"",""success"": true,""terminal_id"": ""100381990118"",""transaction_type"": ""MOTO"",""card_entry"": ""MANUAL_PHONE"",""customer_receipt"":""EFTPOS FROM WESTPAC\r\nVAAS Product 4\r\n275 Kent St\r\nSydney\r\nMID02447508\r\nTSP100381990118\r\nTIME 07SEP17   15:21\r\nRRN     1517890741\r\nTRAN 000212   CREDIT\r\nVisa Credit     \r\nVisa               M\r\nCARD............0794\r\nAUTH          653230\r\n\r\nMOTO   AUD10000\r\n\r\nTOTAL      AUD10000\r\n\r\n\r\n(000)APPROVED\r\n\r\n\r\n *CUSTOMER COPY*\r\n\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""merchant_receipt"":""EFTPOS FROM WESTPAC\r\nVAAS Product4\r\n275 Kent St\r\nSydney\r\nMID02447508\r\nTSP100381990118\r\nTIME 07SEP17   15:21\r\nRRN     1517890741\r\nTRAN 000212   CREDIT\r\nVisa Credit     \r\nVisa               M\r\nCARD............0794\r\nAUTH          653230\r\n\r\nPURCHASE   AUD10000\r\n\r\nTOTAL      AUD10000\r\n\r\n\r\n(000) APPROVED\r\n\r\n\r\n\r\n\r\n\r\n\r\n\r\n""},""datetime"": ""2018-02-06T04:19:00.545"",""event"": ""moto_purchase_response"",""id"": ""4""}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            MotoPurchaseResponse response = new MotoPurchaseResponse(msg);

            Assert.Equal(msg.EventName, "moto_purchase_response");
            Assert.True(response.PurchaseResponse.Success);
            Assert.Equal(response.PurchaseResponse.RequestId, "4");
            Assert.Equal(response.PurchaseResponse.PosRefId, "a-zA-Z0-9");
            Assert.Equal(response.PurchaseResponse.SchemeName, "");
            Assert.Equal(response.PurchaseResponse.GetAuthCode(), "653230");
            Assert.Equal(response.PurchaseResponse.GetRRN(), "1517890741");
            Assert.Equal(response.PurchaseResponse.GetPurchaseAmount(), 1000);
            Assert.Equal(response.PurchaseResponse.GetSurchargeAmount(), 200);
            Assert.Equal(response.PurchaseResponse.GetBankNonCashAmount(), 1200);
            Assert.Equal(response.PurchaseResponse.GetBankCashAmount(), 200);
            Assert.NotNull(response.PurchaseResponse.GetCustomerReceipt());
            Assert.NotNull(response.PurchaseResponse.GetMerchantReceipt());
            Assert.Equal(response.PurchaseResponse.GetResponseText(), "APPROVED");
            Assert.Equal(response.PurchaseResponse.GetResponseCode(), "000");
            Assert.Equal(response.PurchaseResponse.GetCardEntry(), "MANUAL_PHONE");
            Assert.Equal(response.PurchaseResponse.GetAccountType(), "CREDIT");
            Assert.Equal(response.PurchaseResponse.GetBankDate(), "07092017");
            Assert.Equal(response.PurchaseResponse.GetBankTime(), "152137");
            Assert.Equal(response.PurchaseResponse.GetMaskedPan(), "............0794");
            Assert.Equal(response.PurchaseResponse.GetTerminalId(), "100381990118");
            Assert.False(response.PurchaseResponse.WasCustomerReceiptPrinted());
            Assert.False(response.PurchaseResponse.WasMerchantReceiptPrinted());
            Assert.Equal(response.PurchaseResponse.GetResponseValue("pos_ref_id"), response.PosRefId);

            response = new MotoPurchaseResponse();
            Assert.Null(response.PurchaseResponse);
            Assert.Null(response.PurchaseResponse?.PosRefId);
        }

        [Fact]
        public void TestPhoneForAuthRequired()
        {
            string posRefId = "xyz";
            string merchantId = "12345678";
            string requestId = "20";
            string phoneNumnber = "1800999999";

            PhoneForAuthRequired request = new PhoneForAuthRequired(posRefId, requestId, phoneNumnber, merchantId);

            Assert.Equal(request.PosRefId, posRefId);
            Assert.Equal(request.RequestId, requestId);
            Assert.Equal(request.GetPhoneNumber(), phoneNumnber);
            Assert.Equal(request.GetMerchantId(), merchantId);
        }

        [Fact]
        public void TestPhoneForAuthRequiredWithMessage()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""event"":""authorisation_code_required"",""id"":""20"",""datetime"":""2017-11-01T06:09:33.918"",""data"":{""merchant_id"":""12345678"",""auth_centre_phone_number"":""1800999999"",""pos_ref_id"": ""xyz""}}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            PhoneForAuthRequired request = new PhoneForAuthRequired(msg);

            Assert.Equal(msg.EventName, "authorisation_code_required");
            Assert.Equal(request.PosRefId, "xyz");
            Assert.Equal(request.RequestId, "20");
            Assert.Equal(request.GetPhoneNumber(), "1800999999");
            Assert.Equal(request.GetMerchantId(), "12345678");
        }

        [Fact]
        public void TestAuthCodeAdvice()
        {
            string posRefId = "xyz";
            string authcode = "1234ab";

            AuthCodeAdvice request = new AuthCodeAdvice(posRefId, authcode);
            Message msg = request.ToMessage();

            Assert.Equal(request.PosRefId, msg.GetDataStringValue("pos_ref_id"));
            Assert.Equal(request.AuthCode, msg.GetDataStringValue("auth_code"));
        }

    }
}
