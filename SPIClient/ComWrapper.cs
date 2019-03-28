using log4net;
using log4net.Config;
using Newtonsoft.Json;
using SPIClient.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace SPIClient
{
    public delegate void CBTransactionFlowStateChanged(TransactionFlowState transactionFlowState);
    public delegate void CBPairingFlowStateChanged(PairingFlowState pairingFlowState);
    public delegate void CBSecretsChanged(Secrets secrets);
    public delegate void CBSpiStatusChanged(SpiStatusEventArgs spiStatus);
    public delegate void CBDeviceAddressStatusChanged(DeviceAddressStatus deviceAddressStatus);

    public delegate void CBPrintingResponse(Message message);
    public delegate void CBTerminalStatusResponse(Message message);
    public delegate void CBTerminalConfigurationResponse(Message message);
    public delegate void CBBatteryLevelChanged(Message message);

    public delegate void CBPayAtTableGetBillStatus(BillStatusRequest billStatusRequest, out BillStatusResponse billStatusResponse);
    public delegate void CBPayAtTableBillPaymentReceived(BillPaymentInfo billPaymentInfo, out BillStatusResponse billStatusResponse);
    public delegate void CBPayAtTableBillPaymentFlowEndedResponse(Message message);
    public delegate void CBPayAtTableGetOpenTables(BillStatusRequest billStatusRequest, out GetOpenTablesResponse getOpenTablesResponse);

    /// <summary>
    /// This class is wrapper for COM interop.
    /// </summary>
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class ComWrapper
    {
        private Spi _spi;
        private SpiPayAtTable _pat;
        private IntPtr ptr;

        private CBTransactionFlowStateChanged callBackTransactionState;
        private CBPairingFlowStateChanged callBackPairingFlowState;
        private CBSecretsChanged callBackSecrets;
        private CBSpiStatusChanged callBackStatus;
        private CBDeviceAddressStatusChanged callBackDeviceAddressStatus;

        private CBPrintingResponse callBackPrintingResponse;
        private CBTerminalStatusResponse callBackTerminalStatusResponse;
        private CBTerminalConfigurationResponse callBackTerminalConfigurationResponse;
        private CBBatteryLevelChanged callBackBatteryLevelChanged;

        private CBPayAtTableGetBillStatus callBackPayAtTableGetBillStatus;
        private CBPayAtTableBillPaymentReceived callBackPayAtTableBillPaymentReceived;
        private CBPayAtTableBillPaymentFlowEndedResponse callBackPayAtTableBillPaymentFlowEndedResponse;
        private CBPayAtTableGetOpenTables callBackPayAtTableGetOpenTables;

        public void Main(Spi spi, DelegationPointers delegationPointers)
        {
            _spi = spi; // It is ok to not have the secrets yet to start with.

            ptr = new IntPtr(delegationPointers.CBTransactionStatePtr);
            callBackTransactionState = (CBTransactionFlowStateChanged)Marshal.GetDelegateForFunctionPointer(ptr, typeof(CBTransactionFlowStateChanged));

            ptr = new IntPtr(delegationPointers.CBPairingFlowStatePtr);
            callBackPairingFlowState = (CBPairingFlowStateChanged)Marshal.GetDelegateForFunctionPointer(ptr, typeof(CBPairingFlowStateChanged));

            ptr = new IntPtr(delegationPointers.CBSecretsPtr);
            callBackSecrets = (CBSecretsChanged)Marshal.GetDelegateForFunctionPointer(ptr, typeof(CBSecretsChanged));

            ptr = new IntPtr(delegationPointers.CBStatusPtr);
            callBackStatus = (CBSpiStatusChanged)Marshal.GetDelegateForFunctionPointer(ptr, typeof(CBSpiStatusChanged));

            if (delegationPointers.CBDeviceAddressChangedPtr > 0)
            {
                ptr = new IntPtr(delegationPointers.CBDeviceAddressChangedPtr);
                callBackDeviceAddressStatus = (CBDeviceAddressStatusChanged)Marshal.GetDelegateForFunctionPointer(ptr, typeof(CBDeviceAddressStatusChanged));
            }

            #region Battery and Printing Delegetions

            if (delegationPointers.CBPrintingResponsePtr > 0)
            {
                ptr = new IntPtr(delegationPointers.CBPrintingResponsePtr);
                callBackPrintingResponse = (CBPrintingResponse)Marshal.GetDelegateForFunctionPointer(ptr, typeof(CBPrintingResponse));
            }

            if (delegationPointers.CBTerminalStatusResponsePtr > 0)
            {
                ptr = new IntPtr(delegationPointers.CBTerminalStatusResponsePtr);
                callBackTerminalStatusResponse = (CBTerminalStatusResponse)Marshal.GetDelegateForFunctionPointer(ptr, typeof(CBTerminalStatusResponse));
            }

            if (delegationPointers.CBTerminalConfigurationResponsePtr > 0)
            {
                ptr = new IntPtr(delegationPointers.CBTerminalConfigurationResponsePtr);
                callBackTerminalConfigurationResponse = (CBTerminalConfigurationResponse)Marshal.GetDelegateForFunctionPointer(ptr, typeof(CBTerminalConfigurationResponse));
            }

            if (delegationPointers.CBBatteryLevelChangedPtr > 0)
            {
                ptr = new IntPtr(delegationPointers.CBBatteryLevelChangedPtr);
                callBackBatteryLevelChanged = (CBBatteryLevelChanged)Marshal.GetDelegateForFunctionPointer(ptr, typeof(CBBatteryLevelChanged));
            }

            #endregion

            _spi.StatusChanged += OnSpiStatusChanged;
            _spi.PairingFlowStateChanged += OnPairingFlowStateChanged;
            _spi.SecretsChanged += OnSecretsChanged;
            _spi.TxFlowStateChanged += OnTxFlowStateChanged;
            _spi.DeviceAddressChanged += OnDeviceAddressStatusChanged;

            _spi.BatteryLevelChanged = OnBatteryLevelChanged;
            _spi.PrintingResponse = OnPrintingResponse;
            _spi.TerminalConfigurationResponse = OnTerminalConfigurationResponse;
            _spi.TerminalStatusResponse = OnTerminalStatusResponse;
        }

        public void Main(Spi spi, SpiPayAtTable pat, DelegationPointers delegationPointers)
        {
            _spi = spi; // It is ok to not have the secrets yet to start with.
            _pat = pat;

            ptr = new IntPtr(delegationPointers.CBTransactionStatePtr);
            callBackTransactionState = (CBTransactionFlowStateChanged)Marshal.GetDelegateForFunctionPointer(ptr, typeof(CBTransactionFlowStateChanged));

            ptr = new IntPtr(delegationPointers.CBPairingFlowStatePtr);
            callBackPairingFlowState = (CBPairingFlowStateChanged)Marshal.GetDelegateForFunctionPointer(ptr, typeof(CBPairingFlowStateChanged));

            ptr = new IntPtr(delegationPointers.CBSecretsPtr);
            callBackSecrets = (CBSecretsChanged)Marshal.GetDelegateForFunctionPointer(ptr, typeof(CBSecretsChanged));

            ptr = new IntPtr(delegationPointers.CBStatusPtr);
            callBackStatus = (CBSpiStatusChanged)Marshal.GetDelegateForFunctionPointer(ptr, typeof(CBSpiStatusChanged));

            if (delegationPointers.CBDeviceAddressChangedPtr > 0)
            {
                ptr = new IntPtr(delegationPointers.CBDeviceAddressChangedPtr);
                callBackDeviceAddressStatus = (CBDeviceAddressStatusChanged)Marshal.GetDelegateForFunctionPointer(ptr, typeof(CBDeviceAddressStatusChanged));
            }

            #region PayAtTable Delegetions

            ptr = new IntPtr(delegationPointers.CBPayAtTableGetBillDetailsPtr);
            callBackPayAtTableGetBillStatus = (CBPayAtTableGetBillStatus)Marshal.GetDelegateForFunctionPointer(ptr, typeof(CBPayAtTableGetBillStatus));

            ptr = new IntPtr(delegationPointers.CBPayAtTableBillPaymentReceivedPtr);
            callBackPayAtTableBillPaymentReceived = (CBPayAtTableBillPaymentReceived)Marshal.GetDelegateForFunctionPointer(ptr, typeof(CBPayAtTableBillPaymentReceived));

            ptr = new IntPtr(delegationPointers.CBPayAtTableBillPaymentFlowEndedResponsePtr);
            callBackPayAtTableBillPaymentFlowEndedResponse = (CBPayAtTableBillPaymentFlowEndedResponse)Marshal.GetDelegateForFunctionPointer(ptr, typeof(CBPayAtTableBillPaymentFlowEndedResponse));

            ptr = new IntPtr(delegationPointers.CBPayAtTableGetOpenTablesPtr);
            callBackPayAtTableGetOpenTables = (CBPayAtTableGetOpenTables)Marshal.GetDelegateForFunctionPointer(ptr, typeof(CBPayAtTableGetOpenTables));

            #endregion

            #region Battery and Printing Delegetions

            if (delegationPointers.CBPrintingResponsePtr > 0)
            {
                ptr = new IntPtr(delegationPointers.CBPrintingResponsePtr);
                callBackPrintingResponse = (CBPrintingResponse)Marshal.GetDelegateForFunctionPointer(ptr, typeof(CBPrintingResponse));
            }

            if (delegationPointers.CBTerminalStatusResponsePtr > 0)
            {
                ptr = new IntPtr(delegationPointers.CBTerminalStatusResponsePtr);
                callBackTerminalStatusResponse = (CBTerminalStatusResponse)Marshal.GetDelegateForFunctionPointer(ptr, typeof(CBTerminalStatusResponse));
            }

            if (delegationPointers.CBTerminalConfigurationResponsePtr > 0)
            {
                ptr = new IntPtr(delegationPointers.CBTerminalConfigurationResponsePtr);
                callBackTerminalConfigurationResponse = (CBTerminalConfigurationResponse)Marshal.GetDelegateForFunctionPointer(ptr, typeof(CBTerminalConfigurationResponse));
            }

            if (delegationPointers.CBBatteryLevelChangedPtr > 0)
            {
                ptr = new IntPtr(delegationPointers.CBBatteryLevelChangedPtr);
                callBackBatteryLevelChanged = (CBBatteryLevelChanged)Marshal.GetDelegateForFunctionPointer(ptr, typeof(CBBatteryLevelChanged));
            }

            #endregion

            _spi.StatusChanged += OnSpiStatusChanged;
            _spi.PairingFlowStateChanged += OnPairingFlowStateChanged;
            _spi.SecretsChanged += OnSecretsChanged;
            _spi.TxFlowStateChanged += OnTxFlowStateChanged;
            _spi.DeviceAddressChanged += OnDeviceAddressStatusChanged;

            _spi.BatteryLevelChanged = OnBatteryLevelChanged;
            _spi.PrintingResponse = OnPrintingResponse;
            _spi.TerminalConfigurationResponse = OnTerminalConfigurationResponse;
            _spi.TerminalStatusResponse = OnTerminalStatusResponse;

            _pat.GetBillStatus = OnPayAtTableGetBillStatus;
            _pat.BillPaymentReceived = OnPayAtTableBillPaymentReceived;
            _pat.BillPaymentFlowEnded = OnPayAtTableBillPaymentFlowEnded;
            _pat.GetOpenTables = OnPayAtTableGetOpenTables;
        }

        private void OnTxFlowStateChanged(object sender, TransactionFlowState transactionFlowState)
        {
            callBackTransactionState(transactionFlowState);
        }

        private void OnPairingFlowStateChanged(object sender, PairingFlowState pairingFlowState)
        {
            callBackPairingFlowState(pairingFlowState);
        }

        private void OnSecretsChanged(object sender, Secrets secrets)
        {
            callBackSecrets(secrets);
        }

        private void OnSpiStatusChanged(object sender, SpiStatusEventArgs spiStatus)
        {
            callBackStatus(spiStatus);
        }

        private void OnDeviceAddressStatusChanged(object sender, DeviceAddressStatus deviceAddressStatus)
        {
            callBackDeviceAddressStatus(deviceAddressStatus);
        }

        private void OnBatteryLevelChanged(Message msg)
        {
            callBackBatteryLevelChanged(msg);
        }

        private void OnPrintingResponse(Message msg)
        {
            callBackPrintingResponse(msg);
        }
        private void OnTerminalConfigurationResponse(Message msg)
        {
            callBackTerminalConfigurationResponse(msg);
        }
        private void OnTerminalStatusResponse(Message msg)
        {
            callBackTerminalStatusResponse(msg);
        }

        private void OnPayAtTableBillPaymentFlowEnded(Message msg)
        {
            callBackPayAtTableBillPaymentFlowEndedResponse(msg);
        }

        private GetOpenTablesResponse OnPayAtTableGetOpenTables(string operatorId)
        {
            BillStatusRequest billStatusRequest = new BillStatusRequest
            {
                BillId = "",
                TableId = "",
                OperatorId = operatorId,
                PaymentFlowStarted = false
            };
            GetOpenTablesResponse getOpenTablesResponse = new GetOpenTablesResponse();
            callBackPayAtTableGetOpenTables(billStatusRequest, out getOpenTablesResponse);

            return getOpenTablesResponse;
        }

        private BillStatusResponse OnPayAtTableGetBillStatus(string billId, string tableId, string operatorId, bool paymentFlowStarted)
        {
            BillStatusRequest billStatusRequest = new BillStatusRequest
            {
                BillId = billId,
                TableId = tableId,
                OperatorId = operatorId,
                PaymentFlowStarted = paymentFlowStarted
            };

            BillStatusResponse billStatusResponse = new BillStatusResponse();
            callBackPayAtTableGetBillStatus(billStatusRequest, out billStatusResponse);
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

        public PrintingResponse PrintingResponseInit(Message m)
        {
            return new PrintingResponse(m);
        }

        public TerminalStatusResponse TerminalStatusResponseInit(Message m)
        {
            return new TerminalStatusResponse(m);
        }

        public TerminalConfigurationResponse TerminalConfigurationResponseInit(Message m)
        {
            return new TerminalConfigurationResponse(m);
        }

        public TerminalBattery TerminalBatteryInit(Message m)
        {
            return new TerminalBattery(m);
        }

        public BillPaymentFlowEndedResponse BillPaymentFlowEndedResponseInit(Message m)
        {
            return new BillPaymentFlowEndedResponse(m);
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
            XmlConfigurator.ConfigureAndWatch(new FileInfo("SPIClient.dll.config"));
        }
    }

    /// <summary>
    /// This class is wrapper for COM interop.
    /// </summary>
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class GetOpenTablesCom
    {
        private List<OpenTablesEntry> OpenTablesList;

        public GetOpenTablesCom()
        {
            OpenTablesList = new List<OpenTablesEntry>();
        }


        public void AddToOpenTablesList(OpenTablesEntry openTablesEntry)
        {
            OpenTablesList.Add(openTablesEntry);
        }

        public string ToOpenTablesJson()
        {
            var openTableListJson = JsonConvert.SerializeObject(OpenTablesList);

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(openTableListJson));
        }
    }

    /// <summary>
    /// This class is wrapper for COM interop.
    /// </summary>
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class BillStatusRequest
    {
        public BillStatusRequest() { }

        public string BillId { get; set; }

        public string TableId { get; set; }

        public string OperatorId { get; set; }

        public bool PaymentFlowStarted { get; set; }
    }

    /// <summary>
    /// This class is wrapper for COM interop.
    /// </summary>
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class BillPaymentInfo
    {
        public BillPaymentInfo() { }

        public BillPayment BillPayment { get; set; }

        public string UpdatedBillData { get; set; }
    }

    /// <summary>
    /// This class is wrapper for COM interop.
    /// </summary>
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class DelegationPointers
    {
        public DelegationPointers() { }

        public Int32 CBTransactionStatePtr { get; set; }

        public Int32 CBPairingFlowStatePtr { get; set; }

        public Int32 CBSecretsPtr { get; set; }

        public Int32 CBStatusPtr { get; set; }

        public Int32 CBDeviceAddressChangedPtr { get; set; }

        public Int32 CBPayAtTableGetBillDetailsPtr { get; set; }

        public Int32 CBPayAtTableBillPaymentReceivedPtr { get; set; }

        public Int32 CBPayAtTableBillPaymentFlowEndedResponsePtr { get; set; }

        public Int32 CBPayAtTableGetOpenTablesPtr { get; set; }

        public Int32 CBPrintingResponsePtr { get; set; }

        public Int32 CBTerminalStatusResponsePtr { get; set; }

        public Int32 CBTerminalConfigurationResponsePtr { get; set; }

        public Int32 CBBatteryLevelChangedPtr { get; set; }

    }
}
