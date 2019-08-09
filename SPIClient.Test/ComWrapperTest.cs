using Newtonsoft.Json.Linq;
using SPIClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;

namespace Test
{
    public class ComWrapperTest
    {
        [Fact]
        public void ToOpenTablesJson_ValidJson_IsSet()
        {
            // arrange
            var getOpenTablesCom = new GetOpenTablesCom();

            // act
            string jsonStr = getOpenTablesCom.ToOpenTablesJson();

            //assert
            Assert.NotNull(jsonStr);
        }

        [Fact]
        public void AddToOpenTablesList_ValidOpenTablesList_SetObjects()
        {
            // arrange
            var getOpenTablesCom = new GetOpenTablesCom();
            var openTablesEntry = new OpenTablesEntry();
            openTablesEntry.TableId = "1";
            openTablesEntry.Label = "1";
            openTablesEntry.BillOutstandingAmount = 1000;

            // act
            getOpenTablesCom.AddToOpenTablesList(openTablesEntry);
            var openTablesEntries = (List<OpenTablesEntry>)SpiClientTestUtils.GetInstanceField(typeof(GetOpenTablesCom), getOpenTablesCom, "OpenTablesList");

            // assert
            Assert.Equal(openTablesEntries[0].TableId, openTablesEntry.TableId);
            Assert.Equal(openTablesEntries[0].Label, openTablesEntry.Label);
            Assert.Equal(openTablesEntries[0].BillOutstandingAmount, openTablesEntry.BillOutstandingAmount);
        }

        [Fact]
        public void PurchaseResponseInit_OnValidResponse_ReturnObjects()
        {
            // arrange
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""data"":{""account_type"":""SAVINGS"",""auth_code"":""278045"",""bank_cash_amount"":200,""bank_date"":""06062019"",""bank_noncash_amount"":1200,""bank_settlement_date"":""06062019"",""bank_time"":""110750"",""card_entry"":""MAG_STRIPE"",""cash_amount"":200,""currency"":""AUD"",""customer_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\nTIME 06JUN19   11:07\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190606001102\r\nDebit(S)         SAV\r\nCARD............5581\r\nAUTH          278045\r\n\r\nPURCHASE    AUD10.00\r\nCASH         AUD2.00\r\nSURCHARGE    AUD2.00\r\nTOTAL       AUD14.00\r\n\r\n   (000) APPROVED\r\n\r\n  *CUSTOMER COPY*\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""customer_receipt_printed"":false,""expiry_date"":""0822"",""host_response_code"":""000"",""host_response_text"":""APPROVED"",""informative_text"":""                "",""masked_pan"":""............5581"",""merchant_acquirer"":""EFTPOS FROM BANK SA"",""merchant_addr"":""213 Miller Street"",""merchant_city"":""Sydney"",""merchant_country"":""Australia"",""merchant_id"":""22341842"",""merchant_name"":""Merchant4"",""merchant_postcode"":""2060"",""merchant_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\nTIME 06JUN19   11:07\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190606001102\r\nDebit(S)         SAV\r\nCARD............5581\r\nAUTH          278045\r\n\r\nPURCHASE    AUD10.00\r\nCASH         AUD2.00\r\nSURCHARGE    AUD2.00\r\nTOTAL       AUD14.00\r\n\r\n   (000) APPROVED\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""merchant_receipt_printed"":false,""online_indicator"":""Y"",""pos_ref_id"":""prchs-06-06-2019-11-07-50"",""purchase_amount"":1000,""rrn"":""190606001102"",""scheme_name"":""Debit"",""stan"":""001102"",""success"":true,""surcharge_amount"":200,""terminal_id"":""100612348842"",""terminal_ref_id"":""12348842_06062019110812"",""transaction_type"":""PURCHASE""},""datetime"":""2019-06-06T11:08:12.946"",""event"":""purchase_response"",""id"":""prchs5""}}";

            // act
            var msg = Message.FromJson(jsonStr, secrets);
            var comWrapper = new ComWrapper();
            var response = comWrapper.PurchaseResponseInit(msg);

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
        }

        [Fact]
        public void RefundResponseInit_OnValidResponse_ReturnObjects()
        {
            // arrange
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""data"":{""account_type"":""CREDIT"",""auth_code"":""067849"",""bank_date"":""06062019"",""bank_noncash_amount"":1000,""bank_settlement_date"":""06062019"",""bank_time"":""114905"",""card_entry"":""EMV_CTLS"",""currency"":""AUD"",""customer_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\nTIME 06JUN19   11:49\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190606001105\r\nVisa(C)           CR\r\nCARD............5581\r\nAUTH          067849\r\n\r\nREFUND      AUD10.00\r\n\r\n   (000) APPROVED\r\n\r\n  *CUSTOMER COPY*\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""customer_receipt_printed"":false,""emv_actioncode"":""ARQ"",""emv_actioncode_values"":""67031BCC5AD15818"",""expiry_date"":""0822"",""host_response_code"":""000"",""host_response_text"":""APPROVED"",""informative_text"":""                "",""masked_pan"":""............5581"",""merchant_acquirer"":""EFTPOS FROM BANK SA"",""merchant_addr"":""213 Miller Street"",""merchant_city"":""Sydney"",""merchant_country"":""Australia"",""merchant_id"":""22341842"",""merchant_name"":""Merchant4"",""merchant_postcode"":""2060"",""merchant_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\nTIME 06JUN19   11:49\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190606001105\r\nVisa(C)           CR\r\nCARD............5581\r\nAUTH          067849\r\n\r\nREFUND      AUD10.00\r\n\r\n   (000) APPROVED\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""merchant_receipt_printed"":false,""online_indicator"":""Y"",""pos_ref_id"":""rfnd-06-06-2019-11-49-05"",""refund_amount"":1000,""rrn"":""190606001105"",""scheme_name"":""Visa"",""stan"":""001105"",""success"":true,""terminal_id"":""100612348842"",""terminal_ref_id"":""12348842_06062019114915"",""transaction_type"":""REFUND""},""datetime"":""2019-06-06T11:49:15.038"",""event"":""refund_response"",""id"":""refund150""}}";

            // act
            var msg = Message.FromJson(jsonStr, secrets);
            var comWrapper = new ComWrapper();
            var response = comWrapper.RefundResponseInit(msg);

            // assert
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
        }

        [Fact]
        public void SettlementInit_OnValidResponse_ReturnObjects()
        {
            // arrange 
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""data"":{""accumulacxted_purchase_count"":""1"",""accumulated_purchase_value"":""1000"",""accumulated_settle_by_acquirer_count"":""1"",""accumulated_settle_by_acquirer_value"":""1000"",""accumulated_total_count"":""1"",""accumulated_total_value"":""1000"",""bank_date"":""14062019"",""bank_time"":""160940"",""host_response_code"":""941"",""host_response_text"":""CUTOVER COMPLETE"",""merchant_acquirer"":""EFTPOS FROM BANK SA"",""merchant_address"":""213 Miller Street"",""merchant_city"":""Sydney"",""merchant_country"":""Australia"",""merchant_name"":""Merchant4"",""merchant_postcode"":""2060"",""merchant_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\n\r\nAustralia\r\n\r\n\r\n SETTLEMENT CUTOVER\r\nTSP     100612348842\r\nTIME   14JUN19 16:09\r\nTRAN   001137-001137\r\nFROM   13JUN19 20:00\r\nTO     14JUN19 16:09\r\n\r\nDebit\r\nTOT     0      $0.00\r\n\r\nMasterCard\r\nTOT     0      $0.00\r\n\r\nVisa\r\nPUR     1     $10.00\r\nTOT     1     $10.00\r\n\r\nBANKED  1     $10.00\r\n\r\nAmex\r\nTOT     0      $0.00\r\n\r\nDiners\r\nTOT     0      $0.00\r\n\r\nJCB\r\nTOT     0      $0.00\r\n\r\nUnionPay\r\nTOT     0      $0.00\r\n\r\nTOTAL\r\nPUR     1     $10.00\r\nTOT     1     $10.00\r\n\r\n (941) CUTOVER COMP\r\n\r\n\r\n\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""schemes"":[{""scheme_name"":""Debit"",""settle_by_acquirer"":""Yes"",""total_count"":""0"",""total_value"":""0""},{""scheme_name"":""MasterCard"",""settle_by_acquirer"":""Yes"",""total_count"":""0"",""total_value"":""0""},{""scheme_name"":""Visa"",""settle_by_acquirer"":""Yes"",""total_count"":""1"",""total_purchase_count"":""1"",""total_purchase_value"":""1000"",""total_value"":""1000""},{""scheme_name"":""Amex"",""settle_by_acquirer"":""No"",""total_count"":""0"",""total_value"":""0""},{""scheme_name"":""Diners"",""settle_by_acquirer"":""No"",""total_count"":""0"",""total_value"":""0""},{""scheme_name"":""JCB"",""settle_by_acquirer"":""No"",""total_count"":""0"",""total_value"":""0""},{""scheme_name"":""UnionPay"",""settle_by_acquirer"":""No"",""total_count"":""0"",""total_value"":""0""}],""settlement_period_end_date"":""14Jun19"",""settlement_period_end_time"":""16:09"",""settlement_period_start_date"":""13Jun19"",""settlement_period_start_time"":""20:00"",""settlement_triggered_date"":""14Jun19"",""settlement_triggered_time"":""16:09:40"",""stan"":""000000"",""success"":true,""terminal_id"":""100612348842"",""transaction_range"":""001137-001137""},""datetime"":""2019-06-14T16:09:46.395"",""event"":""settle_response"",""id"":""settle116""}}";

            // act
            var msg = Message.FromJson(jsonStr, secrets);
            var comWrapper = new ComWrapper();
            var response = comWrapper.SettlementInit(msg);

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
        }

        [Fact]
        public void SecretsInit_OnValidRequest_IsSet()
        {
            // arrange
            const string encKey = "81CF9E6A14CDAF244A30B298D4CECB505C730CE352C6AF6E1DE61B3232E24D3F";
            const string hmacKey = "D35060723C9EECDB8AEA019581381CB08F64469FC61A5A04FE553EBDB5CD55B9";

            // act
            var comWrapper = new ComWrapper();
            var secrets = comWrapper.SecretsInit(encKey, hmacKey);

            // assert
            Assert.Equal(encKey, secrets.EncKey);
            Assert.Equal(hmacKey, secrets.HmacKey);
        }

        [Fact]
        public void GetLastTransactionResponseInit_OnValidResponse_ReturnObjects()
        {
            // arrange
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""data"":{""account_type"":""CREDIT"",""auth_code"":""139059"",""bank_date"":""14062019"",""bank_noncash_amount"":1000,""bank_settlement_date"":""14062019"",""bank_time"":""153747"",""card_entry"":""EMV_CTLS"",""currency"":""AUD"",""customer_receipt"":"""",""customer_receipt_printed"":false,""emv_actioncode"":""ARP"",""emv_actioncode_values"":""9BDDE227547B41F43030"",""emv_pix"":""1010"",""emv_rid"":""A000000003"",""emv_tsi"":""0000"",""emv_tvr"":""0000000000"",""expiry_date"":""1122"",""host_response_code"":""000"",""host_response_text"":""APPROVED"",""informative_text"":""                "",""masked_pan"":""............3952"",""merchant_acquirer"":""EFTPOS FROM BANK SA"",""merchant_addr"":""213 Miller Street"",""merchant_city"":""Sydney"",""merchant_country"":""Australia"",""merchant_id"":""22341842"",""merchant_name"":""Merchant4"",""merchant_postcode"":""2060"",""merchant_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\nTIME 14JUN19   15:37\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190614001137\r\nVisa Credit     \r\nVisa(C)           CR\r\nCARD............3952\r\nAID   A0000000031010\r\nTVR       0000000000\r\nAUTH          139059\r\n\r\nPURCHASE    AUD10.00\r\n\r\n   (000) APPROVED\r\n\r\n*DUPLICATE  RECEIPT*\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""merchant_receipt_printed"":false,""online_indicator"":""Y"",""pos_ref_id"":""prchs-14-06-2019-15-37-49"",""purchase_amount"":1000,""rrn"":""190614001137"",""scheme_app_name"":""Visa Credit"",""scheme_name"":""Visa"",""stan"":""001137"",""success"":true,""terminal_id"":""100612348842"",""terminal_ref_id"":""12348842_14062019153831"",""transaction_type"":""PURCHASE""},""datetime"":""2019-06-14T15:38:31.620"",""event"":""last_transaction"",""id"":""glt10""}}";

            // act
            var msg = Message.FromJson(jsonStr, secrets);
            var comWrapper = new ComWrapper();
            var response = comWrapper.GetLastTransactionResponseInit(msg);

            // assert
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
        }

        [Fact]
        public void CopyMerchantReceiptToCustomerReceipt_OnValidResponse_ReturnObjects()
        {
            // arrange
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""data"":{""account_type"":""CREDIT"",""auth_code"":""139059"",""bank_date"":""14062019"",""bank_noncash_amount"":1000,""bank_settlement_date"":""14062019"",""bank_time"":""153747"",""card_entry"":""EMV_CTLS"",""currency"":""AUD"",""customer_receipt"":"""",""customer_receipt_printed"":false,""emv_actioncode"":""ARP"",""emv_actioncode_values"":""9BDDE227547B41F43030"",""emv_pix"":""1010"",""emv_rid"":""A000000003"",""emv_tsi"":""0000"",""emv_tvr"":""0000000000"",""expiry_date"":""1122"",""host_response_code"":""000"",""host_response_text"":""APPROVED"",""informative_text"":""                "",""masked_pan"":""............3952"",""merchant_acquirer"":""EFTPOS FROM BANK SA"",""merchant_addr"":""213 Miller Street"",""merchant_city"":""Sydney"",""merchant_country"":""Australia"",""merchant_id"":""22341842"",""merchant_name"":""Merchant4"",""merchant_postcode"":""2060"",""merchant_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\nTIME 14JUN19   15:37\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190614001137\r\nVisa Credit     \r\nVisa(C)           CR\r\nCARD............3952\r\nAID   A0000000031010\r\nTVR       0000000000\r\nAUTH          139059\r\n\r\nPURCHASE    AUD10.00\r\n\r\n   (000) APPROVED\r\n\r\n*DUPLICATE  RECEIPT*\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""merchant_receipt_printed"":false,""online_indicator"":""Y"",""pos_ref_id"":""prchs-14-06-2019-15-37-49"",""purchase_amount"":1000,""rrn"":""190614001137"",""scheme_app_name"":""Visa Credit"",""scheme_name"":""Visa"",""stan"":""001137"",""success"":true,""terminal_id"":""100612348842"",""terminal_ref_id"":""12348842_14062019153831"",""transaction_type"":""PURCHASE""},""datetime"":""2019-06-14T15:38:31.620"",""event"":""last_transaction"",""id"":""glt10""}}";
            var msg = Message.FromJson(jsonStr, secrets);
            var comWrapper = new ComWrapper();
            var response = comWrapper.GetLastTransactionResponseInit(msg);

            // act 
            response.CopyMerchantReceiptToCustomerReceipt();

            // assert
            Assert.Equal(msg.GetDataStringValue("merchant_receipt"), msg.GetDataStringValue("customer_receipt"));
        }

        [Fact]
        public void CashoutOnlyResponseInit_OnValidResponse_ReturnObjects()
        {
            // arrange
            const string jsonStr = @"{""message"": {""data"":{""account_type"":""SAVINGS"",""auth_code"":""265035"",""bank_cash_amount"":1200,""bank_date"":""17062018"",""bank_settlement_date"":""18062018"",""bank_time"":""170950"",""card_entry"":""EMV_INSERT"",""cash_amount"":1200,""currency"":""AUD"",""customer_receipt"":""EFTPOS FROM WESTPAC\\r\\nMerchant4\\r\\n213 Miller Street\\r\\nSydney 2060\\r\\nAustralia\\r\\n\\r\\nTIME 17JUN18   17:09\\r\\nMID         22341845\\r\\nTSP     100312348845\\r\\nRRN     180617000151\\r\\nDebit(I)         SAV\\r\\nCARD............2797\\r\\nAUTH          265035\\r\\n\\r\\nCASH        AUD10.00\\r\\nSURCHARGE    AUD2.00\\r\\nTOTAL       AUD12.00\\r\\n\\r\\n   (000) APPROVED\\r\\n\\r\\n  *CUSTOMER COPY*\\r\\n\\r\\n\\r\\n\\r\\n\\r\\n\\r\\n\\r\\n\\r\\n"",""customer_receipt_printed"":true,""expiry_date"":""0722"",""host_response_code"":""000"",""host_response_text"":""APPROVED"",""informative_text"":""                "",""masked_pan"":""............2797"",""merchant_acquirer"":""EFTPOS FROM WESTPAC"",""merchant_addr"":""213 Miller Street"",""merchant_city"":""Sydney"",""merchant_country"":""Australia"",""merchant_id"":""22341845"",""merchant_name"":""Merchant4"",""merchant_postcode"":""2060"",""merchant_receipt"":""EFTPOS FROM WESTPAC\\r\\nMerchant4\\r\\n213 Miller Street\\r\\nSydney 2060\\r\\nAustralia\\r\\n\\r\\nTIME 17JUN18   17:09\\r\\nMID         22341845\\r\\nTSP     100312348845\\r\\nRRN     180617000151\\r\\nDebit(I)         SAV\\r\\nCARD............2797\\r\\nAUTH          265035\\r\\n\\r\\nCASH        AUD10.00\\r\\nSURCHARGE    AUD2.00\\r\\nTOTAL       AUD12.00\\r\\n\\r\\n   (000) APPROVED\\r\\n\\r\\n\\r\\n\\r\\n\\r\\n\\r\\n\\r\\n"",""merchant_receipt_printed"":true,""online_indicator"":""Y"",""pos_ref_id"":""launder-18-06-2018-03-09-17"",""rrn"":""180617000151"",""scheme_name"":""Debit"",""stan"":""000151"",""success"":true,""surcharge_amount"":200,""terminal_id"":""100312348845"",""terminal_ref_id"":""12348845_18062018031010"",""transaction_type"":""CASH""},""datetime"":""2018-06-18T03:10:10.580"",""event"":""cash_response"",""id"":""cshout4""}}";

            // act
            var msg = Message.FromJson(jsonStr, null);
            var comWrapper = new ComWrapper();
            var response = comWrapper.CashoutOnlyResponseInit(msg);

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
            Assert.Equal(response.GetResponseValue("pos_ref_id"), response.PosRefId);
        }

        [Fact]
        public void MotoPurchaseResponseInit_OnValidResponse_ReturnObjects()
        {
            // arrange
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""data"":{""pos_ref_id"":""a-zA-Z0-9"",""account_type"": ""CREDIT"",""purchase_amount"": 1000,""surcharge_amount"": 200,""bank_noncash_amount"": 1200,""bank_cash_amount"": 200,""auth_code"": ""653230"",""bank_date"": ""07092017"",""bank_time"": ""152137"",""bank_settlement_date"": ""21102017"",""currency"": ""AUD"",""emv_actioncode"": """",""emv_actioncode_values"": """",""emv_pix"": """",""emv_rid"": """",""emv_tsi"": """",""emv_tvr"": """",""expiry_date"": ""1117"",""host_response_code"": ""000"",""host_response_text"": ""APPROVED"",""informative_text"": ""                "",""masked_pan"": ""............0794"",""merchant_acquirer"": ""EFTPOS FROM WESTPAC"",""merchant_addr"": ""275 Kent St"",""merchant_city"": ""Sydney"",""merchant_country"": ""Australia"",""merchant_id"": ""02447508"",""merchant_name"": ""VAAS Product 4"",""merchant_postcode"": ""2000"",""online_indicator"": ""Y"",""scheme_app_name"": """",""scheme_name"": """",""stan"": ""000212"",""rrn"": ""1517890741"",""success"": true,""terminal_id"": ""100381990118"",""transaction_type"": ""MOTO"",""card_entry"": ""MANUAL_PHONE"",""customer_receipt"":""EFTPOS FROM WESTPAC\r\nVAAS Product 4\r\n275 Kent St\r\nSydney\r\nMID02447508\r\nTSP100381990118\r\nTIME 07SEP17   15:21\r\nRRN     1517890741\r\nTRAN 000212   CREDIT\r\nVisa Credit     \r\nVisa               M\r\nCARD............0794\r\nAUTH          653230\r\n\r\nMOTO   AUD10000\r\n\r\nTOTAL      AUD10000\r\n\r\n\r\n(000)APPROVED\r\n\r\n\r\n *CUSTOMER COPY*\r\n\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""merchant_receipt"":""EFTPOS FROM WESTPAC\r\nVAAS Product4\r\n275 Kent St\r\nSydney\r\nMID02447508\r\nTSP100381990118\r\nTIME 07SEP17   15:21\r\nRRN     1517890741\r\nTRAN 000212   CREDIT\r\nVisa Credit     \r\nVisa               M\r\nCARD............0794\r\nAUTH          653230\r\n\r\nPURCHASE   AUD10000\r\n\r\nTOTAL      AUD10000\r\n\r\n\r\n(000) APPROVED\r\n\r\n\r\n\r\n\r\n\r\n\r\n\r\n""},""datetime"": ""2018-02-06T04:19:00.545"",""event"": ""moto_purchase_response"",""id"": ""4""}}";

            // act
            var msg = Message.FromJson(jsonStr, secrets);
            var comWrapper = new ComWrapper();
            var response = comWrapper.MotoPurchaseResponseInit(msg);

            // assert
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
        }

        [Fact]
        public void PreautResponseInit_OnValidResponse_ReturnObjects()
        {
            // arrange
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""data"":{""account_type"":""CREDIT"",""auth_code"":""318981"",""bank_date"":""11062019"",""bank_noncash_amount"":1000,""bank_settlement_date"":""11062019"",""bank_time"":""182808"",""card_entry"":""EMV_INSERT"",""currency"":""AUD"",""customer_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\nTIME 11JUN19   18:28\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190611001110\r\nVisa Credit     \r\nVisa(I)           CR\r\nCARD............3952\r\nAID   A0000000031010\r\nTVR       0080048000\r\nAUTH          318981\r\nPRE-AUTH ID 15765372\r\n\r\nPRE-AUTH    AUD10.00\r\n\r\n   (000) APPROVED\r\n\r\n  *CUSTOMER COPY*\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""customer_receipt_printed"":false,""emv_actioncode"":""TC"",""emv_actioncode_values"":""C0A8342DF36207F1"",""emv_pix"":""1010"",""emv_rid"":""A000000003"",""emv_tsi"":""F800"",""emv_tvr"":""0080048000"",""expiry_date"":""1122"",""host_response_code"":""000"",""host_response_text"":""APPROVED"",""informative_text"":""                "",""masked_pan"":""............3952"",""merchant_acquirer"":""EFTPOS FROM BANK SA"",""merchant_addr"":""213 Miller Street"",""merchant_city"":""Sydney"",""merchant_country"":""Australia"",""merchant_id"":""22341842"",""merchant_name"":""Merchant4"",""merchant_postcode"":""2060"",""merchant_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\nTIME 11JUN19   18:28\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190611001110\r\nVisa Credit     \r\nVisa(I)           CR\r\nCARD............3952\r\nAID   A0000000031010\r\nTVR       0080048000\r\nAUTH          318981\r\nPRE-AUTH ID 15765372\r\n\r\nPRE-AUTH    AUD10.00\r\n\r\n   (000) APPROVED\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""merchant_receipt_printed"":false,""online_indicator"":""Y"",""pos_ref_id"":""propen-11-06-2019-18-28-08"",""preauth_amount"":1000,""preauth_id"":""15765372"",""rrn"":""190611001110"",""scheme_app_name"":""Visa Credit"",""scheme_name"":""Visa"",""stan"":""001110"",""success"":true,""terminal_id"":""100612348842"",""terminal_ref_id"":""12348842_11062019182827"",""transaction_type"":""PRE-AUTH""},""datetime"":""2019-06-11T18:28:27.237"",""event"":""preauth_response"",""id"":""prac17""}}";

            // act
            var msg = Message.FromJson(jsonStr, secrets);
            var comWrapper = new ComWrapper();
            var response = comWrapper.PreauthResponseInit(msg);

            // assert
            Assert.Equal("preauth_response", msg.EventName);
            Assert.Equal("15765372", response.PreauthId);
            Assert.Equal("propen-11-06-2019-18-28-08", response.PosRefId);
            Assert.Equal(0, response.GetCompletionAmount());
            Assert.Equal(1000, response.GetBalanceAmount());
            Assert.Equal(0, response.GetPreviousBalanceAmount());
            Assert.Equal(0, response.GetSurchargeAmount());
            Assert.True(response.Details.Success);
            Assert.Equal("prac17", response.Details.RequestId);
            Assert.Equal("Visa", response.Details.SchemeName);
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
        public void AccountVerifyResponseInit_OnValidResponse_ReturnObjects()
        {
            // arrange
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""data"":{""account_type"":""CREDIT"",""auth_code"":""316810"",""bank_date"":""11062019"",""bank_settlement_date"":""11062019"",""bank_time"":""182739"",""card_entry"":""EMV_INSERT"",""currency"":""AUD"",""customer_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\nTIME 11JUN19   18:27\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190611001109\r\nVisa Credit     \r\nVisa(I)           CR\r\nCARD............3952\r\nAID   A0000000031010\r\nTVR       0080048000\r\nAUTH          316810\r\n\r\nA/C VERIFIED AUD0.00\r\n\r\n   (000) APPROVED\r\n\r\n  *CUSTOMER COPY*\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""customer_receipt_printed"":false,""emv_actioncode"":""TC"",""emv_actioncode_values"":""F1F17B37A5BEF2B1"",""emv_pix"":""1010"",""emv_rid"":""A000000003"",""emv_tsi"":""F800"",""emv_tvr"":""0080048000"",""expiry_date"":""1122"",""host_response_code"":""000"",""host_response_text"":""APPROVED"",""informative_text"":""                "",""masked_pan"":""............3952"",""merchant_acquirer"":""EFTPOS FROM BANK SA"",""merchant_addr"":""213 Miller Street"",""merchant_city"":""Sydney"",""merchant_country"":""Australia"",""merchant_id"":""22341842"",""merchant_name"":""Merchant4"",""merchant_postcode"":""2060"",""merchant_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\nTIME 11JUN19   18:27\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190611001109\r\nVisa Credit     \r\nVisa(I)           CR\r\nCARD............3952\r\nAID   A0000000031010\r\nTVR       0080048000\r\nAUTH          316810\r\n\r\nA/C VERIFIED AUD0.00\r\n\r\n   (000) APPROVED\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""merchant_receipt_printed"":false,""online_indicator"":""Y"",""pos_ref_id"":""actvfy-11-06-2019-18-27-39"",""rrn"":""190611001109"",""scheme_app_name"":""Visa Credit"",""scheme_name"":""Visa"",""stan"":""001109"",""success"":true,""terminal_id"":""100612348842"",""terminal_ref_id"":""12348842_11062019182754"",""transaction_type"":""A/C VERIFIED""},""datetime"":""2019-06-11T18:27:54.933"",""event"":""account_verify_response"",""id"":""prav15""}}";

            // act
            var msg = Message.FromJson(jsonStr, secrets);
            var comWrapper = new ComWrapper();
            var response = comWrapper.AccountVerifyResponseInit(msg);

            // assert
            Assert.Equal("account_verify_response", msg.EventName);
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
            Assert.Equal("11062019", response.Details.GetBankDate());
            Assert.Equal("182739", response.Details.GetBankTime());
            Assert.Equal("............3952", response.Details.GetMaskedPan());
            Assert.Equal("100612348842", response.Details.GetTerminalId());
            Assert.False(response.Details.WasCustomerReceiptPrinted());
            Assert.False(response.Details.WasMerchantReceiptPrinted());
            Assert.Equal(DateTime.ParseExact(msg.GetDataStringValue("bank_settlement_date"), "ddMMyyyy", CultureInfo.InvariantCulture).Date, response.Details.GetSettlementDate());
        }

        [Fact]
        public void PrintingResponseInit_OnValidResponse_ReturnObjects()
        {
            // arrange
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""data"":{""success"":true},""datetime"":""2019-06-14T18:51:00.948"",""event"":""print_response"",""id"":""C24.0""}}";

            // act
            var msg = Message.FromJson(jsonStr, secrets);
            var comWrapper = new ComWrapper();
            var response = comWrapper.PrintingResponseInit(msg);

            // assert
            Assert.Equal("print_response", msg.EventName);
            Assert.True(response.IsSuccess());
            Assert.Equal("C24.0", msg.Id);
            Assert.Equal("", response.GetErrorReason());
            Assert.Equal("", response.GetErrorDetail());
            Assert.Equal("", response.GetResponseValueWithAttribute("error_detail"));
        }

        [Fact]
        public void TerminalStatusResponseInit_OnValidResponse_ReturnObjects()
        {
            // arrange
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""data"":{""battery_level"":""100"",""charging"":true,""status"":""IDLE"",""success"":true},""datetime"":""2019-06-18T13:00:38.820"",""event"":""terminal_status"",""id"":""trmnl4""}}";

            // act
            var msg = Message.FromJson(jsonStr, secrets);
            var comWrapper = new ComWrapper();
            var response = comWrapper.TerminalStatusResponseInit(msg);

            // assert
            Assert.Equal("terminal_status", msg.EventName);
            Assert.True(response.isSuccess());
            Assert.Equal("100", response.GetBatteryLevel());
            Assert.Equal("IDLE", response.GetStatus());
            Assert.True(response.IsCharging());
        }

        [Fact]
        public void TerminalConfigurationResponseInit_OnValidResponse_ReturnObjects()
        {
            // arrange
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""data"":{""comms_selected"":""WIFI"",""merchant_id"":""22341842"",""pa_version"":""SoftPay03.16.03"",""payment_interface_version"":""02.02.00"",""plugin_version"":""v2.6.11"",""serial_number"":""321-404-842"",""success"":true,""terminal_id"":""12348842"",""terminal_model"":""VX690""},""datetime"":""2019-06-18T13:00:41.075"",""event"":""terminal_configuration"",""id"":""trmnlcnfg5""}}";

            // act
            var msg = Message.FromJson(jsonStr, secrets);
            var comWrapper = new ComWrapper();
            var response = comWrapper.TerminalConfigurationResponseInit(msg);

            // assert
            Assert.Equal("terminal_configuration", msg.EventName);
            Assert.True(response.isSuccess());
            Assert.Equal("WIFI", response.GetCommsSelected());
            Assert.Equal("22341842", response.GetMerchantId());
            Assert.Equal("SoftPay03.16.03", response.GetPAVersion());
            Assert.Equal("02.02.00", response.GetPaymentInterfaceVersion());
            Assert.Equal("v2.6.11", response.GetPluginVersion());
            Assert.Equal("321-404-842", response.GetSerialNumber());
            Assert.Equal("12348842", response.GetTerminalId());
            Assert.Equal("VX690", response.GetTerminalModel());
        }

        [Fact]
        public void TerminalBatteryInit_OnValidResponse_ReturnObjects()
        {
            // arrange
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""data"":{""battery_level"":""40""},""datetime"":""2019-06-18T13:02:41.777"",""event"":""battery_level_changed"",""id"":""C1.3""}}";

            // act
            var msg = Message.FromJson(jsonStr, secrets);
            var comWrapper = new ComWrapper();
            var response = comWrapper.TerminalBatteryInit(msg);

            // assert
            Assert.Equal("battery_level_changed", msg.EventName);
            Assert.Equal("40", response.BatteryLevel);
        }

        [Fact]
        public void BillPaymentFlowEndedResponseInit_OnValidResponse_ReturnObjects()
        {
            // arrange
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""data"":{""bill_id"":""1554246591041.23"",""bill_outstanding_amount"":1000,""bill_total_amount"":1000,""card_total_amount"":0,""card_total_count"":0,""cash_total_amount"":0,""cash_total_count"":0,""operator_id"":""1"",""table_id"":""1""},""datetime"":""2019-04-03T10:11:21.328"",""event"":""bill_payment_flow_ended"",""id"":""C12.4""}}";

            // act
            var msg = Message.FromJson(jsonStr, secrets);
            var comWrapper = new ComWrapper();
            var response = comWrapper.BillPaymentFlowEndedResponseInit(msg);

            // assert
            Assert.Equal("bill_payment_flow_ended", msg.EventName);
            Assert.Equal("1554246591041.23", response.BillId);
            Assert.Equal(1000, response.BillOutstandingAmount);
            Assert.Equal(1000, response.BillTotalAmount);
            Assert.Equal("1", response.TableId);
            Assert.Equal("1", response.OperatorId);
            Assert.Equal(0, response.CardTotalCount);
            Assert.Equal(0, response.CardTotalAmount);
            Assert.Equal(0, response.CashTotalCount);
            Assert.Equal(0, response.CashTotalAmount);
        }

        [Fact]
        public void GetSchemeSettlementEntries_OnValidResponse_ReturnCount()
        {
            // arrange
            var stlEnqMsg = new SettlementEnquiryRequest(RequestIdHelper.Id("stlenq")).ToMessage();
            var transactionFlowState = new TransactionFlowState("1", TransactionType.SettlementEnquiry, 0, stlEnqMsg, $"Waiting for EFTPOS connection to make a settlement enquiry");
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""data"":{""accumulacxted_purchase_count"":""1"",""accumulated_purchase_value"":""1000"",""accumulated_settle_by_acquirer_count"":""1"",""accumulated_settle_by_acquirer_value"":""1000"",""accumulated_total_count"":""1"",""accumulated_total_value"":""1000"",""bank_date"":""14062019"",""bank_time"":""160940"",""host_response_code"":""941"",""host_response_text"":""CUTOVER COMPLETE"",""merchant_acquirer"":""EFTPOS FROM BANK SA"",""merchant_address"":""213 Miller Street"",""merchant_city"":""Sydney"",""merchant_country"":""Australia"",""merchant_name"":""Merchant4"",""merchant_postcode"":""2060"",""merchant_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\n\r\nAustralia\r\n\r\n\r\n SETTLEMENT CUTOVER\r\nTSP     100612348842\r\nTIME   14JUN19 16:09\r\nTRAN   001137-001137\r\nFROM   13JUN19 20:00\r\nTO     14JUN19 16:09\r\n\r\nDebit\r\nTOT     0      $0.00\r\n\r\nMasterCard\r\nTOT     0      $0.00\r\n\r\nVisa\r\nPUR     1     $10.00\r\nTOT     1     $10.00\r\n\r\nBANKED  1     $10.00\r\n\r\nAmex\r\nTOT     0      $0.00\r\n\r\nDiners\r\nTOT     0      $0.00\r\n\r\nJCB\r\nTOT     0      $0.00\r\n\r\nUnionPay\r\nTOT     0      $0.00\r\n\r\nTOTAL\r\nPUR     1     $10.00\r\nTOT     1     $10.00\r\n\r\n (941) CUTOVER COMP\r\n\r\n\r\n\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""schemes"":[{""scheme_name"":""Debit"",""settle_by_acquirer"":""Yes"",""total_count"":""0"",""total_value"":""0""},{""scheme_name"":""MasterCard"",""settle_by_acquirer"":""Yes"",""total_count"":""0"",""total_value"":""0""},{""scheme_name"":""Visa"",""settle_by_acquirer"":""Yes"",""total_count"":""1"",""total_purchase_count"":""1"",""total_purchase_value"":""1000"",""total_value"":""1000""},{""scheme_name"":""Amex"",""settle_by_acquirer"":""No"",""total_count"":""0"",""total_value"":""0""},{""scheme_name"":""Diners"",""settle_by_acquirer"":""No"",""total_count"":""0"",""total_value"":""0""},{""scheme_name"":""JCB"",""settle_by_acquirer"":""No"",""total_count"":""0"",""total_value"":""0""},{""scheme_name"":""UnionPay"",""settle_by_acquirer"":""No"",""total_count"":""0"",""total_value"":""0""}],""settlement_period_end_date"":""14Jun19"",""settlement_period_end_time"":""16:09"",""settlement_period_start_date"":""13Jun19"",""settlement_period_start_time"":""20:00"",""settlement_triggered_date"":""14Jun19"",""settlement_triggered_time"":""16:09:40"",""stan"":""000000"",""success"":true,""terminal_id"":""100612348842"",""transaction_range"":""001137-001137""},""datetime"":""2019-06-14T16:09:46.395"",""event"":""settle_response"",""id"":""settle116""}}";

            // act
            var msg = Message.FromJson(jsonStr, secrets);
            transactionFlowState.Response = msg;
            var comWrapper = new ComWrapper();
            var schemeArray = comWrapper.GetSchemeSettlementEntries(transactionFlowState);
            var settleResponse = new Settlement(transactionFlowState.Response);
            var schemes = settleResponse.GetSchemeSettlementEntries();
            var schemeList = new List<SchemeSettlementEntry>();
            foreach (var s in schemes)
            {
                schemeList.Add(s);
            }

            // assert
            Assert.Equal(schemeArray.ToList().Count, schemeList.Count);
        }

        [Fact]
        public void GetSpiVersion_OnValidResponse_ReturnObject()
        {
            // act
            var spiVersion = Spi.GetVersion();
            var comWrapper = new ComWrapper();
            var comSpiVersion = comWrapper.GetSpiVersion();

            // assert
            Assert.Equal(spiVersion, comSpiVersion);
        }

        [Fact]
        public void GetPosVersion_OnValidResponse_ReturnObject()
        {
            // act
            var comWrapper = new ComWrapper();
            var comPosVersion = comWrapper.GetPosVersion();

            // assert
            Assert.Equal("0", comPosVersion);
        }

        [Fact]
        public void NewBillId_ValidRequest_ReturnObject()
        {
            // act
            var comWrapper = new ComWrapper();
            var newBillId = comWrapper.NewBillId();

            // assert
            Assert.Equal((DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds.ToString(), newBillId);
        }

    }
}
