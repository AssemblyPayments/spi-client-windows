using SPIClient;
using Xunit;

namespace Test
{
    public class CashoutTest
    {
        [Fact]
        public void TestCashoutOnlyRequest()
        {
            string posRefId = "123";
            int amountCents = 1000;

            SpiConfig config = new SpiConfig();
            config.PrintMerchantCopy = true;
            config.PromptForCustomerCopyOnEftpos = false;
            config.SignatureFlowOnEftpos = true;

            TransactionOptions options = new TransactionOptions();
            string receiptHeader = "Receipt Header";
            string receiptFooter = "Receipt Footer";
            options.SetCustomerReceiptHeader(receiptHeader);
            options.SetCustomerReceiptFooter(receiptFooter);
            options.SetMerchantReceiptHeader(receiptHeader);
            options.SetMerchantReceiptFooter(receiptFooter);

            CashoutOnlyRequest request = new CashoutOnlyRequest(amountCents, posRefId);
            request.SurchargeAmount = 100;
            request.Config = config;
            request.Options = options;

            Message msg = request.ToMessage();

            Assert.Equal(request.PosRefId, posRefId);
            Assert.Equal(request.CashoutAmount, amountCents);
            Assert.Equal(config.PrintMerchantCopy, msg.GetDataBoolValue("print_merchant_copy", false));
            Assert.Equal(config.PromptForCustomerCopyOnEftpos, msg.GetDataBoolValue("prompt_for_customer_copy", false));
            Assert.Equal(config.SignatureFlowOnEftpos, msg.GetDataBoolValue("print_for_signature_required_transactions", false));
            Assert.Equal(receiptHeader, msg.GetDataStringValue("customer_receipt_header"));
            Assert.Equal(receiptFooter, msg.GetDataStringValue("customer_receipt_footer"));
            Assert.Equal(receiptHeader, msg.GetDataStringValue("merchant_receipt_header"));
            Assert.Equal(receiptFooter, msg.GetDataStringValue("merchant_receipt_footer"));
        }

        [Fact]
        public void TestCashoutOnlyResponse()
        {
            string jsonStr = @"{""message"": {""data"":{""account_type"":""SAVINGS"",""auth_code"":""265035"",""bank_cash_amount"":1200,""bank_date"":""17062018"",""bank_settlement_date"":""18062018"",""bank_time"":""170950"",""card_entry"":""EMV_INSERT"",""cash_amount"":1200,""currency"":""AUD"",""customer_receipt"":""EFTPOS FROM WESTPAC\\r\\nMerchant4\\r\\n213 Miller Street\\r\\nSydney 2060\\r\\nAustralia\\r\\n\\r\\nTIME 17JUN18   17:09\\r\\nMID         22341845\\r\\nTSP     100312348845\\r\\nRRN     180617000151\\r\\nDebit(I)         SAV\\r\\nCARD............2797\\r\\nAUTH          265035\\r\\n\\r\\nCASH        AUD10.00\\r\\nSURCHARGE    AUD2.00\\r\\nTOTAL       AUD12.00\\r\\n\\r\\n   (000) APPROVED\\r\\n\\r\\n  *CUSTOMER COPY*\\r\\n\\r\\n\\r\\n\\r\\n\\r\\n\\r\\n\\r\\n\\r\\n"",""customer_receipt_printed"":true,""expiry_date"":""0722"",""host_response_code"":""000"",""host_response_text"":""APPROVED"",""informative_text"":""                "",""masked_pan"":""............2797"",""merchant_acquirer"":""EFTPOS FROM WESTPAC"",""merchant_addr"":""213 Miller Street"",""merchant_city"":""Sydney"",""merchant_country"":""Australia"",""merchant_id"":""22341845"",""merchant_name"":""Merchant4"",""merchant_postcode"":""2060"",""merchant_receipt"":""EFTPOS FROM WESTPAC\\r\\nMerchant4\\r\\n213 Miller Street\\r\\nSydney 2060\\r\\nAustralia\\r\\n\\r\\nTIME 17JUN18   17:09\\r\\nMID         22341845\\r\\nTSP     100312348845\\r\\nRRN     180617000151\\r\\nDebit(I)         SAV\\r\\nCARD............2797\\r\\nAUTH          265035\\r\\n\\r\\nCASH        AUD10.00\\r\\nSURCHARGE    AUD2.00\\r\\nTOTAL       AUD12.00\\r\\n\\r\\n   (000) APPROVED\\r\\n\\r\\n\\r\\n\\r\\n\\r\\n\\r\\n\\r\\n"",""merchant_receipt_printed"":true,""online_indicator"":""Y"",""pos_ref_id"":""launder-18-06-2018-03-09-17"",""rrn"":""180617000151"",""scheme_name"":""Debit"",""stan"":""000151"",""success"":true,""surcharge_amount"":200,""terminal_id"":""100312348845"",""terminal_ref_id"":""12348845_18062018031010"",""transaction_type"":""CASH""},""datetime"":""2018-06-18T03:10:10.580"",""event"":""cash_response"",""id"":""cshout4""}}";

            Message msg = Message.FromJson(jsonStr, null);
            CashoutOnlyResponse response = new CashoutOnlyResponse(msg);

            Assert.True(response.Success);
            Assert.Equal(response.RequestId, "cshout4");
            Assert.Equal(response.PosRefId, "launder-18-06-2018-03-09-17");
            Assert.Equal(response.SchemeName, "Debit");
            Assert.Equal(response.GetRRN(), "180617000151");
            Assert.Equal(response.GetCashoutAmount(), 1200);
            Assert.Equal(response.GetBankNonCashAmount(), 0);
            Assert.Equal(response.GetBankCashAmount(), 1200);
            Assert.Equal(response.GetSurchargeAmount(), 200);
            Assert.NotNull(response.GetCustomerReceipt());
            Assert.Equal(response.GetResponseText(), "APPROVED");
            Assert.Equal(response.GetResponseCode(), "000");
            Assert.Equal(response.GetTerminalReferenceId(), "12348845_18062018031010");
            Assert.Equal(response.GetAccountType(), "SAVINGS");
            Assert.Equal(response.GetBankDate(), "17062018");
            Assert.NotNull(response.GetMerchantReceipt());
            Assert.Equal(response.GetBankTime(), "170950");
            Assert.Equal(response.GetMaskedPan(), "............2797");
            Assert.Equal(response.GetTerminalId(), "100312348845");
            Assert.Equal(response.GetAuthCode(), "265035");
            Assert.True(response.WasCustomerReceiptPrinted());
            Assert.True(response.WasMerchantReceiptPrinted());
            Assert.Equal(response.GetResponseValue("pos_ref_id"), response.PosRefId);

            response = new CashoutOnlyResponse();
            Assert.Null(response.PosRefId);
        }
    }
}
