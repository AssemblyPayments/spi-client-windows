using System;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SPIClient
{
    /// <summary>
    /// Events statically declares the various event names in messages.
    /// </summary>
    public static class Events
    {
        public const string PairRequest = "pair_request";
        public const string KeyRequest = "key_request";
        public const string KeyResponse = "key_response";
        public const string KeyCheck = "key_check";
        public const string PairResponse = "pair_response";
        public const string DropKeysAdvice = "drop_keys";

        public const string LoginRequest = "login_request";
        public const string LoginResponse = "login_response";

        public const string Ping = "ping";
        public const string Pong = "pong";

        public const string PurchaseRequest = "purchase";
        public const string PurchaseResponse = "purchase_response";
        public const string CancelTransactionRequest = "cancel_transaction";
        public const string CancelTransactionResponse = "cancel_response";
        public const string GetLastTransactionRequest = "get_last_transaction";
        public const string GetLastTransactionResponse = "last_transaction";
        public const string GetTransactionRequest = "get_transaction";
        public const string GetTransactionResponse = "get_transaction_response";
        public const string ReversalRequest = "reverse_transaction";
        public const string ReversalResponse = "reverse_transaction_response";
        public const string RefundRequest = "refund";
        public const string RefundResponse = "refund_response";
        public const string SignatureRequired = "signature_required";
        public const string SignatureDeclined = "signature_decline";
        public const string SignatureAccepted = "signature_accept";
        public const string AuthCodeRequired = "authorisation_code_required";
        public const string AuthCodeAdvice = "authorisation_code_advice";

        public const string CashoutOnlyRequest = "cash";
        public const string CashoutOnlyResponse = "cash_response";

        public const string MotoPurchaseRequest = "moto_purchase";
        public const string MotoPurchaseResponse = "moto_purchase_response";

        public const string SettleRequest = "settle";
        public const string SettleResponse = "settle_response";
        public const string SettlementEnquiryRequest = "settlement_enquiry";
        public const string SettlementEnquiryResponse = "settlement_enquiry_response";

        public const string SetPosInfoRequest = "set_pos_info";
        public const string SetPosInfoResponse = "set_pos_info_response";

        public const string KeyRollRequest = "request_use_next_keys";
        public const string KeyRollResponse = "response_use_next_keys";

        public const string Error = "error";

        public const string InvalidHmacSignature = "_INVALID_SIGNATURE_";

        // Pay At Table Related Messages
        public const string PayAtTableGetTableConfig = "get_table_config"; // incoming. When eftpos wants to ask us for P@T configuration.
        public const string PayAtTableSetTableConfig = "set_table_config"; // outgoing. When we want to instruct eftpos with the P@T configuration.
        public const string PayAtTableGetBillDetails = "get_bill_details"; // incoming. When eftpos wants to aretrieve the bill for a table.
        public const string PayAtTableBillDetails = "bill_details";        // outgoing. We reply with this when eftpos requests to us get_bill_details.
        public const string PayAtTableBillPayment = "bill_payment";        // incoming. When the eftpos advices 
        public const string PayAtTableGetOpenTables = "get_open_tables";
        public const string PayAtTableOpenTables = "open_tables";
        public const string PayAtTableBillPaymentFlowEnded = "bill_payment_flow_ended";
        public const string PayAtTableBillPaymentFlowEndedAck = "bill_payment_flow_ended_ack";

        public const string PrintingRequest = "print";
        public const string PrintingResponse = "print_response";

        public const string TerminalStatusRequest = "get_terminal_status";
        public const string TerminalStatusResponse = "terminal_status";

        public const string TerminalConfigurationRequest = "get_terminal_configuration";
        public const string TerminalConfigurationResponse = "terminal_configuration";

        public const string BatteryLevelChanged = "battery_level_changed";


        public const string TransactionUpdateMessage = "txn_update_message";
    }

    /// <summary>
    /// MessageStamp represents what is required to turn an outgoing Message into Json
    /// including encryption and date setting.
    /// </summary>
    public class MessageStamp
    {
        public string PosId { get; set; }
        public Secrets Secrets { get; set; }
        public int PosCounter { get; set; }
        public string ConnId { get; private set; }

        public MessageStamp(string posId, Secrets secrets)
        {
            PosId = posId;
            Secrets = secrets;
        }

        internal void ResetConnection()
        {
            SetConnectionId("");
            PosCounter = new Random().Next(100); // pseudo random number starting from 100
        }

        internal void SetConnectionId(string connId)
        {
            if (connId != null)
                ConnId = connId;
        }
    }

    /// <summary>
    /// MessageEnvelope represents the outer structure of any message that is exchanged
    /// between the Pos and the PinPad and vice-versa.
    /// See http://www.simplepaymentapi.com/#/api/message-encryption
    /// </summary>
    public class MessageEnvelope
    {
        /// <summary>
        /// The Message field is set only when in Un-encrypted form.
        /// In fact it is the only field in an envelope in the Un-Encrypted form.
        /// </summary>
        [JsonProperty("message", NullValueHandling = NullValueHandling.Ignore)]
        public Message Message { get; }

        /// <summary>
        /// The enc field is set only when in Encrypted form.
        /// It contains the encrypted Json of another MessageEnvelope 
        /// </summary>
        [JsonProperty("enc", NullValueHandling = NullValueHandling.Ignore)]
        public string Enc { get; }

        /// <summary>
        /// The hmac field is set only when in Encrypted form.
        /// It is the signature of the "enc" field.
        /// </summary>
        [JsonProperty("hmac", NullValueHandling = NullValueHandling.Ignore)]
        public string Hmac { get; }

        /// <summary>
        /// The pos_id field is only filled for outgoing Encrypted messages.
        /// </summary>
        [JsonProperty("pos_id", NullValueHandling = NullValueHandling.Ignore)]
        public string PosId { get; private set; }

        [JsonConstructor()]
        public MessageEnvelope(Message message, string enc, string hmac)
        {
            Message = message;
            Enc = enc;
            Hmac = hmac;
        }

        public MessageEnvelope(Message message)
        {
            Message = message;
        }

        public MessageEnvelope(string enc, string hmac, string posId)
        {
            Hmac = hmac;
            Enc = enc;
            PosId = posId;
        }
    }

    /// <summary>
    /// Message represents the contents of a Message.
    /// See http://www.simplepaymentapi.com/#/api/message-encryption
    /// These attributes work for COM interop.
    /// </summary>
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class Message
    {
        /// <summary>
        /// This default stucture works for COM interop.
        /// </summary>
        public Message() { }

        [JsonProperty("id")]
        public string Id { get; }

        [JsonProperty("event")]
        public string EventName { get; }

        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Data { get; }

        [JsonProperty("datetime")]
        public string DateTimeStamp { get; private set; }

        [JsonProperty("pos_counter")]
        internal int PosCounter { get; set; }

        [JsonProperty("conn_id")]
        internal string ConnId { get; set; }

        /// <summary>
        /// Pos_id is set here only for outgoing Un-encrypted messages. 
        /// (not in the envelope's top level which would just have the "message" field.)
        /// </summary>
        [JsonProperty("pos_id", NullValueHandling = NullValueHandling.Ignore)]
        public string PosId { get; private set; }

        /// <summary>
        /// Sometimes the logic around the incoming message
        /// might need access to the sugnature, for example in the key_check.
        /// </summary>
        [JsonIgnore]
        public string IncomingHmac { get; private set; }

        /// <summary>
        /// Denotes whether an outgoing message needs to be encrypted in ToJson()
        /// </summary>
        private readonly bool _needsEncryption;

        /// <summary>
        /// Set on an incoming message just so you can have a look at what it looked like in its json form.
        /// </summary>
        [JsonIgnore]
        public string DecryptedJson { get; private set; }

        [JsonConstructor]
        public Message(string id, string eventName, JObject data, bool needsEncryption)
        {
            Id = id;
            EventName = eventName;
            Data = data;
            _needsEncryption = needsEncryption;
        }

        public enum SuccessState { Unknown, Success, Failed }

        public SuccessState GetSuccessState()
        {
            if (Data == null) return SuccessState.Unknown;
            JToken success = null;
            var found = Data.TryGetValue("success", out success);
            if (found) return (bool)success ? SuccessState.Success : SuccessState.Failed;
            return SuccessState.Unknown;
        }

        public string GetError()
        {
            JToken e = null;
            var found = Data.TryGetValue("error_reason", out e);
            if (found) return (string)e;
            return null;
        }

        public string GetErrorDetail()
        {
            return GetDataStringValue("error_detail");
        }

        public string GetDataStringValue(string attribute)
        {
            JToken v;
            var found = Data.TryGetValue(attribute, out v);
            if (found) return (string)v;
            return "";
        }

        public int GetDataIntValue(string attribute)
        {
            JToken v;
            var value = 0;

            var found = Data.TryGetValue(attribute, out v);
            var parse = v != null && int.TryParse(v.ToString(), out value);
            return found ? value : 0;
        }

        public bool GetDataBoolValue(string attribute, bool defaultIfNotFound)
        {
            JToken v;
            var found = Data.TryGetValue(attribute, out v);
            if (found) return (bool)v;
            return defaultIfNotFound;
        }

        public static Message FromJson(string msgJson, Secrets secrets)
        {
            var jsonSerializerSettings = new JsonSerializerSettings() { DateParseHandling = DateParseHandling.None };

            var env = JsonConvert.DeserializeObject<MessageEnvelope>(msgJson, jsonSerializerSettings);
            if (env.Message != null)
            {
                var message = env.Message;
                message.DecryptedJson = msgJson;
                return message;
            }

            if (secrets == null)
            {
                // This may happen if we somehow received an encrypted message from eftpos but we're not configered with secrets.
                // For example, if we cancel the pairing process a little late in the game and we get an encrypted key_check message after we've dropped the keys.
                return new Message("UNKNOWN", "NOSECRETS", null, false);
            }

            var sig = Crypto.HmacSignature(secrets.HmacKeyBytes, env.Enc);
            if (sig != env.Hmac)
            {
                return new Message("_", Events.InvalidHmacSignature, null, false);
            }
            var decryptedJson = Crypto.AesDecrypt(secrets.EncKeyBytes, env.Enc);
            
            try
            {
                var decryptedEnv =
                    JsonConvert.DeserializeObject<MessageEnvelope>(decryptedJson, jsonSerializerSettings);
                var message = decryptedEnv.Message;
                message.IncomingHmac = env.Hmac;
                message.DecryptedJson = decryptedJson;
                return decryptedEnv.Message;
            }
            catch (JsonSerializationException e)
            {
                return new Message("UNKNOWN", "UNPARSEABLE", new JObject("msg", decryptedJson), false);
            }
        }

        public string ToJson(MessageStamp stamp)
        {
            PosCounter = stamp.PosCounter++;
            ConnId = stamp.ConnId;
            DateTimeStamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff");
            PosId = stamp.PosId;

            DecryptedJson = JsonConvert.SerializeObject(new MessageEnvelope(this));

            if (!_needsEncryption)
                return DecryptedJson;

            var encMsg = Crypto.AesEncrypt(stamp.Secrets.EncKeyBytes, DecryptedJson);
            var hmacSig = Crypto.HmacSignature(stamp.Secrets.HmacKeyBytes, encMsg);
            var encrMessageEnvelope = new MessageEnvelope(encMsg, hmacSig, stamp.PosId);
            var encrMessageEnvelopeJson = JsonConvert.SerializeObject(encrMessageEnvelope);
            return encrMessageEnvelopeJson;
        }
    }
}