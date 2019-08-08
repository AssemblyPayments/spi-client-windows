using SPIClient;
using System;
using System.Globalization;
using Xunit;

namespace Test
{
    public class PurchaseTest
    {
        [Fact]
        public void PurchaseRequest_OnValidRequest_ReturnObjects()
        {
            // arrange
            const int purchaseAmount = 1000;
            const string posRefId = "test";

            // act
            var request = new PurchaseRequest(purchaseAmount, posRefId);
            var msg = request.ToMessage();

            // assert
            Assert.Equal("purchase", msg.EventName);
            Assert.Equal(purchaseAmount, msg.GetDataIntValue("purchase_amount"));
            Assert.Equal(posRefId, msg.GetDataStringValue("pos_ref_id"));
            Assert.Equal(purchaseAmount, request.AmountCents);
            Assert.NotNull(request.Id);
            Assert.Equal("Purchase: $10.00; Tip: $.00; Cashout: $.00;", request.AmountSummary());
        }

        [Fact]
        public void PurchaseRequest_OnValidRequestWithFull_ReturnObjects()
        {
            // arrange
            const int purchaseAmount = 1000;
            const string posRefId = "test";
            const int surchargeAmount = 100;
            const int tipAmount = 200;
            const bool promptForCashout = true;
            const int cashoutAmount = 200;

            // act
            var request = new PurchaseRequest(purchaseAmount, posRefId);
            request.TipAmount = tipAmount;
            request.SurchargeAmount = surchargeAmount;
            request.PromptForCashout = promptForCashout;
            request.CashoutAmount = cashoutAmount;
            var msg = request.ToMessage();

            // assert
            Assert.Equal(purchaseAmount, msg.GetDataIntValue("purchase_amount"));
            Assert.Equal(posRefId, msg.GetDataStringValue("pos_ref_id"));
            Assert.Equal(surchargeAmount, msg.GetDataIntValue("surcharge_amount"));
            Assert.Equal(cashoutAmount, msg.GetDataIntValue("cash_amount"));
            Assert.Equal(promptForCashout, msg.GetDataBoolValue("prompt_for_cashout", false));
        }

        [Fact]
        public void PurchaseRequest_OnValidRequestWithConfig_ReturnObjects()
        {
            // arrange
            const int purchaseAmount = 1000;
            const string posRefId = "test";

            var config = new SpiConfig();
            config.PrintMerchantCopy = true;
            config.PromptForCustomerCopyOnEftpos = false;
            config.SignatureFlowOnEftpos = true;

            // act
            var request = new PurchaseRequest(purchaseAmount, posRefId);
            request.Config = config;
            var msg = request.ToMessage();

            // assert
            Assert.Equal(config.PrintMerchantCopy, msg.GetDataBoolValue("print_merchant_copy", false));
            Assert.Equal(config.PromptForCustomerCopyOnEftpos, msg.GetDataBoolValue("prompt_for_customer_copy", false));
            Assert.Equal(config.SignatureFlowOnEftpos, msg.GetDataBoolValue("print_for_signature_required_transactions", false));
        }

        [Fact]
        public void PurchaseRequest_OnValidRequestWithOptions_ReturnObjects()
        {
            // arrange
            const int purchaseAmount = 1000;
            const string posRefId = "test";
            const string merchantReceiptHeader = "";
            const string merchantReceiptFooter = "merchantfooter";
            const string customerReceiptHeader = "customerheader";
            const string customerReceiptFooter = "";

            var options = new TransactionOptions();
            options.SetMerchantReceiptFooter(merchantReceiptFooter);
            options.SetCustomerReceiptHeader(customerReceiptHeader);

            // act
            var request = new PurchaseRequest(purchaseAmount, posRefId);
            request.Options = options;
            var msg = request.ToMessage();

            // assert
            Assert.Equal(merchantReceiptHeader, msg.GetDataStringValue("merchant_receipt_header"));
            Assert.Equal(merchantReceiptFooter, msg.GetDataStringValue("merchant_receipt_footer"));
            Assert.Equal(customerReceiptHeader, msg.GetDataStringValue("customer_receipt_header"));
            Assert.Equal(customerReceiptFooter, msg.GetDataStringValue("customer_receipt_footer"));
        }

        [Fact]
        public void PurchaseRequest_OnValidRequestWithOptionsNone_ReturnObjects()
        {
            // arrange
            const int purchaseAmount = 1000;
            const string posRefId = "test";

            // act
            var request = new PurchaseRequest(purchaseAmount, posRefId);
            var msg = request.ToMessage();

            // assert
            Assert.Equal("", msg.GetDataStringValue("merchant_receipt_header"));
            Assert.Equal("", msg.GetDataStringValue("merchant_receipt_footer"));
            Assert.Equal("", msg.GetDataStringValue("customer_receipt_header"));
            Assert.Equal("", msg.GetDataStringValue("customer_receipt_footer"));
        }

        [Fact]
        public void PurchaseResponse_OnValidResponse_ReturnObjects()
        {
            // arrange
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""data"":{""account_type"":""SAVINGS"",""auth_code"":""278045"",""bank_cash_amount"":200,""bank_date"":""06062019"",""bank_noncash_amount"":1200,""bank_settlement_date"":""06062019"",""bank_time"":""110750"",""card_entry"":""MAG_STRIPE"",""cash_amount"":200,""currency"":""AUD"",""customer_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\nTIME 06JUN19   11:07\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190606001102\r\nDebit(S)         SAV\r\nCARD............5581\r\nAUTH          278045\r\n\r\nPURCHASE    AUD10.00\r\nCASH         AUD2.00\r\nSURCHARGE    AUD2.00\r\nTOTAL       AUD14.00\r\n\r\n   (000) APPROVED\r\n\r\n  *CUSTOMER COPY*\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""customer_receipt_printed"":false,""expiry_date"":""0822"",""host_response_code"":""000"",""host_response_text"":""APPROVED"",""informative_text"":""                "",""masked_pan"":""............5581"",""merchant_acquirer"":""EFTPOS FROM BANK SA"",""merchant_addr"":""213 Miller Street"",""merchant_city"":""Sydney"",""merchant_country"":""Australia"",""merchant_id"":""22341842"",""merchant_name"":""Merchant4"",""merchant_postcode"":""2060"",""merchant_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\nTIME 06JUN19   11:07\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190606001102\r\nDebit(S)         SAV\r\nCARD............5581\r\nAUTH          278045\r\n\r\nPURCHASE    AUD10.00\r\nCASH         AUD2.00\r\nSURCHARGE    AUD2.00\r\nTOTAL       AUD14.00\r\n\r\n   (000) APPROVED\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""merchant_receipt_printed"":false,""online_indicator"":""Y"",""pos_ref_id"":""prchs-06-06-2019-11-07-50"",""purchase_amount"":1000,""rrn"":""190606001102"",""scheme_name"":""Debit"",""stan"":""001102"",""success"":true,""surcharge_amount"":200,""terminal_id"":""100612348842"",""terminal_ref_id"":""12348842_06062019110812"",""transaction_type"":""PURCHASE""},""datetime"":""2019-06-06T11:08:12.946"",""event"":""purchase_response"",""id"":""prchs5""}}";

            // act
            var msg = Message.FromJson(jsonStr, secrets);
            var response = new PurchaseResponse(msg);

            // assert
            Assert.Equal("purchase_response", msg.EventName);
            Assert.True(response.Success);
            Assert.Equal("prchs5", response.RequestId);
            Assert.Equal("prchs-06-06-2019-11-07-50", response.PosRefId);
            Assert.Equal("Debit", response.SchemeName);
            Assert.Equal("190606001102", response.GetRRN());
            Assert.Equal(1000, response.GetPurchaseAmount());
            Assert.Equal(200, response.GetCashoutAmount());
            Assert.Equal(0, response.GetTipAmount());
            Assert.Equal(200, response.GetSurchargeAmount());
            Assert.Equal(1200, response.GetBankNonCashAmount());
            Assert.Equal(200, response.GetBankCashAmount());
            Assert.NotNull(response.GetCustomerReceipt());
            Assert.NotNull(response.GetMerchantReceipt());
            Assert.Equal("APPROVED", response.GetResponseText());
            Assert.Equal("000", response.GetResponseCode());
            Assert.Equal("12348842_06062019110812", response.GetTerminalReferenceId());
            Assert.Equal("MAG_STRIPE", response.GetCardEntry());
            Assert.Equal("SAVINGS", response.GetAccountType());
            Assert.Equal("278045", response.GetAuthCode());
            Assert.Equal("06062019", response.GetBankDate());
            Assert.Equal("110750", response.GetBankTime());
            Assert.Equal("............5581", response.GetMaskedPan());
            Assert.Equal("100612348842", response.GetTerminalId());
            Assert.False(response.WasCustomerReceiptPrinted());
            Assert.False(response.WasMerchantReceiptPrinted());
            Assert.Equal(DateTime.ParseExact(msg.GetDataStringValue("bank_settlement_date"), "ddMMyyyy", CultureInfo.InvariantCulture).Date, response.GetSettlementDate());
            Assert.Equal(response.GetResponseValue("pos_ref_id"), response.PosRefId);

            // act
            response = new PurchaseResponse();

            // assert
            Assert.Null(SpiClientTestUtils.GetInstanceField(response.GetType(), response, "_m"));
            Assert.Null(response.PosRefId);
        }

        [Fact]
        public void CancelTransactionRequest_OnValidRequest_ReturnObject()
        {
            // arrange
            var request = new CancelTransactionRequest();

            // act
            var msg = request.ToMessage();

            // assert
            Assert.NotNull(msg);
            Assert.Equal("cancel_transaction", msg.EventName);
        }

        [Fact]
        public void CancelTransactionResponse_OnValidResponse_ReturnObjects()
        {
            // arrange
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"": {""event"": ""cancel_response"", ""id"": ""0"", ""datetime"": ""2018-02-06T15:16:44.094"", ""data"": {""pos_ref_id"": ""123456abc"", ""success"": false, ""error_reason"": ""TXN_PAST_POINT_OF_NO_RETURN"", ""error_detail"":""Txn has passed the point of no return"" }}}";

            // act
            var msg = Message.FromJson(jsonStr, secrets);
            var response = new CancelTransactionResponse(msg);

            // assert
            Assert.Equal("cancel_response", msg.EventName);
            Assert.False(response.Success);
            Assert.Equal("123456abc", response.PosRefId);
            Assert.Equal("TXN_PAST_POINT_OF_NO_RETURN", response.GetErrorReason());
            Assert.True(response.WasTxnPastPointOfNoReturn());
            Assert.NotNull(response.GetErrorDetail());
            Assert.Equal(response.GetResponseValueWithAttribute("pos_ref_id"), response.PosRefId);

            // act
            response = new CancelTransactionResponse();

            // assert
            Assert.Null(SpiClientTestUtils.GetInstanceField(response.GetType(), response, "_m"));
            Assert.Null(response.PosRefId);
        }

        [Fact]
        public void GetLastTransactionRequest_OnValidRequest_ReturnObject()
        {
            // arrange
            var request = new GetLastTransactionRequest();

            // act
            var msg = request.ToMessage();

            // assert
            Assert.NotNull(msg);
            Assert.Equal("get_last_transaction", msg.EventName);
        }

        [Fact]
        public void GetLastTransactionResponse_OnValidResponse_ReturnObjects()
        {
            // arrange
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""data"":{""account_type"":""CREDIT"",""auth_code"":""139059"",""bank_date"":""14062019"",""bank_noncash_amount"":1000,""bank_settlement_date"":""14062019"",""bank_time"":""153747"",""card_entry"":""EMV_CTLS"",""currency"":""AUD"",""customer_receipt"":"""",""customer_receipt_printed"":false,""emv_actioncode"":""ARP"",""emv_actioncode_values"":""9BDDE227547B41F43030"",""emv_pix"":""1010"",""emv_rid"":""A000000003"",""emv_tsi"":""0000"",""emv_tvr"":""0000000000"",""expiry_date"":""1122"",""host_response_code"":""000"",""host_response_text"":""APPROVED"",""informative_text"":""                "",""masked_pan"":""............3952"",""merchant_acquirer"":""EFTPOS FROM BANK SA"",""merchant_addr"":""213 Miller Street"",""merchant_city"":""Sydney"",""merchant_country"":""Australia"",""merchant_id"":""22341842"",""merchant_name"":""Merchant4"",""merchant_postcode"":""2060"",""merchant_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\nTIME 14JUN19   15:37\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190614001137\r\nVisa Credit     \r\nVisa(C)           CR\r\nCARD............3952\r\nAID   A0000000031010\r\nTVR       0000000000\r\nAUTH          139059\r\n\r\nPURCHASE    AUD10.00\r\n\r\n   (000) APPROVED\r\n\r\n*DUPLICATE  RECEIPT*\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""merchant_receipt_printed"":false,""online_indicator"":""Y"",""pos_ref_id"":""prchs-14-06-2019-15-37-49"",""purchase_amount"":1000,""rrn"":""190614001137"",""scheme_app_name"":""Visa Credit"",""scheme_name"":""Visa"",""stan"":""001137"",""success"":true,""terminal_id"":""100612348842"",""terminal_ref_id"":""12348842_14062019153831"",""transaction_type"":""PURCHASE""},""datetime"":""2019-06-14T15:38:31.620"",""event"":""last_transaction"",""id"":""glt10""}}";

            // act
            var msg = Message.FromJson(jsonStr, secrets);
            var response = new GetLastTransactionResponse(msg);
            response.CopyMerchantReceiptToCustomerReceipt();

            //assert
            Assert.Equal("last_transaction", msg.EventName);
            Assert.True(response.WasRetrievedSuccessfully());
            Assert.Equal(Message.SuccessState.Success, response.GetSuccessState());
            Assert.True(response.WasSuccessfulTx());
            Assert.Equal("PURCHASE", response.GetTxType());
            Assert.Equal("prchs-14-06-2019-15-37-49", response.GetPosRefId());
            Assert.Equal(1000, response.GetBankNonCashAmount());
            Assert.Equal("Visa", response.GetSchemeName());
            Assert.Equal("Visa", response.GetSchemeApp());
            Assert.Equal(0, response.GetAmount());
            Assert.Equal(0, response.GetTransactionAmount());
            Assert.Equal("14062019153747", response.GetBankDateTimeString());
            Assert.Equal("190614001137", response.GetRRN());
            Assert.Equal("APPROVED", response.GetResponseText());
            Assert.Equal("000", response.GetResponseCode());
            Assert.Equal(msg.GetDataStringValue("customer_receipt"), msg.GetDataStringValue("merchant_receipt"));

            // act
            response = new GetLastTransactionResponse();

            // assert
            Assert.Null(SpiClientTestUtils.GetInstanceField(response.GetType(), response, "_m"));
        }

        [Fact]
        public void GetLastTransactionResponse_OnValidResponseTimeOutOfSyncError_ReturnObjects()
        {
            // arrange
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""data"":{""account_type"":""NOT-SET"",""bank_date"":""07062019"",""bank_settlement_date"":""06062019"",""bank_time"":""143821"",""card_entry"":""NOT-SET"",""error_detail"":""see 'host_response_text' for details"",""error_reason"":""TIME_OUT_OF_SYNC"",""host_response_code"":""511"",""host_response_text"":""TRANS CANCELLED"",""pos_ref_id"":""prchs-07-06-2019-14-38-20"",""rrn"":""190606000000"",""scheme_name"":""TOTAL"",""stan"":""000000"",""success"":false,""terminal_ref_id"":""12348842_07062019144136""},""datetime"":""2019-06-07T14:41:36.857"",""event"":""last_transaction"",""id"":""glt18""}}";

            // act
            var msg = Message.FromJson(jsonStr, secrets);
            var response = new GetLastTransactionResponse(msg);

            // assert
            Assert.Equal("last_transaction", msg.EventName);
            Assert.Equal("see 'host_response_text' for details", msg.GetErrorDetail());
            Assert.True(response.WasTimeOutOfSyncError());
            Assert.True(response.WasRetrievedSuccessfully());
            Assert.Equal(Message.SuccessState.Failed, response.GetSuccessState());
            Assert.False(response.WasSuccessfulTx());
            Assert.Equal("prchs-07-06-2019-14-38-20", response.GetPosRefId());
            Assert.Equal(0, response.GetBankNonCashAmount());
            Assert.Equal("TOTAL", response.GetSchemeName());
            Assert.Equal("07062019143821", response.GetBankDateTimeString());
            Assert.Equal("190606000000", response.GetRRN());
            Assert.Equal("TRANS CANCELLED", response.GetResponseText());
            Assert.Equal("511", response.GetResponseCode());
        }

        [Fact]
        public void GetLastTransactionResponse_OnValidResponseOperationInProgressError_ReturnObjects()
        {
            // arrange
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""data"":{""account_type"":""NOT-SET"",""bank_date"":""07062019"",""bank_settlement_date"":""06062019"",""bank_time"":""143821"",""card_entry"":""NOT-SET"",""error_detail"":""see 'host_response_text' for details"",""error_reason"":""OPERATION_IN_PROGRESS"",""host_response_code"":""511"",""host_response_text"":""TRANS CANCELLED"",""pos_ref_id"":""prchs-07-06-2019-14-38-20"",""rrn"":""190606000000"",""scheme_name"":""TOTAL"",""stan"":""000000"",""success"":false,""terminal_ref_id"":""12348842_07062019144136""},""datetime"":""2019-06-07T14:41:36.857"",""event"":""last_transaction"",""id"":""glt18""}}";

            // act
            var msg = Message.FromJson(jsonStr, secrets);
            var response = new GetLastTransactionResponse(msg);

            // assert
            Assert.Equal("last_transaction", msg.EventName);
            Assert.True(response.WasOperationInProgressError());
            Assert.True(response.WasRetrievedSuccessfully());
            Assert.Equal(Message.SuccessState.Failed, response.GetSuccessState());
            Assert.False(response.WasSuccessfulTx());
            Assert.Equal("prchs-07-06-2019-14-38-20", response.GetPosRefId());
            Assert.Equal(0, response.GetBankNonCashAmount());
            Assert.Equal("TOTAL", response.GetSchemeName());
            Assert.Equal("07062019143821", response.GetBankDateTimeString());
            Assert.Equal("190606000000", response.GetRRN());
            Assert.Equal("TRANS CANCELLED", response.GetResponseText());
            Assert.Equal("511", response.GetResponseCode());
        }

        [Fact]
        public void GetLastTransactionResponse_OnValidWaitingForSignatureResponse_ReturnObjects()
        {
            // arrange
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""data"":{""account_type"":""NOT-SET"",""bank_date"":""07062019"",""bank_settlement_date"":""06062019"",""bank_time"":""143821"",""card_entry"":""NOT-SET"",""error_detail"":""see 'host_response_text' for details"",""error_reason"":""OPERATION_IN_PROGRESS_AWAITING_SIGNATURE"",""host_response_code"":""511"",""host_response_text"":""TRANS CANCELLED"",""pos_ref_id"":""prchs-07-06-2019-14-38-20"",""rrn"":""190606000000"",""scheme_name"":""TOTAL"",""stan"":""000000"",""success"":false,""terminal_ref_id"":""12348842_07062019144136""},""datetime"":""2019-06-07T14:41:36.857"",""event"":""last_transaction"",""id"":""glt18""}}";

            // act
            var msg = Message.FromJson(jsonStr, secrets);
            var response = new GetLastTransactionResponse(msg);

            // assert
            Assert.Equal("last_transaction", msg.EventName);
            Assert.True(response.IsWaitingForSignatureResponse());
            Assert.True(response.WasRetrievedSuccessfully());
            Assert.Equal(Message.SuccessState.Failed, response.GetSuccessState());
            Assert.False(response.WasSuccessfulTx());
            Assert.Equal("prchs-07-06-2019-14-38-20", response.GetPosRefId());
            Assert.Equal(0, response.GetBankNonCashAmount());
            Assert.Equal("TOTAL", response.GetSchemeName());
            Assert.Equal("07062019143821", response.GetBankDateTimeString());
            Assert.Equal("190606000000", response.GetRRN());
            Assert.Equal("TRANS CANCELLED", response.GetResponseText());
            Assert.Equal("511", response.GetResponseCode());
        }

        [Fact]
        public void GetLastTransactionResponse_OnValidResponseWaitingForAuthCode_ReturnObjects()
        {
            // arrange
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""data"":{""account_type"":""NOT-SET"",""bank_date"":""07062019"",""bank_settlement_date"":""06062019"",""bank_time"":""143821"",""card_entry"":""NOT-SET"",""error_detail"":""see 'host_response_text' for details"",""error_reason"":""OPERATION_IN_PROGRESS_AWAITING_PHONE_AUTH_CODE"",""host_response_code"":""511"",""host_response_text"":""TRANS CANCELLED"",""pos_ref_id"":""prchs-07-06-2019-14-38-20"",""rrn"":""190606000000"",""scheme_name"":""TOTAL"",""stan"":""000000"",""success"":false,""terminal_ref_id"":""12348842_07062019144136""},""datetime"":""2019-06-07T14:41:36.857"",""event"":""last_transaction"",""id"":""glt18""}}";

            // act
            var msg = Message.FromJson(jsonStr, secrets);
            var response = new GetLastTransactionResponse(msg);

            // assert
            Assert.Equal("last_transaction", msg.EventName);
            Assert.True(response.IsWaitingForAuthCode());
            Assert.True(response.WasRetrievedSuccessfully());
            Assert.Equal(Message.SuccessState.Failed, response.GetSuccessState());
            Assert.False(response.WasSuccessfulTx());
            Assert.Equal("prchs-07-06-2019-14-38-20", response.GetPosRefId());
            Assert.Equal(0, response.GetBankNonCashAmount());
            Assert.Equal("TOTAL", response.GetSchemeName());
            Assert.Equal("07062019143821", response.GetBankDateTimeString());
            Assert.Equal("190606000000", response.GetRRN());
            Assert.Equal("TRANS CANCELLED", response.GetResponseText());
            Assert.Equal("511", response.GetResponseCode());
        }

        [Fact]
        public void GetLastTransactionResponse_OnValidResponseStillInProgress_ReturnObjects()
        {
            // arrange
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""data"":{""account_type"":""NOT-SET"",""bank_date"":""07062019"",""bank_settlement_date"":""06062019"",""bank_time"":""143821"",""card_entry"":""NOT-SET"",""error_detail"":""see 'host_response_text' for details"",""error_reason"":""OPERATION_IN_PROGRESS"",""host_response_code"":""511"",""host_response_text"":""TRANS CANCELLED"",""pos_ref_id"":""prchs-07-06-2019-14-38-20"",""rrn"":""190606000000"",""scheme_name"":""TOTAL"",""stan"":""000000"",""success"":false,""terminal_ref_id"":""12348842_07062019144136""},""datetime"":""2019-06-07T14:41:36.857"",""event"":""last_transaction"",""id"":""glt18""}}";

            // act
            var msg = Message.FromJson(jsonStr, secrets);
            var response = new GetLastTransactionResponse(msg);

            // assert
            Assert.Equal("last_transaction", msg.EventName);
            Assert.True(response.IsStillInProgress("prchs-07-06-2019-14-38-20"));
            Assert.True(response.WasRetrievedSuccessfully());
            Assert.Equal(Message.SuccessState.Failed, response.GetSuccessState());
            Assert.False(response.WasSuccessfulTx());
            Assert.Equal("prchs-07-06-2019-14-38-20", response.GetPosRefId());
            Assert.Equal(0, response.GetBankNonCashAmount());
            Assert.Equal("TOTAL", response.GetSchemeName());
            Assert.Equal("07062019143821", response.GetBankDateTimeString());
            Assert.Equal("190606000000", response.GetRRN());
            Assert.Equal("TRANS CANCELLED", response.GetResponseText());
            Assert.Equal("511", response.GetResponseCode());
        }

        [Fact]
        public void RefundRequest_OnValidRequest_ReturnObjects()
        {
            // arrange
            const int refundAmount = 1000;
            const string posRefId = "test";
            const bool suppressMerchantPassword = true;

            // act
            var request = new RefundRequest(refundAmount, posRefId, suppressMerchantPassword);
            var msg = request.ToMessage();

            // assert
            Assert.Equal("refund", msg.EventName);
            Assert.Equal(refundAmount, msg.GetDataIntValue("refund_amount"));
            Assert.Equal(posRefId, msg.GetDataStringValue("pos_ref_id"));
            Assert.Equal(suppressMerchantPassword, msg.GetDataBoolValue("suppress_merchant_password", false));
            Assert.NotNull(request.Id);
        }

        [Fact]
        public void RefundRequest_OnValidRequestWithConfig_ReturnObjects()
        {
            // arrange
            const int refundAmount = 1000;
            const string posRefId = "test";
            const bool suppressMerchantPassword = true;

            var config = new SpiConfig();
            config.PrintMerchantCopy = true;
            config.PromptForCustomerCopyOnEftpos = false;
            config.SignatureFlowOnEftpos = true;

            // act
            var request = new RefundRequest(refundAmount, posRefId, suppressMerchantPassword);
            request.Config = config;
            var msg = request.ToMessage();

            // assert
            Assert.Equal(config.PrintMerchantCopy, msg.GetDataBoolValue("print_merchant_copy", false));
            Assert.Equal(config.PromptForCustomerCopyOnEftpos, msg.GetDataBoolValue("prompt_for_customer_copy", false));
            Assert.Equal(config.SignatureFlowOnEftpos, msg.GetDataBoolValue("print_for_signature_required_transactions", false));
        }

        [Fact]
        public void RefundRequest_OnValidRequestWithOptions_ReturnObjects()
        {
            // arrange
            const int refundAmount = 1000;
            const string posRefId = "test";
            const bool suppressMerchantPassword = true;
            const string merchantReceiptHeader = "";
            const string merchantReceiptFooter = "merchantfooter";
            const string customerReceiptHeader = "customerheader";
            const string customerReceiptFooter = "";

            var options = new TransactionOptions();
            options.SetMerchantReceiptFooter(merchantReceiptFooter);
            options.SetCustomerReceiptHeader(customerReceiptHeader);

            // act
            var request = new RefundRequest(refundAmount, posRefId, suppressMerchantPassword);
            request.Options = options;
            var msg = request.ToMessage();

            // assert
            Assert.Equal(merchantReceiptHeader, msg.GetDataStringValue("merchant_receipt_header"));
            Assert.Equal(merchantReceiptFooter, msg.GetDataStringValue("merchant_receipt_footer"));
            Assert.Equal(customerReceiptHeader, msg.GetDataStringValue("customer_receipt_header"));
            Assert.Equal(customerReceiptFooter, msg.GetDataStringValue("customer_receipt_footer"));
        }

        [Fact]
        public void RefundRequest_OnValidRequestWithOptionsNone_ReturnObjects()
        {
            // arrange
            const int refundAmount = 1000;
            const string posRefId = "test";
            const bool suppressMerchantPassword = true;

            // act 
            var request = new RefundRequest(refundAmount, posRefId, suppressMerchantPassword);
            var msg = request.ToMessage();

            // assert
            Assert.Equal("", msg.GetDataStringValue("merchant_receipt_header"));
            Assert.Equal("", msg.GetDataStringValue("merchant_receipt_footer"));
            Assert.Equal("", msg.GetDataStringValue("customer_receipt_header"));
            Assert.Equal("", msg.GetDataStringValue("customer_receipt_footer"));
        }

        [Fact]
        public void RefundResponse_OnValidResponse_ReturnObjects()
        {
            // arrange
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""data"":{""account_type"":""CREDIT"",""auth_code"":""067849"",""bank_date"":""06062019"",""bank_noncash_amount"":1000,""bank_settlement_date"":""06062019"",""bank_time"":""114905"",""card_entry"":""EMV_CTLS"",""currency"":""AUD"",""customer_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\nTIME 06JUN19   11:49\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190606001105\r\nVisa(C)           CR\r\nCARD............5581\r\nAUTH          067849\r\n\r\nREFUND      AUD10.00\r\n\r\n   (000) APPROVED\r\n\r\n  *CUSTOMER COPY*\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""customer_receipt_printed"":false,""emv_actioncode"":""ARQ"",""emv_actioncode_values"":""67031BCC5AD15818"",""expiry_date"":""0822"",""host_response_code"":""000"",""host_response_text"":""APPROVED"",""informative_text"":""                "",""masked_pan"":""............5581"",""merchant_acquirer"":""EFTPOS FROM BANK SA"",""merchant_addr"":""213 Miller Street"",""merchant_city"":""Sydney"",""merchant_country"":""Australia"",""merchant_id"":""22341842"",""merchant_name"":""Merchant4"",""merchant_postcode"":""2060"",""merchant_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\nTIME 06JUN19   11:49\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190606001105\r\nVisa(C)           CR\r\nCARD............5581\r\nAUTH          067849\r\n\r\nREFUND      AUD10.00\r\n\r\n   (000) APPROVED\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""merchant_receipt_printed"":false,""online_indicator"":""Y"",""pos_ref_id"":""rfnd-06-06-2019-11-49-05"",""refund_amount"":1000,""rrn"":""190606001105"",""scheme_name"":""Visa"",""stan"":""001105"",""success"":true,""terminal_id"":""100612348842"",""terminal_ref_id"":""12348842_06062019114915"",""transaction_type"":""REFUND""},""datetime"":""2019-06-06T11:49:15.038"",""event"":""refund_response"",""id"":""refund150""}}";

            // act
            var msg = Message.FromJson(jsonStr, secrets);
            var response = new RefundResponse(msg);

            //assert
            Assert.Equal("refund_response", msg.EventName);
            Assert.True(response.Success);
            Assert.Equal("refund150", response.RequestId);
            Assert.Equal("rfnd-06-06-2019-11-49-05", response.PosRefId);
            Assert.Equal("Visa", response.SchemeName);
            Assert.Equal("Visa", response.SchemeAppName);
            Assert.Equal("190606001105", response.GetRRN());
            Assert.Equal(1000, response.GetRefundAmount());
            Assert.NotNull(response.GetCustomerReceipt());
            Assert.NotNull(response.GetMerchantReceipt());
            Assert.Equal("APPROVED", response.GetResponseText());
            Assert.Equal("000", response.GetResponseCode());
            Assert.Equal("12348842_06062019114915", response.GetTerminalReferenceId());
            Assert.Equal("EMV_CTLS", response.GetCardEntry());
            Assert.Equal("CREDIT", response.GetAccountType());
            Assert.Equal("067849", response.GetAuthCode());
            Assert.Equal("06062019", response.GetBankDate());
            Assert.Equal("114905", response.GetBankTime());
            Assert.Equal("............5581", response.GetMaskedPan());
            Assert.Equal("100612348842", response.GetTerminalId());
            Assert.False(response.WasCustomerReceiptPrinted());
            Assert.False(response.WasMerchantReceiptPrinted());
            Assert.Equal(DateTime.ParseExact(msg.GetDataStringValue("bank_settlement_date"), "ddMMyyyy", CultureInfo.InvariantCulture).Date, response.GetSettlementDate());
            Assert.Equal(response.GetResponseValue("pos_ref_id"), response.PosRefId);

            // act
            response = new RefundResponse();

            // assert
            Assert.Null(SpiClientTestUtils.GetInstanceField(response.GetType(), response, "_m"));
            Assert.Null(response.PosRefId);
        }

        [Fact]
        public void SignatureRequired_OnValidResponse_ReturnObjects()
        {
            // arrange
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""data"":{""merchant_receipt"": ""\nEFTPOS FROM WESTPAC\nVAAS Product 2\n275 Kent St\nSydney 2000\nAustralia\n\n\nMID         02447506\nTSP     100381990116\nTIME 26APR17   11:29\nRRN     170426000358\nTRAN 000358   CREDIT\nAmex               S\nCARD............4477\nAUTH          764167\n\nPURCHASE   AUD100.00\nTIP          AUD5.00\n\nTOTAL      AUD105.00\n\n\n (001) APPROVE WITH\n     SIGNATURE\n\n\n\n\n\n\nSIGN:_______________\n\n\n\n\n\n\n\n"",""pos_ref_id"":""prchs-06-06-2019-11-49-05""},""datetime"": ""2017-04-26T11:30:21.000"",""event"": ""signature_required"",""id"": ""24""}}";

            // act
            var msg = Message.FromJson(jsonStr, secrets);
            var response = new SignatureRequired(msg);

            // assert
            Assert.Equal("signature_required", msg.EventName);
            Assert.Equal("24", response.RequestId);
            Assert.Equal("prchs-06-06-2019-11-49-05", response.PosRefId);
            Assert.NotNull(response.GetMerchantReceipt());
        }

        [Fact]
        public void SignatureRequired_OnValidResponseMissingReceipt_IsCorrect()
        {
            // arrange
            const string posRefId = "test";
            const string requestId = "12";
            const string receiptToSign = "MISSING RECEIPT\n DECLINE AND TRY AGAIN.";

            // act
            var response = new SignatureRequired(posRefId, requestId, receiptToSign);

            // assert
            Assert.Equal(response.GetMerchantReceipt(), receiptToSign);
        }

        [Fact]
        public void SignatureDecline_OnValidResponse_ReturnObjects()
        {
            // arrange
            const string posRefId = "test";

            // act 
            var request = new SignatureDecline(posRefId);
            var msg = request.ToMessage();

            // assert
            Assert.Equal("signature_decline", msg.EventName);
            Assert.Equal(posRefId, msg.GetDataStringValue("pos_ref_id"));
        }

        [Fact]
        public void SignatureAccept_OnValidResponse_ReturnObjects()
        {
            // arrange
            const string posRefId = "test";

            // act 
            var request = new SignatureAccept(posRefId);
            var msg = request.ToMessage();

            // assert
            Assert.Equal(msg.EventName, "signature_accept");
            Assert.Equal(posRefId, msg.GetDataStringValue("pos_ref_id"));
        }

        [Fact]
        public void MotoPurchaseRequest_OnValidRequest_ReturnObjects()
        {
            // arrange
            const string posRefId = "test";
            const int purchaseAmount = 1000;
            const int surchargeAmount = 200;
            const bool suppressMerchantPassword = true;

            // act
            var request = new MotoPurchaseRequest(purchaseAmount, posRefId);
            request.SurchargeAmount = surchargeAmount;
            request.SuppressMerchantPassword = suppressMerchantPassword;
            var msg = request.ToMessage();

            // assert 
            Assert.Equal("moto_purchase", msg.EventName);
            Assert.Equal(posRefId, msg.GetDataStringValue("pos_ref_id"));
            Assert.Equal(surchargeAmount, msg.GetDataIntValue("surcharge_amount"));
            Assert.Equal(purchaseAmount, msg.GetDataIntValue("purchase_amount"));
            Assert.Equal(suppressMerchantPassword, msg.GetDataBoolValue("suppress_merchant_password", false));
        }

        [Fact]
        public void MotoPurchaseRequest_OnValidRequestWithConfig_ReturnObjects()
        {
            // arrange
            const int purchaseAmount = 1000;
            const string posRefId = "test";

            var config = new SpiConfig();
            config.PrintMerchantCopy = true;
            config.PromptForCustomerCopyOnEftpos = false;
            config.SignatureFlowOnEftpos = true;

            // act
            var request = new MotoPurchaseRequest(purchaseAmount, posRefId);
            request.Config = config;
            var msg = request.ToMessage();

            //assert
            Assert.Equal(config.PrintMerchantCopy, msg.GetDataBoolValue("print_merchant_copy", false));
            Assert.Equal(config.PromptForCustomerCopyOnEftpos, msg.GetDataBoolValue("prompt_for_customer_copy", false));
            Assert.Equal(config.SignatureFlowOnEftpos, msg.GetDataBoolValue("print_for_signature_required_transactions", false));
        }

        [Fact]
        public void MotoPurchaseRequest_OnValidRequestWithOptions_ReturnObjects()
        {
            // arrange
            const int purchaseAmount = 1000;
            const string posRefId = "test";
            const string merchantReceiptHeader = "";
            const string merchantReceiptFooter = "merchantfooter";
            const string customerReceiptHeader = "customerheader";
            const string customerReceiptFooter = "";

            var options = new TransactionOptions();
            options.SetMerchantReceiptFooter(merchantReceiptFooter);
            options.SetCustomerReceiptHeader(customerReceiptHeader);

            // act
            var request = new MotoPurchaseRequest(purchaseAmount, posRefId);
            request.Options = options;
            var msg = request.ToMessage();

            // assert
            Assert.Equal(merchantReceiptHeader, msg.GetDataStringValue("merchant_receipt_header"));
            Assert.Equal(merchantReceiptFooter, msg.GetDataStringValue("merchant_receipt_footer"));
            Assert.Equal(customerReceiptHeader, msg.GetDataStringValue("customer_receipt_header"));
            Assert.Equal(customerReceiptFooter, msg.GetDataStringValue("customer_receipt_footer"));
        }

        [Fact]
        public void MotoPurchaseRequest_OnValidRequestWithOptionNone_ReturnObjects()
        {
            // arrange
            const int purchaseAmount = 1000;
            const string posRefId = "test";

            // act
            var request = new MotoPurchaseRequest(purchaseAmount, posRefId);
            var msg = request.ToMessage();

            // assert
            Assert.Equal("", msg.GetDataStringValue("merchant_receipt_header"));
            Assert.Equal("", msg.GetDataStringValue("merchant_receipt_footer"));
            Assert.Equal("", msg.GetDataStringValue("customer_receipt_header"));
            Assert.Equal("", msg.GetDataStringValue("customer_receipt_footer"));
        }

        [Fact]
        public void MotoPurchaseResponse_OnValidResponse_ReturnObjects()
        {
            // arrange
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""data"":{""pos_ref_id"":""a-zA-Z0-9"",""account_type"": ""CREDIT"",""purchase_amount"": 1000,""surcharge_amount"": 200,""bank_noncash_amount"": 1200,""bank_cash_amount"": 200,""auth_code"": ""653230"",""bank_date"": ""07092017"",""bank_time"": ""152137"",""bank_settlement_date"": ""21102017"",""currency"": ""AUD"",""emv_actioncode"": """",""emv_actioncode_values"": """",""emv_pix"": """",""emv_rid"": """",""emv_tsi"": """",""emv_tvr"": """",""expiry_date"": ""1117"",""host_response_code"": ""000"",""host_response_text"": ""APPROVED"",""informative_text"": ""                "",""masked_pan"": ""............0794"",""merchant_acquirer"": ""EFTPOS FROM WESTPAC"",""merchant_addr"": ""275 Kent St"",""merchant_city"": ""Sydney"",""merchant_country"": ""Australia"",""merchant_id"": ""02447508"",""merchant_name"": ""VAAS Product 4"",""merchant_postcode"": ""2000"",""online_indicator"": ""Y"",""scheme_app_name"": """",""scheme_name"": """",""stan"": ""000212"",""rrn"": ""1517890741"",""success"": true,""terminal_id"": ""100381990118"",""transaction_type"": ""MOTO"",""card_entry"": ""MANUAL_PHONE"",""customer_receipt"":""EFTPOS FROM WESTPAC\r\nVAAS Product 4\r\n275 Kent St\r\nSydney\r\nMID02447508\r\nTSP100381990118\r\nTIME 07SEP17   15:21\r\nRRN     1517890741\r\nTRAN 000212   CREDIT\r\nVisa Credit     \r\nVisa               M\r\nCARD............0794\r\nAUTH          653230\r\n\r\nMOTO   AUD10000\r\n\r\nTOTAL      AUD10000\r\n\r\n\r\n(000)APPROVED\r\n\r\n\r\n *CUSTOMER COPY*\r\n\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""merchant_receipt"":""EFTPOS FROM WESTPAC\r\nVAAS Product4\r\n275 Kent St\r\nSydney\r\nMID02447508\r\nTSP100381990118\r\nTIME 07SEP17   15:21\r\nRRN     1517890741\r\nTRAN 000212   CREDIT\r\nVisa Credit     \r\nVisa               M\r\nCARD............0794\r\nAUTH          653230\r\n\r\nPURCHASE   AUD10000\r\n\r\nTOTAL      AUD10000\r\n\r\n\r\n(000) APPROVED\r\n\r\n\r\n\r\n\r\n\r\n\r\n\r\n""},""datetime"": ""2018-02-06T04:19:00.545"",""event"": ""moto_purchase_response"",""id"": ""4""}}";

            // act
            var msg = Message.FromJson(jsonStr, secrets);
            var response = new MotoPurchaseResponse(msg);

            //assert
            Assert.Equal("moto_purchase_response", msg.EventName);
            Assert.True(response.PurchaseResponse.Success);
            Assert.Equal("4", response.PurchaseResponse.RequestId);
            Assert.Equal("a-zA-Z0-9", response.PurchaseResponse.PosRefId);
            Assert.Equal("", response.PurchaseResponse.SchemeName);
            Assert.Equal("653230", response.PurchaseResponse.GetAuthCode());
            Assert.Equal("1517890741", response.PurchaseResponse.GetRRN());
            Assert.Equal(1000, response.PurchaseResponse.GetPurchaseAmount());
            Assert.Equal(200, response.PurchaseResponse.GetSurchargeAmount());
            Assert.Equal(1200, response.PurchaseResponse.GetBankNonCashAmount());
            Assert.Equal(200, response.PurchaseResponse.GetBankCashAmount());
            Assert.NotNull(response.PurchaseResponse.GetCustomerReceipt());
            Assert.NotNull(response.PurchaseResponse.GetMerchantReceipt());
            Assert.Equal("APPROVED", response.PurchaseResponse.GetResponseText());
            Assert.Equal("000", response.PurchaseResponse.GetResponseCode());
            Assert.Equal("MANUAL_PHONE", response.PurchaseResponse.GetCardEntry());
            Assert.Equal("CREDIT", response.PurchaseResponse.GetAccountType());
            Assert.Equal("07092017", response.PurchaseResponse.GetBankDate());
            Assert.Equal("152137", response.PurchaseResponse.GetBankTime());
            Assert.Equal("............0794", response.PurchaseResponse.GetMaskedPan());
            Assert.Equal("100381990118", response.PurchaseResponse.GetTerminalId());
            Assert.False(response.PurchaseResponse.WasCustomerReceiptPrinted());
            Assert.False(response.PurchaseResponse.WasMerchantReceiptPrinted());
            Assert.Equal(response.PurchaseResponse.GetResponseValue("pos_ref_id"), response.PosRefId);

            // act
            response = new MotoPurchaseResponse();

            // assert
            Assert.Null(response.PurchaseResponse);
            Assert.Null(response.PurchaseResponse?.PosRefId);
        }

        [Fact]
        public void PhoneForAuthRequired_OnValidRequest_ReturnObjects()
        {
            // arrange
            const string posRefId = "xyz";
            const string merchantId = "12345678";
            const string requestId = "20";
            const string phoneNumnber = "1800999999";

            // act
            var request = new PhoneForAuthRequired(posRefId, requestId, phoneNumnber, merchantId);

            // assert
            Assert.Equal(posRefId, request.PosRefId);
            Assert.Equal(requestId, request.RequestId);
            Assert.Equal(phoneNumnber, request.GetPhoneNumber());
            Assert.Equal(merchantId, request.GetMerchantId());
        }

        [Fact]
        public void PhoneForAuthRequired_OnValidRequestWithMessage_ReturnObjects()
        {
            // arrange
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""event"":""authorisation_code_required"",""id"":""20"",""datetime"":""2017-11-01T06:09:33.918"",""data"":{""merchant_id"":""12345678"",""auth_centre_phone_number"":""1800999999"",""pos_ref_id"": ""xyz""}}}";

            // act
            var msg = Message.FromJson(jsonStr, secrets);
            var request = new PhoneForAuthRequired(msg);

            // assert
            Assert.Equal("authorisation_code_required", msg.EventName);
            Assert.Equal("xyz", request.PosRefId);
            Assert.Equal("20", request.RequestId);
            Assert.Equal("1800999999", request.GetPhoneNumber());
            Assert.Equal("12345678", request.GetMerchantId());
        }

        [Fact]
        public void AuthCodeAdvice_OnValidRequest_ReturnObjects()
        {
            // arrane
            const string posRefId = "xyz";
            const string authcode = "1234ab";

            // act 
            var request = new AuthCodeAdvice(posRefId, authcode);
            var msg = request.ToMessage();

            // assert
            Assert.Equal(request.PosRefId, msg.GetDataStringValue("pos_ref_id"));
            Assert.Equal(request.AuthCode, msg.GetDataStringValue("auth_code"));
        }

    }
}
