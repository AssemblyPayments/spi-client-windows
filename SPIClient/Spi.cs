using System;
using Serilog;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SPIClient.Service;

namespace SPIClient
{

    /// <summary>
    /// Subscribe to this event to know when the Printing response
    /// </summary>
    public delegate void SpiPrintingResponse(Message message);

    /// <summary>
    /// Subscribe to this event to know when the Terminal Status response
    /// </summary>
    public delegate void SpiTerminalStatusResponse(Message message);

    /// <summary>
    /// Subscribe to this event to know when the Terminal Configuration response
    /// </summary>
    public delegate void SpiTerminalConfigurationResponse(Message message);

    /// <summary>
    /// Subscribe to this event to know when the Battery level changed
    /// </summary>
    public delegate void SpiBatteryLevelChanged(Message message);

    /// <summary>
    /// Delegate for transaction update message
    /// </summary>
    public delegate void SpiTransactionUpdateMessage(Message message);

    /// <summary>
    /// These attributes work for COM interop.
    /// </summary>
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class Spi : IDisposable
    {
        #region Public Properties and Events

        public readonly SpiConfig Config = new SpiConfig();

        /// <summary>
        /// The Current Status of this Spi instance. Unpaired, PairedConnecting or PairedConnected.
        /// </summary>
        public SpiStatus CurrentStatus
        {
            get => _currentStatus;
            private set
            {
                if (_currentStatus == value)
                    return;
                _currentStatus = value;
                _statusChanged(this, new SpiStatusEventArgs { SpiStatus = value });
            }
        }

        /// <summary>
        /// Subscribe to this Event to know when the Status has changed.
        /// </summary>
        public event EventHandler<SpiStatusEventArgs> StatusChanged
        {
            add => _statusChanged = _statusChanged + value;
            remove => _statusChanged = _statusChanged - value;
        }

        public DeviceAddressStatus CurrentDeviceStatus { get; internal set; }

        /// <summary>
        /// Subscribe to this event when you want to know if the address of the device have changed
        /// </summary>
        public event EventHandler<DeviceAddressStatus> DeviceAddressChanged
        {
            add => _deviceAddressChanged = _deviceAddressChanged + value;
            remove => _deviceAddressChanged = _deviceAddressChanged - value;
        }

        /// <summary>
        /// The current Flow that this Spi instance is currently in.
        /// </summary>
        public SpiFlow CurrentFlow { get; internal set; }

        /// <summary>
        /// When CurrentFlow==Pairing, this represents the state of the pairing process. 
        /// </summary>
        public PairingFlowState CurrentPairingFlowState { get; private set; }

        /// <summary>
        /// Subscribe to this event to know when the CurrentPairingFlowState changes 
        /// </summary>
        public event EventHandler<PairingFlowState> PairingFlowStateChanged
        {
            add => _pairingFlowStateChanged = _pairingFlowStateChanged + value;
            remove => _pairingFlowStateChanged = _pairingFlowStateChanged - value;
        }

        /// <summary>
        /// When CurrentFlow==Transaction, this represents the state of the transaction process.
        /// </summary>
        public TransactionFlowState CurrentTxFlowState { get; internal set; }

        /// <summary>
        /// Subscribe to this event to know when the CurrentPairingFlowState changes
        /// </summary>
        public event EventHandler<TransactionFlowState> TxFlowStateChanged
        {
            add => _txFlowStateChanged = _txFlowStateChanged + value;
            remove => _txFlowStateChanged = _txFlowStateChanged - value;
        }

        /// <summary>
        /// Subscribe to this event to know when the Secrets change, such as at the end of the pairing process,
        /// or everytime that the keys are periodicaly rolled. You then need to persist the secrets safely
        /// so you can instantiate Spi with them next time around.
        /// </summary>
        public event EventHandler<Secrets> SecretsChanged
        {
            add => _secretsChanged = _secretsChanged + value;
            remove => _secretsChanged = _secretsChanged - value;
        }
        #endregion

        #region Setup Methods

        /// <summary>
        /// This default stucture works for COM interop.
        /// </summary>
        public Spi() { }

        /// <summary>
        /// Constructor chaining
        /// </summary>
        /// <param name="posId"></param>
        /// <param name="eftposAddress"></param>
        /// <param name="secrets"></param>
        public Spi(string posId, string eftposAddress, Secrets secrets) : this(posId, "", eftposAddress, secrets)
        {
            _pairUsingEftposAddress = true;
        }

        /// <summary>
        /// Create a new Spi instance. 
        /// If you provide secrets, it will start in PairedConnecting status; Otherwise it will start in Unpaired status.
        /// </summary>
        /// <param name="posId">Uppercase AlphaNumeric string that Indentifies your POS instance. This value is displayed on the EFTPOS screen.</param>
        /// <param name="serialNumber">Serial number of the EFTPOS device</param>
        /// <param name="eftposAddress">The IP address of the target EFTPOS.</param>
        /// <param name="secrets">The Pairing secrets, if you know it already, or null otherwise</param>
        public Spi(string posId, string serialNumber, string eftposAddress, Secrets secrets)
        {
            _posId = posId;
            _serialNumber = serialNumber;
            _secrets = secrets;
            _eftposAddress = "ws://" + eftposAddress;

            // Our stamp for signing outgoing messages
            _spiMessageStamp = new MessageStamp(_posId, _secrets);
            _secrets = secrets;

            // We will maintain some state
            _mostRecentPingSent = null;
            _mostRecentPongReceived = null;
            _missedPongsCount = 0;

            // configure global logging
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(@"Spi.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }

        public SpiPayAtTable EnablePayAtTable()
        {
            _spiPat = new SpiPayAtTable(this);
            return _spiPat;
        }

        public SpiPreauth EnablePreauth()
        {
            _spiPreauth = new SpiPreauth(this, _txLock);
            return _spiPreauth;
        }

        /// <summary>
        /// Call this method after constructing an instance of the class and subscribing to events.
        /// It will start background maintenance threads. 
        /// Most importantly, it connects to the Eftpos server if it has secrets. 
        /// </summary>
        public void Start()
        {
            if (string.IsNullOrWhiteSpace(_posVendorId) || string.IsNullOrWhiteSpace(_posVersion))
            {
                // POS information is now required to be set
                Log.Warning("Missing POS vendor ID and version. posVendorId and posVersion are required before starting");
                throw new NullReferenceException("Missing POS vendor ID and version. posVendorId and posVersion are required before starting");
            }

            if (!IsPosIdValid(_posId))
            {
                // continue, as they can set the posId later on
                _posId = "";
                Log.Warning("Invalid parameter, please correct them before pairing");
            }

            if (!IsEftposAddressValid(_eftposAddress))
            {
                // continue, as they can set the eftposAddress later on
                _eftposAddress = "";
                Log.Warning("Invalid parameter, please correct them before pairing");
            }

            _resetConn();
            _startTransactionMonitoringThread();

            CurrentFlow = SpiFlow.Idle;
            if (_secrets != null)
            {
                Log.Information("Starting in Paired State");
                CurrentStatus = SpiStatus.PairedConnecting;
                _conn.Connect(); // This is non-blocking
            }
            else
            {
                Log.Information("Starting in Unpaired State");
                _currentStatus = SpiStatus.Unpaired;
            }
        }


        /// <summary>
        /// Set the acquirer code of your bank, please contact mx51's for acquirer code.
        /// </summary>
        public bool SetAcquirerCode(string acquirerCode)
        {
            _acquirerCode = acquirerCode;
            return true;
        }

        /// <summary>
        /// Set the api key used for auto address discovery feature, please contact mx51 for Api key.
        /// </summary>
        /// <returns></returns>
        public bool SetDeviceApiKey(string deviceApiKey)
        {
            _deviceApiKey = deviceApiKey;
            return true;
        }

        /// <summary>
        /// Allows you to set the serial number of the Eftpos
        /// </summary>
        public bool SetSerialNumber(string serialNumber)
        {
            var was = _serialNumber;
            _serialNumber = serialNumber;

            if (HasSerialNumberChanged(was))
            {
                _autoResolveEftposAddress();
            }
            else
            {
                if (CurrentDeviceStatus == null)
                {
                    CurrentDeviceStatus = new DeviceAddressStatus();
                }

                CurrentDeviceStatus.DeviceAddressResponseCode = DeviceAddressResponseCode.SERIAL_NUMBER_NOT_CHANGED;
                _deviceAddressChanged?.Invoke(this, CurrentDeviceStatus);
            }

            return true;
        }

        /// <summary>
        /// Allows you to set the auto address discovery feature. 
        /// </summary>
        /// <returns></returns>
        public bool SetAutoAddressResolution(bool autoAddressResolutionEnable)
        {
            var was = _autoAddressResolutionEnabled;
            _autoAddressResolutionEnabled = autoAddressResolutionEnable;

            if (autoAddressResolutionEnable && !was)
            {
                // we're turning it on
                _autoResolveEftposAddress();
            }

            return true;
        }

        /// <summary>
        /// Call this method to set the client library test mode.
        /// Set it to true only while you are developing the integration. 
        /// It defaults to false. For a real merchant, always leave it set to false. 
        /// </summary>
        /// <param name="testMode"></param>
        /// <returns></returns>
        public bool SetTestMode(bool testMode)
        {
            if (testMode == _inTestMode)
                return true;

            // we're changing mode
            _inTestMode = testMode;
            _autoResolveEftposAddress();

            return true;
        }

        /// <summary>
        /// Allows you to set the PosId which identifies this instance of your POS.
        /// Can only be called in in the unpaired state. 
        /// </summary>
        public bool SetPosId(string posId)
        {
            if (CurrentStatus != SpiStatus.Unpaired)
                return false;

            _posId = ""; // reset posId to give more explicit feedback

            if (!IsPosIdValid(posId))
            {
                Log.Information("Pos Id set to null");
                return false;
            }

            _posId = posId;
            _spiMessageStamp.PosId = posId;
            return true;
        }

        /// <summary>
        /// Allows you to set the PinPad address only if auto address is not enabled. Sometimes the PinPad might change IP address 
        /// (we recommend reserving static IPs if possible).
        /// Either way you need to allow your User to enter the IP address of the PinPad.
        /// </summary>
        public bool SetEftposAddress(string address)
        {
            if (CurrentStatus == SpiStatus.PairedConnected)
                return false;

            _eftposAddress = ""; // reset eftposAddress to give more explicit feedback

            if (!IsEftposAddressValid(address))
            {
                Log.Information("Eftpos Address set to null");
                return false;
            }

            _eftposAddress = "ws://" + address;
            _conn.Address = _eftposAddress;
            return true;
        }

        /**
         * Sets values used to identify the POS software to the EFTPOS terminal.
         * <p>
         * Must be set before starting!
         *
         * @param posVendorId Vendor identifier of the POS itself.
         * @param posVersion  Version string of the POS itself.
         */
        public void SetPosInfo(string posVendorId, string posVersion)
        {
            _posVendorId = posVendorId;
            _posVersion = posVersion;
        }

        public static string GetVersion()
        {
            return _version;
        }
        #endregion

        #region Flow Management Methods

        /// <summary>
        /// Call this one when a flow is finished and you want to go back to idle state.
        /// Typically when your user clicks the "OK" bubtton to acknowldge that pairing is
        /// finished, or that transaction is finished.
        /// When true, you can dismiss the flow screen and show back the idle screen.
        /// </summary>
        /// <returns>true means we have moved back to the Idle state. false means current flow was not finished yet.</returns>
        public bool AckFlowEndedAndBackToIdle()
        {
            if (CurrentFlow == SpiFlow.Idle)
                return true; // already idle

            if (CurrentFlow == SpiFlow.Pairing && CurrentPairingFlowState.Finished)
            {
                CurrentFlow = SpiFlow.Idle;
                return true;
            }

            if (CurrentFlow == SpiFlow.Transaction && CurrentTxFlowState.Finished)
            {
                CurrentFlow = SpiFlow.Idle;
                return true;
            }

            return false;
        }

        #endregion

        #region Pairing Flow Methods

        /// <summary>
        /// This will connect to the Eftpos and start the pairing process.
        /// Only call this if you are in the Unpaired state.
        /// Subscribe to the PairingFlowStateChanged event to get updates on the pairing process.
        /// </summary>
        /// <returns>Whether pairing has initiated or not</returns>
        public bool Pair()
        {
            Log.Warning("Trying to pair ....");

            if (CurrentStatus != SpiStatus.Unpaired)
            {
                Log.Warning("Tried to Pair, but we're already paired. Stop pairing.");
                return false;
            }

            if (!IsPosIdValid(_posId) || !IsEftposAddressValid(_eftposAddress))
            {
                Log.Warning("Invalid Pos Id or Eftpos address, stop pairing.");
                return false;
            }

            CurrentFlow = SpiFlow.Pairing;
            CurrentPairingFlowState = new PairingFlowState
            {
                Successful = false,
                Finished = false,
                Message = "Connecting...",
                AwaitingCheckFromEftpos = false,
                AwaitingCheckFromPos = false,
                ConfirmationCode = ""
            };

            _pairingFlowStateChanged(this, CurrentPairingFlowState);
            _conn.Connect(); // Non-Blocking
            return true;
        }

        /// <summary>
        /// Call this when your user clicks yes to confirm the pairing code on your 
        /// screen matches the one on the Eftpos.
        /// </summary>
        public void PairingConfirmCode()
        {
            if (!CurrentPairingFlowState.AwaitingCheckFromPos)
            {
                // We weren't expecting this
                return;
            }

            CurrentPairingFlowState.AwaitingCheckFromPos = false;
            if (CurrentPairingFlowState.AwaitingCheckFromEftpos)
            {
                // But we are still waiting for confirmation from Eftpos side.
                Log.Information("Pair Code Confirmed from POS side, but am still waiting for confirmation from Eftpos.");
                CurrentPairingFlowState.Message =
                    "Click YES on EFTPOS if code is: " + CurrentPairingFlowState.ConfirmationCode;
                _pairingFlowStateChanged(this, CurrentPairingFlowState);
            }
            else
            {
                // Already confirmed from Eftpos - So all good now. We're Paired also from the POS perspective.
                Log.Information("Pair Code Confirmed from POS side, and was already confirmed from Eftpos side. Pairing finalised.");
                _onPairingSuccess();
            }

        }

        /// <summary>
        /// Call this if your user clicks CANCEL or NO during the pairing process.
        /// </summary>
        public void PairingCancel()
        {
            if (CurrentFlow != SpiFlow.Pairing || CurrentPairingFlowState.Finished)
                return;

            if (CurrentPairingFlowState.AwaitingCheckFromPos && !CurrentPairingFlowState.AwaitingCheckFromEftpos)
            {
                // This means that the Eftpos already thinks it's paired.
                // Let's tell it to drop keys
                _send(new DropKeysRequest().ToMessage());
            }
            _onPairingFailed();
        }

        /// <summary>
        /// Call this when your uses clicks the Unpair button.
        /// This will disconnect from the Eftpos and forget the secrets.
        /// The CurrentState is then changed to Unpaired.
        /// Call this only if you are not yet in the Unpaired state.
        /// </summary>
        public bool Unpair()
        {
            if (CurrentStatus == SpiStatus.Unpaired)
                return false;

            if (CurrentFlow != SpiFlow.Idle)
                return false;
            ;

            // Best effort letting the eftpos know that we're dropping the keys, so it can drop them as well.
            _send(new DropKeysRequest().ToMessage());
            _doUnpair();
            return true;
        }

        #endregion

        #region Transaction Methods

        /// <summary>
        /// Initiates a purchase transaction. Be subscribed to TxFlowStateChanged event to get updates on the process.
        /// </summary>
        /// <param name="posRefId">Alphanumeric Identifier for your purchase.</param>
        /// <param name="amountCents">Amount in Cents to charge</param>
        /// <returns>InitiateTxResult</returns>
        public InitiateTxResult InitiatePurchaseTx(string posRefId, int amountCents)
        {
            if (CurrentStatus == SpiStatus.Unpaired) return new InitiateTxResult(false, "Not Paired");

            lock (_txLock)
            {
                if (CurrentFlow != SpiFlow.Idle) return new InitiateTxResult(false, "Not Idle");
                var purchaseRequest = PurchaseHelper.CreatePurchaseRequest(amountCents, posRefId);
                purchaseRequest.Config = Config;
                var purchaseMsg = purchaseRequest.ToMessage();
                CurrentFlow = SpiFlow.Transaction;
                CurrentTxFlowState = new TransactionFlowState(
                    posRefId, TransactionType.Purchase, amountCents, purchaseMsg,
                    $"Waiting for EFTPOS connection to make payment request for ${amountCents / 100.0:.00}");
                if (_send(purchaseMsg))
                {
                    CurrentTxFlowState.Sent($"Asked EFTPOS to accept payment for ${amountCents / 100.0:.00}");
                }
            }
            _txFlowStateChanged(this, CurrentTxFlowState);
            return new InitiateTxResult(true, "Purchase Initiated");
        }

        /// <summary>
        /// Initiates a purchase transaction. Be subscribed to TxFlowStateChanged event to get updates on the process.
        /// <para>Tip and cashout are not allowed simultaneously.</para>
        /// </summary>
        /// <param name="posRefId">An Unique Identifier for your Order/Purchase</param>
        /// <param name="purchaseAmount">The Purchase Amount in Cents.</param>
        /// <param name="tipAmount">The Tip Amount in Cents</param>
        /// <param name="cashoutAmount">The Cashout Amount in Cents</param>
        /// <param name="promptForCashout">Whether to prompt your customer for cashout on the Eftpos</param>
        /// <returns>InitiateTxResult</returns>
        public InitiateTxResult InitiatePurchaseTxV2(string posRefId, int purchaseAmount, int tipAmount, int cashoutAmount, bool promptForCashout)
        {
            return InitiatePurchaseTxV2(posRefId, purchaseAmount, tipAmount, cashoutAmount, promptForCashout, new TransactionOptions());
        }

        /// <summary>
        /// Initiates a purchase transaction. Be subscribed to TxFlowStateChanged event to get updates on the process.
        /// <para>Tip and cashout are not allowed simultaneously.</para>
        /// </summary>
        /// <param name="posRefId">An Unique Identifier for your Order/Purchase</param>
        /// <param name="purchaseAmount">The Purchase Amount in Cents.</param>
        /// <param name="tipAmount">The Tip Amount in Cents</param>
        /// <param name="cashoutAmount">The Cashout Amount in Cents</param>
        /// <param name="promptForCashout">Whether to prompt your customer for cashout on the Eftpos</param>
        /// <param name="options">The Setting to set Header and Footer for the Receipt</param>
        /// <returns>InitiateTxResult</returns>
        public InitiateTxResult InitiatePurchaseTxV2(string posRefId, int purchaseAmount, int tipAmount, int cashoutAmount, bool promptForCashout, TransactionOptions options)
        {
            return InitiatePurchaseTxV2(posRefId, purchaseAmount, tipAmount, cashoutAmount, promptForCashout, options, 0);
        }

        /// <summary>
        /// Initiates a purchase transaction. Be subscribed to TxFlowStateChanged event to get updates on the process.
        /// <para>Tip and cashout are not allowed simultaneously.</para>
        /// </summary>
        /// <param name="posRefId">An Unique Identifier for your Order/Purchase</param>
        /// <param name="purchaseAmount">The Purchase Amount in Cents.</param>
        /// <param name="tipAmount">The Tip Amount in Cents</param>
        /// <param name="cashoutAmount">The Cashout Amount in Cents</param>
        /// <param name="surchargeAmount">The Surcharge Amount in Cents</param>
        /// <param name="promptForCashout">Whether to prompt your customer for cashout on the Eftpos</param>
        /// <param name="options">The Setting to set Header and Footer for the Receipt</param>
        /// <param name="surchargeAmount">The Surcharge Amount in Cents</param>
        /// <returns>InitiateTxResult</returns>
        public InitiateTxResult InitiatePurchaseTxV2(string posRefId, int purchaseAmount, int tipAmount, int cashoutAmount, bool promptForCashout, TransactionOptions options, int surchargeAmount)
        {
            if (CurrentStatus == SpiStatus.Unpaired) return new InitiateTxResult(false, "Not Paired");

            if (tipAmount > 0 && (cashoutAmount > 0 || promptForCashout)) return new InitiateTxResult(false, "Cannot Accept Tips and Cashout at the same time.");

            // no printing available, reset header and footer and disable print
            if (!TerminalHelper.IsPrinterAvailable(_terminalModel) && _isPrintingConfigEnabled())
            {
                options = new TransactionOptions();
                Config.PromptForCustomerCopyOnEftpos = false;
                Config.PrintMerchantCopy = false;
                Config.SignatureFlowOnEftpos = false;
                Log.Warning("Printing is enabled on a terminal without printer. Printing options will now be disabled.");
            }

            lock (_txLock)
            {
                if (CurrentFlow != SpiFlow.Idle) return new InitiateTxResult(false, "Not Idle");
                CurrentFlow = SpiFlow.Transaction;

                var purchase = PurchaseHelper.CreatePurchaseRequestV2(posRefId, purchaseAmount, tipAmount, cashoutAmount, promptForCashout, surchargeAmount);
                purchase.Config = Config;
                purchase.Options = options;
                var purchaseMsg = purchase.ToMessage();
                CurrentTxFlowState = new TransactionFlowState(
                    posRefId, TransactionType.Purchase, purchaseAmount, purchaseMsg,
                    $"Waiting for EFTPOS connection to make payment request. {purchase.AmountSummary()}");
                if (_send(purchaseMsg))
                {
                    CurrentTxFlowState.Sent($"Asked EFTPOS to accept payment for ${purchase.AmountSummary()}");
                }
            }
            _txFlowStateChanged(this, CurrentTxFlowState);
            return new InitiateTxResult(true, "Purchase Initiated");
        }

        /// <summary>
        /// Initiates a refund transaction. Be subscribed to TxFlowStateChanged event to get updates on the process.
        /// </summary>
        /// <param name="posRefId">Alphanumeric Identifier for your refund.</param>
        /// <param name="amountCents">Amount in Cents to charge</param>
        /// <returns>InitiateTxResult</returns>
        public InitiateTxResult InitiateRefundTx(string posRefId, int amountCents)
        {
            return InitiateRefundTx(posRefId, amountCents, false);
        }

        /// <summary>
        /// Initiates a refund transaction. Be subscribed to TxFlowStateChanged event to get updates on the process.
        /// </summary>
        /// <param name="posRefId">Alphanumeric Identifier for your refund.</param>
        /// <param name="amountCents">Amount in Cents to charge</param>
        /// <param name="isSuppressMerchantPassword">Merchant Password control in VAA</param>
        /// <returns>InitiateTxResult</returns>
        public InitiateTxResult InitiateRefundTx(string posRefId, int amountCents, bool suppressMerchantPassword)
        {
            return InitiateRefundTx(posRefId, amountCents, suppressMerchantPassword, new TransactionOptions());
        }

        /// <summary>
        /// Initiates a refund transaction. Be subscribed to TxFlowStateChanged event to get updates on the process.
        /// </summary>
        /// <param name="posRefId">Alphanumeric Identifier for your refund.</param>
        /// <param name="amountCents">Amount in Cents to charge</param>
        /// <param name="suppressMerchantPassword">Merchant Password control in VAA</param>
        /// <param name="options">The Setting to set Header and Footer for the Receipt</param>
        /// <returns>InitiateTxResult</returns>
        public InitiateTxResult InitiateRefundTx(string posRefId, int amountCents, bool suppressMerchantPassword, TransactionOptions options)
        {
            if (CurrentStatus == SpiStatus.Unpaired) return new InitiateTxResult(false, "Not Paired");

            // no printing available, reset header and footer and disable print
            if (!TerminalHelper.IsPrinterAvailable(_terminalModel) && _isPrintingConfigEnabled())
            {
                options = new TransactionOptions();
                Config.PromptForCustomerCopyOnEftpos = false;
                Config.PrintMerchantCopy = false;
                Config.SignatureFlowOnEftpos = false;
                Log.Warning("Printing is enabled on a terminal without printer. Printing options will now be disabled.");
            }

            lock (_txLock)
            {
                if (CurrentFlow != SpiFlow.Idle) return new InitiateTxResult(false, "Not Idle");
                var refundRequest = PurchaseHelper.CreateRefundRequest(amountCents, posRefId, suppressMerchantPassword);
                refundRequest.Config = Config;
                refundRequest.Options = options;
                var refundMsg = refundRequest.ToMessage();
                CurrentFlow = SpiFlow.Transaction;
                CurrentTxFlowState = new TransactionFlowState(
                    posRefId, TransactionType.Refund, amountCents, refundMsg,
                    $"Waiting for EFTPOS connection to make refund request for ${amountCents / 100.0:.00}");
                if (_send(refundMsg))
                {
                    CurrentTxFlowState.Sent($"Asked EFTPOS to refund ${amountCents / 100.0:.00}");
                }
            }
            _txFlowStateChanged(this, CurrentTxFlowState);
            return new InitiateTxResult(true, "Refund Initiated");
        }

        /// <summary>
        /// Let the EFTPOS know whether merchant accepted or declined the signature
        /// </summary>
        /// <param name="accepted">whether merchant accepted the signature from customer or not</param>
        /// <returns>MidTxResult - false only if you called it in the wrong state</returns>
        public MidTxResult AcceptSignature(bool accepted)
        {
            lock (_txLock)
            {
                if (CurrentFlow != SpiFlow.Transaction || CurrentTxFlowState.Finished || !CurrentTxFlowState.AwaitingSignatureCheck)
                {
                    Log.Information("Asked to accept signature but I was not waiting for one.");
                    return new MidTxResult(false, "Asked to accept signature but I was not waiting for one.");
                }

                CurrentTxFlowState.SignatureResponded(accepted ? "Accepting Signature..." : "Declining Signature...");
                _send(accepted
                    ? new SignatureAccept(CurrentTxFlowState.PosRefId).ToMessage()
                    : new SignatureDecline(CurrentTxFlowState.PosRefId).ToMessage());
            }
            _txFlowStateChanged(this, CurrentTxFlowState);
            return new MidTxResult(true, "");
        }


        /// <summary>
        /// Submit the Code obtained by your user when phoning for auth. 
        /// It will return immediately to tell you whether the code has a valid format or not. 
        /// If valid==true is returned, no need to do anything else. Expect updates via standard callback.
        /// If valid==false is returned, you can show your user the accompanying message, and invite them to enter another code. 
        /// </summary>
        /// <param name="authCode">The code obtained by your user from the merchant call centre. It should be a 6-character alpha-numeric value.</param>
        /// <returns>Whether code has a valid format or not.</returns>
        public SubmitAuthCodeResult SubmitAuthCode(string authCode)
        {
            if (authCode.Length != 6)
            {
                return new SubmitAuthCodeResult(false, "Not a 6-digit code.");
            }

            lock (_txLock)
            {
                if (CurrentFlow != SpiFlow.Transaction || CurrentTxFlowState.Finished || !CurrentTxFlowState.AwaitingPhoneForAuth)
                {
                    Log.Information("Asked to send auth code but I was not waiting for one.");
                    return new SubmitAuthCodeResult(false, "Was not waiting for one.");
                }

                CurrentTxFlowState.AuthCodeSent($"Submitting Auth Code {authCode}");
                _send(new AuthCodeAdvice(CurrentTxFlowState.PosRefId, authCode).ToMessage());
            }
            _txFlowStateChanged(this, CurrentTxFlowState);
            return new SubmitAuthCodeResult(true, "Valid Code.");
        }

        /// <summary>
        /// Attempts to cancel a Transaction. 
        /// Be subscribed to TxFlowStateChanged event to see how it goes.
        /// Wait for the transaction to be finished and then see whether cancellation was successful or not.
        /// </summary>
        /// <returns>MidTxResult - false only if you called it in the wrong state</returns>
        public MidTxResult CancelTransaction()
        {
            lock (_txLock)
            {
                if (CurrentFlow != SpiFlow.Transaction || CurrentTxFlowState.Finished)
                {
                    Log.Information("Asked to cancel transaction but I was not in the middle of one.");
                    return new MidTxResult(false, "Asked to cancel transaction but I was not in the middle of one.");
                }

                // TH-1C, TH-3C - Merchant pressed cancel
                if (CurrentTxFlowState.RequestSent)
                {
                    var cancelReq = new CancelTransactionRequest();
                    CurrentTxFlowState.Cancelling("Attempting to Cancel Transaction...");
                    _send(cancelReq.ToMessage());
                }
                else
                {
                    // We Had Not Even Sent Request Yet. Consider as known failed.
                    CurrentTxFlowState.Failed(null, "Transaction Cancelled. Request Had not even been sent yet.");
                }
            }
            _txFlowStateChanged(this, CurrentTxFlowState);
            _sendTransactionReport();

            return new MidTxResult(true, "");
        }

        /// <summary>
        /// Initiates a cashout only transaction. Be subscribed to TxFlowStateChanged event to get updates on the process.
        /// </summary>
        /// <param name="posRefId">Alphanumeric Identifier for your transaction.</param>
        /// <param name="amountCents">Amount in Cents to cash out</param>
        /// <returns>InitiateTxResult</returns>
        public InitiateTxResult InitiateCashoutOnlyTx(string posRefId, int amountCents)
        {
            return InitiateCashoutOnlyTx(posRefId, amountCents, 0);
        }

        /// <summary>
        /// Initiates a cashout only transaction. Be subscribed to TxFlowStateChanged event to get updates on the process.
        /// </summary>
        /// <param name="posRefId">Alphanumeric Identifier for your transaction.</param>
        /// <param name="amountCents">Amount in Cents to cash out</param>
        /// <param name="surchargeAmount">The Surcharge Amount in Cents</param>
        /// <returns>InitiateTxResult</returns>
        public InitiateTxResult InitiateCashoutOnlyTx(string posRefId, int amountCents, int surchargeAmount)
        {
            return InitiateCashoutOnlyTx(posRefId, amountCents, surchargeAmount, new TransactionOptions());
        }

        /// <summary>
        /// Initiates a cashout only transaction. Be subscribed to TxFlowStateChanged event to get updates on the process.
        /// </summary>
        /// <param name="posRefId">Alphanumeric Identifier for your transaction.</param>
        /// <param name="amountCents">Amount in Cents to cash out</param>
        /// <param name="surchargeAmount">The Surcharge Amount in Cents</param>
        /// <param name="options">The Setting to set Header and Footer for the Receipt</param>
        /// <returns>InitiateTxResult</returns>
        public InitiateTxResult InitiateCashoutOnlyTx(string posRefId, int amountCents, int surchargeAmount, TransactionOptions options)
        {
            if (CurrentStatus == SpiStatus.Unpaired) return new InitiateTxResult(false, "Not Paired");

            // no printing available, reset header and footer and disable print
            if (!TerminalHelper.IsPrinterAvailable(_terminalModel) && _isPrintingConfigEnabled())
            {
                options = new TransactionOptions();
                Config.PromptForCustomerCopyOnEftpos = false;
                Config.PrintMerchantCopy = false;
                Config.SignatureFlowOnEftpos = false;
                Log.Warning("Printing is enabled on a terminal without printer. Printing options will now be disabled.");
            }

            lock (_txLock)
            {
                if (CurrentFlow != SpiFlow.Idle) return new InitiateTxResult(false, "Not Idle");
                var cashoutMsg = new CashoutOnlyRequest(amountCents, posRefId)
                {
                    SurchargeAmount = surchargeAmount,
                    Options = options,
                    Config = Config
                }.ToMessage();

                CurrentFlow = SpiFlow.Transaction;
                CurrentTxFlowState = new TransactionFlowState(
                    posRefId, TransactionType.CashoutOnly, amountCents, cashoutMsg,
                    $"Waiting for EFTPOS connection to send cashout request for ${amountCents / 100.0:.00}");
                if (_send(cashoutMsg))
                {
                    CurrentTxFlowState.Sent($"Asked EFTPOS to do cashout for ${amountCents / 100.0:.00}");
                }
            }
            _txFlowStateChanged(this, CurrentTxFlowState);
            return new InitiateTxResult(true, "Cashout Initiated");
        }

        /// <summary>
        /// Initiates a Mail Order / Telephone Order Purchase Transaction
        /// </summary>
        /// <param name="posRefId">Alphanumeric Identifier for your transaction.</param>
        /// <param name="amountCents">Amount in Cents</param>
        /// <returns>InitiateTxResult</returns>
        public InitiateTxResult InitiateMotoPurchaseTx(string posRefId, int amountCents)
        {
            return InitiateMotoPurchaseTx(posRefId, amountCents, 0);
        }

        /// <summary>
        /// Initiates a Mail Order / Telephone Order Purchase Transaction
        /// </summary>
        /// <param name="posRefId">Alphanumeric Identifier for your transaction.</param>
        /// <param name="amountCents">Amount in Cents</param>
        /// <param name="surchargeAmount">The Surcharge Amount in Cents</param>
        /// <returns>InitiateTxResult</returns>
        public InitiateTxResult InitiateMotoPurchaseTx(string posRefId, int amountCents, int surchargeAmount)
        {
            return InitiateMotoPurchaseTx(posRefId, amountCents, surchargeAmount, false);
        }

        /// <summary>
        /// Initiates a Mail Order / Telephone Order Purchase Transaction
        /// </summary>
        /// <param name="posRefId">Alphanumeric Identifier for your transaction.</param>
        /// <param name="amountCents">Amount in Cents</param>
        /// <param name="surchargeAmount">The Surcharge Amount in Cents</param>
        /// <param name="isSuppressMerchantPassword">>Merchant Password control in VAA</param>
        /// <returns>InitiateTxResult</returns>
        public InitiateTxResult InitiateMotoPurchaseTx(string posRefId, int amountCents, int surchargeAmount, bool suppressMerchantPassword)
        {
            return InitiateMotoPurchaseTx(posRefId, amountCents, surchargeAmount, suppressMerchantPassword, new TransactionOptions());
        }

        /// <summary>
        /// Initiates a Mail Order / Telephone Order Purchase Transaction
        /// </summary>
        /// <param name="posRefId">Alphanumeric Identifier for your transaction.</param>
        /// <param name="amountCents">Amount in Cents</param>
        /// <param name="surchargeAmount">The Surcharge Amount in Cents</param>
        /// <param name="isSuppressMerchantPassword">>Merchant Password control in VAA</param>
        /// <param name="options">The Setting to set Header and Footer for the Receipt</param>
        /// <returns>InitiateTxResult</returns>
        public InitiateTxResult InitiateMotoPurchaseTx(string posRefId, int amountCents, int surchargeAmount, bool suppressMerchantPassword, TransactionOptions options)
        {
            if (CurrentStatus == SpiStatus.Unpaired) return new InitiateTxResult(false, "Not Paired");

            // no printing available, reset header and footer and disable print
            if (!TerminalHelper.IsPrinterAvailable(_terminalModel) && _isPrintingConfigEnabled())
            {
                options = new TransactionOptions();
                Config.PromptForCustomerCopyOnEftpos = false;
                Config.PrintMerchantCopy = false;
                Config.SignatureFlowOnEftpos = false;
                Log.Warning("Printing is enabled on a terminal without printer. Printing options will now be disabled.");
            }

            lock (_txLock)
            {
                if (CurrentFlow != SpiFlow.Idle) return new InitiateTxResult(false, "Not Idle");
                var motoPurchaseMsg = new MotoPurchaseRequest(amountCents, posRefId)
                {
                    SurchargeAmount = surchargeAmount,
                    SuppressMerchantPassword = suppressMerchantPassword,
                    Config = Config,
                    Options = options
                }.ToMessage();

                CurrentFlow = SpiFlow.Transaction;
                CurrentTxFlowState = new TransactionFlowState(
                    posRefId, TransactionType.MOTO, amountCents, motoPurchaseMsg,
                    $"Waiting for EFTPOS connection to send MOTO request for ${amountCents / 100.0:.00}");
                if (_send(motoPurchaseMsg))
                {
                    CurrentTxFlowState.Sent($"Asked EFTPOS do MOTO for ${amountCents / 100.0:.00}");
                }
            }
            _txFlowStateChanged(this, CurrentTxFlowState);
            return new InitiateTxResult(true, "MOTO Initiated");
        }

        /// <summary>
        /// Initiates a settlement transaction.
        /// Be subscribed to TxFlowStateChanged event to get updates on the process.
        /// </summary>
        public InitiateTxResult InitiateSettleTx(string posRefId)
        {
            return InitiateSettleTx(posRefId, new TransactionOptions());
        }

        /// <summary>
        /// Initiates a settlement transaction.
        /// Be subscribed to TxFlowStateChanged event to get updates on the process.
        /// <param name="options">The Setting to set Header and Footer for the Receipt</param>
        /// </summary>
        public InitiateTxResult InitiateSettleTx(string posRefId, TransactionOptions options)
        {
            if (CurrentStatus == SpiStatus.Unpaired) return new InitiateTxResult(false, "Not Paired");

            // no printing available, reset header and footer and disable print
            if (!TerminalHelper.IsPrinterAvailable(_terminalModel) && _isPrintingConfigEnabled())
            {
                options = new TransactionOptions();
                Config.PromptForCustomerCopyOnEftpos = false;
                Config.PrintMerchantCopy = false;
                Config.SignatureFlowOnEftpos = false;
                Log.Warning("Printing is enabled on a terminal without printer. Printing options will now be disabled.");
            }

            lock (_txLock)
            {
                if (CurrentFlow != SpiFlow.Idle) return new InitiateTxResult(false, "Not Idle");
                var settleMsg = new SettleRequest(RequestIdHelper.Id("settle"))
                {
                    Config = Config,
                    Options = options
                }.ToMessage();

                CurrentFlow = SpiFlow.Transaction;
                CurrentTxFlowState = new TransactionFlowState(
                    posRefId, TransactionType.Settle, 0, settleMsg,
                    $"Waiting for EFTPOS connection to make a settle request");
                if (_send(settleMsg))
                {
                    CurrentTxFlowState.Sent($"Asked EFTPOS to settle.");
                }
            }
            _txFlowStateChanged(this, CurrentTxFlowState);
            return new InitiateTxResult(true, "Settle Initiated");
        }

        /// <summary>
        /// </summary>
        public InitiateTxResult InitiateSettlementEnquiry(string posRefId)
        {
            return InitiateSettlementEnquiry(posRefId, new TransactionOptions());
        }

        /// <summary>
        /// <param name="options">The Setting to set Header and Footer for the Receipt</param>
        /// </summary>
        public InitiateTxResult InitiateSettlementEnquiry(string posRefId, TransactionOptions options)
        {
            if (CurrentStatus == SpiStatus.Unpaired) return new InitiateTxResult(false, "Not Paired");

            lock (_txLock)
            {
                if (CurrentFlow != SpiFlow.Idle) return new InitiateTxResult(false, "Not Idle");
                var stlEnqMsg = new SettlementEnquiryRequest(RequestIdHelper.Id("stlenq"))
                {
                    Config = Config,
                    Options = options
                }.ToMessage();

                CurrentFlow = SpiFlow.Transaction;
                CurrentTxFlowState = new TransactionFlowState(
                    posRefId, TransactionType.SettlementEnquiry, 0, stlEnqMsg,
                    $"Waiting for EFTPOS connection to make a settlement enquiry");
                if (_send(stlEnqMsg))
                {
                    CurrentTxFlowState.Sent($"Asked EFTPOS to make a settlement enquiry.");
                }
            }
            _txFlowStateChanged(this, CurrentTxFlowState);
            return new InitiateTxResult(true, "Settle Initiated");
        }

        /// <summary>
        /// Initiates a Get Last Transaction. Use this when you want to retrieve the most recent transaction
        /// that was processed by the Eftpos.
        /// Be subscribed to TxFlowStateChanged event to get updates on the process.
        /// </summary>
        [Obsolete("Use InitiateGetTx()")]
        public InitiateTxResult InitiateGetLastTx()
        {
            if (CurrentStatus == SpiStatus.Unpaired) return new InitiateTxResult(false, "Not Paired");

            lock (_txLock)
            {
                if (CurrentFlow != SpiFlow.Idle) return new InitiateTxResult(false, "Not Idle");
                
                CurrentFlow = SpiFlow.Transaction;
                var gltRequestMsg = new GetLastTransactionRequest().ToMessage();
                var posRefId = gltRequestMsg.Id; // GetLastTx is not trying to get anything specific back. So we just use the message id.
                
                CurrentTxFlowState = new TransactionFlowState(
                    posRefId, TransactionType.GetLastTransaction, 0, gltRequestMsg,
                    $"Waiting for EFTPOS connection to make a Get-Last-Transaction request.");
                if (_send(gltRequestMsg))
                {
                    CurrentTxFlowState.Sent($"Asked EFTPOS to Get Last Transaction.");
                }
            }
            _txFlowStateChanged(this, CurrentTxFlowState);
            return new InitiateTxResult(true, "GLT Initiated");
        }

        /// <summary>
        /// Initiates a Get Transaction request. Use this when you want to retrieve from one of the last 10 transactions
        /// that was processed by the Eftpos.
        /// Be subscribed to TxFlowStateChanged event to get updates on the process.
        /// </summary>
        /// <param name="posRefId">This is the posRefId of the transaction you are trying to retrieve</param>
        public InitiateTxResult InitiateGetTx(string posRefId)
        {
            if (CurrentStatus == SpiStatus.Unpaired) return new InitiateTxResult(false, "Not Paired");

            lock (_txLock)
            {
                if (CurrentFlow != SpiFlow.Idle) return new InitiateTxResult(false, "Not Idle");

                var gtRequestMsg = new GetTransactionRequest(posRefId).ToMessage();
                CurrentFlow = SpiFlow.Transaction;
                CurrentTxFlowState = new TransactionFlowState(posRefId, TransactionType.GetTransaction, 0, gtRequestMsg, $"Waiting for EFTPOS connection to make a Get Transaction request.");
                CurrentTxFlowState.CallingGt(gtRequestMsg.Id);
                if (_send(gtRequestMsg))
                {
                    CurrentTxFlowState.Sent($"Asked EFTPOS to Get Transaction {posRefId}.");
                }
            }
            _txFlowStateChanged(this, CurrentTxFlowState);
            return new InitiateTxResult(true, "GT Initiated");

        }

        /// <summary>
        /// This is useful to recover from your POS crashing in the middle of a transaction.
        /// When you restart your POS, if you had saved enough state, you can call this method to recover the client library state.
        /// You need to have the posRefId that you passed in with the original transaction, and the transaction type.
        /// This method will return immediately whether recovery has started or not.
        /// If recovery has started, you need to bring up the transaction modal to your user a be listening to TxFlowStateChanged.
        /// </summary>
        /// <param name="posRefId">The is that you had assigned to the transaction that you are trying to recover.</param>
        /// <param name="txType">The transaction type.</param>
        /// <returns></returns>
        public InitiateTxResult InitiateRecovery(string posRefId, TransactionType txType)
        {
            if (CurrentStatus == SpiStatus.Unpaired) return new InitiateTxResult(false, "Not Paired");

            lock (_txLock)
            {
                if (CurrentFlow != SpiFlow.Idle) return new InitiateTxResult(false, "Not Idle");

                CurrentFlow = SpiFlow.Transaction;

                var gtRequestMsg = new GetTransactionRequest(posRefId).ToMessage();
                CurrentTxFlowState = new TransactionFlowState(posRefId, txType, 0, gtRequestMsg, $"Waiting for EFTPOS connection to attempt recovery.");
                CurrentTxFlowState.CallingGt(gtRequestMsg.Id);

                if (_send(gtRequestMsg))
                {
                    CurrentTxFlowState.Sent($"Asked EFTPOS to recover state.");
                }
            }
            _txFlowStateChanged(this, CurrentTxFlowState);
            return new InitiateTxResult(true, "Recovery Initiated");
        }

        /// <summary>
        /// Alpha Build - Please do not use
        /// </summary>
        /// <param name="posRefId"></param>
        /// <returns></returns>
        public InitiateTxResult InitiateReversal(string posRefId)
        {
            if (CurrentStatus == SpiStatus.Unpaired) return new InitiateTxResult(false, "Not Paired");

            lock (_txLock)
            {

                if (CurrentFlow != SpiFlow.Idle) return new InitiateTxResult(false, "Not Idle");

                CurrentFlow = SpiFlow.Transaction;

                var reversalRequestMsg = new ReversalRequest(posRefId).ToMessage();
                CurrentTxFlowState = new TransactionFlowState(posRefId, TransactionType.Reversal, 0, reversalRequestMsg, $"Waiting for EFTPOS to make a reversal request");
                if (_send(reversalRequestMsg))
                {
                    CurrentTxFlowState.Sent($"Asked EFTPOS reversal");
                }
            }

            return new InitiateTxResult(true, "Reversal Initiated");
        }
        
        public void PrintReport(string key, string payload)
        {
            if (CurrentStatus == SpiStatus.PairedConnected)
            {
                lock (_txLock)
                {
                    _send(new PrintingRequest(key, payload).ToMessage());
                }
            }
        }
        #endregion

        #region Device Management Methods
        public void GetTerminalStatus()
        {
            if (CurrentStatus == SpiStatus.PairedConnected)
            {
                lock (_txLock)
                {
                    _send(new TerminalStatusRequest().ToMessage());
                }
            }
        }

        public void GetTerminalConfiguration()
        {
            if (CurrentStatus == SpiStatus.PairedConnected)
            {
                lock (_txLock)
                {
                    _send(new TerminalConfigurationRequest().ToMessage());
                }
            }
        }

        /// <summary>
        /// Async call to get the current terminal address, this does not update the internals address of the library.
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetTerminalAddress()
        {
            var service = new DeviceAddressService();
            var addressResponse = await service.RetrieveDeviceAddress(_serialNumber, _deviceApiKey, _acquirerCode, _inTestMode);
            var deviceAddressStatus = DeviceHelper.GenerateDeviceAddressStatus(addressResponse, _eftposAddress);

            return deviceAddressStatus.Address;
        }
        #endregion

        #region Internals for Pairing Flow

        /// <summary>
        /// Handling the 2nd interaction of the pairing process, i.e. an incoming KeyRequest.
        /// </summary>
        /// <param name="m">incoming message</param>
        private void _handleKeyRequest(Message m)
        {
            CurrentPairingFlowState.Message = "Negotiating Pairing...";
            _pairingFlowStateChanged(this, CurrentPairingFlowState);

            // Use the helper. It takes the incoming request, and generates the secrets and the response.
            var result = PairingHelper.GenerateSecretsAndKeyResponse(new KeyRequest(m));
            _secrets = result.Secrets; // we now have secrets, although pairing is not fully finished yet.
            _spiMessageStamp.Secrets = _secrets; // updating our stamp with the secrets so can encrypt messages later.
            _send(result.KeyResponse.ToMessage()); // send the key_response, i.e. interaction 3 of pairing.
        }

        /// <summary>
        /// Handling the 4th interaction of the pairing process i.e. an incoming KeyCheck.
        /// </summary>
        /// <param name="m"></param>
        private void _handleKeyCheck(Message m)
        {
            var keyCheck = new KeyCheck(m);
            CurrentPairingFlowState.ConfirmationCode = keyCheck.ConfirmationCode;
            CurrentPairingFlowState.AwaitingCheckFromEftpos = true;
            CurrentPairingFlowState.AwaitingCheckFromPos = true;
            CurrentPairingFlowState.Message = "Confirm that the following Code is showing on the Terminal";
            _pairingFlowStateChanged(this, CurrentPairingFlowState);
        }

        /// <summary>
        /// Handling the 5th and final interaction of the pairing process, i.e. an incoming PairResponse
        /// </summary>
        /// <param name="m"></param>
        private void _handlePairResponse(Message m)
        {
            var pairResp = new PairResponse(m);

            CurrentPairingFlowState.AwaitingCheckFromEftpos = false;
            if (pairResp.Success)
            {
                if (CurrentPairingFlowState.AwaitingCheckFromPos)
                {
                    // Waiting for PoS, auto confirming code
                    Log.Information("Confirming pairing from library.");
                    PairingConfirmCode();
                }

                Log.Information("Got Pair Confirm from Eftpos, and already had confirm from POS. Now just waiting for first pong.");
                _onPairingSuccess();

                // I need to ping even if the pos user has not said yes yet, 
                // because otherwise within 5 seconds connection will be dropped by eftpos.
                _startPeriodicPing();
            }
            else
            {
                _onPairingFailed();
            }
        }

        private void _handleDropKeysAdvice(Message m)
        {
            Log.Information("Eftpos was Unpaired. I shall unpair from my end as well.");
            _doUnpair();
        }

        private void _onPairingSuccess()
        {
            CurrentPairingFlowState.Successful = true;
            CurrentPairingFlowState.Finished = true;
            CurrentPairingFlowState.Message = "Pairing Successful!";
            CurrentStatus = SpiStatus.PairedConnected;
            _secretsChanged(this, _secrets);
            _pairingFlowStateChanged(this, CurrentPairingFlowState);
        }

        private void _onPairingFailed()
        {
            _secrets = null;
            _spiMessageStamp.Secrets = null;
            _conn.Disconnect();

            CurrentStatus = SpiStatus.Unpaired;
            CurrentPairingFlowState.Message = "Pairing Failed";
            CurrentPairingFlowState.Finished = true;
            CurrentPairingFlowState.Successful = false;
            CurrentPairingFlowState.AwaitingCheckFromPos = false;
            _pairingFlowStateChanged(this, CurrentPairingFlowState);
        }

        private void _doUnpair()
        {
            CurrentStatus = SpiStatus.Unpaired;
            _conn.Disconnect();
            _secrets = null;
            _spiMessageStamp.Secrets = null;
            _secretsChanged(this, _secrets);
        }

        /// <summary>
        /// Sometimes the server asks us to roll our secrets.
        /// </summary>
        /// <param name="m"></param>
        private void _handleKeyRollingRequest(Message m)
        {
            // we calculate the new ones...
            var krRes = KeyRollingHelper.PerformKeyRolling(m, _secrets);
            _secrets = krRes.NewSecrets; // and update our secrets with them
            _spiMessageStamp.Secrets = _secrets; // and our stamp
            _send(krRes.KeyRollingConfirmation); // and we tell the server that all is well.
            _secretsChanged(this, _secrets);
        }

        #endregion

        #region Internals for Transaction Management

        /// <summary>
        /// The PinPad server will send us this message when a customer signature is reqired.
        /// We need to ask the customer to sign the incoming receipt.
        /// And then tell the pinpad whether the signature is ok or not.
        /// </summary>
        /// <param name="m"></param>
        private void _handleSignatureRequired(Message m)
        {
            lock (_txLock)
            {
                var incomingPosRefId = m.GetDataStringValue("pos_ref_id");
                if (CurrentFlow != SpiFlow.Transaction || CurrentTxFlowState.Finished || !CurrentTxFlowState.PosRefId.Equals(incomingPosRefId))
                {
                    Log.Information($"Received Signature Required but I was not waiting for one. Incoming Pos Ref ID: {incomingPosRefId}");
                    return;
                }
                CurrentTxFlowState.SignatureRequired(new SignatureRequired(m), "Ask Customer to Sign the Receipt");
            }
            _txFlowStateChanged(this, CurrentTxFlowState);
        }

        /// <summary>
        /// The PinPad server will send us this message when an auth code is required.
        /// </summary>
        /// <param name="m"></param>
        private void _handleAuthCodeRequired(Message m)
        {
            lock (_txLock)
            {
                var incomingPosRefId = m.GetDataStringValue("pos_ref_id");
                if (CurrentFlow != SpiFlow.Transaction || CurrentTxFlowState.Finished || !CurrentTxFlowState.PosRefId.Equals(incomingPosRefId))
                {
                    Log.Information($"Received Auth Code Required but I was not waiting for one. Incoming Pos Ref ID: {incomingPosRefId}");
                    return;
                }
                var phoneForAuthRequired = new PhoneForAuthRequired(m);
                var msg = $"Auth Code Required. Call {phoneForAuthRequired.GetPhoneNumber()} and quote merchant id {phoneForAuthRequired.GetMerchantId()}";
                CurrentTxFlowState.PhoneForAuthRequired(phoneForAuthRequired, msg);
            }
            _txFlowStateChanged(this, CurrentTxFlowState);
        }

        /// <summary>
        /// The PinPad server will reply to our PurchaseRequest with a PurchaseResponse.
        /// </summary>
        /// <param name="m"></param>
        private void _handlePurchaseResponse(Message m)
        {
            lock (_txLock)
            {
                var incomingPosRefId = m.GetDataStringValue("pos_ref_id");
                if (CurrentFlow != SpiFlow.Transaction || CurrentTxFlowState.Finished || !CurrentTxFlowState.PosRefId.Equals(incomingPosRefId))
                {
                    Log.Information($"Received Purchase response but I was not waiting for one. Incoming Pos Ref ID: {incomingPosRefId}");
                    return;
                }
                // TH-1A, TH-2A

                CurrentTxFlowState.Completed(m.GetSuccessState(), m, "Purchase Transaction Ended.");
                // TH-6A, TH-6E
            }
            _txFlowStateChanged(this, CurrentTxFlowState);
            _sendTransactionReport();
        }

        /// <summary>
        /// The PinPad server will reply to our CashoutOnlyRequest with a CashoutOnlyResponse.
        /// </summary>
        /// <param name="m"></param>
        private void _handleCashoutOnlyResponse(Message m)
        {
            lock (_txLock)
            {
                var incomingPosRefId = m.GetDataStringValue("pos_ref_id");
                if (CurrentFlow != SpiFlow.Transaction || CurrentTxFlowState.Finished || !CurrentTxFlowState.PosRefId.Equals(incomingPosRefId))
                {
                    Log.Information($"Received Cashout Response but I was not waiting for one. Incoming Pos Ref ID: {incomingPosRefId}");
                    return;
                }
                // TH-1A, TH-2A

                CurrentTxFlowState.Completed(m.GetSuccessState(), m, "Cashout Transaction Ended.");
                // TH-6A, TH-6E
            }
            _txFlowStateChanged(this, CurrentTxFlowState);
            _sendTransactionReport();
        }

        /// <summary>
        /// The PinPad server will reply to our MotoPurchaseRequest with a MotoPurchaseResponse.
        /// </summary>
        /// <param name="m"></param>
        private void _handleMotoPurchaseResponse(Message m)
        {
            lock (_txLock)
            {
                var incomingPosRefId = m.GetDataStringValue("pos_ref_id");
                if (CurrentFlow != SpiFlow.Transaction || CurrentTxFlowState.Finished || !CurrentTxFlowState.PosRefId.Equals(incomingPosRefId))
                {
                    Log.Information($"Received Moto Response but I was not waiting for one. Incoming Pos Ref ID: {incomingPosRefId}");
                    return;
                }
                // TH-1A, TH-2A

                CurrentTxFlowState.Completed(m.GetSuccessState(), m, "Moto Transaction Ended.");
                // TH-6A, TH-6E
            }
            _txFlowStateChanged(this, CurrentTxFlowState);
            _sendTransactionReport();
        }

        /// <summary>
        /// The PinPad server will reply to our RefundRequest with a RefundResponse.
        /// </summary>
        /// <param name="m"></param>
        private void _handleRefundResponse(Message m)
        {
            lock (_txLock)
            {
                var incomingPosRefId = m.GetDataStringValue("pos_ref_id");
                if (CurrentFlow != SpiFlow.Transaction || CurrentTxFlowState.Finished || !CurrentTxFlowState.PosRefId.Equals(incomingPosRefId))
                {
                    Log.Information($"Received Refund response but I was not waiting for this one. Incoming Pos Ref ID: {incomingPosRefId}");
                    return;
                }
                // TH-1A, TH-2A

                CurrentTxFlowState.Completed(m.GetSuccessState(), m, "Refund Transaction Ended.");
                // TH-6A, TH-6E
            }
            _txFlowStateChanged(this, CurrentTxFlowState);
            _sendTransactionReport();
        }

        /// <summary>
        /// Handle the Settlement Response received from the PinPad
        /// </summary>
        /// <param name="m"></param>
        private void _handleSettleResponse(Message m)
        {
            lock (_txLock)
            {
                if (CurrentFlow != SpiFlow.Transaction || CurrentTxFlowState.Finished)
                {
                    Log.Information($"Received Settle response but I was not waiting for one. {m.DecryptedJson}");
                    return;
                }
                // TH-1A, TH-2A

                CurrentTxFlowState.Completed(m.GetSuccessState(), m, "Settle Transaction Ended.");
                // TH-6A, TH-6E
            }
            _txFlowStateChanged(this, CurrentTxFlowState);
            _sendTransactionReport();
        }

        /// <summary>
        /// Handle the Settlement Enquiry Response received from the PinPad
        /// </summary>
        /// <param name="m"></param>
        private void _handleSettlementEnquiryResponse(Message m)
        {
            lock (_txLock)
            {
                if (CurrentFlow != SpiFlow.Transaction || CurrentTxFlowState.Finished)
                {
                    Log.Information($"Received Settlement Enquiry response but I was not waiting for one. {m.DecryptedJson}");
                    return;
                }
                // TH-1A, TH-2A

                CurrentTxFlowState.Completed(m.GetSuccessState(), m, "Settlement Enquiry Ended.");
                // TH-6A, TH-6E
            }
            _txFlowStateChanged(this, CurrentTxFlowState);
            _sendTransactionReport();
        }

        /// <summary>
        /// Handle the Reversal Response received from the PinPad
        /// </summary>
        /// <param name="m"></param>
        private void _handleReversalTransaction(Message m)
        {
            lock (_txLock)
            {
                var incomingPosRefId = m.GetDataStringValue("pos_ref_id");
                if (CurrentFlow != SpiFlow.Transaction || CurrentTxFlowState.Finished || !CurrentTxFlowState.PosRefId.Equals(incomingPosRefId))
                {
                    Log.Information($"Received Reversal response but I was not waiting for this one. Incoming Pos Ref ID: {incomingPosRefId}");
                    return;
                }

                CurrentTxFlowState.Completed(m.GetSuccessState(), m, "Reversal Transaction Ended.");
            }
            _txFlowStateChanged(this, CurrentTxFlowState);
            _sendTransactionReport();
        }


        /// <summary>
        /// Sometimes we receive event type "error" from the server, such as when calling cancel_transaction and there is no transaction in progress.
        /// </summary>
        /// <param name="m"></param>
        private void _handleErrorEvent(Message m)
        {
            lock (_txLock)
            {
                if (CurrentFlow == SpiFlow.Transaction
                    && !CurrentTxFlowState.Finished
                    && CurrentTxFlowState.AttemptingToCancel
                    && m.GetError() == "NO_TRANSACTION")
                {
                    // TH-2E
                    Log.Information($"Was trying to cancel a transaction but there is nothing to cancel. Calling GT to see what's up");
                    _callGetTransaction(CurrentTxFlowState.PosRefId);
                }
                else
                {
                    Log.Information($"Received Error Event But Don't know what to do with it. {m.DecryptedJson}");
                }
            }
        }

        /// <summary>
        /// When the PinPad returns to us what the Transaction was.
        /// </summary>
        /// <param name="m"></param>
        private void _handleGetTransactionResponse(Message m)
        {
            lock (_txLock)
            {
                var txState = CurrentTxFlowState;
                if (CurrentFlow != SpiFlow.Transaction || txState.Finished)
                {
                    Log.Information($"Received gt response but we were not in the middle of a tx. ignoring.");
                    return;
                }

                if (!txState.AwaitingGtResponse)
                {
                    Log.Information($"Received a gt response but we had not asked for one within this transaction. Perhaps leftover from previous one. ignoring.");
                    return;
                }

                if (txState.GtRequestId != m.Id)
                {
                    Log.Information($"Received a gt response but the message id does not match the gt request that we sent. strange. ignoring.");
                    return;
                }

                Log.Information($"Got Transaction response.");
                txState.GotGtResponse(); 
                var gtResponse = new GetTransactionResponse(m);

                if (!gtResponse.WasRetrievedSuccessfully())
                {
                    // GetTransaction Failed... let's figure out one of reason and act accordingly
                    if (gtResponse.IsWaitingForSignatureResponse())
                    {
                        if (!txState.AwaitingSignatureCheck)
                        {
                            Log.Information($"GTR-01: Eftpos is waiting for us to send it signature accept/decline, but we were not aware of this. " +
                                      $"The user can only really decline at this stage as there is no receipt to print for signing.");
                            CurrentTxFlowState.SignatureRequired(new SignatureRequired(txState.PosRefId, m.Id, "MISSING RECEIPT\n DECLINE AND TRY AGAIN."), "Recovered in Signature Required but we don't have receipt. You may Decline then Retry.");
                        }
                        else
                        {
                            Log.Information($"Waiting for Signature response ... stay waiting.");
                            // No need to publish txFlowStateChanged. Can return;
                            return;
                        }
                    }
                    else if (gtResponse.IsWaitingForAuthCode() && !txState.AwaitingPhoneForAuth)
                    {
                        Log.Information($"GTR-02: Eftpos is waiting for us to send it auth code, but we were not aware of this. " +
                                  $"We can only cancel the transaction at this stage as we don't have enough information to recover from this.");
                        CurrentTxFlowState.PhoneForAuthRequired(new PhoneForAuthRequired(txState.PosRefId, m.Id, "UNKNOWN", "UNKNOWN"), "Recovered mid Phone-For-Auth but don't have details. You may Cancel then Retry.");
                    }
                    else if (gtResponse.IsTransactionInProgress())
                    {
                        Log.Information($"GTR-03: Transaction is currently in progress... stay waiting.");
                        return;
                    }
                    else if (gtResponse.PosRefIdNotFound()) 
                    {
                        Log.Information($"GTR-04: Get transaction failed, PosRefId is not found.");
                        txState.Completed(Message.SuccessState.Failed, m, $"PosRefId not found for {gtResponse.GetPosRefId()}.");
                    }
                    else if (gtResponse.PosRefIdInvalid())
                    {
                        Log.Information($"GTR-05: Get transaction failed, PosRefId is invalid.");
                        txState.Completed(Message.SuccessState.Failed, m, $"PosRefId invalid for {gtResponse.GetPosRefId()}.");
                    }
                    else if (gtResponse.PosRefIdMissing())
                    {
                        Log.Information($"GTR-06: Get transaction failed, PosRefId is missing.");
                        txState.Completed(Message.SuccessState.Failed, m, $"PosRefId is missing for {gtResponse.GetPosRefId()}.");
                    }
                    else if (gtResponse.IsSomethingElseBlocking())
                    {
                        Log.Information($"GTR-07: Terminal is Blocked by something else... stay waiting.");
                        return;
                    }
                    else
                    {
                        // get transaction failed, but we weren't given a specific reason 
                        Log.Information($"GTR-08: Unexpected Response in Get Transaction - Received posRefId:{gtResponse.GetPosRefId()} Error:{m.GetError()}. Ignoring.");
                        txState.Completed(Message.SuccessState.Failed, m, $"Get Transaction failed, {m.GetError()}.");
                    }
                }
                else
                {
                    var tx = gtResponse.GetTxMessage();
                    if (tx == null)
                    {
                        // tx payload missing from get transaction protocol, could be a VAA issue.
                        Log.Information("GTR-09: Unexpected Response in Get Transaction. Missing TX payload... stay waiting");
                        return;
                    }
                    
                    // get transaction was successful
                    gtResponse.CopyMerchantReceiptToCustomerReceipt();
                    if (txState.Type == TransactionType.GetTransaction)
                    {
                        // this was a get transaction request, not for recovery
                        Log.Information("GTR-10: Retrieved Transaction as asked directly by the user.");
                        txState.Completed(tx.GetSuccessState(), tx, $"Transaction Retrieved for {gtResponse.GetPosRefId()}.");
                    }
                    else
                    {
                        // this was a get transaction from a recovery
                        Log.Information("GTR-11: Retrieved transaction during recovery.");
                        txState.Completed(tx.GetSuccessState(), tx, $"Transaction Recovered for {gtResponse.GetPosRefId()}.");
                    }
                }
            }
            _txFlowStateChanged(this, CurrentTxFlowState);
            _sendTransactionReport();
        }

        /// <summary>
        /// When the PinPad returns to us what the Last Transaction was.
        /// </summary>
        /// <param name="m"></param>
        private void _handleGetLastTransactionResponse(Message m)
        {
            lock (_txLock)
            {
                var txState = CurrentTxFlowState;
                if (CurrentFlow != SpiFlow.Transaction || txState.Finished || txState.Type != TransactionType.GetLastTransaction)
                {
                    Log.Information($"Received glt response but we were not expecting one. ignoring.");
                    return;
                }

                Log.Information($"Got Last Transaction Response..");
                var gtlResponse = new GetLastTransactionResponse(m);
                if (!gtlResponse.WasRetrievedSuccessfully())
                {
                    Log.Information($"Error in Response for Get Last Transaction - Received posRefId:{gtlResponse.GetPosRefId()} Error:{m.GetError()}. UnknownCompleted.");
                    txState.UnknownCompleted("Failed to Retrieve Last Transaction");
                }
                else
                {
                    Log.Information($"Retrieved Last Transaction as asked directly by the user.");
                    gtlResponse.CopyMerchantReceiptToCustomerReceipt();
                    txState.Completed(m.GetSuccessState(), m, "Last Transaction Retrieved");
                }
            }
            _txFlowStateChanged(this, CurrentTxFlowState);
        }

        //When the transaction cancel response is returned.
        private void _handleCancelTransactionResponse(Message m)
        {
            lock (_txLock)
            {
                var incomingPosRefId = m.GetDataStringValue("pos_ref_id");
                var txState = CurrentTxFlowState;
                var cancelResponse = new CancelTransactionResponse(m);

                if (CurrentFlow != SpiFlow.Transaction || txState.Finished || !txState.PosRefId.Equals(incomingPosRefId))
                {
                    if (!cancelResponse.WasTxnPastPointOfNoReturn())
                    {
                        Log.Information($"Received Cancel Required but I was not waiting for one. Incoming Pos Ref ID: {incomingPosRefId}");
                        return;
                    }
                }

                if (cancelResponse.Success) return;

                Log.Warning("Failed to cancel transaction: reason=" + cancelResponse.GetErrorReason() + ", detail=" + cancelResponse.GetErrorDetail());

                txState.CancelFailed("Failed to cancel transaction: " + cancelResponse.GetErrorDetail() + ". Check EFTPOS.");
            }

            _txFlowStateChanged(this, CurrentTxFlowState);
            _sendTransactionReport();
        }

        private void _handleSetPosInfoResponse(Message m)
        {
            lock (_txLock)
            {
                var response = new SetPosInfoResponse(m);
                if (response.isSuccess())
                {
                    _hasSetInfo = true;
                    Log.Information("Setting POS info successful");
                }
                else
                {
                    Log.Warning("Setting POS info failed: reason=" + response.getErrorReason() + ", detail=" + response.getErrorDetail());
                }
            }
        }

        private void _startTransactionMonitoringThread()
        {
            var tmt = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                while (true)
                {
                    var needsPublishing = false;
                    lock (_txLock)
                    {
                        var txState = CurrentTxFlowState;
                        if (CurrentFlow == SpiFlow.Transaction && !txState.Finished)
                        {
                            var state = txState;
                            if (state.AttemptingToCancel && DateTime.Now > state.CancelAttemptTime.Add(_maxWaitForCancelTx))
                            {
                                // TH-2T - too long since cancel attempt - Consider unknown
                                Log.Information($"Been too long waiting for transaction to cancel.");
                                txState.UnknownCompleted("Waited long enough for Cancel Transaction result. Check EFTPOS. ");
                                needsPublishing = true;
                            }
                            else if (state.RequestSent && DateTime.Now > state.LastStateRequestTime.Add(_checkOnTxFrequency))
                            {
                                // It's been a while since we received an update.
                                
                                if (txState.Type == TransactionType.GetLastTransaction)
                                {
                                    // It is not possible to recover a GLT with a GT, so we send another GLT
                                    txState.LastStateRequestTime = DateTime.Now;
                                    _send(new GetLastTransactionRequest().ToMessage());
                                    Log.Information($"Been to long waiting for GLT response. Sending another GLT. Last checked at {state.LastStateRequestTime}...");
                                }
                                else
                                {
                                    // let's call a GT to see what is happening
                                    Log.Information($"Checking on our transaction. Last checked at {state.LastStateRequestTime}...");
                                    _callGetTransaction(CurrentTxFlowState.PosRefId);
                                }
                            }
                        }
                    }
                    if (needsPublishing) _txFlowStateChanged(this, CurrentTxFlowState);
                    Thread.Sleep(_txMonitorCheckFrequency);
                }
            });
            tmt.Start();
        }

        private void _handlePrintingResponse(Message m)
        {
            lock (_txLock)
            {
                PrintingResponse?.Invoke(m);
            }
        }

        private void _handleTerminalStatusResponse(Message m)
        {
            lock (_txLock)
            {
                TerminalStatusResponse?.Invoke(m);
            }
        }

        private void _handleTerminalConfigurationResponse(Message m)
        {
            lock (_txLock)
            {
                if (_pairUsingEftposAddress)
                {
                    var response = new TerminalConfigurationResponse(m);
                    if (response.isSuccess())
                    {
                        _serialNumber = response.GetSerialNumber();
                        _terminalModel = response.GetTerminalModel();
                    }
                }

                TerminalConfigurationResponse?.Invoke(m);
            }
        }

        private void _handleBatteryLevelChanged(Message m)
        {
            lock (_txLock)
            {
                BatteryLevelChanged?.Invoke(m);
            }
        }

        private void _handleTransactionUpdateMessage(Message m)
        {
            lock (_txLock)
            {
                TransactionUpdateMessage?.Invoke(m);
            }
        }

        private bool _isPrintingConfigEnabled()
        {
            if (Config.PromptForCustomerCopyOnEftpos || Config.PrintMerchantCopy || Config.SignatureFlowOnEftpos)
            {
                return true;
            }

            return false;
        }

        #endregion

        #region Internals for Connection Management

        private void _resetConn()
        {
            // Setup the Connection
            _conn = new Connection { Address = _eftposAddress };
            // Register our Event Handlers
            _conn.ConnectionStatusChanged += _onSpiConnectionStatusChanged;
            _conn.MessageReceived += _onSpiMessageReceived;
            _conn.ErrorReceived += _onWsErrorReceived;
        }

        /// <summary>
        /// This method will be called when the connection status changes.
        /// You are encouraged to display a PinPad Connection Indicator on the POS screen.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="state"></param>
        private void _onSpiConnectionStatusChanged(object sender, ConnectionStateEventArgs state)
        {
            switch (state.ConnectionState)
            {
                case ConnectionState.Connecting:
                    Log.Information($"I'm Connecting to the Eftpos at {_eftposAddress}...");
                    break;

                case ConnectionState.Connected:
                    _retriesSinceLastDeviceAddressResolution = 0;
                    _spiMessageStamp.ResetConnection();

                    if (CurrentFlow == SpiFlow.Pairing && CurrentStatus == SpiStatus.Unpaired)
                    {

                        CurrentPairingFlowState.Message = "Requesting to Pair...";
                        _pairingFlowStateChanged(this, CurrentPairingFlowState);
                        var pr = PairingHelper.NewPairequest();
                        _send(pr.ToMessage());
                    }
                    else
                    {
                        Log.Information($"I'm Connected to {_eftposAddress}...");
                        _spiMessageStamp.Secrets = _secrets;
                        _startPeriodicPing();
                    }
                    break;

                case ConnectionState.Disconnected:
                    // Let's reset some lifecycle related to connection state, ready for next connection
                    Log.Information($"I'm disconnected from {_eftposAddress}...");
                    _mostRecentPingSent = null;
                    _mostRecentPongReceived = null;
                    _missedPongsCount = 0;
                    _stopPeriodicPing();
                    _spiMessageStamp.ResetConnection();

                    if (CurrentStatus != SpiStatus.Unpaired)
                    {
                        CurrentStatus = SpiStatus.PairedConnecting;

                        lock (_txLock)
                        {
                            if (CurrentFlow == SpiFlow.Transaction && !CurrentTxFlowState.Finished)
                            {
                                // we're in the middle of a transaction, just so you know!
                                // TH-1D
                                Log.Warning($"Lost connection in the middle of a transaction...");
                            }

                            // As we have no way to recover from a reversal in the event of a disconnection, we will fail the reversal.
                            if (CurrentTxFlowState.Type == TransactionType.Reversal)
                            {
                                CurrentTxFlowState.Completed(Message.SuccessState.Failed, null, $"We were in the middle of a reversal when a disconnection happened, let's fail the reversal.");
                                _txFlowStateChanged(this, CurrentTxFlowState);
                            }
                        }

                        Task.Factory.StartNew(() =>
                        {
                            if (_conn == null) return; // This means the instance has been disposed. Aborting.

                            if (_autoAddressResolutionEnabled)
                            {
                                if (_retriesSinceLastDeviceAddressResolution >= _retriesBeforeResolvingDeviceAddress)
                                {
                                    _autoResolveEftposAddress();
                                    _retriesSinceLastDeviceAddressResolution = 0;
                                }
                                else
                                {
                                    _retriesSinceLastDeviceAddressResolution += 1;
                                }
                            }

                            Log.Information($"Will try to reconnect in {_sleepBeforeReconnectMs}ms ...");
                            Thread.Sleep(_sleepBeforeReconnectMs);
                            if (CurrentStatus != SpiStatus.Unpaired)
                            {
                                // This is non-blocking
                                _conn?.Connect();
                            }
                        });
                    }
                    else if (CurrentFlow == SpiFlow.Pairing)
                    {
                        if (CurrentPairingFlowState.Finished) return;

                        if (_retriesSinceLastPairing >= _retriesBeforePairing)
                        {
                            _retriesSinceLastPairing = 0;
                            Log.Warning("Lost Connection during pairing.");
                            _onPairingFailed();
                            _pairingFlowStateChanged(this, CurrentPairingFlowState);
                            return;
                        }
                        else
                        {
                            Log.Information($"Will try to re-pair in {_sleepBeforeReconnectMs}ms ...");
                            Thread.Sleep(_sleepBeforeReconnectMs);
                            if (CurrentStatus != SpiStatus.PairedConnected)
                            {
                                // This is non-blocking
                                _conn?.Connect();
                            }

                            _retriesSinceLastPairing += 1;
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        /// <summary>
        /// This is an important piece of the puzzle. It's a background thread that periodically
        /// sends Pings to the server. If it doesn't receive Pongs, it considers the connection as broken
        /// so it disconnects. 
        /// </summary>
        private void _startPeriodicPing()
        {
            if (_periodicPingThread != null)
            {
                // If we were already set up, clean up before restarting.
                _periodicPingThread.Abort();
                _periodicPingThread = null;
            }

            _periodicPingThread = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                while (_conn.Connected && _secrets != null)
                {
                    _doPing();// first ping

                    Thread.Sleep(_pongTimeout);
                    if (_mostRecentPingSent != null &&
                        (_mostRecentPongReceived == null || _mostRecentPongReceived.Id != _mostRecentPingSent.Id))
                    {
                        _missedPongsCount += 1;
                        Log.Information($"Eftpos didn't reply to my Ping. Missed Count: {_missedPongsCount}/{_missedPongsToDisconnect}. ");

                        if (_missedPongsCount < _missedPongsToDisconnect)
                        {
                            Log.Information("Trying another ping...");
                            continue;
                        }

                        // This means that we have reached missed pong limit.
                        // We consider this connection as broken.
                        // Let's Disconnect.
                        Log.Information("Disconnecting...");
                        _conn.Disconnect();
                        break;
                    }
                    _missedPongsCount = 0;
                    Thread.Sleep(_pingFrequency - _pongTimeout);
                }
            });
            _periodicPingThread.Start();
        }

        /// <summary>
        /// We call this ourselves as soon as we're ready to transact with the PinPad after a connection is established.
        /// This function is effectively called after we received the first pong response from the PinPad.
        /// </summary>
        private void _onReadyToTransact()
        {
            Log.Information("On Ready To Transact!");

            // So, we have just made a connection and pinged successfully.
            CurrentStatus = SpiStatus.PairedConnected;

            lock (_txLock)
            {
                if (CurrentFlow == SpiFlow.Transaction && !CurrentTxFlowState.Finished)
                {
                    if (CurrentTxFlowState.RequestSent)
                    {
                        // TH-3A - We've just reconnected and were in the middle of Tx.
                        // Let's get the transaction to check what we might have missed out on.
                        _callGetTransaction(CurrentTxFlowState.PosRefId);
                    }
                    else
                    {
                        // TH-3AR - We had not even sent the request yet. Let's do that now
                        _send(CurrentTxFlowState.Request);
                        CurrentTxFlowState.Sent($"Sending Request Now...");
                        _txFlowStateChanged(this, CurrentTxFlowState);
                    }
                }
                else
                {
                    if (!_hasSetInfo)
                    {
                        _callSetPosInfo();
                        transactionReport = TransactionReportHelper.CreateTransactionReportEnvelope(_posVendorId, _posVersion, _libraryLanguage, GetVersion(), _serialNumber);
                    }

                    // let's also tell the eftpos our latest table configuration.
                    _spiPat?.PushPayAtTableConfig();

                    if (_pairUsingEftposAddress)
                    {
                        GetTerminalConfiguration();
                    }
                }
            }
        }

        private void _callSetPosInfo()
        {
            SetPosInfoRequest setPosInfoRequest = new SetPosInfoRequest(_posVersion, _posVendorId, _libraryLanguage, GetVersion(), DeviceInfo.GetAppDeviceInfo());
            _send(setPosInfoRequest.toMessage());
        }

        /// <summary>
        /// When we disconnect, we should also stop the periodic ping.
        /// </summary>
        private void _stopPeriodicPing()
        {
            if (_periodicPingThread != null)
            {
                // If we were already set up, clean up before restarting.
                _periodicPingThread.Abort();
                _periodicPingThread = null;
            }
        }

        // Send a Ping to the Server
        private void _doPing()
        {
            var ping = PingHelper.GeneratePingRequest();

            _mostRecentPingSent = ping;
            _send(ping);
            _mostRecentPingSentTime = DateTime.Now;
        }

        /// <summary>
        /// Received a Pong from the server
        /// </summary>
        /// <param name="m"></param>
        private void _handleIncomingPong(Message m)
        {
            if (_mostRecentPongReceived == null)
            {
                // First pong received after a connection, and after the pairing process is fully finalised.
                // Receive connection id from PinPad after first pong, store this as this needs to be passed for every request.
                _spiMessageStamp.SetConnectionId(m.ConnId);

                if (CurrentStatus != SpiStatus.Unpaired)
                {
                    Log.Information("First pong of connection and in paired state.");
                    _onReadyToTransact();
                }
                else
                {
                    Log.Information("First pong of connection but pairing process not finalised yet.");
                }
            }

            _mostRecentPongReceived = m;
            Log.Debug($"PongLatency:{DateTime.Now.Subtract(_mostRecentPingSentTime)}");
        }

        /// <summary>
        /// The server will also send us pings. We need to reply with a pong so it doesn't disconnect us.
        /// </summary>
        /// <param name="m"></param>
        private void _handleIncomingPing(Message m)
        {
            var pong = PongHelper.GeneratePongResponse(m);
            _send(pong);
        }

        /// <summary>
        /// Ask the PinPad to tell us about the transaction with the posRefId
        /// </summary>
        private void _callGetTransaction(string posRefId)
        {
            var gtRequestMsg = new GetTransactionRequest(posRefId).ToMessage();
            CurrentTxFlowState.CallingGt(gtRequestMsg.Id);
            _send(gtRequestMsg);
        }
        
        /// <summary>
        /// This method will be called whenever we receive a message from the Connection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="messageJson"></param>
        private void _onSpiMessageReceived(object sender, MessageEventArgs messageJson)
        {
            // First we parse the incoming message
            var m = Message.FromJson(messageJson.Message, _secrets);
            Log.Debug("Received: {Json:1}", m.DecryptedJson);

            if (SpiPreauth.IsPreauthEvent(m.EventName))
            {
                _spiPreauth?._handlePreauthMessage(m);
                return;
            }

            // And then we switch on the event type.
            switch (m.EventName)
            {
                case Events.KeyRequest:
                    _handleKeyRequest(m);
                    break;
                case Events.KeyCheck:
                    _handleKeyCheck(m);
                    break;
                case Events.PairResponse:
                    _handlePairResponse(m);
                    break;
                case Events.DropKeysAdvice:
                    _handleDropKeysAdvice(m);
                    break;
                case Events.PurchaseResponse:
                    _handlePurchaseResponse(m);
                    break;
                case Events.RefundResponse:
                    _handleRefundResponse(m);
                    break;
                case Events.CashoutOnlyResponse:
                    _handleCashoutOnlyResponse(m);
                    break;
                case Events.MotoPurchaseResponse:
                    _handleMotoPurchaseResponse(m);
                    break;
                case Events.SignatureRequired:
                    _handleSignatureRequired(m);
                    break;
                case Events.AuthCodeRequired:
                    _handleAuthCodeRequired(m);
                    break;
                case Events.GetTransactionResponse:
                    _handleGetTransactionResponse(m);
                    break;
                case Events.GetLastTransactionResponse:
                    _handleGetLastTransactionResponse(m);
                    break;
                case Events.SettleResponse:
                    _handleSettleResponse(m);
                    break;
                case Events.SettlementEnquiryResponse:
                    _handleSettlementEnquiryResponse(m);
                    break;
                case Events.ReversalResponse:
                    _handleReversalTransaction(m);
                    break;
                case Events.Ping:
                    _handleIncomingPing(m);
                    break;
                case Events.Pong:
                    _handleIncomingPong(m);
                    break;
                case Events.KeyRollRequest:
                    _handleKeyRollingRequest(m);
                    break;
                case Events.CancelTransactionResponse:
                    _handleCancelTransactionResponse(m);
                    break;
                case Events.SetPosInfoResponse:
                    _handleSetPosInfoResponse(m);
                    break;
                case Events.PayAtTableGetTableConfig:
                    if (_spiPat == null)
                    {
                        _send(PayAtTableConfig.FeatureDisableMessage(RequestIdHelper.Id("patconf")));
                        break;
                    }
                    _spiPat._handleGetTableConfig(m);
                    break;
                case Events.PayAtTableGetBillDetails:
                    _spiPat?._handleGetBillDetailsRequest(m);
                    break;
                case Events.PayAtTableBillPayment:
                    _spiPat?._handleBillPaymentAdvice(m);
                    break;
                case Events.PayAtTableGetOpenTables:
                    _spiPat?._handleGetOpenTablesRequest(m);
                    break;
                case Events.PayAtTableBillPaymentFlowEnded:
                    _spiPat?._handleBillPaymentFlowEnded(m);
                    break;
                case Events.PrintingResponse:
                    _handlePrintingResponse(m);
                    break;
                case Events.TerminalStatusResponse:
                    _handleTerminalStatusResponse(m);
                    break;
                case Events.TerminalConfigurationResponse:
                    _handleTerminalConfigurationResponse(m);
                    break;
                case Events.BatteryLevelChanged:
                    _handleBatteryLevelChanged(m);
                    break;
                case Events.TransactionUpdateMessage:
                    _handleTransactionUpdateMessage(m);
                    break;
                case Events.Error:
                    _handleErrorEvent(m);
                    break;
                case Events.InvalidHmacSignature:
                    Log.Information("I could not verify message from Eftpos. You might have to Un-pair Eftpos and then reconnect.");
                    break;
                default:
                    Log.Information($"I don't Understand Event: {m.EventName}, {m.Data}. Perhaps I have not implemented it yet.");
                    break;
            }
        }

        private void _onWsErrorReceived(object sender, MessageEventArgs error)
        {
            Log.Warning("Received WS Error: " + error.Message);
        }

        internal bool _send(Message message)
        {   
            var json = message.ToJson(_spiMessageStamp);
            if (_conn.Connected)
            {
                Log.Debug("Sending: {Json:1}", message.DecryptedJson);
                _conn.Send(json);
                return true;
            }
            else
            {
                Log.Debug("Asked to send, but not connected: {Json:1}", message.DecryptedJson);
                return false;
            }
        }
        #endregion

        #region Internals for Validations

        private bool IsPosIdValid(string posId)
        {
            if (posId?.Length > 16)
            {
                Log.Warning("Pos Id is greater than 16 characters");
                return false;
            }

            if (string.IsNullOrWhiteSpace(posId))
            {
                Log.Warning("Pos Id cannot be null or empty");
                return false;
            }

            if (!regexItemsForPosId.IsMatch(posId))
            {
                Log.Warning("Pos Id cannot include special characters");
                return false;
            }

            return true;
        }

        private bool IsEftposAddressValid(string eftposAddress)
        {
            if (string.IsNullOrWhiteSpace(eftposAddress))
            {
                Log.Warning("The Eftpos address cannot be null or empty");
                return false;
            }

            if (!regexItemsForEftposAddress.IsMatch(eftposAddress.Replace("ws://", "")))
            {
                Log.Warning("The Eftpos address is not in the right format");
                return false;
            }

            return true;
        }

        #endregion

        #region Device Management 

        private bool HasSerialNumberChanged(string updatedSerialNumber)
        {
            return _serialNumber != updatedSerialNumber;
        }

        private async void _autoResolveEftposAddress()
        {
            if (!_autoAddressResolutionEnabled)
                return;

            if (string.IsNullOrWhiteSpace(_serialNumber) || string.IsNullOrWhiteSpace(_deviceApiKey))
            {
                Log.Warning("Missing serialNumber and/or deviceApiKey. Need to set them before for Auto Address to work.");
                return;
            }

            var service = new DeviceAddressService();
            var addressResponse = await service.RetrieveDeviceAddress(_serialNumber, _deviceApiKey, _acquirerCode, _inTestMode);
            var deviceAddressStatus = DeviceHelper.GenerateDeviceAddressStatus(addressResponse, _eftposAddress);
            CurrentDeviceStatus = deviceAddressStatus;

            if (deviceAddressStatus.DeviceAddressResponseCode == DeviceAddressResponseCode.DEVICE_SERVICE_ERROR)
            {
                Log.Warning("Could not communicate with device address service.");
                return;
            }
            else if (deviceAddressStatus.DeviceAddressResponseCode == DeviceAddressResponseCode.ADDRESS_NOT_CHANGED)
            {
                Log.Information("Address resolved, but device address has not changed.");
         
                // even though address haven't changed - dispatch event as PoS depend on this
                _deviceAddressChanged?.Invoke(this, CurrentDeviceStatus);
                return;
            }

            // new address, update device and connection address
            _eftposAddress = "ws://" + deviceAddressStatus.Address;
            _conn.Address = _eftposAddress;
            Log.Information($"Address resolved to {deviceAddressStatus.Address}");

            // dispatch event
            _deviceAddressChanged?.Invoke(this, CurrentDeviceStatus);

        }

        #endregion

        #region Analytics
        private void _sendTransactionReport()
        {
            transactionReport.TxType = CurrentTxFlowState.Type.ToString();
            transactionReport.TxResult = CurrentTxFlowState.Success.ToString();
            transactionReport.TxStartTime = DateTimeToUnixTimeStamp(CurrentTxFlowState.RequestTime);
            transactionReport.TxEndTime = DateTimeToUnixTimeStamp(CurrentTxFlowState.RequestTime);
            transactionReport.DurationMs = (int)(CurrentTxFlowState.CompletedTime - CurrentTxFlowState.RequestTime).TotalMilliseconds;
            transactionReport.CurrentFlow = CurrentFlow.ToString();
            transactionReport.CurrentTxFlowState = CurrentTxFlowState.Type.ToString();
            transactionReport.CurrentStatus = CurrentStatus.ToString();
            transactionReport.PosRefId = CurrentTxFlowState.PosRefId;
            transactionReport.Event = $"Waiting for Signature: {CurrentTxFlowState.AwaitingSignatureCheck}, " +
                $"Attemtping to Cancel: {CurrentTxFlowState.AttemptingToCancel}, Finished: {CurrentTxFlowState.Finished}";
            transactionReport.SerialNumber = _serialNumber;

            _ = Task.Factory.StartNew(() =>
                {
                    var service = new AnalyticsService();
                    var analyticsResponse = service.ReportTransaction(transactionReport, _deviceApiKey, _acquirerCode, _inTestMode);

                    if (analyticsResponse.Result == null)
                    {
                        Log.Warning("Error reporting to anaytics service.");
                        return;
                    }
                });
        }

        /// <summary>
        /// Private method for converting date time to unix time
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        private static long DateTimeToUnixTimeStamp(DateTime dateTime)
        {
            var unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var unixTimeStampInTicks = (dateTime.ToUniversalTime() - unixStart).Ticks;

            return unixTimeStampInTicks / TimeSpan.TicksPerSecond;
        }
        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            Log.Information("Disposing...");
            Log.CloseAndFlush();
            _conn?.Disconnect();
            _conn = null;
        }

        #endregion

        #region Private State

        private string _posId;
        private string _eftposAddress;
        private string _serialNumber;
        private string _deviceApiKey;
        private string _acquirerCode;
        private string _terminalModel;
        private bool _inTestMode;
        private bool _autoAddressResolutionEnabled = true; // enabled by default
        private Secrets _secrets;
        private MessageStamp _spiMessageStamp;
        private string _posVendorId;
        private string _posVersion;
        private bool _hasSetInfo;
        private bool _pairUsingEftposAddress;
        private const string _libraryLanguage = ".net";
        private Connection _conn;
        private readonly TimeSpan _pongTimeout = TimeSpan.FromSeconds(5);
        private readonly TimeSpan _pingFrequency = TimeSpan.FromSeconds(18);

        private SpiStatus _currentStatus;
        private EventHandler<SpiStatusEventArgs> _statusChanged;
        private EventHandler<DeviceAddressStatus> _deviceAddressChanged;
        private EventHandler<PairingFlowState> _pairingFlowStateChanged;
        internal EventHandler<TransactionFlowState> _txFlowStateChanged;
        private EventHandler<Secrets> _secretsChanged;

        public SpiPrintingResponse PrintingResponse;
        public SpiTerminalStatusResponse TerminalStatusResponse;
        public SpiTerminalConfigurationResponse TerminalConfigurationResponse;
        public SpiBatteryLevelChanged BatteryLevelChanged;
        public SpiTransactionUpdateMessage TransactionUpdateMessage;
        private TransactionReport transactionReport;
        private Message _mostRecentPingSent;
        private DateTime _mostRecentPingSentTime;
        private Message _mostRecentPongReceived;
        private int _missedPongsCount;
        private int _retriesSinceLastDeviceAddressResolution = 0;
        private Thread _periodicPingThread;

        private readonly object _txLock = new Object();
        private readonly TimeSpan _txMonitorCheckFrequency = TimeSpan.FromSeconds(1);
        private readonly TimeSpan _checkOnTxFrequency = TimeSpan.FromSeconds(20.0);
        private readonly TimeSpan _maxWaitForCancelTx = TimeSpan.FromSeconds(10.0);
        private readonly int _sleepBeforeReconnectMs = 3000;
        private readonly int _missedPongsToDisconnect = 2;
        private readonly int _retriesBeforeResolvingDeviceAddress = 3;

        private int _retriesSinceLastPairing = 0;
        private readonly int _retriesBeforePairing = 3;

        private SpiPayAtTable _spiPat;

        private SpiPreauth _spiPreauth;

        private static readonly string _version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        private readonly Regex regexItemsForEftposAddress = new Regex(@"^[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}$");
        private readonly Regex regexItemsForPosId = new Regex("^[a-zA-Z0-9]*$");

        #endregion        
    }
}