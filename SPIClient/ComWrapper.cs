using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SPIClient
{
    public delegate void CBTxFlowStateChanged(TransactionFlowState txState);
    public delegate void CBPairingFlowStateChanged(PairingFlowState pairingFlowState);
    public delegate void CBSecretsChanged(Secrets secrets);
    public delegate void CBSpiStatusChanged(SpiStatusEventArgs status);
    public delegate void CBPayAtTableGetBillStatus(BillStatusInfo billStatusInfo, out BillStatusResponse billStatusResponse);
    public delegate void CBPayAtTableBillPaymentReceived(BillPaymentInfo billPaymentInfo, out BillStatusResponse billStatusResponse);

    /// <summary>
    /// This class is wrapper for COM interop.
    /// </summary>
    [ComVisible(true)]
    [Guid("F99A837B-58EE-4BD9-B742-F0EC0655D39B")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class ComWrapper
    {
        private Spi _spi;
        private SpiPayAtTable _pat;

        IntPtr ptr;
        CBTxFlowStateChanged callBackTxState;
        CBPairingFlowStateChanged callBackPairingFlowState;
        CBSecretsChanged callBackSecrets;
        CBSpiStatusChanged callBackStatus;
        CBPayAtTableGetBillStatus callBackPayAtTableGetBillStatus;
        CBPayAtTableBillPaymentReceived callBackPayAtTableBillPaymentReceived;

        public void Main(Spi spi, Int32 cBTxStatePtr, Int32 cBPairingFlowStatePtr, Int32 cBsecretsPtr, Int32 cBStatusPtr)
        {
            _spi = spi; // It is ok to not have the secrets yet to start with.

            ptr = new IntPtr(cBTxStatePtr);
            callBackTxState = (CBTxFlowStateChanged)Marshal.GetDelegateForFunctionPointer(ptr, typeof(CBTxFlowStateChanged));

            ptr = new IntPtr(cBPairingFlowStatePtr);
            callBackPairingFlowState = (CBPairingFlowStateChanged)Marshal.GetDelegateForFunctionPointer(ptr, typeof(CBPairingFlowStateChanged));

            ptr = new IntPtr(cBsecretsPtr);
            callBackSecrets = (CBSecretsChanged)Marshal.GetDelegateForFunctionPointer(ptr, typeof(CBSecretsChanged));

            ptr = new IntPtr(cBStatusPtr);
            callBackStatus = (CBSpiStatusChanged)Marshal.GetDelegateForFunctionPointer(ptr, typeof(CBSpiStatusChanged));

            _spi.StatusChanged += OnSpiStatusChanged;
            _spi.PairingFlowStateChanged += OnPairingFlowStateChanged;
            _spi.SecretsChanged += OnSecretsChanged;
            _spi.TxFlowStateChanged += OnTxFlowStateChanged;
        }

        public void Main(Spi spi, SpiPayAtTable pat, Int32 cBTxStatePtr, Int32 cBPairingFlowStatePtr, Int32 cBsecretsPtr, Int32 cBStatusPtr, Int32 cBPayAtTableGetBillDetailsPtr, Int32 cBPayAtTableBillPaymentReceivedPtr)
        {
            _spi = spi; // It is ok to not have the secrets yet to start with.
            _pat = pat;

            ptr = new IntPtr(cBTxStatePtr);
            callBackTxState = (CBTxFlowStateChanged)Marshal.GetDelegateForFunctionPointer(ptr, typeof(CBTxFlowStateChanged));

            ptr = new IntPtr(cBPairingFlowStatePtr);
            callBackPairingFlowState = (CBPairingFlowStateChanged)Marshal.GetDelegateForFunctionPointer(ptr, typeof(CBPairingFlowStateChanged));

            ptr = new IntPtr(cBsecretsPtr);
            callBackSecrets = (CBSecretsChanged)Marshal.GetDelegateForFunctionPointer(ptr, typeof(CBSecretsChanged));

            ptr = new IntPtr(cBStatusPtr);
            callBackStatus = (CBSpiStatusChanged)Marshal.GetDelegateForFunctionPointer(ptr, typeof(CBSpiStatusChanged));

            ptr = new IntPtr(cBPayAtTableGetBillDetailsPtr);
            callBackPayAtTableGetBillStatus = (CBPayAtTableGetBillStatus)Marshal.GetDelegateForFunctionPointer(ptr, typeof(CBPayAtTableGetBillStatus));

            ptr = new IntPtr(cBPayAtTableBillPaymentReceivedPtr);
            callBackPayAtTableBillPaymentReceived = (CBPayAtTableBillPaymentReceived)Marshal.GetDelegateForFunctionPointer(ptr, typeof(CBPayAtTableBillPaymentReceived));

            _spi.StatusChanged += OnSpiStatusChanged;
            _spi.PairingFlowStateChanged += OnPairingFlowStateChanged;
            _spi.SecretsChanged += OnSecretsChanged;
            _spi.TxFlowStateChanged += OnTxFlowStateChanged;

            _pat.GetBillStatus = OnPayAtTableGetBillStatus;
            _pat.BillPaymentReceived = OnPayAtTableBillPaymentReceived;
        }

        private void OnTxFlowStateChanged(object sender, TransactionFlowState txState)
        {
            callBackTxState(txState);
        }

        private void OnPairingFlowStateChanged(object sender, PairingFlowState pairingFlowState)
        {
            callBackPairingFlowState(pairingFlowState);
        }

        private void OnSecretsChanged(object sender, Secrets secrets)
        {
            callBackSecrets(secrets);
        }

        private void OnSpiStatusChanged(object sender, SpiStatusEventArgs status)
        {
            callBackStatus(status);
        }

        private BillStatusResponse OnPayAtTableGetBillStatus(string billId, string tableId, string operatorId)
        {
            BillStatusInfo billStatusInfo = new BillStatusInfo();
            billStatusInfo.BillId = billId;
            billStatusInfo.TableId = tableId;
            billStatusInfo.OperatorId = operatorId;

            BillStatusResponse billStatusResponse = new BillStatusResponse();
            callBackPayAtTableGetBillStatus(billStatusInfo, out billStatusResponse);
            return billStatusResponse;
        }

        private BillStatusResponse OnPayAtTableBillPaymentReceived(BillPayment billPayment, string updatedBillData)
        {
            BillPaymentInfo billPaymentInfo = new BillPaymentInfo();
            billPaymentInfo.BillPayment = billPayment;
            billPaymentInfo.UpdatedBillData = updatedBillData;

            BillStatusResponse billStatusResponse = new BillStatusResponse();
            callBackPayAtTableBillPaymentReceived(billPaymentInfo, out billStatusResponse);
            return billStatusResponse;
        }

        public string Get_Id(string prefix)
        {
            return RequestIdHelper.Id(prefix);
        }

        public String GetSpiStatusEnumName(int intSpiStatus)
        {
            SpiStatus spiStatus = (SpiStatus)intSpiStatus;
            return spiStatus.ToString();
        }

        public String GetSpiFlowEnumName(int intSpiFlow)
        {
            SpiFlow spiFlow = (SpiFlow)intSpiFlow;
            return spiFlow.ToString();
        }

        public String GetSuccessStateEnumName(int intSuccessState)
        {
            Message.SuccessState successState = (Message.SuccessState)intSuccessState;
            return successState.ToString();
        }

        public String GetTransactionTypeEnumName(int intTransactionType)
        {
            TransactionType transactionType = (TransactionType)intTransactionType;
            return transactionType.ToString();
        }

        public String GetPaymentTypeEnumName(int intPaymentType)
        {
            PaymentType paymentType = (PaymentType)intPaymentType;
            return paymentType.ToString();
        }
        public Spi SpiInit(string posId, string serialNumber, string eftposAddress, Secrets secrets)
        {
            return new Spi(posId, serialNumber, eftposAddress, secrets);
        }

        public PurchaseResponse PurchaseResponseInit(Message m)
        {
            return new PurchaseResponse(m);
        }

        public RefundResponse RefundResponseInit(Message m)
        {
            return new RefundResponse(m);
        }

        public Settlement SettlementInit(Message m)
        {
            return new Settlement(m);
        }

        public Secrets SecretsInit(string encKey, string hmacKey)
        {
            return new Secrets(encKey, hmacKey);
        }

        public GetLastTransactionResponse GetLastTransactionResponseInit(Message m)
        {
            return new GetLastTransactionResponse(m);
        }

        public CashoutOnlyResponse CashoutOnlyResponseInit(Message m)
        {
            return new CashoutOnlyResponse(m);
        }

        public MotoPurchaseResponse MotoPurchaseResponseInit(Message m)
        {
            return new MotoPurchaseResponse(m);
        }

        public PreauthResponse PreauthResponseInit(Message m)
        {
            return new PreauthResponse(m);
        }

        public AccountVerifyResponse AccountVerifyResponseInit(Message m)
        {
            return new AccountVerifyResponse(m);
        }

        public SchemeSettlementEntry[] GetSchemeSettlementEntries(TransactionFlowState txState)
        {
            var settleResponse = new Settlement(txState.Response);
            var schemes = settleResponse.GetSchemeSettlementEntries();
            var schemeList = new List<SchemeSettlementEntry>();
            foreach (var s in schemes)
            {
                schemeList.Add(s);
            }

            return schemeList.ToArray();
        }

        public string GetSpiVersion()
        {
            return Spi.GetVersion();
        }

        public string GetPosVersion()
        {
            if (Assembly.GetEntryAssembly() == null)
            {
                return "0";
            }
            else
            {
                return Assembly.GetEntryAssembly().GetName().Version.ToString();
            }
        }

        public string NewBillId()
        {
            return (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds.ToString();
        }

        private static readonly ILog log = LogManagerWrapper.GetLogger("spi");
    }

    public static class LogManagerWrapper
    {
        private static readonly string LOG_CONFIG_FILE = @"path\to\log4net.config";

        public static ILog GetLogger(string type)
        {
            // If no loggers have been created, load our own.
            if (LogManager.GetCurrentLoggers().Length == 0)
            {
                LoadConfig();
            }
            return LogManager.GetLogger(type);
        }

        private static void LoadConfig()
        {
            //// TODO: Do exception handling for File access issues and supply sane defaults if it's unavailable.   
            XmlConfigurator.ConfigureAndWatch(new FileInfo(LOG_CONFIG_FILE));
        }
    }

    /// <summary>
    /// This class is wrapper for COM interop.
    /// </summary>
    [ComVisible(true)]
    [Guid("B8EC93A1-DD22-4928-96A2-E2B21719D3E2")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class BillStatusInfo
    {
        public BillStatusInfo() { }

        public string BillId { get; set; }

        public string TableId { get; set; }

        public string OperatorId { get; set; }
    }

    /// <summary>
    /// This class is wrapper for COM interop.
    /// </summary>
    [ComVisible(true)]
    [Guid("E277FA15-EAD0-4BDF-8F76-701E2464D0EA")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class BillPaymentInfo
    {
        public BillPaymentInfo() { }

        public BillPayment BillPayment { get; set; }

        public string UpdatedBillData { get; set; }
    }
}
