using SPIClient;
using Xunit;

namespace Test
{
    public class CashoutTest
    {
        [Fact]
        // todo: Refactor into multiple unit tests
        public void CashoutOnlyRequest_OnValidRequestWithTransactionsOptions_ReturnObjects()
        {
            // arrange
            const string posRefId = "123";
            const int amountCents = 1000;
            const string receiptHeader = "Receipt Header";
            const string receiptFooter = "Receipt Footer";

            var config = new SpiConfig
            {
                PrintMerchantCopy = true,
                PromptForCustomerCopyOnEftpos = false,
                SignatureFlowOnEftpos = true
            };

            var options = new TransactionOptions();
            options.SetCustomerReceiptHeader(receiptHeader); // todo: any method in arrange should be unit tested
            options.SetCustomerReceiptFooter(receiptFooter);
            options.SetMerchantReceiptHeader(receiptHeader);
            options.SetMerchantReceiptFooter(receiptFooter);

            // act
            var request = new CashoutOnlyRequest(amountCents, posRefId)
            {
                SurchargeAmount = 100,
                Config = config,
                Options = options
            };
            var msg = request.ToMessage();

            // assert
            Assert.Equal(posRefId, request.PosRefId);
            Assert.Equal(amountCents, request.CashoutAmount);
            Assert.Equal(config.PrintMerchantCopy, msg.GetDataBoolValue("print_merchant_copy", false));
            Assert.Equal(config.PromptForCustomerCopyOnEftpos, msg.GetDataBoolValue("prompt_for_customer_copy", false));
            Assert.Equal(config.SignatureFlowOnEftpos, msg.GetDataBoolValue("print_for_signature_required_transactions", false));
            Assert.Equal(receiptHeader, msg.GetDataStringValue("customer_receipt_header"));
            Assert.Equal(receiptFooter, msg.GetDataStringValue("customer_receipt_footer"));
            Assert.Equal(receiptHeader, msg.GetDataStringValue("merchant_receipt_header"));
            Assert.Equal(receiptFooter, msg.GetDataStringValue("merchant_receipt_footer"));
        }

        [Fact]
        public void CashoutOnlyResponse_OnValidResponse_ReturnObjects()
        {
            // arrange
            const string jsonStr = @"{""message"": {""data"":{""account_type"":""SAVINGS"",""auth_code"":""265035"",""bank_cash_amount"":1200,""bank_date"":""17062018"",""bank_settlement_date"":""18062018"",""bank_time"":""170950"",""card_entry"":""EMV_INSERT"",""cash_amount"":1200,""currency"":""AUD"",""customer_receipt"":""EFTPOS FROM WESTPAC\\r\\nMerchant4\\r\\n213 Miller Street\\r\\nSydney 2060\\r\\nAustralia\\r\\n\\r\\nTIME 17JUN18   17:09\\r\\nMID         22341845\\r\\nTSP     100312348845\\r\\nRRN     180617000151\\r\\nDebit(I)         SAV\\r\\nCARD............2797\\r\\nAUTH          265035\\r\\n\\r\\nCASH        AUD10.00\\r\\nSURCHARGE    AUD2.00\\r\\nTOTAL       AUD12.00\\r\\n\\r\\n   (000) APPROVED\\r\\n\\r\\n  *CUSTOMER COPY*\\r\\n\\r\\n\\r\\n\\r\\n\\r\\n\\r\\n\\r\\n\\r\\n"",""customer_receipt_printed"":true,""expiry_date"":""0722"",""host_response_code"":""000"",""host_response_text"":""APPROVED"",""informative_text"":""                "",""masked_pan"":""............2797"",""merchant_acquirer"":""EFTPOS FROM WESTPAC"",""merchant_addr"":""213 Miller Street"",""merchant_city"":""Sydney"",""merchant_country"":""Australia"",""merchant_id"":""22341845"",""merchant_name"":""Merchant4"",""merchant_postcode"":""2060"",""merchant_receipt"":""EFTPOS FROM WESTPAC\\r\\nMerchant4\\r\\n213 Miller Street\\r\\nSydney 2060\\r\\nAustralia\\r\\n\\r\\nTIME 17JUN18   17:09\\r\\nMID         22341845\\r\\nTSP     100312348845\\r\\nRRN     180617000151\\r\\nDebit(I)         SAV\\r\\nCARD............2797\\r\\nAUTH          265035\\r\\n\\r\\nCASH        AUD10.00\\r\\nSURCHARGE    AUD2.00\\r\\nTOTAL       AUD12.00\\r\\n\\r\\n   (000) APPROVED\\r\\n\\r\\n\\r\\n\\r\\n\\r\\n\\r\\n\\r\\n"",""merchant_receipt_printed"":true,""online_indicator"":""Y"",""pos_ref_id"":""launder-18-06-2018-03-09-17"",""rrn"":""180617000151"",""scheme_name"":""Debit"",""stan"":""000151"",""success"":true,""surcharge_amount"":200,""terminal_id"":""100312348845"",""terminal_ref_id"":""12348845_18062018031010"",""transaction_type"":""CASH""},""datetime"":""2018-06-18T03:10:10.580"",""event"":""cash_response"",""id"":""cshout4""}}";
            var msg = Message.FromJson(jsonStr, null); // todo: any method in arrange should be unit tested

            // act
            var response = new CashoutOnlyResponse(msg);

            // assert
            Assert.True(response.Success);
            Assert.Equal("cshout4", response.RequestId);
            Assert.Equal("launder-18-06-2018-03-09-17", response.PosRefId);
            Assert.Equal("Debit", response.SchemeName);
            Assert.Equal("180617000151", response.GetRRN());
            Assert.Equal(1200, response.GetCashoutAmount());
            Assert.Equal(0, response.GetBankNonCashAmount());
            Assert.Equal(1200, response.GetBankCashAmount());
            Assert.Equal(200, response.GetSurchargeAmount());
            Assert.NotNull(response.GetCustomerReceipt());
            Assert.Equal("APPROVED", response.GetResponseText());
            Assert.Equal("000", response.GetResponseCode());
            Assert.Equal("12348845_18062018031010", response.GetTerminalReferenceId());
            Assert.Equal("SAVINGS", response.GetAccountType());
            Assert.Equal("17062018", response.GetBankDate());
            Assert.NotNull(response.GetMerchantReceipt());
            Assert.Equal("170950", response.GetBankTime());
            Assert.Equal("............2797", response.GetMaskedPan());
            Assert.Equal("100312348845", response.GetTerminalId());
            Assert.Equal("265035", response.GetAuthCode());
            Assert.True(response.WasCustomerReceiptPrinted());
            Assert.True(response.WasMerchantReceiptPrinted());
        }
    }
}
