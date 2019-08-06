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
        public void ParseDate_OnValidDate_IsSet()
        {
            // arrange
            var data = new JObject(
                new JProperty("settlement_period_start_time", "05:01"),
                new JProperty("settlement_period_start_date", "05Oct17"),

                new JProperty("settlement_period_end_time", "06:02"),
                new JProperty("settlement_period_end_date", "06Nov18"),

                new JProperty("settlement_triggered_time", "07:03:45"),
                new JProperty("settlement_triggered_date", "07Dec19")
                );

            // act
            var m = new Message("77", "event_y", data, false);
            var r = new Settlement(m);
            var startTime = r.GetPeriodStartTime();
            var endTime = r.GetPeriodEndTime();
            var trigTime = r.GetTriggeredTime();

            // assert
            Assert.Equal(new DateTime(2017, 10, 5, 5, 1, 0), startTime);
            Assert.Equal(new DateTime(2018, 11, 6, 6, 2, 0), endTime);
            Assert.Equal(new DateTime(2019, 12, 7, 7, 3, 45), trigTime);
        }

        [Fact]
        public void SettleRequest_OnValidRequest_ReturnObjects()
        {
            // arrange
            const string posRefId = "test";

            // act
            var request = new SettleRequest(posRefId);
            var msg = request.ToMessage();

            // assert
            Assert.Equal("settle", msg.EventName);
            Assert.Equal(posRefId, request.Id);
        }

        [Fact]
        public void SettleRequest_OnValidRequestWithConfig_ReturnObjects()
        {
            // arrange
            const string posRefId = "test";

            var config = new SpiConfig();
            config.PrintMerchantCopy = true;
            config.PromptForCustomerCopyOnEftpos = true;
            config.SignatureFlowOnEftpos = true;

            // act
            var request = new SettleRequest(posRefId);
            request.Config = config;
            var msg = request.ToMessage();

            // assert
            Assert.True(msg.GetDataBoolValue("print_merchant_copy", false));
            Assert.False(msg.GetDataBoolValue("prompt_for_customer_copy", false));
            Assert.False(msg.GetDataBoolValue("print_for_signature_required_transactions", false));
        }

        [Fact]
        public void SettleRequest_OnValidRequestWithOptions_ReturnObjects()
        {
            // arrange
            const string posRefId = "test";
            const string merchantReceiptHeader = "";
            const string merchantReceiptFooter = "merchantfooter";
            const string customerReceiptHeader = "customerheader";
            const string customerReceiptFooter = "";

            var options = new TransactionOptions();
            options.SetMerchantReceiptFooter(merchantReceiptFooter);
            options.SetCustomerReceiptHeader(customerReceiptHeader);

            // act
            var request = new SettleRequest(posRefId);
            request.Options = options;
            var msg = request.ToMessage();

            // assert
            Assert.Equal(merchantReceiptHeader, msg.GetDataStringValue("merchant_receipt_header"));
            Assert.Equal(merchantReceiptFooter, msg.GetDataStringValue("merchant_receipt_footer"));
            Assert.Equal(customerReceiptHeader, msg.GetDataStringValue("customer_receipt_header"));
            Assert.Equal(customerReceiptFooter, msg.GetDataStringValue("customer_receipt_footer"));
        }

        [Fact]
        public void SettleRequest_OnValidRequestWithOptionsNone_ReturnObjects()
        {
            // arrange
            const string posRefId = "test";

            // act
            var request = new SettleRequest(posRefId);
            var msg = request.ToMessage();

            // assert
            Assert.Equal("", msg.GetDataStringValue("merchant_receipt_header"));
            Assert.Equal("", msg.GetDataStringValue("merchant_receipt_footer"));
            Assert.Equal("", msg.GetDataStringValue("customer_receipt_header"));
            Assert.Equal("", msg.GetDataStringValue("customer_receipt_footer"));
        }

        [Fact]
        public void SettlementResponse_OnValidResponse_ReturnObjects()
        {
            // arrange
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""data"":{""accumulacxted_purchase_count"":""1"",""accumulated_purchase_value"":""1000"",""accumulated_settle_by_acquirer_count"":""1"",""accumulated_settle_by_acquirer_value"":""1000"",""accumulated_total_count"":""1"",""accumulated_total_value"":""1000"",""bank_date"":""14062019"",""bank_time"":""160940"",""host_response_code"":""941"",""host_response_text"":""CUTOVER COMPLETE"",""merchant_acquirer"":""EFTPOS FROM BANK SA"",""merchant_address"":""213 Miller Street"",""merchant_city"":""Sydney"",""merchant_country"":""Australia"",""merchant_name"":""Merchant4"",""merchant_postcode"":""2060"",""merchant_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\n\r\nAustralia\r\n\r\n\r\n SETTLEMENT CUTOVER\r\nTSP     100612348842\r\nTIME   14JUN19 16:09\r\nTRAN   001137-001137\r\nFROM   13JUN19 20:00\r\nTO     14JUN19 16:09\r\n\r\nDebit\r\nTOT     0      $0.00\r\n\r\nMasterCard\r\nTOT     0      $0.00\r\n\r\nVisa\r\nPUR     1     $10.00\r\nTOT     1     $10.00\r\n\r\nBANKED  1     $10.00\r\n\r\nAmex\r\nTOT     0      $0.00\r\n\r\nDiners\r\nTOT     0      $0.00\r\n\r\nJCB\r\nTOT     0      $0.00\r\n\r\nUnionPay\r\nTOT     0      $0.00\r\n\r\nTOTAL\r\nPUR     1     $10.00\r\nTOT     1     $10.00\r\n\r\n (941) CUTOVER COMP\r\n\r\n\r\n\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""schemes"":[{""scheme_name"":""Debit"",""settle_by_acquirer"":""Yes"",""total_count"":""0"",""total_value"":""0""},{""scheme_name"":""MasterCard"",""settle_by_acquirer"":""Yes"",""total_count"":""0"",""total_value"":""0""},{""scheme_name"":""Visa"",""settle_by_acquirer"":""Yes"",""total_count"":""1"",""total_purchase_count"":""1"",""total_purchase_value"":""1000"",""total_value"":""1000""},{""scheme_name"":""Amex"",""settle_by_acquirer"":""No"",""total_count"":""0"",""total_value"":""0""},{""scheme_name"":""Diners"",""settle_by_acquirer"":""No"",""total_count"":""0"",""total_value"":""0""},{""scheme_name"":""JCB"",""settle_by_acquirer"":""No"",""total_count"":""0"",""total_value"":""0""},{""scheme_name"":""UnionPay"",""settle_by_acquirer"":""No"",""total_count"":""0"",""total_value"":""0""}],""settlement_period_end_date"":""14Jun19"",""settlement_period_end_time"":""16:09"",""settlement_period_start_date"":""13Jun19"",""settlement_period_start_time"":""20:00"",""settlement_triggered_date"":""14Jun19"",""settlement_triggered_time"":""16:09:40"",""stan"":""000000"",""success"":true,""terminal_id"":""100612348842"",""transaction_range"":""001137-001137""},""datetime"":""2019-06-14T16:09:46.395"",""event"":""settle_response"",""id"":""settle116""}}";

            // act
            var msg = Message.FromJson(jsonStr, secrets);
            var response = new Settlement(msg);

            // assert
            Assert.Equal("settle_response", msg.EventName);
            Assert.True(response.Success);
            Assert.Equal("settle116", response.RequestId);
            Assert.Equal(1, response.GetSettleByAcquirerCount());
            Assert.Equal(1000, response.GetSettleByAcquirerValue());
            Assert.Equal(1, response.GetTotalCount());
            Assert.Equal(1000, response.GetTotalValue());
            Assert.Equal(DateTime.ParseExact(msg.GetDataStringValue("settlement_period_start_time") + msg.GetDataStringValue("settlement_period_start_date"), "HH:mmddMMMyy", CultureInfo.InvariantCulture), response.GetPeriodStartTime());
            Assert.Equal(DateTime.ParseExact(msg.GetDataStringValue("settlement_period_end_time") + msg.GetDataStringValue("settlement_period_end_date"), "HH:mmddMMMyy", CultureInfo.InvariantCulture), response.GetPeriodEndTime());
            Assert.Equal(DateTime.ParseExact(msg.GetDataStringValue("settlement_triggered_time") + msg.GetDataStringValue("settlement_triggered_date"), "HH:mm:ssddMMMyy", CultureInfo.InvariantCulture), response.GetTriggeredTime());
            Assert.Equal("CUTOVER COMPLETE", response.GetResponseText());
            Assert.NotNull(response.GetReceipt());
            Assert.Equal("001137-001137", response.GetTransactionRange());
            Assert.Equal("100612348842", response.GetTerminalId());
            Assert.False(response.WasMerchantReceiptPrinted());
            Assert.Equal(msg.Data["schemes"].ToArray().Select(jToken => new SchemeSettlementEntry((JObject)jToken)).ToList().Count, response.GetSchemeSettlementEntries().ToList().Count);

            // act
            response = new Settlement();

            // assert
            Assert.Null(response.RequestId);
        }

        [Fact]
        public void SettlementEnquiryRequest_OnValidRequest_ReturnObjects()
        {
            // arrange
            const string posRefId = "test";

            // act
            var request = new SettlementEnquiryRequest(posRefId);
            var msg = request.ToMessage();

            // assert
            Assert.Equal("settlement_enquiry", msg.EventName);
            Assert.Equal(posRefId, request.Id);
        }

        [Fact]
        public void SettlementEnquiryRequest_OnValidRequestWithConfig_ReturnObjects()
        {
            // arrange
            const string posRefId = "test";

            var config = new SpiConfig();
            config.PrintMerchantCopy = true;
            config.PromptForCustomerCopyOnEftpos = true;
            config.SignatureFlowOnEftpos = false;

            // act
            var request = new SettlementEnquiryRequest(posRefId);
            request.Config = config;
            var msg = request.ToMessage();

            // assert
            Assert.True(msg.GetDataBoolValue("print_merchant_copy", false));
            Assert.False(msg.GetDataBoolValue("prompt_for_customer_copy", false));
            Assert.False(msg.GetDataBoolValue("print_for_signature_required_transactions", false));
        }

        [Fact]
        public void SettlementEnquiryRequest_OnValidRequestWithOptions_ReturnObjects()
        {
            // arrange
            const string posRefId = "test";
            const string merchantReceiptHeader = "";
            const string merchantReceiptFooter = "merchantfooter";
            const string customerReceiptHeader = "customerheader";
            const string customerReceiptFooter = "";

            var options = new TransactionOptions();
            options.SetMerchantReceiptFooter(merchantReceiptFooter);
            options.SetCustomerReceiptHeader(customerReceiptHeader);

            // act
            var request = new SettlementEnquiryRequest(posRefId);
            request.Options = options;
            var msg = request.ToMessage();

            // assert
            Assert.Equal(merchantReceiptHeader, msg.GetDataStringValue("merchant_receipt_header"));
            Assert.Equal(merchantReceiptFooter, msg.GetDataStringValue("merchant_receipt_footer"));
            Assert.Equal(customerReceiptHeader, msg.GetDataStringValue("customer_receipt_header"));
            Assert.Equal(customerReceiptFooter, msg.GetDataStringValue("customer_receipt_footer"));
        }

        [Fact]
        public void SettlementEnquiryRequest_OnValidRequestWithOptionsNone_ReturnObjects()
        {
            // arrange
            const string posRefId = "test";

            // act
            var request = new SettlementEnquiryRequest(posRefId);
            var msg = request.ToMessage();

            // assert
            Assert.Equal("", msg.GetDataStringValue("merchant_receipt_header"));
            Assert.Equal("", msg.GetDataStringValue("merchant_receipt_footer"));
            Assert.Equal("", msg.GetDataStringValue("customer_receipt_header"));
            Assert.Equal("", msg.GetDataStringValue("customer_receipt_footer"));
        }

        [Fact]
        public void SchemeSettlementEntry_OnValidRequest_ReturnObjectt()
        {
            // arrange 
            const string schemeName = "VISA";
            const bool settleByAcquirer = true;
            const int totalCount = 1;
            const int totalValue = 1;

            // act
            var request = new SchemeSettlementEntry(schemeName, settleByAcquirer, totalCount, totalValue);

            // assert
            Assert.Equal("SchemeName: VISA, SettleByAcquirer: True, TotalCount: 1, TotalValue: 1", request.ToString());
        }
    }
}