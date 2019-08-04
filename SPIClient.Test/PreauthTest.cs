using SPIClient;
using System;
using System.Globalization;
using Xunit;

namespace Test
{
    public class PreauthTest
    {
        [Fact]
        public void AccountVerifyRequest_ValidRequest_ReturnRequestObject()
        {
            // arrange
            const string posRefId = "test";

            // act
            var request = new AccountVerifyRequest(posRefId);
            var msg = request.ToMessage();

            // assert
            Assert.Equal(msg.EventName, "account_verify");
            Assert.Equal(request.PosRefId, posRefId);
        }

        [Fact]
        public void AccountVerifyResponse_ValidResponse_ReturnResponseObject()
        {
            // arrange
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""data"":{""account_type"":""CREDIT"",""auth_code"":""316810"",""bank_date"":""11062019"",""bank_settlement_date"":""11062019"",""bank_time"":""182739"",""card_entry"":""EMV_INSERT"",""currency"":""AUD"",""customer_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\nTIME 11JUN19   18:27\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190611001109\r\nVisa Credit     \r\nVisa(I)           CR\r\nCARD............3952\r\nAID   A0000000031010\r\nTVR       0080048000\r\nAUTH          316810\r\n\r\nA/C VERIFIED AUD0.00\r\n\r\n   (000) APPROVED\r\n\r\n  *CUSTOMER COPY*\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""customer_receipt_printed"":false,""emv_actioncode"":""TC"",""emv_actioncode_values"":""F1F17B37A5BEF2B1"",""emv_pix"":""1010"",""emv_rid"":""A000000003"",""emv_tsi"":""F800"",""emv_tvr"":""0080048000"",""expiry_date"":""1122"",""host_response_code"":""000"",""host_response_text"":""APPROVED"",""informative_text"":""                "",""masked_pan"":""............3952"",""merchant_acquirer"":""EFTPOS FROM BANK SA"",""merchant_addr"":""213 Miller Street"",""merchant_city"":""Sydney"",""merchant_country"":""Australia"",""merchant_id"":""22341842"",""merchant_name"":""Merchant4"",""merchant_postcode"":""2060"",""merchant_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\nTIME 11JUN19   18:27\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190611001109\r\nVisa Credit     \r\nVisa(I)           CR\r\nCARD............3952\r\nAID   A0000000031010\r\nTVR       0080048000\r\nAUTH          316810\r\n\r\nA/C VERIFIED AUD0.00\r\n\r\n   (000) APPROVED\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""merchant_receipt_printed"":false,""online_indicator"":""Y"",""pos_ref_id"":""actvfy-11-06-2019-18-27-39"",""rrn"":""190611001109"",""scheme_app_name"":""Visa Credit"",""scheme_name"":""Visa"",""stan"":""001109"",""success"":true,""terminal_id"":""100612348842"",""terminal_ref_id"":""12348842_11062019182754"",""transaction_type"":""A/C VERIFIED""},""datetime"":""2019-06-11T18:27:54.933"",""event"":""account_verify_response"",""id"":""prav15""}}";
            var msg = Message.FromJson(jsonStr, secrets);

            // act
            var response = new AccountVerifyResponse(msg);

            // assert
            Assert.Equal("account_verify_response", msg.EventName );
            Assert.Equal("actvfy-11-06-2019-18-27-39", response.PosRefId);
            Assert.True(response.Details.Success);
            Assert.Equal("prav15", response.Details.RequestId);
            Assert.Equal("Visa", response.Details.SchemeName);
            Assert.Equal("190611001109", response.Details.GetRRN());
            Assert.NotNull(response.Details.GetCustomerReceipt());
            Assert.NotNull(response.Details.GetMerchantReceipt());
            Assert.Equal("APPROVED", response.Details.GetResponseText());
            Assert.Equal("000", response.Details.GetResponseCode());
            Assert.Equal("EMV_INSERT", response.Details.GetCardEntry());
            Assert.Equal("CREDIT", response.Details.GetAccountType());
            Assert.Equal("316810", response.Details.GetAuthCode());
            Assert.Equal("11062019", response.Details.GetBankDate() );
            Assert.Equal("182739", response.Details.GetBankTime());
            Assert.Equal("............3952", response.Details.GetMaskedPan());
            Assert.Equal("100612348842", response.Details.GetTerminalId());
            Assert.False(response.Details.WasCustomerReceiptPrinted());
            Assert.False(response.Details.WasMerchantReceiptPrinted());
            Assert.Equal(DateTime.ParseExact(msg.GetDataStringValue("bank_settlement_date"), "ddMMyyyy", CultureInfo.InvariantCulture).Date, response.Details.GetSettlementDate());
        }

        [Fact]
        public void PreauthOpenRequest_ValidRequest_ReturnObject()
        {
            // arrange
            const int preauthAmount = 1000;
            const string posRefId = "test";
            var request = new PreauthOpenRequest(preauthAmount, posRefId);

            // act
            var msg = request.ToMessage();

            // assert
            Assert.Equal("preauth", msg.EventName);
            Assert.Equal(request.PosRefId, msg.GetDataStringValue("pos_ref_id"));
            Assert.Equal(request.PreauthAmount, msg.GetDataIntValue("preauth_amount"));
        }

        [Fact]
        // todo: Refactor into multiple unit tests
        public void PreAuthOpenRequest_OnValidRequestWithConfig_ReturnRequestObject()
        {
            // arrange
            const int preauthAmount = 1000;
            const string posRefId = "test";

            var config = new SpiConfig
            {
                PrintMerchantCopy = true,
                PromptForCustomerCopyOnEftpos = false,
                SignatureFlowOnEftpos = true
            };

            var request = new PreauthOpenRequest(preauthAmount, posRefId)
            {
                Config = config
            };

            // act
            var msg = request.ToMessage();

            // assert 
            Assert.Equal(config.PrintMerchantCopy, msg.GetDataBoolValue("print_merchant_copy", false));
            Assert.Equal(config.PromptForCustomerCopyOnEftpos, msg.GetDataBoolValue("prompt_for_customer_copy", false));
            Assert.Equal(config.SignatureFlowOnEftpos, msg.GetDataBoolValue("print_for_signature_required_transactions", false));
        }

        [Fact]
        public void PreAuthOpenRequest_OnValidRequestWithOptions_ReturnRequestObject()
        {
            // arrange
            const int preauthAmount = 1000;
            const string posRefId = "test";
            const string merchantReceiptHeader = "";
            const string merchantReceiptFooter = "merchantfooter";
            const string customerReceiptHeader = "customerheader";
            const string customerReceiptFooter = "";

            var options = new TransactionOptions();
            options.SetMerchantReceiptFooter(merchantReceiptFooter);
            options.SetCustomerReceiptHeader(customerReceiptHeader);

            // act
            var request = new PreauthOpenRequest(preauthAmount, posRefId)
            {
                Options = options
            };
            var msg = request.ToMessage();

            // assert
            Assert.Equal(merchantReceiptHeader, msg.GetDataStringValue("merchant_receipt_header"));
            Assert.Equal(merchantReceiptFooter, msg.GetDataStringValue("merchant_receipt_footer"));
            Assert.Equal(customerReceiptHeader, msg.GetDataStringValue("customer_receipt_header"));
            Assert.Equal(customerReceiptFooter, msg.GetDataStringValue("customer_receipt_footer"));
        }

        [Fact]
        public void PreAuthOpenRequest_OnValidRequestWithNoOptions_ReturnEmptyOptions()
        {
            // arrange
            const int preauthAmount = 1000;
            const string posRefId = "test";

            // act
            var request = new PreauthOpenRequest(preauthAmount, posRefId);
            var msg = request.ToMessage();

            // assert
            Assert.Equal("", msg.GetDataStringValue("merchant_receipt_header"));
            Assert.Equal("", msg.GetDataStringValue("merchant_receipt_footer"));
            Assert.Equal("", msg.GetDataStringValue("customer_receipt_header"));
            Assert.Equal("", msg.GetDataStringValue("customer_receipt_footer"));
        }

        [Fact]
        public void PreautOpenResponse_OnValidRequest_ReturnResponseObject()
        {
            // arrange
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""data"":{""account_type"":""CREDIT"",""auth_code"":""318981"",""bank_date"":""11062019"",""bank_noncash_amount"":1000,""bank_settlement_date"":""11062019"",""bank_time"":""182808"",""card_entry"":""EMV_INSERT"",""currency"":""AUD"",""customer_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\nTIME 11JUN19   18:28\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190611001110\r\nVisa Credit     \r\nVisa(I)           CR\r\nCARD............3952\r\nAID   A0000000031010\r\nTVR       0080048000\r\nAUTH          318981\r\nPRE-AUTH ID 15765372\r\n\r\nPRE-AUTH    AUD10.00\r\n\r\n   (000) APPROVED\r\n\r\n  *CUSTOMER COPY*\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""customer_receipt_printed"":false,""emv_actioncode"":""TC"",""emv_actioncode_values"":""C0A8342DF36207F1"",""emv_pix"":""1010"",""emv_rid"":""A000000003"",""emv_tsi"":""F800"",""emv_tvr"":""0080048000"",""expiry_date"":""1122"",""host_response_code"":""000"",""host_response_text"":""APPROVED"",""informative_text"":""                "",""masked_pan"":""............3952"",""merchant_acquirer"":""EFTPOS FROM BANK SA"",""merchant_addr"":""213 Miller Street"",""merchant_city"":""Sydney"",""merchant_country"":""Australia"",""merchant_id"":""22341842"",""merchant_name"":""Merchant4"",""merchant_postcode"":""2060"",""merchant_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\nTIME 11JUN19   18:28\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190611001110\r\nVisa Credit     \r\nVisa(I)           CR\r\nCARD............3952\r\nAID   A0000000031010\r\nTVR       0080048000\r\nAUTH          318981\r\nPRE-AUTH ID 15765372\r\n\r\nPRE-AUTH    AUD10.00\r\n\r\n   (000) APPROVED\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""merchant_receipt_printed"":false,""online_indicator"":""Y"",""pos_ref_id"":""propen-11-06-2019-18-28-08"",""preauth_amount"":1000,""preauth_id"":""15765372"",""rrn"":""190611001110"",""scheme_app_name"":""Visa Credit"",""scheme_name"":""Visa"",""stan"":""001110"",""success"":true,""terminal_id"":""100612348842"",""terminal_ref_id"":""12348842_11062019182827"",""transaction_type"":""PRE-AUTH""},""datetime"":""2019-06-11T18:28:27.237"",""event"":""preauth_response"",""id"":""prac17""}}";

            // act
            var msg = Message.FromJson(jsonStr, secrets);
            var response = new PreauthResponse(msg);

            // assert
            Assert.Equal("preauth_response", msg.EventName);
            Assert.Equal("15765372", response.PreauthId);
            Assert.Equal("propen-11-06-2019-18-28-08", response.PosRefId);
            Assert.Equal(0, response.GetCompletionAmount());
            Assert.Equal(1000, response.GetBalanceAmount() );
            Assert.Equal(0, response.GetPreviousBalanceAmount());
            Assert.Equal(0, response.GetSurchargeAmount());
            Assert.True(response.Details.Success);
            Assert.Equal("prac17", response.Details.RequestId);
            Assert.Equal("Visa", response.Details.SchemeName );
            Assert.Equal("Visa", response.Details.SchemeAppName);
            Assert.Equal("190611001110", response.Details.GetRRN());
            Assert.NotNull(response.Details.GetCustomerReceipt());
            Assert.NotNull(response.Details.GetMerchantReceipt());
            Assert.Equal("APPROVED", response.Details.GetResponseText());
            Assert.Equal("000", response.Details.GetResponseCode());
            Assert.Equal("EMV_INSERT", response.Details.GetCardEntry());
            Assert.Equal("CREDIT", response.Details.GetAccountType());
            Assert.Equal("318981", response.Details.GetAuthCode());
            Assert.Equal("11062019", response.Details.GetBankDate());
            Assert.Equal("182808", response.Details.GetBankTime());
            Assert.Equal("............3952", response.Details.GetMaskedPan());
            Assert.Equal("100612348842", response.Details.GetTerminalId());
            Assert.Equal("12348842_11062019182827", response.Details.GetTerminalReferenceId());
            Assert.False(response.WasCustomerReceiptPrinted());
            Assert.False(response.WasMerchantReceiptPrinted());
            Assert.Equal(DateTime.ParseExact(msg.GetDataStringValue("bank_settlement_date"), "ddMMyyyy", CultureInfo.InvariantCulture).Date, response.Details.GetSettlementDate());
        }

        [Fact]
        public void PreauthTopupRequest_OnValidRequest_ReturnRequestObject()
        {
            // arrange
            const int topupAmount = 1000;
            const string posRefId = "test";
            const string preauthId = "123456";

            // act
            var request = new PreauthTopupRequest(preauthId, topupAmount, posRefId);
            var msg = request.ToMessage();

            // assert
            Assert.Equal("preauth_topup", msg.EventName);
            Assert.Equal(request.PosRefId, msg.GetDataStringValue("pos_ref_id"));
            Assert.Equal(request.TopupAmount, msg.GetDataIntValue("topup_amount"));
        }

        [Fact]
        public void PreauthTopupRequest_OnValidRequestWithConfig_ReturnRequestObject()
        {
            // arrange
            const int topupAmount = 1000;
            const string posRefId = "test";
            const string preauthId = "123456";

            var config = new SpiConfig
            {
                PrintMerchantCopy = true,
                PromptForCustomerCopyOnEftpos = false,
                SignatureFlowOnEftpos = true
            };

            // act
            var request = new PreauthTopupRequest(preauthId, topupAmount, posRefId)
            {
                Config = config
            };
            var msg = request.ToMessage();

            // assert
            Assert.Equal(config.PrintMerchantCopy, msg.GetDataBoolValue("print_merchant_copy", false));
            Assert.Equal(config.PromptForCustomerCopyOnEftpos, msg.GetDataBoolValue("prompt_for_customer_copy", false));
            Assert.Equal(config.SignatureFlowOnEftpos, msg.GetDataBoolValue("print_for_signature_required_transactions", false));
        }

        [Fact]
        public void PreAuthTopupRequest_OnValidRequestWithOptions_ReturnRequestObject()
        {
            // arrange
            const int topupAmount = 1000;
            const string posRefId = "test";
            const string preauthId = "123456";
            const string merchantReceiptHeader = "";
            const string merchantReceiptFooter = "merchantfooter";
            const string customerReceiptHeader = "customerheader";
            const string customerReceiptFooter = "";

            var options = new TransactionOptions();
            options.SetMerchantReceiptFooter(merchantReceiptFooter);
            options.SetCustomerReceiptHeader(customerReceiptHeader);

            // act
            var request = new PreauthTopupRequest(preauthId, topupAmount, posRefId)
            {
                Options = options
            };
            var msg = request.ToMessage();

            // assert
            Assert.Equal(merchantReceiptHeader, msg.GetDataStringValue("merchant_receipt_header"));
            Assert.Equal(merchantReceiptFooter, msg.GetDataStringValue("merchant_receipt_footer"));
            Assert.Equal(customerReceiptHeader, msg.GetDataStringValue("customer_receipt_header"));
            Assert.Equal(customerReceiptFooter, msg.GetDataStringValue("customer_receipt_footer"));
        }

        [Fact]
        public void PreAuthTopupRequest_OnValidRequestWithNoOptions_ReturnEmptyOptions()
        {
            // arrange
            const int topupAmount = 1000;
            const string posRefId = "test";
            const string preauthId = "123456";

            var options = new TransactionOptions();
            options.SetMerchantReceiptFooter("");
            options.SetMerchantReceiptHeader("");
            options.SetCustomerReceiptHeader("");
            options.SetCustomerReceiptFooter("");

            // act
            var request = new PreauthTopupRequest(preauthId, topupAmount, posRefId);
            var msg = request.ToMessage();

            // assert
            Assert.Equal("", msg.GetDataStringValue("merchant_receipt_header"));
            Assert.Equal("", msg.GetDataStringValue("merchant_receipt_footer"));
            Assert.Equal("", msg.GetDataStringValue("customer_receipt_header"));
            Assert.Equal("", msg.GetDataStringValue("customer_receipt_footer"));
        }

        [Fact]
        public void TestPreauthTopupResponse()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""data"":{""account_type"":""CREDIT"",""auth_code"":""318981"",""balance_amount"":1500,""bank_date"":""11062019"",""bank_settlement_date"":""11062019"",""bank_time"":""182852"",""card_entry"":""MANUAL"",""currency"":""AUD"",""customer_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\nTIME 11JUN19   18:28\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190611001111\r\nVisa(M)           CR\r\nCARD............3952\r\nAUTH          318981\r\nPRE-AUTH ID 15765372\r\n\r\nPRE-AUTH    AUD10.00\r\nTOP-UP       AUD5.00\r\nBALANCE     AUD15.00\r\n\r\n   (000) APPROVED\r\n\r\n  *CUSTOMER COPY*\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""customer_receipt_printed"":false,""existing_preauth_amount"":1000,""host_response_code"":""000"",""host_response_text"":""APPROVED"",""informative_text"":""                "",""masked_pan"":""............3952"",""merchant_acquirer"":""EFTPOS FROM BANK SA"",""merchant_addr"":""213 Miller Street"",""merchant_city"":""Sydney"",""merchant_country"":""Australia"",""merchant_id"":""22341842"",""merchant_name"":""Merchant4"",""merchant_postcode"":""2060"",""merchant_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\nTIME 11JUN19   18:28\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190611001111\r\nVisa(M)           CR\r\nCARD............3952\r\nAUTH          318981\r\nPRE-AUTH ID 15765372\r\n\r\nPRE-AUTH    AUD10.00\r\nTOP-UP       AUD5.00\r\nBALANCE     AUD15.00\r\n\r\n   (000) APPROVED\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""merchant_receipt_printed"":false,""online_indicator"":""Y"",""pos_ref_id"":""prtopup-15765372-11-06-2019-18-28-50"",""preauth_id"":""15765372"",""rrn"":""190611001111"",""scheme_name"":""Visa"",""stan"":""001111"",""success"":true,""terminal_id"":""100612348842"",""terminal_ref_id"":""12348842_11062019182857"",""topup_amount"":500,""transaction_type"":""TOPUP""},""datetime"":""2019-06-11T18:28:57.154"",""event"":""preauth_topup_response"",""id"":""prtu21""}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            PreauthResponse response = new PreauthResponse(msg);

            Assert.Equal(msg.EventName, "preauth_topup_response");
            Assert.Equal(response.PreauthId, "15765372");
            Assert.Equal(response.PosRefId, "prtopup-15765372-11-06-2019-18-28-50");
            Assert.Equal(response.GetCompletionAmount(), 0);
            Assert.Equal(response.GetBalanceAmount(), 1500);
            Assert.Equal(response.GetPreviousBalanceAmount(), 1000);
            Assert.Equal(response.GetSurchargeAmount(), 0);
            Assert.True(response.Details.Success);
            Assert.Equal(response.Details.RequestId, "prtu21");
            Assert.Equal(response.Details.SchemeName, "Visa");
            Assert.Equal(response.Details.SchemeAppName, "Visa");
            Assert.Equal(response.Details.GetRRN(), "190611001111");
            Assert.NotNull(response.Details.GetCustomerReceipt());
            Assert.NotNull(response.Details.GetMerchantReceipt());
            Assert.Equal(response.Details.GetResponseText(), "APPROVED");
            Assert.Equal(response.Details.GetResponseCode(), "000");
            Assert.Equal(response.Details.GetCardEntry(), "MANUAL");
            Assert.Equal(response.Details.GetAccountType(), "CREDIT");
            Assert.Equal(response.Details.GetAuthCode(), "318981");
            Assert.Equal(response.Details.GetBankDate(), "11062019");
            Assert.Equal(response.Details.GetBankTime(), "182852");
            Assert.Equal(response.Details.GetMaskedPan(), "............3952");
            Assert.Equal(response.Details.GetTerminalId(), "100612348842");
            Assert.Equal(response.Details.GetTerminalReferenceId(), "12348842_11062019182857");
            Assert.False(response.WasCustomerReceiptPrinted());
            Assert.False(response.WasMerchantReceiptPrinted());
            Assert.Equal(response.Details.GetSettlementDate(), DateTime.ParseExact(msg.GetDataStringValue("bank_settlement_date"), "ddMMyyyy", CultureInfo.InvariantCulture).Date);
        }

        [Fact]
        public void TestPreauthPartialCancellationRequest()
        {
            int partialCancellationAmount = 1000;
            string posRefId = "test";
            string preauthId = "123456";

            PreauthPartialCancellationRequest request = new PreauthPartialCancellationRequest(preauthId, partialCancellationAmount, posRefId);
            Message msg = request.ToMessage();

            Assert.Equal(msg.EventName, "preauth_partial_cancellation");
            Assert.Equal(request.PosRefId, msg.GetDataStringValue("pos_ref_id"));
            Assert.Equal(request.PartialCancellationAmount, msg.GetDataIntValue("preauth_cancel_amount"));
        }

        [Fact]
        public void TestPreauthPartialCancellationRequestWithConfig()
        {
            int partialCancellationAmount = 1000;
            string posRefId = "test";
            string preauthId = "123456";

            SpiConfig config = new SpiConfig();
            config.PrintMerchantCopy = true;
            config.PromptForCustomerCopyOnEftpos = false;
            config.SignatureFlowOnEftpos = true;

            PreauthPartialCancellationRequest request = new PreauthPartialCancellationRequest(preauthId, partialCancellationAmount, posRefId);
            request.Config = config;

            Message msg = request.ToMessage();

            Assert.Equal(config.PrintMerchantCopy, msg.GetDataBoolValue("print_merchant_copy", false));
            Assert.Equal(config.PromptForCustomerCopyOnEftpos, msg.GetDataBoolValue("prompt_for_customer_copy", false));
            Assert.Equal(config.SignatureFlowOnEftpos, msg.GetDataBoolValue("print_for_signature_required_transactions", false));
        }

        [Fact]
        public void TestPreauthPartialCancellationRequestWithOptions()
        {
            int partialCancellationAmount = 1000;
            string posRefId = "test";
            string preauthId = "123456";
            string merchantReceiptHeader = "";
            string merchantReceiptFooter = "merchantfooter";
            string customerReceiptHeader = "customerheader";
            string customerReceiptFooter = "";

            TransactionOptions options = new TransactionOptions();
            options.SetMerchantReceiptFooter(merchantReceiptFooter);
            options.SetCustomerReceiptHeader(customerReceiptHeader);

            PreauthPartialCancellationRequest request = new PreauthPartialCancellationRequest(preauthId, partialCancellationAmount, posRefId);
            request.Options = options;
            Message msg = request.ToMessage();

            Assert.Equal(merchantReceiptHeader, msg.GetDataStringValue("merchant_receipt_header"));
            Assert.Equal(merchantReceiptFooter, msg.GetDataStringValue("merchant_receipt_footer"));
            Assert.Equal(customerReceiptHeader, msg.GetDataStringValue("customer_receipt_header"));
            Assert.Equal(customerReceiptFooter, msg.GetDataStringValue("customer_receipt_footer"));
        }

        [Fact]
        public void TestPreauthPartialCancellationRequestWithOptions_None()
        {
            int partialCancellationAmount = 1000;
            string posRefId = "test";
            string preauthId = "123456";

            PreauthPartialCancellationRequest request = new PreauthPartialCancellationRequest(preauthId, partialCancellationAmount, posRefId);
            Message msg = request.ToMessage();

            Assert.Equal("", msg.GetDataStringValue("merchant_receipt_header"));
            Assert.Equal("", msg.GetDataStringValue("merchant_receipt_footer"));
            Assert.Equal("", msg.GetDataStringValue("customer_receipt_header"));
            Assert.Equal("", msg.GetDataStringValue("customer_receipt_footer"));
        }

        [Fact]
        public void TestPreauthPartialCancellationResponse()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""data"":{""account_type"":""CREDIT"",""balance_amount"":1000,""bank_date"":""11062019"",""bank_settlement_date"":""11062019"",""bank_time"":""182926"",""card_entry"":""MANUAL"",""currency"":""AUD"",""customer_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\n*--PARTIAL CANCEL--*\r\n\r\nTIME 11JUN19   18:29\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190611001112\r\nVisa(M)           CR\r\nCARD............3952\r\nPRE-AUTH ID 15765372\r\n\r\nPRE-AUTH    AUD15.00\r\nCANCEL       AUD5.00\r\nBALANCE     AUD10.00\r\n\r\n   (000) APPROVED\r\n\r\n*--PARTIAL CANCEL--*\r\n\r\n  *CUSTOMER COPY*\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""customer_receipt_printed"":false,""existing_preauth_amount"":1500,""host_response_code"":""000"",""host_response_text"":""APPROVED"",""informative_text"":""                "",""masked_pan"":""............3952"",""merchant_acquirer"":""EFTPOS FROM BANK SA"",""merchant_addr"":""213 Miller Street"",""merchant_city"":""Sydney"",""merchant_country"":""Australia"",""merchant_id"":""22341842"",""merchant_name"":""Merchant4"",""merchant_postcode"":""2060"",""merchant_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\n*--PARTIAL CANCEL--*\r\n\r\nTIME 11JUN19   18:29\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190611001112\r\nVisa(M)           CR\r\nCARD............3952\r\nPRE-AUTH ID 15765372\r\n\r\nPRE-AUTH    AUD15.00\r\nCANCEL       AUD5.00\r\nBALANCE     AUD10.00\r\n\r\n   (000) APPROVED\r\n\r\n*--PARTIAL CANCEL--*\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""merchant_receipt_printed"":false,""online_indicator"":""Y"",""pos_ref_id"":""prtopd-15765372-11-06-2019-18-29-22"",""preauth_cancel_amount"":500,""preauth_id"":""15765372"",""rrn"":""190611001112"",""scheme_name"":""Visa"",""stan"":""001112"",""success"":true,""terminal_id"":""100612348842"",""terminal_ref_id"":""12348842_11062019182927"",""transaction_type"":""CANCEL""},""datetime"":""2019-06-11T18:29:27.258"",""event"":""preauth_partial_cancellation_response"",""id"":""prpc24""}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            PreauthResponse response = new PreauthResponse(msg);

            Assert.Equal(msg.EventName, "preauth_partial_cancellation_response");
            Assert.Equal(response.PreauthId, "15765372");
            Assert.Equal(response.PosRefId, "prtopd-15765372-11-06-2019-18-29-22");
            Assert.Equal(response.GetCompletionAmount(), 0);
            Assert.Equal(response.GetBalanceAmount(), 1000);
            Assert.Equal(response.GetPreviousBalanceAmount(), 1500);
            Assert.Equal(response.GetSurchargeAmount(), 0);
            Assert.True(response.Details.Success);
            Assert.Equal(response.Details.RequestId, "prpc24");
            Assert.Equal(response.Details.SchemeName, "Visa");
            Assert.Equal(response.Details.SchemeAppName, "Visa");
            Assert.Equal(response.Details.GetRRN(), "190611001112");
            Assert.NotNull(response.Details.GetCustomerReceipt());
            Assert.NotNull(response.Details.GetMerchantReceipt());
            Assert.Equal(response.Details.GetResponseText(), "APPROVED");
            Assert.Equal(response.Details.GetResponseCode(), "000");
            Assert.Equal(response.Details.GetCardEntry(), "MANUAL");
            Assert.Equal(response.Details.GetAccountType(), "CREDIT");
            Assert.Equal(response.Details.GetBankDate(), "11062019");
            Assert.Equal(response.Details.GetBankTime(), "182926");
            Assert.Equal(response.Details.GetMaskedPan(), "............3952");
            Assert.Equal(response.Details.GetTerminalId(), "100612348842");
            Assert.Equal(response.Details.GetTerminalReferenceId(), "12348842_11062019182927");
            Assert.False(response.WasCustomerReceiptPrinted());
            Assert.False(response.WasMerchantReceiptPrinted());
            Assert.Equal(response.Details.GetSettlementDate(), DateTime.ParseExact(msg.GetDataStringValue("bank_settlement_date"), "ddMMyyyy", CultureInfo.InvariantCulture).Date);
        }

        [Fact]
        public void TestPreauthExtendRequest()
        {
            string posRefId = "test";
            string preauthId = "123456";

            PreauthExtendRequest request = new PreauthExtendRequest(preauthId, posRefId);
            Message msg = request.ToMessage();

            Assert.Equal(msg.EventName, "preauth_extend");
            Assert.Equal(request.PosRefId, msg.GetDataStringValue("pos_ref_id"));
            Assert.Equal(request.PreauthId, msg.GetDataStringValue("preauth_id"));
        }

        [Fact]
        public void TestPreauthExtendRequestWithConfig()
        {
            string posRefId = "test";
            string preauthId = "123456";

            SpiConfig config = new SpiConfig();
            config.PrintMerchantCopy = true;
            config.PromptForCustomerCopyOnEftpos = false;
            config.SignatureFlowOnEftpos = true;

            PreauthExtendRequest request = new PreauthExtendRequest(preauthId, posRefId);
            request.Config = config;

            Message msg = request.ToMessage();

            Assert.Equal(config.PrintMerchantCopy, msg.GetDataBoolValue("print_merchant_copy", false));
            Assert.Equal(config.PromptForCustomerCopyOnEftpos, msg.GetDataBoolValue("prompt_for_customer_copy", false));
            Assert.Equal(config.SignatureFlowOnEftpos, msg.GetDataBoolValue("print_for_signature_required_transactions", false));
        }

        [Fact]
        public void TestPreauthExtendRequestWithOptions()
        {
            string posRefId = "test";
            string preauthId = "123456";
            string merchantReceiptHeader = "";
            string merchantReceiptFooter = "merchantfooter";
            string customerReceiptHeader = "customerheader";
            string customerReceiptFooter = "";

            TransactionOptions options = new TransactionOptions();
            options.SetMerchantReceiptFooter(merchantReceiptFooter);
            options.SetCustomerReceiptHeader(customerReceiptHeader);

            PreauthExtendRequest request = new PreauthExtendRequest(preauthId, posRefId);
            request.Options = options;
            Message msg = request.ToMessage();

            Assert.Equal(merchantReceiptHeader, msg.GetDataStringValue("merchant_receipt_header"));
            Assert.Equal(merchantReceiptFooter, msg.GetDataStringValue("merchant_receipt_footer"));
            Assert.Equal(customerReceiptHeader, msg.GetDataStringValue("customer_receipt_header"));
            Assert.Equal(customerReceiptFooter, msg.GetDataStringValue("customer_receipt_footer"));
        }

        [Fact]
        public void TestPreauthExtendRequestWithOptions_None()
        {
            string posRefId = "test";
            string preauthId = "123456";

            PreauthExtendRequest request = new PreauthExtendRequest(preauthId, posRefId);
            Message msg = request.ToMessage();

            Assert.Equal("", msg.GetDataStringValue("merchant_receipt_header"));
            Assert.Equal("", msg.GetDataStringValue("merchant_receipt_footer"));
            Assert.Equal("", msg.GetDataStringValue("customer_receipt_header"));
            Assert.Equal("", msg.GetDataStringValue("customer_receipt_footer"));
        }

        [Fact]
        public void TestPreauthExtendResponse()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""data"":{""account_type"":""CREDIT"",""auth_code"":""793647"",""balance_amount"":1000,""bank_date"":""11062019"",""bank_settlement_date"":""11062019"",""bank_time"":""182942"",""card_entry"":""MANUAL"",""currency"":""AUD"",""customer_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\nTIME 11JUN19   18:29\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190611001113\r\nVisa(M)           CR\r\nCARD............3952\r\nAUTH          793647\r\nPRE-AUTH ID 15765372\r\n\r\nPRE-AUTH    AUD10.00\r\nTOP-UP       AUD5.00\r\nCANCEL       AUD5.00\r\nBALANCE     AUD10.00\r\nPRE-AUTH EXT AUD0.00\r\n\r\n   (000) APPROVED\r\n\r\n  *CUSTOMER COPY*\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""customer_receipt_printed"":false,""existing_preauth_amount"":1000,""host_response_code"":""000"",""host_response_text"":""APPROVED"",""informative_text"":""                "",""masked_pan"":""............3952"",""merchant_acquirer"":""EFTPOS FROM BANK SA"",""merchant_addr"":""213 Miller Street"",""merchant_city"":""Sydney"",""merchant_country"":""Australia"",""merchant_id"":""22341842"",""merchant_name"":""Merchant4"",""merchant_postcode"":""2060"",""merchant_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\nTIME 11JUN19   18:29\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190611001113\r\nVisa(M)           CR\r\nCARD............3952\r\nAUTH          793647\r\nPRE-AUTH ID 15765372\r\n\r\nPRE-AUTH    AUD10.00\r\nTOP-UP       AUD5.00\r\nCANCEL       AUD5.00\r\nBALANCE     AUD10.00\r\nPRE-AUTH EXT AUD0.00\r\n\r\n   (000) APPROVED\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""merchant_receipt_printed"":false,""online_indicator"":""Y"",""pos_ref_id"":""prtopd-15765372-11-06-2019-18-29-39"",""preauth_cancel_amount"":500,""preauth_id"":""15765372"",""rrn"":""190611001113"",""scheme_name"":""Visa"",""stan"":""001113"",""success"":true,""terminal_id"":""100612348842"",""terminal_ref_id"":""12348842_11062019182946"",""topup_amount"":500,""transaction_type"":""PRE-AUTH EXT""},""datetime"":""2019-06-11T18:29:46.234"",""event"":""preauth_extend_response"",""id"":""prext26""}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            PreauthResponse response = new PreauthResponse(msg);

            Assert.Equal(msg.EventName, "preauth_extend_response");
            Assert.Equal(response.PreauthId, "15765372");
            Assert.Equal(response.PosRefId, "prtopd-15765372-11-06-2019-18-29-39");
            Assert.Equal(response.GetCompletionAmount(), 0);
            Assert.Equal(response.GetBalanceAmount(), 1000);
            Assert.Equal(response.GetPreviousBalanceAmount(), 1000);
            Assert.Equal(response.GetSurchargeAmount(), 0);
            Assert.True(response.Details.Success);
            Assert.Equal(response.Details.GetAuthCode(), "793647");
            Assert.Equal(response.Details.RequestId, "prext26");
            Assert.Equal(response.Details.SchemeName, "Visa");
            Assert.Equal(response.Details.SchemeAppName, "Visa");
            Assert.Equal(response.Details.GetRRN(), "190611001113");
            Assert.NotNull(response.Details.GetCustomerReceipt());
            Assert.NotNull(response.Details.GetMerchantReceipt());
            Assert.Equal(response.Details.GetResponseText(), "APPROVED");
            Assert.Equal(response.Details.GetResponseCode(), "000");
            Assert.Equal(response.Details.GetCardEntry(), "MANUAL");
            Assert.Equal(response.Details.GetAccountType(), "CREDIT");
            Assert.Equal(response.Details.GetBankDate(), "11062019");
            Assert.Equal(response.Details.GetBankTime(), "182942");
            Assert.Equal(response.Details.GetMaskedPan(), "............3952");
            Assert.Equal(response.Details.GetTerminalId(), "100612348842");
            Assert.Equal(response.Details.GetTerminalReferenceId(), "12348842_11062019182946");
            Assert.False(response.WasCustomerReceiptPrinted());
            Assert.False(response.WasMerchantReceiptPrinted());
            Assert.Equal(response.Details.GetSettlementDate(), DateTime.ParseExact(msg.GetDataStringValue("bank_settlement_date"), "ddMMyyyy", CultureInfo.InvariantCulture).Date);
        }

        [Fact]
        public void TestPreauthCancelRequest()
        {
            string posRefId = "test";
            string preauthId = "123456";

            PreauthCancelRequest request = new PreauthCancelRequest(preauthId, posRefId);
            Message msg = request.ToMessage();

            Assert.Equal(msg.EventName, "preauth_cancellation");
            Assert.Equal(request.PosRefId, msg.GetDataStringValue("pos_ref_id"));
            Assert.Equal(request.PreauthId, msg.GetDataStringValue("preauth_id"));
        }

        [Fact]
        public void TestPreauthCancelRequestWithConfig()
        {
            string posRefId = "test";
            string preauthId = "123456";

            SpiConfig config = new SpiConfig();
            config.PrintMerchantCopy = true;
            config.PromptForCustomerCopyOnEftpos = false;
            config.SignatureFlowOnEftpos = true;

            PreauthCancelRequest request = new PreauthCancelRequest(preauthId, posRefId);
            request.Config = config;

            Message msg = request.ToMessage();

            Assert.Equal(config.PrintMerchantCopy, msg.GetDataBoolValue("print_merchant_copy", false));
            Assert.Equal(config.PromptForCustomerCopyOnEftpos, msg.GetDataBoolValue("prompt_for_customer_copy", false));
            Assert.Equal(config.SignatureFlowOnEftpos, msg.GetDataBoolValue("print_for_signature_required_transactions", false));
        }

        [Fact]
        public void TestPreauthCancelRequestWithOptions()
        {
            string posRefId = "test";
            string preauthId = "123456";
            string merchantReceiptHeader = "";
            string merchantReceiptFooter = "merchantfooter";
            string customerReceiptHeader = "customerheader";
            string customerReceiptFooter = "";

            TransactionOptions options = new TransactionOptions();
            options.SetMerchantReceiptFooter(merchantReceiptFooter);
            options.SetCustomerReceiptHeader(customerReceiptHeader);

            PreauthCancelRequest request = new PreauthCancelRequest(preauthId, posRefId);
            request.Options = options;
            Message msg = request.ToMessage();

            Assert.Equal(merchantReceiptHeader, msg.GetDataStringValue("merchant_receipt_header"));
            Assert.Equal(merchantReceiptFooter, msg.GetDataStringValue("merchant_receipt_footer"));
            Assert.Equal(customerReceiptHeader, msg.GetDataStringValue("customer_receipt_header"));
            Assert.Equal(customerReceiptFooter, msg.GetDataStringValue("customer_receipt_footer"));
        }

        [Fact]
        public void TestPreauthCancelRequestWithOptions_None()
        {
            string posRefId = "test";
            string preauthId = "123456";

            PreauthCancelRequest request = new PreauthCancelRequest(preauthId, posRefId);
            Message msg = request.ToMessage();

            Assert.Equal("", msg.GetDataStringValue("merchant_receipt_header"));
            Assert.Equal("", msg.GetDataStringValue("merchant_receipt_footer"));
            Assert.Equal("", msg.GetDataStringValue("customer_receipt_header"));
            Assert.Equal("", msg.GetDataStringValue("customer_receipt_footer"));
        }

        [Fact]
        public void TestPreauthCancelResponse()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""data"":{""account_type"":""NOT-SET"",""bank_date"":""11062019"",""bank_settlement_date"":""11062019"",""bank_time"":""183041"",""card_entry"":""NOT-SET"",""error_detail"":""Pre Auth ID 15765372 has already been completed"",""error_reason"":""PRE_AUTH_ID_ALREADY_COMPLETED"",""host_response_text"":""DECLINED"",""pos_ref_id"":""prtopd-15765372-11-06-2019-18-30-40"",""preauth_amount"":0,""preauth_id"":""15765372"",""scheme_name"":""Visa"",""stan"":""001114"",""success"":false,""terminal_ref_id"":""12348842_11062019183043"",""transaction_type"":""PRE-AUTH CANCEL""},""datetime"":""2019-06-11T18:30:43.104"",""event"":""preauth_cancellation_response"",""id"":""prac31""}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            PreauthResponse response = new PreauthResponse(msg);

            Assert.Equal(msg.EventName, "preauth_cancellation_response");
            Assert.Equal(response.PreauthId, "15765372");
            Assert.Equal(response.PosRefId, "prtopd-15765372-11-06-2019-18-30-40");
            Assert.Equal(response.GetCompletionAmount(), 0);
            Assert.Equal(response.GetBalanceAmount(), 0);
            Assert.Equal(response.GetPreviousBalanceAmount(), 0);
            Assert.Equal(response.GetSurchargeAmount(), 0);
            Assert.False(response.Details.Success);
            Assert.Equal(response.Details.RequestId, "prac31");
            Assert.Equal(response.Details.SchemeName, "Visa");
            Assert.Equal(response.Details.GetResponseText(), "DECLINED");
            Assert.Equal(response.Details.GetCardEntry(), "NOT-SET");
            Assert.Equal(response.Details.GetAccountType(), "NOT-SET");
            Assert.Equal(response.Details.GetBankDate(), "11062019");
            Assert.Equal(response.Details.GetBankTime(), "183041");
            Assert.Equal(response.Details.GetTerminalReferenceId(), "12348842_11062019183043");
            Assert.False(response.WasCustomerReceiptPrinted());
            Assert.False(response.WasMerchantReceiptPrinted());
            Assert.Equal(response.Details.GetSettlementDate(), DateTime.ParseExact(msg.GetDataStringValue("bank_settlement_date"), "ddMMyyyy", CultureInfo.InvariantCulture).Date);
        }

        [Fact]
        public void TestPreauthCompletionRequest()
        {
            int completionAmount = 1000;
            int surchargeAmount = 1000;
            string posRefId = "test";
            string preauthId = "123456";

            PreauthCompletionRequest request = new PreauthCompletionRequest(preauthId, completionAmount, posRefId);
            request.SurchargeAmount = surchargeAmount;
            Message msg = request.ToMessage();

            Assert.Equal(msg.EventName, "completion");
            Assert.Equal(request.PosRefId, msg.GetDataStringValue("pos_ref_id"));
            Assert.Equal(request.PreauthId, msg.GetDataStringValue("preauth_id"));
            Assert.Equal(request.CompletionAmount, msg.GetDataIntValue("completion_amount"));
            Assert.Equal(request.SurchargeAmount, msg.GetDataIntValue("surcharge_amount"));
        }

        [Fact]
        public void TestPreauthCompletionRequestWithConfig()
        {
            int completionAmount = 1000;
            string posRefId = "test";
            string preauthId = "123456";

            SpiConfig config = new SpiConfig();
            config.PrintMerchantCopy = true;
            config.PromptForCustomerCopyOnEftpos = false;
            config.SignatureFlowOnEftpos = true;

            PreauthCompletionRequest request = new PreauthCompletionRequest(preauthId, completionAmount, posRefId);
            request.Config = config;

            Message msg = request.ToMessage();

            Assert.Equal(config.PrintMerchantCopy, msg.GetDataBoolValue("print_merchant_copy", false));
            Assert.Equal(config.PromptForCustomerCopyOnEftpos, msg.GetDataBoolValue("prompt_for_customer_copy", false));
            Assert.Equal(config.SignatureFlowOnEftpos, msg.GetDataBoolValue("print_for_signature_required_transactions", false));
        }

        [Fact]
        public void TestPreauthCompletionRequestWithOptions()
        {
            int completionAmount = 1000;
            string posRefId = "test";
            string preauthId = "123456";
            string merchantReceiptHeader = "";
            string merchantReceiptFooter = "merchantfooter";
            string customerReceiptHeader = "customerheader";
            string customerReceiptFooter = "";

            TransactionOptions options = new TransactionOptions();
            options.SetMerchantReceiptFooter(merchantReceiptFooter);
            options.SetCustomerReceiptHeader(customerReceiptHeader);

            PreauthCompletionRequest request = new PreauthCompletionRequest(preauthId, completionAmount, posRefId);
            request.Options = options;
            Message msg = request.ToMessage();

            Assert.Equal(merchantReceiptHeader, msg.GetDataStringValue("merchant_receipt_header"));
            Assert.Equal(merchantReceiptFooter, msg.GetDataStringValue("merchant_receipt_footer"));
            Assert.Equal(customerReceiptHeader, msg.GetDataStringValue("customer_receipt_header"));
            Assert.Equal(customerReceiptFooter, msg.GetDataStringValue("customer_receipt_footer"));
        }

        [Fact]
        public void TestPreauthCompletionRequestWithOptions_None()
        {
            int completionAmount = 1000;
            string posRefId = "test";
            string preauthId = "123456";

            PreauthCompletionRequest request = new PreauthCompletionRequest(preauthId, completionAmount, posRefId);
            Message msg = request.ToMessage();

            Assert.Equal("", msg.GetDataStringValue("merchant_receipt_header"));
            Assert.Equal("", msg.GetDataStringValue("merchant_receipt_footer"));
            Assert.Equal("", msg.GetDataStringValue("customer_receipt_header"));
            Assert.Equal("", msg.GetDataStringValue("customer_receipt_footer"));
        }

        [Fact]
        public void TestPreauthCompletionResponse()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""data"":{""account_type"":""CREDIT"",""bank_date"":""11062019"",""bank_noncash_amount"":900,""bank_settlement_date"":""11062019"",""bank_time"":""183025"",""card_entry"":""MANUAL"",""completion_amount"":800,""currency"":""AUD"",""customer_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\nTIME 11JUN19   18:30\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190611001114\r\nVisa(M)           CR\r\nCARD............3952\r\nPRE-AUTH ID 15765372\r\n\r\nPCOMP        AUD8.00\r\nSURCHARGE    AUD1.00\r\nTOTAL        AUD9.00\r\n\r\n   (000) APPROVED\r\n\r\n  *CUSTOMER COPY*\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""customer_receipt_printed"":false,""host_response_code"":""000"",""host_response_text"":""APPROVED"",""informative_text"":""                "",""masked_pan"":""............3952"",""merchant_acquirer"":""EFTPOS FROM BANK SA"",""merchant_addr"":""213 Miller Street"",""merchant_city"":""Sydney"",""merchant_country"":""Australia"",""merchant_id"":""22341842"",""merchant_name"":""Merchant4"",""merchant_postcode"":""2060"",""merchant_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\nTIME 11JUN19   18:30\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190611001114\r\nVisa(M)           CR\r\nCARD............3952\r\nPRE-AUTH ID 15765372\r\n\r\nPCOMP        AUD8.00\r\nSURCHARGE    AUD1.00\r\nTOTAL        AUD9.00\r\n\r\n   (000) APPROVED\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""merchant_receipt_printed"":false,""online_indicator"":""Y"",""pos_ref_id"":""prcomp-15765372-11-06-2019-18-30-16"",""preauth_cancel_amount"":500,""preauth_id"":""15765372"",""rrn"":""190611001114"",""scheme_name"":""Visa"",""stan"":""001114"",""success"":true,""surcharge_amount"":100,""terminal_id"":""100612348842"",""terminal_ref_id"":""12348842_11062019183026"",""topup_amount"":500,""transaction_type"":""PCOMP""},""datetime"":""2019-06-11T18:30:26.613"",""event"":""completion_response"",""id"":""prac29""}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            PreauthResponse response = new PreauthResponse(msg);

            Assert.Equal(msg.EventName, "completion_response");
            Assert.Equal(response.PreauthId, "15765372");
            Assert.Equal(response.PosRefId, "prcomp-15765372-11-06-2019-18-30-16");
            Assert.Equal(response.GetCompletionAmount(), 800);
            Assert.Equal(response.GetBalanceAmount(), 0);
            Assert.Equal(response.GetPreviousBalanceAmount(), 800);
            Assert.Equal(response.GetSurchargeAmount(), 100);
            Assert.True(response.Details.Success);
            Assert.Equal(response.Details.GetBankNonCashAmount(), 900);
            Assert.Equal(response.Details.RequestId, "prac29");
            Assert.Equal(response.Details.SchemeName, "Visa");
            Assert.Equal(response.Details.SchemeAppName, "Visa");
            Assert.Equal(response.Details.GetRRN(), "190611001114");
            Assert.NotNull(response.Details.GetCustomerReceipt());
            Assert.NotNull(response.Details.GetMerchantReceipt());
            Assert.Equal(response.Details.GetResponseText(), "APPROVED");
            Assert.Equal(response.Details.GetResponseCode(), "000");
            Assert.Equal(response.Details.GetCardEntry(), "MANUAL");
            Assert.Equal(response.Details.GetAccountType(), "CREDIT");
            Assert.Equal(response.Details.GetBankDate(), "11062019");
            Assert.Equal(response.Details.GetBankTime(), "183025");
            Assert.Equal(response.Details.GetMaskedPan(), "............3952");
            Assert.Equal(response.Details.GetTerminalId(), "100612348842");
            Assert.Equal(response.Details.GetTerminalReferenceId(), "12348842_11062019183026");
            Assert.False(response.WasCustomerReceiptPrinted());
            Assert.False(response.WasMerchantReceiptPrinted());
            Assert.Equal(response.Details.GetSettlementDate(), DateTime.ParseExact(msg.GetDataStringValue("bank_settlement_date"), "ddMMyyyy", CultureInfo.InvariantCulture).Date);
        }
    }
}
