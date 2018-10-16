using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SPIClient
{

    /// <summary>
    /// This class represents the BillDetails that the POS will be asked for throughout a PayAtTable flow.
    /// </summary>
    [ComVisible(true)]
    [Guid("F3190296-2734-42FC-B534-27695295D2B0")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class BillStatusResponse
    {
        /// <summary>
        /// This default stucture works for COM interop.
        /// </summary>
        public BillStatusResponse() { }

        /// <summary>
        /// Set this Error accordingly if you are not able to return the BillDetails that were asked from you.
        /// </summary>
        public BillRetrievalResult Result { get; set; }

        /// <summary>
        /// This is a unique identifier that you assign to each bill.
        /// It migt be for example, the timestamp of when the cover was opened.
        /// </summary>
        public string BillId { get; set; }

        /// <summary>
        /// This is the table id that this bill was for.
        /// The waiter will enter it on the Eftpos at the start of the PayAtTable flow and the Eftpos will 
        /// retrieve the bill using the table id. 
        /// </summary>
        public string TableId { get; set; }

        /// <summary>
        /// The Total Amount on this bill, in cents.
        /// </summary>
        public int TotalAmount { get; set; }

        /// <summary>
        /// The currently outsanding amount on this bill, in cents.
        /// </summary>
        public int OutstandingAmount { get; set; }

        /// <summary>
        /// Your POS is required to persist some state on behalf of the Eftpos so the Eftpos can recover state.
        /// It is just a piece of string that you save against your billId.
        /// WHenever you're asked for BillDetails, make sure you return this piece of data if you have it.
        /// </summary>
        public string BillData { get; set; }

        internal List<PaymentHistoryEntry> getBillPaymentHistory()
        {
            if (string.IsNullOrWhiteSpace(BillData))
            {
                return new List<PaymentHistoryEntry>();
            }

            var bdArray = Convert.FromBase64String(BillData);
            var bdStr = Encoding.UTF8.GetString(bdArray);
            var jsonSerializerSettings = new JsonSerializerSettings() {DateParseHandling = DateParseHandling.None};
            return JsonConvert.DeserializeObject<List<PaymentHistoryEntry>>(bdStr, jsonSerializerSettings);
        }


        internal static string ToBillData(List<PaymentHistoryEntry> ph)
        {
            if (ph.Count < 1)
            {
                return "";
            }

            var bphStr = JsonConvert.SerializeObject(ph);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(bphStr));
        }

        public Message ToMessage(string messageId)
        {
            var data = new JObject(
                    new JProperty("success", Result==BillRetrievalResult.SUCCESS)
            );

            if (!string.IsNullOrWhiteSpace(BillId)) data.Add(new JProperty("bill_id", BillId));
            if (!string.IsNullOrWhiteSpace(TableId)) data.Add(new JProperty("table_id", TableId));

            if (Result == BillRetrievalResult.SUCCESS)
            {
                data.Add(new JProperty("bill_total_amount", TotalAmount));
                data.Add(new JProperty("bill_outstanding_amount", OutstandingAmount));
                data.Add(new JProperty("bill_payment_history", JToken.FromObject(getBillPaymentHistory())));
            }
            else
            {
                data.Add(new JProperty("error_reason", Result.ToString()));
                data.Add(new JProperty("error_detail", Result.ToString()));
            }
            return new Message(messageId, Events.PayAtTableBillDetails, data, true);
        }
    }

    public enum BillRetrievalResult
    {
        SUCCESS,
        INVALID_TABLE_ID,
        INVALID_BILL_ID,
        INVALID_OPERATOR_ID
    }

    public enum PaymentType
    {
        CARD,
        CASH
    }

    /// <summary>
    /// These attributes work for COM interop.
    /// </summary>
    [ComVisible(true)]
    [Guid("2CE8031B-EF7E-4C79-8F98-DD0451FA91EA")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class BillPayment
    {
        public string BillId { get;}
        public string TableId { get;}
        public string OperatorId { get;}

        public PaymentType PaymentType { get;}

        public int PurchaseAmount { get; }
        public int TipAmount { get; }

        public PurchaseResponse PurchaseResponse { get; }

        private Message _incomingAdvice;

        /// <summary>
        /// This default stucture works for COM interop.
        /// </summary>
        public BillPayment() { }

        public BillPayment(Message m)
        {
            _incomingAdvice = m;
            BillId = _incomingAdvice.GetDataStringValue("bill_id");
            TableId = _incomingAdvice.GetDataStringValue("table_id");
            OperatorId = _incomingAdvice.GetDataStringValue("operator_id");

            Enum.TryParse(_incomingAdvice.GetDataStringValue("payment_type"), true, out PaymentType pt);
            PaymentType = pt;

            // this is when we ply the sub object "payment_details" into a purchase response for convenience.
            var purchaseMsg = new Message(m.Id, "payment_details", (JObject) m.Data.GetValue("payment_details"), false);
            PurchaseResponse = new PurchaseResponse(purchaseMsg);

            PurchaseAmount = PurchaseResponse.GetPurchaseAmount();
            TipAmount = PurchaseResponse.GetTipAmount();
        }
    }

    internal class PaymentHistoryEntry
    {
        [JsonProperty("payment_type")]
        public string PaymentType;

        [JsonProperty("payment_summary")]
        public JObject PaymentSummary;

        [JsonConstructor()]
        public PaymentHistoryEntry()
        {

        }

        public string GetTerminalRefId()
        {
            var found = PaymentSummary.TryGetValue("terminal_ref_id", out var terminalRefId);
            if (found) return (string)terminalRefId;
            return null;
        }
    }

    /// <summary>
    /// These attributes work for COM interop.
    /// </summary>
    [ComVisible(true)]
    [Guid("4AD0D23D-0C3C-4B3A-A3F7-55DD6F1089CC")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class PayAtTableConfig
    {
        public bool PayAtTabledEnabled { get; set; }

        public bool OperatorIdEnabled { get; set; }

        public bool SplitByAmountEnabled { get; set; }

        public bool EqualSplitEnabled { get; set; }

        public bool TippingEnabled { get; set; }

        public bool SummaryReportEnabled { get; set; }

        public string LabelPayButton { get; set; }

        public string LabelOperatorId { get; set; }

        public string LabelTableId { get; set; }

        // 
        /// <summary>
        /// Fill in with operator ids that the eftpos terminal will validate against. 
        /// Leave Empty to allow any operator_id through. 
        /// </summary>
        public List<string> AllowedOperatorIds { get; set; }

        public Message ToMessage(string messageId)
        {
            var data = new JObject(
                new JProperty("pay_at_table_enabled", PayAtTabledEnabled),
                new JProperty("operator_id_enabled", OperatorIdEnabled),
                new JProperty("split_by_amount_enabled", SplitByAmountEnabled),
                new JProperty("equal_split_enabled", EqualSplitEnabled),
                new JProperty("tipping_enabled", TippingEnabled),
                new JProperty("summary_report_enabled", SummaryReportEnabled),
                new JProperty("pay_button_label", LabelPayButton),
                new JProperty("operator_id_label", LabelOperatorId),
                new JProperty("table_id_label", LabelTableId),
                new JProperty("operator_id_list", AllowedOperatorIds)
            );

            return new Message(messageId, Events.PayAtTableSetTableConfig, data, true);
        }

        internal static Message FeatureDisableMessage(string messageId){
            var data = new JObject(
                new JProperty("pay_at_table_enabled", false)
            );
            return new Message(messageId, Events.PayAtTableSetTableConfig, data, true);
        }

        /// <summary>
        /// This default stucture works for COM interop.
        /// </summary>
        public PayAtTableConfig() { }
    }


}