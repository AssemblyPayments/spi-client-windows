using System;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;

namespace SPIClient
{
    /// <summary>
    /// Represents the 3 Pairing statuses that the Spi instanxce can be in.
    /// </summary>
    public enum SpiStatus
    {
        /// <summary>
        /// Paired and Connected
        /// </summary>
        PairedConnected,

        /// <summary>
        /// Paired but trying to establish a connection 
        /// </summary>
        PairedConnecting,

        /// <summary>
        /// Unpaired
        /// </summary>
        Unpaired
    };

    /// <summary>
    /// These attributes work for COM interop.
    /// </summary>
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class SpiStatusEventArgs : EventArgs
    {
        /// <summary>
        /// This default stucture works for COM interop.
        /// </summary>
        public SpiStatusEventArgs() { }

        public SpiStatus SpiStatus { get; internal set; }
    }

    /// <summary>
    /// The Spi instance can be in one of these flows at any point in time.
    /// </summary>
    public enum SpiFlow
    {
        /// <summary>
        /// Currently going through the Pairing Process Flow.
        /// Happens during the Unpaired SpiStatus.
        /// </summary>
        Pairing,

        /// <summary>
        /// Currently going through the transaction Process Flow.
        /// Cannot happen in the Unpaired SpiStatus.
        /// </summary>
        Transaction,

        /// <summary>
        /// Not in any of the other states.
        /// </summary>
        Idle
    }

    /// <summary>
    /// Represents the Pairing Flow State during the pairing process 
    /// These attributes work for COM interop.
    /// </summary>
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class PairingFlowState : EventArgs
    {
        /// <summary>
        /// This default stucture works for COM interop.
        /// </summary>
        public PairingFlowState() { }

        /// <summary>
        /// Some text that can be displayed in the Pairing Process Screen
        /// that indicates what the pairing process is up to.
        /// </summary>
        public string Message { get; internal set; }

        /// <summary>
        /// When true, it means that the EFTPOS is shoing the confirmation code,
        /// and your user needs to press YES or NO on the EFTPOS.
        /// </summary>
        public bool AwaitingCheckFromEftpos { get; internal set; }

        /// <summary>
        /// When true, you need to display the YES/NO buttons on you pairing screen
        /// for your user to confirm the code.
        /// </summary>
        public bool AwaitingCheckFromPos { get; internal set; }

        /// <summary>
        /// This is the confirmation code for the pairing process.
        /// </summary>
        public string ConfirmationCode { get; internal set; }

        /// <summary>
        /// Indicates whether the Pairing Flow has finished its job.
        /// </summary>
        public bool Finished { get; internal set; }

        /// <summary>
        /// Indicates whether pairing was successful or not.
        /// </summary>
        public bool Successful { get; internal set; }
    }

    public enum TransactionType
    {
        AccountVerify,
        CashoutOnly,
        GetLastTransaction,
        GetTransaction,
        MOTO,
        Preauth,
        Purchase,
        Refund,
        Reversal,
        Settle,
        SettlementEnquiry
    }

    /// <summary>
    /// Used as a return in the InitiateTx methods to signify whether 
    /// the transaction was initiated or not, and a reason to go with it.
    /// These attributes work for COM interop.
    /// </summary>
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class InitiateTxResult
    {
        /// <summary>
        /// Whether the tx was initiated.
        /// When true, you can expect updated to your registered callback.
        /// When false, you can retry calling the InitiateX method.
        /// </summary>
        public bool Initiated { get; internal set; }

        /// <summary>
        /// Text that gives reason for the Initiated flag, especially in case of false. 
        /// </summary>
        public string Message { get; internal set; }

        /// <summary>
        /// This default stucture works for COM interop.
        /// </summary>
        public InitiateTxResult() { }

        public InitiateTxResult(bool initiated, string message)
        {
            Initiated = initiated;
            Message = message;
        }
    }

    /// <summary>
    /// Used as a return in calls mid transaction to let you know
    /// whether the call was valid or not.
    /// These attributes work for COM interop.
    /// </summary>
     [ClassInterface(ClassInterfaceType.AutoDual)]
    public class MidTxResult
    {
        /// <summary>
        /// Whether your call was valid in ythe current state.
        /// When true, you can expect updated to your registered callback.
        /// When false, typically you have made the call when it was not being waited on.
        /// </summary>
        public bool Valid { get; internal set; }

        /// <summary>
        /// Text that gives reason for the Valid flag, especially in case of false. 
        /// </summary>
        public string Message { get; internal set; }

        /// <summary>
        /// This default stucture works for COM interop.
        /// </summary>
        public MidTxResult() { }

        public MidTxResult(bool valid, string message)
        {
            Valid = valid;
            Message = message;
        }
    }

    /// <summary>
    /// Represents the State during a TransactionFlow
    /// These attributes work for COM interop.
    /// </summary>
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class TransactionFlowState : EventArgs
    {
        /// <summary>
        /// This default stucture works for COM interop.
        /// </summary>
        public TransactionFlowState() { }

        /// <summary>
        ///  The id given to this transaction
        /// </summary>
        public string PosRefId { get; internal set; }

        /// <summary>
        /// Purchase/Refund/Settle/...
        /// </summary>
        public TransactionType Type { get; internal set; }

        /// <summary>
        /// A text message to display on your Transaction Flow Screen
        /// </summary>
        public string DisplayMessage { get; internal set; }

        /// <summary>
        /// Amount in cents for this transaction
        /// </summary>
        public int AmountCents { get; internal set; }

        /// <summary>
        /// Whther the request has been sent to the EFTPOS yet or not.
        /// In the PairedConnecting state, the transaction is initiated
        /// but the request is only sent once the connection is recovered.
        /// </summary>
        public bool RequestSent { get; internal set; }

        /// <summary>
        /// The time when the request was sent to the EFTPOS.
        /// </summary>
        public DateTime RequestTime { get; internal set; }

        /// <summary>
        /// The time when we last asked for an update, including the original request at first
        /// </summary>
        public DateTime LastStateRequestTime { get; internal set; }

        /// <summary>
        /// The id of the gt request message that was sent. Used to match with the response.
        /// </summary>
        public string GtRequestId { get; internal set; }

        /// <summary>
        /// Whether we're currently attempting to Cancel the transaction.
        /// </summary>
        public bool AttemptingToCancel { get; internal set; }

        /// <summary>
        /// When this flag is on, you need to display the dignature accept/decline buttons in your 
        /// transaction flow screen.
        /// </summary>
        public bool AwaitingSignatureCheck { get; internal set; }

        /// <summary>
        /// When this flag is on, you need to show your user the phone number to call to get the authorisation code.
        /// Then you need to provide your user means to enter that given code and submit it via SubmitAuthCode().
        /// </summary>
        public bool AwaitingPhoneForAuth { get; internal set; }

        /// <summary>
        /// Whether this transaction flow is over or not.
        /// </summary>
        public bool Finished { get; internal set; }

        /// <summary>
        /// The success state of this transaction. Starts off as Unknown.
        /// When finished, can be Success, Failed OR Unknown.
        /// </summary>
        public Message.SuccessState Success { get; internal set; }

        /// <summary>
        /// The response at the end of the transaction. 
        /// Might not be present in all edge cases.
        /// You can then turn this Message into the appropriate structure,
        /// such as PurchaseResponse, RefundResponse, etc
        /// </summary>
        public Message Response { get; internal set; }

        /// <summary>
        /// The message the we received from EFTPOS that told us that signature is required.
        /// </summary>
        public SignatureRequired SignatureRequiredMessage { get; internal set; }

        /// <summary>
        /// The message the we received from EFTPOS that told us that Phone For Auth is required.
        /// </summary>
        public PhoneForAuthRequired PhoneForAuthRequiredMessage { get; internal set; }

        /// <summary>
        /// The time when the cancel attempt was made.
        /// </summary>
        internal DateTime CancelAttemptTime { get; set; }

        /// <summary>
        /// The request message that we are sending/sent to the server.
        /// </summary>
        internal Message Request { get; set; }

        /// <summary>
        /// Whether we're currently waiting for a Get Transaction Response to get an update. 
        /// </summary>
        internal bool AwaitingGtResponse { get; set; }


        /// <summary>
        /// The time when the transaction was completed
        /// </summary>
        internal DateTime CompletedTime { get; set; }

        [Obsolete("Use PosRefId instead.")]
        public string Id { get; internal set; }

        internal TransactionFlowState(string posRefId, TransactionType type, int amountCents, Message message, string msg)
        {
            PosRefId = posRefId;
            Id = PosRefId; // obsolete, but let's maintain it for now, to mean same as PosRefId.
            Type = type;
            AmountCents = amountCents;
            RequestSent = false;
            AwaitingSignatureCheck = false;
            Finished = false;
            Success = Message.SuccessState.Unknown;
            Request = message;
            DisplayMessage = msg;
        }

        internal void Sent(string msg)
        {
            RequestSent = true;
            RequestTime = DateTime.Now;
            LastStateRequestTime = DateTime.Now;
            DisplayMessage = msg;
        }

        internal void Cancelling(string msg)
        {
            AttemptingToCancel = true;
            CancelAttemptTime = DateTime.Now;
            DisplayMessage = msg;
        }

        internal void CancelFailed(string msg)
        {
            AttemptingToCancel = false;
            DisplayMessage = msg;
        }

        internal void CallingGt(string gtRequestId)
        {
            AwaitingGtResponse = true;
            LastStateRequestTime = DateTime.Now;
            GtRequestId = gtRequestId;
        }

        internal void GotGtResponse()
        {
            AwaitingGtResponse = false;
        }

        internal void Failed(Message response, string msg)
        {
            Success = Message.SuccessState.Failed;
            Finished = true;
            Response = response;
            DisplayMessage = msg;
            CompletedTime = DateTime.Now;
        }

        internal void SignatureRequired(SignatureRequired spiMessage, string msg)
        {
            SignatureRequiredMessage = spiMessage;
            AwaitingSignatureCheck = true;
            DisplayMessage = msg;
        }

        internal void SignatureResponded(string msg)
        {
            AwaitingSignatureCheck = false;
            DisplayMessage = msg;
        }

        internal void PhoneForAuthRequired(PhoneForAuthRequired spiMessage, string msg)
        {
            PhoneForAuthRequiredMessage = spiMessage;
            AwaitingPhoneForAuth = true;
            DisplayMessage = msg;
        }

        internal void AuthCodeSent(string msg)
        {
            AwaitingPhoneForAuth = false;
            DisplayMessage = msg;
        }

        internal void Completed(Message.SuccessState state, Message response, string msg)
        {
            Success = state;
            Response = response;
            Finished = true;
            AttemptingToCancel = false;
            AwaitingGtResponse = false;
            AwaitingSignatureCheck = false;
            AwaitingPhoneForAuth = false;
            DisplayMessage = msg;
            CompletedTime = DateTime.Now;
        }

        internal void UnknownCompleted(string msg)
        {
            Success = Message.SuccessState.Unknown;
            Response = null;
            Finished = true;
            AttemptingToCancel = false;
            AwaitingGtResponse = false;
            AwaitingSignatureCheck = false;
            AwaitingPhoneForAuth = false;
            DisplayMessage = msg;
            CompletedTime = DateTime.Now;
        }
    }

    /// <summary>
    /// Used as a return in the SubmitAuthCode method to signify whether Code is valid
    /// </summary>
    public class SubmitAuthCodeResult
    {
        public bool ValidFormat { get; }

        /// <summary>
        /// Text that gives reason for Invalidity
        /// </summary>
        public string Message { get; }

        public SubmitAuthCodeResult(bool validFormat, string message)
        {
            ValidFormat = validFormat;
            Message = message;
        }
    }

    /// <summary>
    /// Represents the State during a TransactionFlow
    /// These attributes work for COM interop.
    /// </summary>
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class SpiConfig
    {
        /// <summary>
        /// This default stucture works for COM interop.
        /// </summary>
        public SpiConfig()
        {
            EnabledPrintMerchantCopy = false;
            EnabledPromptForCustomerCopyOnEftpos = false;
            EnabledSignatureFlowOnEftpos = false;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool PromptForCustomerCopyOnEftpos { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool SignatureFlowOnEftpos { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool PrintMerchantCopy { get; set; }

        internal bool EnabledPromptForCustomerCopyOnEftpos { get; set; }
        internal bool EnabledSignatureFlowOnEftpos { get; set; }
        internal bool EnabledPrintMerchantCopy { get; set; }

        internal void AddReceiptConfig(JObject messageData)
        {
            if (PromptForCustomerCopyOnEftpos && EnabledPromptForCustomerCopyOnEftpos)
            {
                messageData.Add("prompt_for_customer_copy", PromptForCustomerCopyOnEftpos);
            }
            if (SignatureFlowOnEftpos && EnabledSignatureFlowOnEftpos)
            {
                messageData.Add("print_for_signature_required_transactions", SignatureFlowOnEftpos);
            }
            if (PrintMerchantCopy && EnabledPrintMerchantCopy)
            {
                messageData.Add("print_merchant_copy", PrintMerchantCopy);
            }
        }

        public override string ToString()
        {
            return $"PromptForCustomerCopyOnEftpos:{PromptForCustomerCopyOnEftpos} SignatureFlowOnEftpos:{SignatureFlowOnEftpos} PrintMerchantCopy: {PrintMerchantCopy}";
        }
    }

    /// <summary>
    /// Represents the State during a TransactionFlow
    /// These attributes work for COM interop.
    /// </summary>
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class TransactionOptions
    {
        private string _customerReceiptHeader = "";
        private string _customerReceiptFooter = "";
        private string _merchantReceiptHeader = "";
        private string _merchantReceiptFooter = "";

        public TransactionOptions() { }

        public void SetCustomerReceiptHeader(String customerReceiptHeader)
        {
            _customerReceiptHeader = customerReceiptHeader;
        }

        public void SetCustomerReceiptFooter(String customerReceiptFooter)
        {
            _customerReceiptFooter = customerReceiptFooter;
        }

        public void SetMerchantReceiptHeader(String merchantReceiptHeader)
        {
            _merchantReceiptHeader = merchantReceiptHeader;
        }

        public void SetMerchantReceiptFooter(String merchantReceiptFooter)
        {
            _merchantReceiptFooter = merchantReceiptFooter;
        }

        public void AddOptions(JObject messageData)
        {
            AddOptionObject(_customerReceiptHeader, "customer_receipt_header", messageData);
            AddOptionObject(_customerReceiptFooter, "customer_receipt_footer", messageData);
            AddOptionObject(_merchantReceiptHeader, "merchant_receipt_header", messageData);
            AddOptionObject(_merchantReceiptFooter, "merchant_receipt_footer", messageData);
        }

        private void AddOptionObject(JToken value, string key, JObject messageData)
        {
            if (value != null)
            {
                messageData.Add(key, value);
            }
            else
            {
                messageData.Remove(key);
            }
        }
    }
}