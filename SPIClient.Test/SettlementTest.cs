using Newtonsoft.Json.Linq;
using SPIClient;
using System;
using System.Globalization;
using System.Linq;
using Xunit;

namespace Test
{
    public class SettlementTest
    {
        [Fact]
        public void TestParseDate()
        {
            JObject data = new JObject(
                new JProperty("settlement_period_start_time", "05:01"),
                new JProperty("settlement_period_start_date", "05Oct17"),

                new JProperty("settlement_period_end_time", "06:02"),
                new JProperty("settlement_period_end_date", "06Nov18"),

                new JProperty("settlement_triggered_time", "07:03:45"),
                new JProperty("settlement_triggered_date", "07Dec19")
                );
            var m = new Message("77", "event_y", data, false);

            var r = new Settlement(m);

            var startTime = r.GetPeriodStartTime();
            Assert.Equal(new DateTime(2017, 10, 5, 5, 1, 0), startTime);

            var endTime = r.GetPeriodEndTime();
            Assert.Equal(new DateTime(2018, 11, 6, 6, 2, 0), endTime);

            var trigTime = r.GetTriggeredTime();
            Assert.Equal(new DateTime(2019, 12, 7, 7, 3, 45), trigTime);
        }

        [Fact]
        public void TestSettleRequest()
        {
            string posRefId = "test";

            SettleRequest request = new SettleRequest(posRefId);
            Message msg = request.ToMessage();

            Assert.Equal(msg.EventName, "settle");
            Assert.Equal(request.Id, posRefId);
        }

        [Fact]
        public void TestSettleRequestWithConfig()
        {
            string posRefId = "test";

            SettleRequest request = new SettleRequest(posRefId);

            SpiConfig config = new SpiConfig();
            config.PrintMerchantCopy = true;
            config.PromptForCustomerCopyOnEftpos = true;
            config.SignatureFlowOnEftpos = true;
            SpiClientTestUtils.SetInstanceField(request, "Config", config);

            Message msg = request.ToMessage();

            Assert.True(msg.GetDataBoolValue("print_merchant_copy", false));
            Assert.False(msg.GetDataBoolValue("prompt_for_customer_copy", false));
            Assert.False(msg.GetDataBoolValue("print_for_signature_required_transactions", false));
        }

        [Fact]
        public void TestSettleRequestWithOptions()
        {
            string posRefId = "test";
            string merchantReceiptHeader = "";
            string merchantReceiptFooter = "merchantfooter";
            string customerReceiptHeader = "customerheader";
            string customerReceiptFooter = "";

            TransactionOptions options = new TransactionOptions();
            options.SetMerchantReceiptFooter(merchantReceiptFooter);
            options.SetCustomerReceiptHeader(customerReceiptHeader);

            SettleRequest request = new SettleRequest(posRefId);
            SpiClientTestUtils.SetInstanceField(request, "Options", options);
            Message msg = request.ToMessage();

            Assert.Equal(merchantReceiptHeader, msg.GetDataStringValue("merchant_receipt_header"));
            Assert.Equal(merchantReceiptFooter, msg.GetDataStringValue("merchant_receipt_footer"));
            Assert.Equal(customerReceiptHeader, msg.GetDataStringValue("customer_receipt_header"));
            Assert.Equal(customerReceiptFooter, msg.GetDataStringValue("customer_receipt_footer"));
        }

        [Fact]
        public void TestSettleRequestWithOptions_None()
        {
            string posRefId = "test";

            SettleRequest request = new SettleRequest(posRefId);
            Message msg = request.ToMessage();

            Assert.Equal("", msg.GetDataStringValue("merchant_receipt_header"));
            Assert.Equal("", msg.GetDataStringValue("merchant_receipt_footer"));
            Assert.Equal("", msg.GetDataStringValue("customer_receipt_header"));
            Assert.Equal("", msg.GetDataStringValue("customer_receipt_footer"));
        }

        [Fact]
        public void TestSettlementResponse()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""data"":{""accumulacxted_purchase_count"":""1"",""accumulated_purchase_value"":""1000"",""accumulated_settle_by_acquirer_count"":""1"",""accumulated_settle_by_acquirer_value"":""1000"",""accumulated_total_count"":""1"",""accumulated_total_value"":""1000"",""bank_date"":""14062019"",""bank_time"":""160940"",""host_response_code"":""941"",""host_response_text"":""CUTOVER COMPLETE"",""merchant_acquirer"":""EFTPOS FROM BANK SA"",""merchant_address"":""213 Miller Street"",""merchant_city"":""Sydney"",""merchant_country"":""Australia"",""merchant_name"":""Merchant4"",""merchant_postcode"":""2060"",""merchant_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\n\r\nAustralia\r\n\r\n\r\n SETTLEMENT CUTOVER\r\nTSP     100612348842\r\nTIME   14JUN19 16:09\r\nTRAN   001137-001137\r\nFROM   13JUN19 20:00\r\nTO     14JUN19 16:09\r\n\r\nDebit\r\nTOT     0      $0.00\r\n\r\nMasterCard\r\nTOT     0      $0.00\r\n\r\nVisa\r\nPUR     1     $10.00\r\nTOT     1     $10.00\r\n\r\nBANKED  1     $10.00\r\n\r\nAmex\r\nTOT     0      $0.00\r\n\r\nDiners\r\nTOT     0      $0.00\r\n\r\nJCB\r\nTOT     0      $0.00\r\n\r\nUnionPay\r\nTOT     0      $0.00\r\n\r\nTOTAL\r\nPUR     1     $10.00\r\nTOT     1     $10.00\r\n\r\n (941) CUTOVER COMP\r\n\r\n\r\n\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""schemes"":[{""scheme_name"":""Debit"",""settle_by_acquirer"":""Yes"",""total_count"":""0"",""total_value"":""0""},{""scheme_name"":""MasterCard"",""settle_by_acquirer"":""Yes"",""total_count"":""0"",""total_value"":""0""},{""scheme_name"":""Visa"",""settle_by_acquirer"":""Yes"",""total_count"":""1"",""total_purchase_count"":""1"",""total_purchase_value"":""1000"",""total_value"":""1000""},{""scheme_name"":""Amex"",""settle_by_acquirer"":""No"",""total_count"":""0"",""total_value"":""0""},{""scheme_name"":""Diners"",""settle_by_acquirer"":""No"",""total_count"":""0"",""total_value"":""0""},{""scheme_name"":""JCB"",""settle_by_acquirer"":""No"",""total_count"":""0"",""total_value"":""0""},{""scheme_name"":""UnionPay"",""settle_by_acquirer"":""No"",""total_count"":""0"",""total_value"":""0""}],""settlement_period_end_date"":""14Jun19"",""settlement_period_end_time"":""16:09"",""settlement_period_start_date"":""13Jun19"",""settlement_period_start_time"":""20:00"",""settlement_triggered_date"":""14Jun19"",""settlement_triggered_time"":""16:09:40"",""stan"":""000000"",""success"":true,""terminal_id"":""100612348842"",""transaction_range"":""001137-001137""},""datetime"":""2019-06-14T16:09:46.395"",""event"":""settle_response"",""id"":""settle116""}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            Settlement response = new Settlement(msg);

            Assert.Equal(msg.EventName, "settle_response");
            Assert.True(response.Success);
            Assert.Equal(response.RequestId, "settle116");
            Assert.Equal(response.GetSettleByAcquirerCount(), 1);
            Assert.Equal(response.GetSettleByAcquirerValue(), 1000);
            Assert.Equal(response.GetTotalCount(), 1);
            Assert.Equal(response.GetTotalValue(), 1000);

            Assert.Equal(response.GetPeriodStartTime(), DateTime.ParseExact(msg.GetDataStringValue("settlement_period_start_time") + msg.GetDataStringValue("settlement_period_start_date"), "HH:mmddMMMyy", CultureInfo.InvariantCulture));

            Assert.Equal(response.GetPeriodEndTime(), DateTime.ParseExact(msg.GetDataStringValue("settlement_period_end_time") + msg.GetDataStringValue("settlement_period_end_date"), "HH:mmddMMMyy", CultureInfo.InvariantCulture));

            Assert.Equal(response.GetTriggeredTime(), DateTime.ParseExact(msg.GetDataStringValue("settlement_triggered_time") + msg.GetDataStringValue("settlement_triggered_date"), "HH:mm:ssddMMMyy", CultureInfo.InvariantCulture));

            Assert.Equal(response.GetResponseText(), "CUTOVER COMPLETE");
            Assert.NotNull(response.GetReceipt());
            Assert.Equal(response.GetTransactionRange(), "001137-001137");
            Assert.Equal(response.GetTerminalId(), "100612348842");
            Assert.False(response.WasMerchantReceiptPrinted());
            Assert.Equal(response.GetSchemeSettlementEntries().ToList().Count, msg.Data["schemes"].ToArray().Select(jToken => new SchemeSettlementEntry((JObject)jToken)).ToList().Count);

            response = new Settlement();
            Assert.Null(response.RequestId);
        }

        [Fact]
        public void TestSettlementEnquiryRequest()
        {
            string posRefId = "test";

            SettlementEnquiryRequest request = new SettlementEnquiryRequest(posRefId);
            Message msg = request.ToMessage();

            Assert.Equal(msg.EventName, "settlement_enquiry");
            Assert.Equal(request.Id, posRefId);
        }

        [Fact]
        public void TestSettlementEnquiryRequestWithConfig()
        {
            string posRefId = "test";

            SettlementEnquiryRequest request = new SettlementEnquiryRequest(posRefId);

            SpiConfig config = new SpiConfig();
            config.PrintMerchantCopy = true;
            config.PromptForCustomerCopyOnEftpos = true;
            config.SignatureFlowOnEftpos = false;
            SpiClientTestUtils.SetInstanceField(request, "Config", config);

            Message msg = request.ToMessage();

            Assert.True(msg.GetDataBoolValue("print_merchant_copy", false));
            Assert.False(msg.GetDataBoolValue("prompt_for_customer_copy", false));
            Assert.False(msg.GetDataBoolValue("print_for_signature_required_transactions", false));
        }

        [Fact]
        public void TestSettlementEnquiryRequestWithOptions()
        {
            string posRefId = "test";
            string merchantReceiptHeader = "";
            string merchantReceiptFooter = "merchantfooter";
            string customerReceiptHeader = "customerheader";
            string customerReceiptFooter = "";

            TransactionOptions options = new TransactionOptions();
            options.SetMerchantReceiptFooter(merchantReceiptFooter);
            options.SetCustomerReceiptHeader(customerReceiptHeader);

            SettlementEnquiryRequest request = new SettlementEnquiryRequest(posRefId);
            SpiClientTestUtils.SetInstanceField(request, "Options", options);
            Message msg = request.ToMessage();

            Assert.Equal(merchantReceiptHeader, msg.GetDataStringValue("merchant_receipt_header"));
            Assert.Equal(merchantReceiptFooter, msg.GetDataStringValue("merchant_receipt_footer"));
            Assert.Equal(customerReceiptHeader, msg.GetDataStringValue("customer_receipt_header"));
            Assert.Equal(customerReceiptFooter, msg.GetDataStringValue("customer_receipt_footer"));
        }

        [Fact]
        public void TestSettlementEnquiryRequestWithOptions_None()
        {
            string posRefId = "test";

            SettlementEnquiryRequest request = new SettlementEnquiryRequest(posRefId);
            Message msg = request.ToMessage();

            Assert.Equal("", msg.GetDataStringValue("merchant_receipt_header"));
            Assert.Equal("", msg.GetDataStringValue("merchant_receipt_footer"));
            Assert.Equal("", msg.GetDataStringValue("customer_receipt_header"));
            Assert.Equal("", msg.GetDataStringValue("customer_receipt_footer"));
        }

        [Fact]
        public void TestSchemeSettlementEntry()
        {
            string schemeName = "VISA";
            bool settleByAcquirer = true;
            int totalCount = 1;
            int totalValue = 1;

            SchemeSettlementEntry request = new SchemeSettlementEntry(schemeName, true, 1, 1);

            Assert.Equal(request.ToString(), "SchemeName: VISA, SettleByAcquirer: True, TotalCount: 1, TotalValue: 1");
        }
    }
}