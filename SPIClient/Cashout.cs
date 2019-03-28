using System;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;

namespace SPIClient
{
    public class CashoutOnlyRequest
    {
        public string PosRefId { get; }

        public int CashoutAmount { get; }

        public int SurchargeAmount { get; set; }

        internal SpiConfig Config = new SpiConfig();

        internal TransactionOptions Options = new TransactionOptions();

        public CashoutOnlyRequest(int amountCents, string posRefId)
        {
            PosRefId = posRefId;
            CashoutAmount = amountCents;
        }

        public Message ToMessage()
        {
            var data = new JObject(
                new JProperty("pos_ref_id", PosRefId),
                new JProperty("cash_amount", CashoutAmount),
                new JProperty("surcharge_amount", SurchargeAmount)
                );

            Config.EnabledPrintMerchantCopy = true;
            Config.EnabledPromptForCustomerCopyOnEftpos = true;
            Config.EnabledSignatureFlowOnEftpos = true;
            Config.AddReceiptConfig(data);
            Options.AddOptions(data);
            return new Message(RequestIdHelper.Id("cshout"), Events.CashoutOnlyRequest, data, true);
        }
    }

    /// <summary>
    /// These attributes work for COM interop.
    /// </summary>
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class CashoutOnlyResponse
    {
        public bool Success { get; }
        public string RequestId { get; }
        public string PosRefId { get; }
        public string SchemeName { get; }

        private readonly Message _m;

        /// <summary>
        /// This default stucture works for COM interop.
        /// </summary>
        public CashoutOnlyResponse() { }

        public CashoutOnlyResponse(Message m)
        {
            _m = m;
            RequestId = _m.Id;
            PosRefId = _m.GetDataStringValue("pos_ref_id");
            SchemeName = _m.GetDataStringValue("scheme_name");
            Success = m.GetSuccessState() == Message.SuccessState.Success;
        }

        public string GetRRN()
        {
            return _m.GetDataStringValue("rrn");
        }

        public int GetCashoutAmount()
        {
            return _m.GetDataIntValue("cash_amount");
        }

        public int GetBankNonCashAmount()
        {
            return _m.GetDataIntValue("bank_noncash_amount");
        }

        public int GetBankCashAmount()
        {
            return _m.GetDataIntValue("bank_cash_amount");
        }

        public string GetCustomerReceipt()
        {
            return _m.GetDataStringValue("customer_receipt");
        }

        public string GetMerchantReceipt()
        {
            return _m.GetDataStringValue("merchant_receipt");
        }

        public string GetResponseText()
        {
            return _m.GetDataStringValue("host_response_text");
        }

        public string GetResponseCode()
        {
            return _m.GetDataStringValue("host_response_code");
        }

        public string GetTerminalReferenceId()
        {
            return _m.GetDataStringValue("terminal_ref_id");
        }

        public string GetAccountType()
        {
            return _m.GetDataStringValue("account_type");
        }

        public string GetAuthCode()
        {
            return _m.GetDataStringValue("auth_code");
        }

        public string GetBankDate()
        {
            return _m.GetDataStringValue("bank_date");
        }

        public string GetBankTime()
        {
            return _m.GetDataStringValue("bank_time");
        }

        public string GetMaskedPan()
        {
            return _m.GetDataStringValue("masked_pan");
        }

        public string GetTerminalId()
        {
            return _m.GetDataStringValue("terminal_id");
        }

        public bool WasMerchantReceiptPrinted()
        {
            return _m.GetDataBoolValue("merchant_receipt_printed", false);
        }

        public bool WasCustomerReceiptPrinted()
        {
            return _m.GetDataBoolValue("customer_receipt_printed", false);
        }

        public int GetSurchargeAmount()
        {
            return _m.GetDataIntValue("surcharge_amount");
        }

        public string GetResponseValue(string attribute)
        {
            return _m.GetDataStringValue(attribute);
        }

    }
}