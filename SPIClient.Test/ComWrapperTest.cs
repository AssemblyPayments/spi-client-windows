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
        private void OnTxFlowStateChanged()
        {
        }

        [Fact]
        public void TestGetOpenTablesCom()
        {
            GetOpenTablesCom getOpenTablesCom = new GetOpenTablesCom();
            OpenTablesEntry openTablesEntry = new OpenTablesEntry();
            openTablesEntry.TableId = "1";
            openTablesEntry.Label = "1";
            openTablesEntry.BillOutstandingAmount = 1000;

            getOpenTablesCom.AddToOpenTablesList(openTablesEntry);
            List<OpenTablesEntry> openTablesEntries = (List<OpenTablesEntry>)SpiClientTestUtils.GetInstanceField(typeof(GetOpenTablesCom), getOpenTablesCom, "OpenTablesList");

            Assert.Equal(openTablesEntries[0].TableId, openTablesEntry.TableId);
            Assert.Equal(openTablesEntries[0].Label, openTablesEntry.Label);
            Assert.Equal(openTablesEntries[0].BillOutstandingAmount, openTablesEntry.BillOutstandingAmount);

            string jsonStr = getOpenTablesCom.ToOpenTablesJson();

            Assert.NotNull(jsonStr);
        }

        [Fact]
        public void TestPurchaseResponseInit()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""data"":{""account_type"":""SAVINGS"",""auth_code"":""278045"",""bank_cash_amount"":200,""bank_date"":""06062019"",""bank_noncash_amount"":1200,""bank_settlement_date"":""06062019"",""bank_time"":""110750"",""card_entry"":""MAG_STRIPE"",""cash_amount"":200,""currency"":""AUD"",""customer_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\nTIME 06JUN19   11:07\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190606001102\r\nDebit(S)         SAV\r\nCARD............5581\r\nAUTH          278045\r\n\r\nPURCHASE    AUD10.00\r\nCASH         AUD2.00\r\nSURCHARGE    AUD2.00\r\nTOTAL       AUD14.00\r\n\r\n   (000) APPROVED\r\n\r\n  *CUSTOMER COPY*\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""customer_receipt_printed"":false,""expiry_date"":""0822"",""host_response_code"":""000"",""host_response_text"":""APPROVED"",""informative_text"":""                "",""masked_pan"":""............5581"",""merchant_acquirer"":""EFTPOS FROM BANK SA"",""merchant_addr"":""213 Miller Street"",""merchant_city"":""Sydney"",""merchant_country"":""Australia"",""merchant_id"":""22341842"",""merchant_name"":""Merchant4"",""merchant_postcode"":""2060"",""merchant_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\nTIME 06JUN19   11:07\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190606001102\r\nDebit(S)         SAV\r\nCARD............5581\r\nAUTH          278045\r\n\r\nPURCHASE    AUD10.00\r\nCASH         AUD2.00\r\nSURCHARGE    AUD2.00\r\nTOTAL       AUD14.00\r\n\r\n   (000) APPROVED\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""merchant_receipt_printed"":false,""online_indicator"":""Y"",""pos_ref_id"":""prchs-06-06-2019-11-07-50"",""purchase_amount"":1000,""rrn"":""190606001102"",""scheme_name"":""Debit"",""stan"":""001102"",""success"":true,""surcharge_amount"":200,""terminal_id"":""100612348842"",""terminal_ref_id"":""12348842_06062019110812"",""transaction_type"":""PURCHASE""},""datetime"":""2019-06-06T11:08:12.946"",""event"":""purchase_response"",""id"":""prchs5""}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            ComWrapper comWrapper = new ComWrapper();
            PurchaseResponse response = comWrapper.PurchaseResponseInit(msg);

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
        }

        [Fact]
        public void TestRefundResponseInit()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""data"":{""account_type"":""CREDIT"",""auth_code"":""067849"",""bank_date"":""06062019"",""bank_noncash_amount"":1000,""bank_settlement_date"":""06062019"",""bank_time"":""114905"",""card_entry"":""EMV_CTLS"",""currency"":""AUD"",""customer_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\nTIME 06JUN19   11:49\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190606001105\r\nVisa(C)           CR\r\nCARD............5581\r\nAUTH          067849\r\n\r\nREFUND      AUD10.00\r\n\r\n   (000) APPROVED\r\n\r\n  *CUSTOMER COPY*\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""customer_receipt_printed"":false,""emv_actioncode"":""ARQ"",""emv_actioncode_values"":""67031BCC5AD15818"",""expiry_date"":""0822"",""host_response_code"":""000"",""host_response_text"":""APPROVED"",""informative_text"":""                "",""masked_pan"":""............5581"",""merchant_acquirer"":""EFTPOS FROM BANK SA"",""merchant_addr"":""213 Miller Street"",""merchant_city"":""Sydney"",""merchant_country"":""Australia"",""merchant_id"":""22341842"",""merchant_name"":""Merchant4"",""merchant_postcode"":""2060"",""merchant_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\nTIME 06JUN19   11:49\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190606001105\r\nVisa(C)           CR\r\nCARD............5581\r\nAUTH          067849\r\n\r\nREFUND      AUD10.00\r\n\r\n   (000) APPROVED\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""merchant_receipt_printed"":false,""online_indicator"":""Y"",""pos_ref_id"":""rfnd-06-06-2019-11-49-05"",""refund_amount"":1000,""rrn"":""190606001105"",""scheme_name"":""Visa"",""stan"":""001105"",""success"":true,""terminal_id"":""100612348842"",""terminal_ref_id"":""12348842_06062019114915"",""transaction_type"":""REFUND""},""datetime"":""2019-06-06T11:49:15.038"",""event"":""refund_response"",""id"":""refund150""}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            ComWrapper comWrapper = new ComWrapper();
            RefundResponse response = comWrapper.RefundResponseInit(msg);

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
        }

        [Fact]
        public void TestSettlementInit()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""data"":{""accumulacxted_purchase_count"":""1"",""accumulated_purchase_value"":""1000"",""accumulated_settle_by_acquirer_count"":""1"",""accumulated_settle_by_acquirer_value"":""1000"",""accumulated_total_count"":""1"",""accumulated_total_value"":""1000"",""bank_date"":""14062019"",""bank_time"":""160940"",""host_response_code"":""941"",""host_response_text"":""CUTOVER COMPLETE"",""merchant_acquirer"":""EFTPOS FROM BANK SA"",""merchant_address"":""213 Miller Street"",""merchant_city"":""Sydney"",""merchant_country"":""Australia"",""merchant_name"":""Merchant4"",""merchant_postcode"":""2060"",""merchant_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\n\r\nAustralia\r\n\r\n\r\n SETTLEMENT CUTOVER\r\nTSP     100612348842\r\nTIME   14JUN19 16:09\r\nTRAN   001137-001137\r\nFROM   13JUN19 20:00\r\nTO     14JUN19 16:09\r\n\r\nDebit\r\nTOT     0      $0.00\r\n\r\nMasterCard\r\nTOT     0      $0.00\r\n\r\nVisa\r\nPUR     1     $10.00\r\nTOT     1     $10.00\r\n\r\nBANKED  1     $10.00\r\n\r\nAmex\r\nTOT     0      $0.00\r\n\r\nDiners\r\nTOT     0      $0.00\r\n\r\nJCB\r\nTOT     0      $0.00\r\n\r\nUnionPay\r\nTOT     0      $0.00\r\n\r\nTOTAL\r\nPUR     1     $10.00\r\nTOT     1     $10.00\r\n\r\n (941) CUTOVER COMP\r\n\r\n\r\n\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""schemes"":[{""scheme_name"":""Debit"",""settle_by_acquirer"":""Yes"",""total_count"":""0"",""total_value"":""0""},{""scheme_name"":""MasterCard"",""settle_by_acquirer"":""Yes"",""total_count"":""0"",""total_value"":""0""},{""scheme_name"":""Visa"",""settle_by_acquirer"":""Yes"",""total_count"":""1"",""total_purchase_count"":""1"",""total_purchase_value"":""1000"",""total_value"":""1000""},{""scheme_name"":""Amex"",""settle_by_acquirer"":""No"",""total_count"":""0"",""total_value"":""0""},{""scheme_name"":""Diners"",""settle_by_acquirer"":""No"",""total_count"":""0"",""total_value"":""0""},{""scheme_name"":""JCB"",""settle_by_acquirer"":""No"",""total_count"":""0"",""total_value"":""0""},{""scheme_name"":""UnionPay"",""settle_by_acquirer"":""No"",""total_count"":""0"",""total_value"":""0""}],""settlement_period_end_date"":""14Jun19"",""settlement_period_end_time"":""16:09"",""settlement_period_start_date"":""13Jun19"",""settlement_period_start_time"":""20:00"",""settlement_triggered_date"":""14Jun19"",""settlement_triggered_time"":""16:09:40"",""stan"":""000000"",""success"":true,""terminal_id"":""100612348842"",""transaction_range"":""001137-001137""},""datetime"":""2019-06-14T16:09:46.395"",""event"":""settle_response"",""id"":""settle116""}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            ComWrapper comWrapper = new ComWrapper();
            Settlement response = comWrapper.SettlementInit(msg);

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
        }

        [Fact]
        public void TestSecretsInit()
        {
            string encKey = "81CF9E6A14CDAF244A30B298D4CECB505C730CE352C6AF6E1DE61B3232E24D3F";
            string hmacKey = "D35060723C9EECDB8AEA019581381CB08F64469FC61A5A04FE553EBDB5CD55B9";

            ComWrapper comWrapper = new ComWrapper();
            Secrets secrets = comWrapper.SecretsInit(encKey, hmacKey);

            Assert.Equal(encKey, secrets.EncKey);
            Assert.Equal(hmacKey, secrets.HmacKey);
        }

        [Fact]
        public void TestGetLastTransactionInit()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""data"":{""account_type"":""CREDIT"",""auth_code"":""139059"",""bank_date"":""14062019"",""bank_noncash_amount"":1000,""bank_settlement_date"":""14062019"",""bank_time"":""153747"",""card_entry"":""EMV_CTLS"",""currency"":""AUD"",""customer_receipt"":"""",""customer_receipt_printed"":false,""emv_actioncode"":""ARP"",""emv_actioncode_values"":""9BDDE227547B41F43030"",""emv_pix"":""1010"",""emv_rid"":""A000000003"",""emv_tsi"":""0000"",""emv_tvr"":""0000000000"",""expiry_date"":""1122"",""host_response_code"":""000"",""host_response_text"":""APPROVED"",""informative_text"":""                "",""masked_pan"":""............3952"",""merchant_acquirer"":""EFTPOS FROM BANK SA"",""merchant_addr"":""213 Miller Street"",""merchant_city"":""Sydney"",""merchant_country"":""Australia"",""merchant_id"":""22341842"",""merchant_name"":""Merchant4"",""merchant_postcode"":""2060"",""merchant_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\nTIME 14JUN19   15:37\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190614001137\r\nVisa Credit     \r\nVisa(C)           CR\r\nCARD............3952\r\nAID   A0000000031010\r\nTVR       0000000000\r\nAUTH          139059\r\n\r\nPURCHASE    AUD10.00\r\n\r\n   (000) APPROVED\r\n\r\n*DUPLICATE  RECEIPT*\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""merchant_receipt_printed"":false,""online_indicator"":""Y"",""pos_ref_id"":""prchs-14-06-2019-15-37-49"",""purchase_amount"":1000,""rrn"":""190614001137"",""scheme_app_name"":""Visa Credit"",""scheme_name"":""Visa"",""stan"":""001137"",""success"":true,""terminal_id"":""100612348842"",""terminal_ref_id"":""12348842_14062019153831"",""transaction_type"":""PURCHASE""},""datetime"":""2019-06-14T15:38:31.620"",""event"":""last_transaction"",""id"":""glt10""}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            ComWrapper comWrapper = new ComWrapper();
            GetLastTransactionResponse response = comWrapper.GetLastTransactionResponseInit(msg);

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
        }

        [Fact]
        public void TestCashoutOnlyResponseInit()
        {
            string jsonStr = @"{""message"": {""data"":{""account_type"":""SAVINGS"",""auth_code"":""265035"",""bank_cash_amount"":1200,""bank_date"":""17062018"",""bank_settlement_date"":""18062018"",""bank_time"":""170950"",""card_entry"":""EMV_INSERT"",""cash_amount"":1200,""currency"":""AUD"",""customer_receipt"":""EFTPOS FROM WESTPAC\\r\\nMerchant4\\r\\n213 Miller Street\\r\\nSydney 2060\\r\\nAustralia\\r\\n\\r\\nTIME 17JUN18   17:09\\r\\nMID         22341845\\r\\nTSP     100312348845\\r\\nRRN     180617000151\\r\\nDebit(I)         SAV\\r\\nCARD............2797\\r\\nAUTH          265035\\r\\n\\r\\nCASH        AUD10.00\\r\\nSURCHARGE    AUD2.00\\r\\nTOTAL       AUD12.00\\r\\n\\r\\n   (000) APPROVED\\r\\n\\r\\n  *CUSTOMER COPY*\\r\\n\\r\\n\\r\\n\\r\\n\\r\\n\\r\\n\\r\\n\\r\\n"",""customer_receipt_printed"":true,""expiry_date"":""0722"",""host_response_code"":""000"",""host_response_text"":""APPROVED"",""informative_text"":""                "",""masked_pan"":""............2797"",""merchant_acquirer"":""EFTPOS FROM WESTPAC"",""merchant_addr"":""213 Miller Street"",""merchant_city"":""Sydney"",""merchant_country"":""Australia"",""merchant_id"":""22341845"",""merchant_name"":""Merchant4"",""merchant_postcode"":""2060"",""merchant_receipt"":""EFTPOS FROM WESTPAC\\r\\nMerchant4\\r\\n213 Miller Street\\r\\nSydney 2060\\r\\nAustralia\\r\\n\\r\\nTIME 17JUN18   17:09\\r\\nMID         22341845\\r\\nTSP     100312348845\\r\\nRRN     180617000151\\r\\nDebit(I)         SAV\\r\\nCARD............2797\\r\\nAUTH          265035\\r\\n\\r\\nCASH        AUD10.00\\r\\nSURCHARGE    AUD2.00\\r\\nTOTAL       AUD12.00\\r\\n\\r\\n   (000) APPROVED\\r\\n\\r\\n\\r\\n\\r\\n\\r\\n\\r\\n\\r\\n"",""merchant_receipt_printed"":true,""online_indicator"":""Y"",""pos_ref_id"":""launder-18-06-2018-03-09-17"",""rrn"":""180617000151"",""scheme_name"":""Debit"",""stan"":""000151"",""success"":true,""surcharge_amount"":200,""terminal_id"":""100312348845"",""terminal_ref_id"":""12348845_18062018031010"",""transaction_type"":""CASH""},""datetime"":""2018-06-18T03:10:10.580"",""event"":""cash_response"",""id"":""cshout4""}}";

            Message msg = Message.FromJson(jsonStr, null);
            ComWrapper comWrapper = new ComWrapper();
            CashoutOnlyResponse response = comWrapper.CashoutOnlyResponseInit(msg);

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
        }

        [Fact]
        public void TestMotoPurchaseResponseInit()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""data"":{""pos_ref_id"":""a-zA-Z0-9"",""account_type"": ""CREDIT"",""purchase_amount"": 1000,""surcharge_amount"": 200,""bank_noncash_amount"": 1200,""bank_cash_amount"": 200,""auth_code"": ""653230"",""bank_date"": ""07092017"",""bank_time"": ""152137"",""bank_settlement_date"": ""21102017"",""currency"": ""AUD"",""emv_actioncode"": """",""emv_actioncode_values"": """",""emv_pix"": """",""emv_rid"": """",""emv_tsi"": """",""emv_tvr"": """",""expiry_date"": ""1117"",""host_response_code"": ""000"",""host_response_text"": ""APPROVED"",""informative_text"": ""                "",""masked_pan"": ""............0794"",""merchant_acquirer"": ""EFTPOS FROM WESTPAC"",""merchant_addr"": ""275 Kent St"",""merchant_city"": ""Sydney"",""merchant_country"": ""Australia"",""merchant_id"": ""02447508"",""merchant_name"": ""VAAS Product 4"",""merchant_postcode"": ""2000"",""online_indicator"": ""Y"",""scheme_app_name"": """",""scheme_name"": """",""stan"": ""000212"",""rrn"": ""1517890741"",""success"": true,""terminal_id"": ""100381990118"",""transaction_type"": ""MOTO"",""card_entry"": ""MANUAL_PHONE"",""customer_receipt"":""EFTPOS FROM WESTPAC\r\nVAAS Product 4\r\n275 Kent St\r\nSydney\r\nMID02447508\r\nTSP100381990118\r\nTIME 07SEP17   15:21\r\nRRN     1517890741\r\nTRAN 000212   CREDIT\r\nVisa Credit     \r\nVisa               M\r\nCARD............0794\r\nAUTH          653230\r\n\r\nMOTO   AUD10000\r\n\r\nTOTAL      AUD10000\r\n\r\n\r\n(000)APPROVED\r\n\r\n\r\n *CUSTOMER COPY*\r\n\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""merchant_receipt"":""EFTPOS FROM WESTPAC\r\nVAAS Product4\r\n275 Kent St\r\nSydney\r\nMID02447508\r\nTSP100381990118\r\nTIME 07SEP17   15:21\r\nRRN     1517890741\r\nTRAN 000212   CREDIT\r\nVisa Credit     \r\nVisa               M\r\nCARD............0794\r\nAUTH          653230\r\n\r\nPURCHASE   AUD10000\r\n\r\nTOTAL      AUD10000\r\n\r\n\r\n(000) APPROVED\r\n\r\n\r\n\r\n\r\n\r\n\r\n\r\n""},""datetime"": ""2018-02-06T04:19:00.545"",""event"": ""moto_purchase_response"",""id"": ""4""}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            ComWrapper comWrapper = new ComWrapper();
            MotoPurchaseResponse response = comWrapper.MotoPurchaseResponseInit(msg);

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
        }

        [Fact]
        public void TestPreautResponseInit()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""data"":{""account_type"":""CREDIT"",""auth_code"":""318981"",""bank_date"":""11062019"",""bank_noncash_amount"":1000,""bank_settlement_date"":""11062019"",""bank_time"":""182808"",""card_entry"":""EMV_INSERT"",""currency"":""AUD"",""customer_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\nTIME 11JUN19   18:28\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190611001110\r\nVisa Credit     \r\nVisa(I)           CR\r\nCARD............3952\r\nAID   A0000000031010\r\nTVR       0080048000\r\nAUTH          318981\r\nPRE-AUTH ID 15765372\r\n\r\nPRE-AUTH    AUD10.00\r\n\r\n   (000) APPROVED\r\n\r\n  *CUSTOMER COPY*\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""customer_receipt_printed"":false,""emv_actioncode"":""TC"",""emv_actioncode_values"":""C0A8342DF36207F1"",""emv_pix"":""1010"",""emv_rid"":""A000000003"",""emv_tsi"":""F800"",""emv_tvr"":""0080048000"",""expiry_date"":""1122"",""host_response_code"":""000"",""host_response_text"":""APPROVED"",""informative_text"":""                "",""masked_pan"":""............3952"",""merchant_acquirer"":""EFTPOS FROM BANK SA"",""merchant_addr"":""213 Miller Street"",""merchant_city"":""Sydney"",""merchant_country"":""Australia"",""merchant_id"":""22341842"",""merchant_name"":""Merchant4"",""merchant_postcode"":""2060"",""merchant_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\nTIME 11JUN19   18:28\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190611001110\r\nVisa Credit     \r\nVisa(I)           CR\r\nCARD............3952\r\nAID   A0000000031010\r\nTVR       0080048000\r\nAUTH          318981\r\nPRE-AUTH ID 15765372\r\n\r\nPRE-AUTH    AUD10.00\r\n\r\n   (000) APPROVED\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""merchant_receipt_printed"":false,""online_indicator"":""Y"",""pos_ref_id"":""propen-11-06-2019-18-28-08"",""preauth_amount"":1000,""preauth_id"":""15765372"",""rrn"":""190611001110"",""scheme_app_name"":""Visa Credit"",""scheme_name"":""Visa"",""stan"":""001110"",""success"":true,""terminal_id"":""100612348842"",""terminal_ref_id"":""12348842_11062019182827"",""transaction_type"":""PRE-AUTH""},""datetime"":""2019-06-11T18:28:27.237"",""event"":""preauth_response"",""id"":""prac17""}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            ComWrapper comWrapper = new ComWrapper();
            PreauthResponse response = comWrapper.PreauthResponseInit(msg);

            Assert.Equal(msg.EventName, "preauth_response");
            Assert.Equal(response.PreauthId, "15765372");
            Assert.Equal(response.PosRefId, "propen-11-06-2019-18-28-08");
            Assert.Equal(response.GetCompletionAmount(), 0);
            Assert.Equal(response.GetBalanceAmount(), 1000);
            Assert.Equal(response.GetPreviousBalanceAmount(), 0);
            Assert.Equal(response.GetSurchargeAmount(), 0);
            Assert.True(response.Details.Success);
            Assert.Equal(response.Details.RequestId, "prac17");
            Assert.Equal(response.Details.SchemeName, "Visa");
            Assert.Equal(response.Details.SchemeAppName, "Visa");
            Assert.Equal(response.Details.GetRRN(), "190611001110");
            Assert.NotNull(response.Details.GetCustomerReceipt());
            Assert.NotNull(response.Details.GetMerchantReceipt());
            Assert.Equal(response.Details.GetResponseText(), "APPROVED");
            Assert.Equal(response.Details.GetResponseCode(), "000");
            Assert.Equal(response.Details.GetCardEntry(), "EMV_INSERT");
            Assert.Equal(response.Details.GetAccountType(), "CREDIT");
            Assert.Equal(response.Details.GetAuthCode(), "318981");
            Assert.Equal(response.Details.GetBankDate(), "11062019");
            Assert.Equal(response.Details.GetBankTime(), "182808");
            Assert.Equal(response.Details.GetMaskedPan(), "............3952");
            Assert.Equal(response.Details.GetTerminalId(), "100612348842");
            Assert.Equal(response.Details.GetTerminalReferenceId(), "12348842_11062019182827");
            Assert.False(response.WasCustomerReceiptPrinted());
            Assert.False(response.WasMerchantReceiptPrinted());
            Assert.Equal(response.Details.GetSettlementDate(), DateTime.ParseExact(msg.GetDataStringValue("bank_settlement_date"), "ddMMyyyy", CultureInfo.InvariantCulture).Date);
        }

        [Fact]
        public void TestAccountVerifyResponseInit()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""data"":{""account_type"":""CREDIT"",""auth_code"":""316810"",""bank_date"":""11062019"",""bank_settlement_date"":""11062019"",""bank_time"":""182739"",""card_entry"":""EMV_INSERT"",""currency"":""AUD"",""customer_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\nTIME 11JUN19   18:27\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190611001109\r\nVisa Credit     \r\nVisa(I)           CR\r\nCARD............3952\r\nAID   A0000000031010\r\nTVR       0080048000\r\nAUTH          316810\r\n\r\nA/C VERIFIED AUD0.00\r\n\r\n   (000) APPROVED\r\n\r\n  *CUSTOMER COPY*\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""customer_receipt_printed"":false,""emv_actioncode"":""TC"",""emv_actioncode_values"":""F1F17B37A5BEF2B1"",""emv_pix"":""1010"",""emv_rid"":""A000000003"",""emv_tsi"":""F800"",""emv_tvr"":""0080048000"",""expiry_date"":""1122"",""host_response_code"":""000"",""host_response_text"":""APPROVED"",""informative_text"":""                "",""masked_pan"":""............3952"",""merchant_acquirer"":""EFTPOS FROM BANK SA"",""merchant_addr"":""213 Miller Street"",""merchant_city"":""Sydney"",""merchant_country"":""Australia"",""merchant_id"":""22341842"",""merchant_name"":""Merchant4"",""merchant_postcode"":""2060"",""merchant_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\nAustralia\r\n\r\nTIME 11JUN19   18:27\r\nMID         22341842\r\nTSP     100612348842\r\nRRN     190611001109\r\nVisa Credit     \r\nVisa(I)           CR\r\nCARD............3952\r\nAID   A0000000031010\r\nTVR       0080048000\r\nAUTH          316810\r\n\r\nA/C VERIFIED AUD0.00\r\n\r\n   (000) APPROVED\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""merchant_receipt_printed"":false,""online_indicator"":""Y"",""pos_ref_id"":""actvfy-11-06-2019-18-27-39"",""rrn"":""190611001109"",""scheme_app_name"":""Visa Credit"",""scheme_name"":""Visa"",""stan"":""001109"",""success"":true,""terminal_id"":""100612348842"",""terminal_ref_id"":""12348842_11062019182754"",""transaction_type"":""A/C VERIFIED""},""datetime"":""2019-06-11T18:27:54.933"",""event"":""account_verify_response"",""id"":""prav15""}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            ComWrapper comWrapper = new ComWrapper();
            AccountVerifyResponse response = comWrapper.AccountVerifyResponseInit(msg);

            Assert.Equal(msg.EventName, "account_verify_response");
            Assert.Equal(response.PosRefId, "actvfy-11-06-2019-18-27-39");
            Assert.True(response.Details.Success);
            Assert.Equal(response.Details.RequestId, "prav15");
            Assert.Equal(response.Details.SchemeName, "Visa");
            Assert.Equal(response.Details.GetRRN(), "190611001109");
            Assert.NotNull(response.Details.GetCustomerReceipt());
            Assert.NotNull(response.Details.GetMerchantReceipt());
            Assert.Equal(response.Details.GetResponseText(), "APPROVED");
            Assert.Equal(response.Details.GetResponseCode(), "000");
            Assert.Equal(response.Details.GetCardEntry(), "EMV_INSERT");
            Assert.Equal(response.Details.GetAccountType(), "CREDIT");
            Assert.Equal(response.Details.GetAuthCode(), "316810");
            Assert.Equal(response.Details.GetBankDate(), "11062019");
            Assert.Equal(response.Details.GetBankTime(), "182739");
            Assert.Equal(response.Details.GetMaskedPan(), "............3952");
            Assert.Equal(response.Details.GetTerminalId(), "100612348842");
            Assert.False(response.Details.WasCustomerReceiptPrinted());
            Assert.False(response.Details.WasMerchantReceiptPrinted());
            Assert.Equal(response.Details.GetSettlementDate(), DateTime.ParseExact(msg.GetDataStringValue("bank_settlement_date"), "ddMMyyyy", CultureInfo.InvariantCulture).Date);
        }

        [Fact]
        public void TestPrintingResponseInit()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""data"":{""success"":true},""datetime"":""2019-06-14T18:51:00.948"",""event"":""print_response"",""id"":""C24.0""}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            ComWrapper comWrapper = new ComWrapper();
            PrintingResponse response = comWrapper.PrintingResponseInit(msg);

            Assert.Equal(msg.EventName, "print_response");
            Assert.True(response.IsSuccess());
            Assert.Equal(msg.Id, "C24.0");
            Assert.Equal(response.GetErrorReason(), "");
            Assert.Equal(response.GetErrorDetail(), "");
            Assert.Equal(response.GetResponseValueWithAttribute("error_detail"), "");
        }

        [Fact]
        public void TestTerminalStatusResponseInit()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""data"":{""battery_level"":""100"",""charging"":true,""status"":""IDLE"",""success"":true},""datetime"":""2019-06-18T13:00:38.820"",""event"":""terminal_status"",""id"":""trmnl4""}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            ComWrapper comWrapper = new ComWrapper();
            TerminalStatusResponse response = comWrapper.TerminalStatusResponseInit(msg);

            Assert.Equal(msg.EventName, "terminal_status");
            Assert.True(response.isSuccess());
            Assert.Equal(response.GetBatteryLevel(), "100");
            Assert.Equal(response.GetStatus(), "IDLE");
            Assert.True(response.IsCharging());
        }

        [Fact]
        public void TestTerminalConfigurationResponseInit()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""data"":{""comms_selected"":""WIFI"",""merchant_id"":""22341842"",""pa_version"":""SoftPay03.16.03"",""payment_interface_version"":""02.02.00"",""plugin_version"":""v2.6.11"",""serial_number"":""321-404-842"",""success"":true,""terminal_id"":""12348842"",""terminal_model"":""VX690""},""datetime"":""2019-06-18T13:00:41.075"",""event"":""terminal_configuration"",""id"":""trmnlcnfg5""}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            ComWrapper comWrapper = new ComWrapper();
            TerminalConfigurationResponse response = comWrapper.TerminalConfigurationResponseInit(msg);

            Assert.Equal(msg.EventName, "terminal_configuration");
            Assert.True(response.isSuccess());
            Assert.Equal(response.GetCommsSelected(), "WIFI");
            Assert.Equal(response.GetMerchantId(), "22341842");
            Assert.Equal(response.GetPAVersion(), "SoftPay03.16.03");
            Assert.Equal(response.GetPaymentInterfaceVersion(), "02.02.00");
            Assert.Equal(response.GetPluginVersion(), "v2.6.11");
            Assert.Equal(response.GetSerialNumber(), "321-404-842");
            Assert.Equal(response.GetTerminalId(), "12348842");
            Assert.Equal(response.GetTerminalModel(), "VX690");
        }

        [Fact]
        public void TestTerminalBatteryInit()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""data"":{""battery_level"":""40""},""datetime"":""2019-06-18T13:02:41.777"",""event"":""battery_level_changed"",""id"":""C1.3""}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            ComWrapper comWrapper = new ComWrapper();
            TerminalBattery response = comWrapper.TerminalBatteryInit(msg);

            Assert.Equal(msg.EventName, "battery_level_changed");
            Assert.Equal(response.BatteryLevel, "40");
        }

        [Fact]
        public void TestBillPaymentFlowEndedResponseInit()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""data"":{""bill_id"":""1554246591041.23"",""bill_outstanding_amount"":1000,""bill_total_amount"":1000,""card_total_amount"":0,""card_total_count"":0,""cash_total_amount"":0,""cash_total_count"":0,""operator_id"":""1"",""table_id"":""1""},""datetime"":""2019-04-03T10:11:21.328"",""event"":""bill_payment_flow_ended"",""id"":""C12.4""}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            ComWrapper comWrapper = new ComWrapper();
            BillPaymentFlowEndedResponse response = comWrapper.BillPaymentFlowEndedResponseInit(msg);

            Assert.Equal(msg.EventName, "bill_payment_flow_ended");
            Assert.Equal(response.BillId, "1554246591041.23");
            Assert.Equal(response.BillOutstandingAmount, 1000);
            Assert.Equal(response.BillTotalAmount, 1000);
            Assert.Equal(response.TableId, "1");
            Assert.Equal(response.OperatorId, "1");
            Assert.Equal(response.CardTotalCount, 0);
            Assert.Equal(response.CardTotalAmount, 0);
            Assert.Equal(response.CashTotalCount, 0);
            Assert.Equal(response.CashTotalAmount, 0);
        }

        [Fact]
        public void TestGetSchemeSettlementEntries()
        {
            var stlEnqMsg = new SettlementEnquiryRequest(RequestIdHelper.Id("stlenq")).ToMessage();
            var transactionFlowState = new TransactionFlowState("1", TransactionType.SettlementEnquiry, 0, stlEnqMsg, $"Waiting for EFTPOS connection to make a settlement enquiry");

            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""data"":{""accumulacxted_purchase_count"":""1"",""accumulated_purchase_value"":""1000"",""accumulated_settle_by_acquirer_count"":""1"",""accumulated_settle_by_acquirer_value"":""1000"",""accumulated_total_count"":""1"",""accumulated_total_value"":""1000"",""bank_date"":""14062019"",""bank_time"":""160940"",""host_response_code"":""941"",""host_response_text"":""CUTOVER COMPLETE"",""merchant_acquirer"":""EFTPOS FROM BANK SA"",""merchant_address"":""213 Miller Street"",""merchant_city"":""Sydney"",""merchant_country"":""Australia"",""merchant_name"":""Merchant4"",""merchant_postcode"":""2060"",""merchant_receipt"":""EFTPOS FROM BANK SA\r\nMerchant4\r\n213 Miller Street\r\nSydney 2060\r\n\r\nAustralia\r\n\r\n\r\n SETTLEMENT CUTOVER\r\nTSP     100612348842\r\nTIME   14JUN19 16:09\r\nTRAN   001137-001137\r\nFROM   13JUN19 20:00\r\nTO     14JUN19 16:09\r\n\r\nDebit\r\nTOT     0      $0.00\r\n\r\nMasterCard\r\nTOT     0      $0.00\r\n\r\nVisa\r\nPUR     1     $10.00\r\nTOT     1     $10.00\r\n\r\nBANKED  1     $10.00\r\n\r\nAmex\r\nTOT     0      $0.00\r\n\r\nDiners\r\nTOT     0      $0.00\r\n\r\nJCB\r\nTOT     0      $0.00\r\n\r\nUnionPay\r\nTOT     0      $0.00\r\n\r\nTOTAL\r\nPUR     1     $10.00\r\nTOT     1     $10.00\r\n\r\n (941) CUTOVER COMP\r\n\r\n\r\n\r\n\r\n\r\n\r\n\r\n\r\n\r\n"",""schemes"":[{""scheme_name"":""Debit"",""settle_by_acquirer"":""Yes"",""total_count"":""0"",""total_value"":""0""},{""scheme_name"":""MasterCard"",""settle_by_acquirer"":""Yes"",""total_count"":""0"",""total_value"":""0""},{""scheme_name"":""Visa"",""settle_by_acquirer"":""Yes"",""total_count"":""1"",""total_purchase_count"":""1"",""total_purchase_value"":""1000"",""total_value"":""1000""},{""scheme_name"":""Amex"",""settle_by_acquirer"":""No"",""total_count"":""0"",""total_value"":""0""},{""scheme_name"":""Diners"",""settle_by_acquirer"":""No"",""total_count"":""0"",""total_value"":""0""},{""scheme_name"":""JCB"",""settle_by_acquirer"":""No"",""total_count"":""0"",""total_value"":""0""},{""scheme_name"":""UnionPay"",""settle_by_acquirer"":""No"",""total_count"":""0"",""total_value"":""0""}],""settlement_period_end_date"":""14Jun19"",""settlement_period_end_time"":""16:09"",""settlement_period_start_date"":""13Jun19"",""settlement_period_start_time"":""20:00"",""settlement_triggered_date"":""14Jun19"",""settlement_triggered_time"":""16:09:40"",""stan"":""000000"",""success"":true,""terminal_id"":""100612348842"",""transaction_range"":""001137-001137""},""datetime"":""2019-06-14T16:09:46.395"",""event"":""settle_response"",""id"":""settle116""}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            transactionFlowState.Response = msg;

            ComWrapper comWrapper = new ComWrapper();
            var schemeArray = comWrapper.GetSchemeSettlementEntries(transactionFlowState);

            var settleResponse = new Settlement(transactionFlowState.Response);
            var schemes = settleResponse.GetSchemeSettlementEntries();
            var schemeList = new List<SchemeSettlementEntry>();
            foreach (var s in schemes)
            {
                schemeList.Add(s);
            }

            Assert.Equal(schemeArray.ToList().Count, schemeList.Count);
        }

        [Fact]
        public void TestGetSpiVersion()
        {
            var spiVersion = Spi.GetVersion();
            ComWrapper comWrapper = new ComWrapper();
            var comSpiVersion = comWrapper.GetSpiVersion();

            Assert.Equal(spiVersion, comSpiVersion);
        }

        [Fact]
        public void TestGetPosVersion()
        {
            ComWrapper comWrapper = new ComWrapper();
            var comPosVersion = comWrapper.GetPosVersion();

            Assert.Equal(comPosVersion, "0");
        }

        [Fact]
        public void TestNewBillId()
        {
            ComWrapper comWrapper = new ComWrapper();
            var newBillId = comWrapper.NewBillId();

            Assert.Equal((DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds.ToString(), newBillId);
        }

    }
}
