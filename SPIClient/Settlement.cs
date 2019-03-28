using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;

namespace SPIClient
{
    /// <summary>
    /// Represents a Request to Settle.
    /// </summary>
    public class SettleRequest
    {
        public string Id { get; }

        internal SpiConfig Config = new SpiConfig();

        internal TransactionOptions Options = new TransactionOptions();

        public SettleRequest(string id)
        {
            Id = id;
        }

        public Message ToMessage()
        {
            var data = new JObject();

            Config.EnabledPrintMerchantCopy = true;
            Config.EnabledPromptForCustomerCopyOnEftpos = false;
            Config.EnabledSignatureFlowOnEftpos = false;
            Config.AddReceiptConfig(data);
            Options.AddOptions(data);
            return new Message(Id, Events.SettleRequest, data, true);
        }
    }

    /// <summary>
    /// Represents a Respsonse for a Request to Settle
    /// These attributes work for COM interop.
    /// </summary>
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class Settlement
    {
        public bool Success { get; }
        public string RequestId { get; }

        private readonly Message _m;

        /// <summary>
        /// This default stucture works for COM interop.
        /// </summary>
        public Settlement() { }

        public Settlement(Message m)
        {
            RequestId = m.Id;
            _m = m;
            Success = m.GetSuccessState() == Message.SuccessState.Success;
        }

        public int GetSettleByAcquirerCount()
        {
            return _m.GetDataIntValue("accumulated_settle_by_acquirer_count");
        }

        public int GetSettleByAcquirerValue()
        {
            return _m.GetDataIntValue("accumulated_settle_by_acquirer_value");
        }

        public int GetTotalCount()
        {
            return _m.GetDataIntValue("accumulated_total_count");
        }

        public int GetTotalValue()
        {
            return _m.GetDataIntValue("accumulated_total_value");
        }

        public DateTime GetPeriodStartTime()
        {
            var timeStr = _m.GetDataStringValue("settlement_period_start_time"); // "05:00"
            var dateStr = _m.GetDataStringValue("settlement_period_start_date"); // "05Oct17"
            return DateTime.ParseExact(timeStr + dateStr, "HH:mmddMMMyy", CultureInfo.InvariantCulture);
        }

        public DateTime GetPeriodEndTime()
        {
            var timeStr = _m.GetDataStringValue("settlement_period_end_time"); // "05:00"
            var dateStr = _m.GetDataStringValue("settlement_period_end_date"); // "05Oct17"
            return DateTime.ParseExact(timeStr + dateStr, "HH:mmddMMMyy", CultureInfo.InvariantCulture);
        }

        public DateTime GetTriggeredTime()
        {
            var timeStr = _m.GetDataStringValue("settlement_triggered_time"); // "05:00:45"
            var dateStr = _m.GetDataStringValue("settlement_triggered_date"); // "05Oct17"
            return DateTime.ParseExact(timeStr + dateStr, "HH:mm:ssddMMMyy", CultureInfo.InvariantCulture);
        }

        public string GetResponseText()
        {
            return _m.GetDataStringValue("host_response_text");
        }

        public string GetReceipt()
        {
            return _m.GetDataStringValue("merchant_receipt");
        }

        public string GetTransactionRange()
        {
            return _m.GetDataStringValue("transaction_range");
        }

        public string GetTerminalId()
        {
            return _m.GetDataStringValue("terminal_id");
        }

        public bool WasMerchantReceiptPrinted()
        {
            return _m.GetDataBoolValue("merchant_receipt_printed", false);
        }

        public IEnumerable<SchemeSettlementEntry> GetSchemeSettlementEntries()
        {
            var found = _m.Data.TryGetValue("schemes", out var schemes);
            if (!found) return new List<SchemeSettlementEntry>();
            return schemes.ToArray().Select(jToken => new SchemeSettlementEntry((JObject)jToken)).ToList();
        }
    }

    /// <summary>
    /// Represents a Respsonse for a Request to Settle
    /// These attributes work for COM interop.
    /// </summary>
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class SchemeSettlementEntry
    {
        public string SchemeName;
        public bool SettleByAcquirer;
        public int TotalCount;
        public int TotalValue;

        /// <summary>
        /// This default stucture works for COM interop.
        /// </summary>
        public SchemeSettlementEntry() { }

        public SchemeSettlementEntry(string schemeName, bool settleByAcquirer, int totalCount, int totalValue)
        {
            SchemeName = schemeName;
            SettleByAcquirer = settleByAcquirer;
            TotalCount = totalCount;
            TotalValue = totalValue;
        }

        public SchemeSettlementEntry(JObject jo)
        {
            SchemeName = (string)jo.GetValue("scheme_name");
            SettleByAcquirer = ((string)jo.GetValue("settle_by_acquirer")).ToLower() == "yes";
            var valueStr = (string)jo.GetValue("total_value");
            int.TryParse(valueStr, out TotalValue);
            var countStr = (string)jo.GetValue("total_count");
            int.TryParse(countStr, out TotalCount);
        }

        public override string ToString()
        {
            return $"{nameof(SchemeName)}: {SchemeName}, {nameof(SettleByAcquirer)}: {SettleByAcquirer}, {nameof(TotalCount)}: {TotalCount}, {nameof(TotalValue)}: {TotalValue}";
        }
    }

    public class SettlementEnquiryRequest
    {
        public string Id { get; }

        internal SpiConfig Config = new SpiConfig();

        internal TransactionOptions Options = new TransactionOptions();

        public SettlementEnquiryRequest(string id)
        {
            Id = id;
        }

        public Message ToMessage()
        {
            var data = new JObject();

            Config.EnabledPrintMerchantCopy = true;
            Config.EnabledPromptForCustomerCopyOnEftpos = false;
            Config.EnabledSignatureFlowOnEftpos = false;
            Config.AddReceiptConfig(data);
            Options.AddOptions(data);
            return new Message(Id, Events.SettlementEnquiryRequest, data, true);
        }
    }
}