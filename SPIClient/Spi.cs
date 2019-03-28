using System;
using System.Reflection;
using System.Runtime.InteropServices;
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
            _spiMessageStamp = new MessageStamp(_posId, _secrets, TimeSpan.Zero);
            _secrets = secrets;

            // We will maintain some state
            _mostRecentPingSent = null;
            _mostRecentPongReceived = null;
            _missedPongsCount = 0;
        }

        public SpiPayAtTable EnablePayAtTable()
        {
            _spiPat = new SpiPayAtTable(this);
            return _spiPat;
        }

        public SpiPayAtTable DisablePayAtTable()
        {
            _spiPat = new SpiPayAtTable(this);
            _spiPat.Config.PayAtTabledEnabled = false;
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
                _log.Warn("Missing POS vendor ID and version. posVendorId and posVersion are required before starting");
                throw new NullReferenceException("Missing POS vendor ID and version. posVendorId and posVersion are required before starting");
            }

            _resetConn();
            _startTransactionMonitoringThread();

            CurrentFlow = SpiFlow.Idle;
            if (_secrets != null)
            {
                _log.Info("Starting in Paired State");
                CurrentStatus = SpiStatus.PairedConnecting;
                _conn.Connect(); // This is non-blocking
            }
            else
            {
                _log.Info("Starting in Unpaired State");
                _currentStatus = SpiStatus.Unpaired;
            }
        }


        /// <summary>
        /// Set the acquirer code of your bank, please contact Assembly's Integration Engineers for acquirer code.
        /// </summary>
        public bool SetAcquirerCode(string acquirerCode)
        {
            _acquirerCode = acquirerCode;
            return true;
        }

        /// <summary>
        /// Set the api key used for auto address discovery feature, please contact Assembly's Integration Engineers for Api key.
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
            if (CurrentStatus != SpiStatus.Unpaired)
                return false;

            var was = _serialNumber;
            _serialNumber = serialNumber;
            if (HasSerialNumberChanged(was))
                _autoResolveEftposAddress();

            return true;
        }

        /// <summary>
        /// Allows you to set the auto address discovery feature. 
        /// </summary>
        /// <returns></returns>
        public bool SetAutoAddressResolution(bool autoAddressResolutionEnable)
        {
            if (CurrentStatus == SpiStatus.PairedConnected)
                return false;

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
            if (CurrentStatus != SpiStatus.Unpaired)
                return false;

            if (testMode == _inTestMode)
                return true;

            // we're changing mode
            _inTestMode = testMode;
            _autoResolveEftposAddress();

            return true;
        }

        /// <summary>
        /// Allows you to set the PosId which identifies this instance of your POS.
        /// Can only be called in thge Unpaired state. 
        /// </summary>
        public bool SetPosId(string posId)
        {
            if (CurrentStatus != SpiStatus.Unpaired)
                return false;

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
            if (CurrentStatus == SpiStatus.PairedConnected || _autoAddressResolutionEnabled)
                return false;
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
            if (CurrentStatus != SpiStatus.Unpaired)
            {
                _log.Warn("Tried to Pair but we're already so.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_posId) || string.IsNullOrWhiteSpace(_eftposAddress))
            {
                _log.Warn("Tried to Pair but missing posId or eftposAddress");
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
                _log.Info("Pair Code Confirmed from POS side, but am still waiting for confirmation from Eftpos.");
                CurrentPairingFlowState.Message =
                    "Click YES on EFTPOS if code is: " + CurrentPairingFlowState.ConfirmationCode;
                _pairingFlowStateChanged(this, CurrentPairingFlowState);
            }
            else
            {
                // Already confirmed from Eftpos - So all good now. We're Paired also from the POS perspective.
                _log.Info("Pair Code Confirmed from POS side, and was already confirmed from Eftpos side. Pairing finalised.");
                _onPairingSuccess();
                _onReadyToTransact();
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
                    _log.Info("Asked to accept signature but I was not waiting for one.");
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
                    _log.Info("Asked to send auth code but I was not waiting for one.");
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
                    _log.Info("Asked to cancel transaction but I was not in the middle of one.");
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
        public InitiateTxResult InitiateGetLastTx()
        {
            if (CurrentStatus == SpiStatus.Unpaired) return new InitiateTxResult(false, "Not Paired");

            lock (_txLock)
            {
                if (CurrentFlow != SpiFlow.Idle) return new InitiateTxResult(false, "Not Idle");

                var gltRequestMsg = new GetLastTransactionRequest().ToMessage();
                CurrentFlow = SpiFlow.Transaction;
                var posRefId = gltRequestMsg.Id; // GetLastTx is not trying to get anything specific back. So we just use the message id.
                CurrentTxFlowState = new TransactionFlowState(
                    posRefId, TransactionType.GetLastTransaction, 0, gltRequestMsg,
                    $"Waiting for EFTPOS connection to make a Get-Last-Transaction request.");
                CurrentTxFlowState.CallingGlt(gltRequestMsg.Id);
                if (_send(gltRequestMsg))
                {
                    CurrentTxFlowState.Sent($"Asked EFTPOS to Get Last Transaction.");
                }
            }
            _txFlowStateChanged(this, CurrentTxFlowState);
            return new InitiateTxResult(true, "GLT Initiated");
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

                var gltRequestMsg = new GetLastTransactionRequest().ToMessage();
                CurrentTxFlowState = new TransactionFlowState(
                    posRefId, txType, 0, gltRequestMsg,
                    $"Waiting for EFTPOS connection to attempt recovery.");

                if (_send(gltRequestMsg))
                {
                    CurrentTxFlowState.Sent($"Asked EFTPOS to recover state.");
                }
            }
            _txFlowStateChanged(this, CurrentTxFlowState);
            return new InitiateTxResult(true, "Recovery Initiated");
        }

        /// <summary>
        /// GltMatch attempts to conclude whether a gltResponse matches an expected transaction and returns
        /// the outcome. 
        /// If Success/Failed is returned, it means that the gtlResponse did match, and that transaction was succesful/failed.
        /// If Unknown is returned, it means that the gltResponse does not match the expected transaction. 
        /// </summary>
        /// <param name="gltResponse">The GetLastTransactionResponse message to check</param>
        /// <param name="posRefId">The Reference Id that you passed in with the original request.</param>

        /// <returns></returns>
        public Message.SuccessState GltMatch(GetLastTransactionResponse gltResponse, string posRefId)
        {
            _log.Info($"GLT CHECK: PosRefId: {posRefId}->{gltResponse.GetPosRefId()}");

            if (!posRefId.Equals(gltResponse.GetPosRefId()))
            {
                return Message.SuccessState.Unknown;
            }

            return gltResponse.GetSuccessState();
        }

        [Obsolete("Use GltMatch(GetLastTransactionResponse gltResponse, string posRefId, TransactionType expectedType)")]
        public Message.SuccessState GltMatch(GetLastTransactionResponse gltResponse, TransactionType expectedType, int expectedAmount, DateTime requestTime, string posRefId)
        {
            return GltMatch(gltResponse, posRefId);
        }

        public void PrintReport(string key, string payload)
        {
            lock (_txLock)
            {
                _send(new PrintingRequest(key, payload).ToMessage());
            }
        }

        public void GetTerminalStatus()
        {
            lock (_txLock)
            {
                _send(new TerminalStatusRequest().ToMessage());
            }
        }

        public void GetTerminalConfiguration()
        {
            lock (_txLock)
            {
                _send(new TerminalConfigurationRequest().ToMessage());
            }
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
                    // Still Waiting for User to say yes on POS
                    _log.Info("Got Pair Confirm from Eftpos, but still waiting for use to confirm from POS.");
                    CurrentPairingFlowState.Message = "Confirm that the following Code is what the EFTPOS showed";
                    _pairingFlowStateChanged(this, CurrentPairingFlowState);
                }
                else
                {
                    _log.Info("Got Pair Confirm from Eftpos, and already had confirm from POS. Now just waiting for first pong.");
                    _onPairingSuccess();
                }

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
            _log.Info("Eftpos was Unpaired. I shall unpair from my end as well.");
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
                    _log.Info($"Received Signature Required but I was not waiting for one. Incoming Pos Ref ID: {incomingPosRefId}");
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
                    _log.Info($"Received Auth Code Required but I was not waiting for one. Incoming Pos Ref ID: {incomingPosRefId}");
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
                    _log.Info($"Received Purchase response but I was not waiting for one. Incoming Pos Ref ID: {incomingPosRefId}");
                    return;
                }
                // TH-1A, TH-2A

                CurrentTxFlowState.Completed(m.GetSuccessState(), m, "Purchase Transaction Ended.");
                // TH-6A, TH-6E
            }
            _txFlowStateChanged(this, CurrentTxFlowState);
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
                    _log.Info($"Received Cashout Response but I was not waiting for one. Incoming Pos Ref ID: {incomingPosRefId}");
                    return;
                }
                // TH-1A, TH-2A

                CurrentTxFlowState.Completed(m.GetSuccessState(), m, "Cashout Transaction Ended.");
                // TH-6A, TH-6E
            }
            _txFlowStateChanged(this, CurrentTxFlowState);
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
                    _log.Info($"Received Moto Response but I was not waiting for one. Incoming Pos Ref ID: {incomingPosRefId}");
                    return;
                }
                // TH-1A, TH-2A

                CurrentTxFlowState.Completed(m.GetSuccessState(), m, "Moto Transaction Ended.");
                // TH-6A, TH-6E
            }
            _txFlowStateChanged(this, CurrentTxFlowState);
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
                    _log.Info($"Received Refund response but I was not waiting for this one. Incoming Pos Ref ID: {incomingPosRefId}");
                    return;
                }
                // TH-1A, TH-2A

                CurrentTxFlowState.Completed(m.GetSuccessState(), m, "Refund Transaction Ended.");
                // TH-6A, TH-6E
            }
            _txFlowStateChanged(this, CurrentTxFlowState);
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
                    _log.Info($"Received Settle response but I was not waiting for one. {m.DecryptedJson}");
                    return;
                }
                // TH-1A, TH-2A

                CurrentTxFlowState.Completed(m.GetSuccessState(), m, "Settle Transaction Ended.");
                // TH-6A, TH-6E
            }
            _txFlowStateChanged(this, CurrentTxFlowState);
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
                    _log.Info($"Received Settlement Enquiry response but I was not waiting for one. {m.DecryptedJson}");
                    return;
                }
                // TH-1A, TH-2A

                CurrentTxFlowState.Completed(m.GetSuccessState(), m, "Settlement Enquiry Ended.");
                // TH-6A, TH-6E
            }
            _txFlowStateChanged(this, CurrentTxFlowState);
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
                    _log.Info($"Was trying to cancel a transaction but there is nothing to cancel. Calling GLT to see what's up");
                    _callGetLastTransaction();
                }
                else
                {
                    _log.Info($"Received Error Event But Don't know what to do with it. {m.DecryptedJson}");
                }
            }
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
                if (CurrentFlow != SpiFlow.Transaction || txState.Finished)
                {
                    _log.Info($"Received glt response but we were not in the middle of a tx. ignoring.");
                    return;
                }

                if (!txState.AwaitingGltResponse)
                {
                    _log.Info($"received a glt response but we had not asked for one within this transaction. Perhaps leftover from previous one. ignoring.");
                    return;
                }

                if (txState.LastGltRequestId != m.Id)
                {
                    _log.Info($"received a glt response but the message id does not match the glt request that we sent. strange. ignoring.");
                    return;
                }

                // TH-4 We were in the middle of a transaction.
                // Let's attempt recovery. This is step 4 of Transaction Processing Handling
                _log.Info($"Got Last Transaction..");
                txState.GotGltResponse();
                var gtlResponse = new GetLastTransactionResponse(m);
                if (!gtlResponse.WasRetrievedSuccessfully())
                {
                    if (gtlResponse.IsStillInProgress(txState.PosRefId))
                    {
                        // TH-4E - Operation In Progress

                        if (gtlResponse.IsWaitingForSignatureResponse() && !txState.AwaitingSignatureCheck)
                        {
                            _log.Info($"Eftpos is waiting for us to send it signature accept/decline, but we were not aware of this. " +
                                      $"The user can only really decline at this stage as there is no receipt to print for signing.");
                            CurrentTxFlowState.SignatureRequired(new SignatureRequired(txState.PosRefId, m.Id, "MISSING RECEIPT\n DECLINE AND TRY AGAIN."), "Recovered in Signature Required but we don't have receipt. You may Decline then Retry.");
                        }
                        else if (gtlResponse.IsWaitingForAuthCode() && !txState.AwaitingPhoneForAuth)
                        {
                            _log.Info($"Eftpos is waiting for us to send it auth code, but we were not aware of this. " +
                                      $"We can only cancel the transaction at this stage as we don't have enough information to recover from this.");
                            CurrentTxFlowState.PhoneForAuthRequired(new PhoneForAuthRequired(txState.PosRefId, m.Id, "UNKNOWN", "UNKNOWN"), "Recovered mid Phone-For-Auth but don't have details. You may Cancel then Retry.");
                        }
                        else
                        {
                            _log.Info($"Operation still in progress... stay waiting.");
                            // No need to publish txFlowStateChanged. Can return;
                            return;
                        }
                    }
                    else if (gtlResponse.WasTimeOutOfSyncError())
                    {
                        // Let's not give up based on a TOOS error.
                        // Let's log it, and ignore it. 
                        _log.Info($"Time-Out-Of-Sync error in Get Last Transaction response. Let's ignore it and we'll try again.");
                        // No need to publish txFlowStateChanged. Can return;
                        return;
                    }
                    else
                    {
                        // TH-4X - Unexpected Response when recovering
                        _log.Info($"Unexpected Response in Get Last Transaction during - Received posRefId:{gtlResponse.GetPosRefId()} Error:{m.GetError()}");
                        txState.UnknownCompleted("Unexpected Error when recovering Transaction Status. Check EFTPOS. ");
                    }
                }
                else
                {
                    if (txState.Type == TransactionType.GetLastTransaction)
                    {
                        // THIS WAS A PLAIN GET LAST TRANSACTION REQUEST, NOT FOR RECOVERY PURPOSES.
                        _log.Info($"Retrieved Last Transaction as asked directly by the user.");
                        gtlResponse.CopyMerchantReceiptToCustomerReceipt();
                        txState.Completed(m.GetSuccessState(), m, "Last Transaction Retrieved");
                    }
                    else
                    {
                        // TH-4A - Let's try to match the received last transaction against the current transaction
                        var successState = GltMatch(gtlResponse, txState.PosRefId);
                        if (successState == Message.SuccessState.Unknown)
                        {
                            // TH-4N: Didn't Match our transaction. Consider Unknown State.
                            _log.Info($"Did not match transaction.");
                            txState.UnknownCompleted("Failed to recover Transaction Status. Check EFTPOS. ");
                        }
                        else
                        {
                            // TH-4Y: We Matched, transaction finished, let's update ourselves
                            gtlResponse.CopyMerchantReceiptToCustomerReceipt();
                            txState.Completed(successState, m, "Transaction Ended.");
                        }
                    }
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
                if (CurrentFlow != SpiFlow.Transaction || CurrentTxFlowState.Finished || !CurrentTxFlowState.PosRefId.Equals(incomingPosRefId))
                {
                    _log.Info($"Received Cancel Required but I was not waiting for one. Incoming Pos Ref ID: {incomingPosRefId}");
                    return;
                }

                var txState = CurrentTxFlowState;
                var cancelResponse = new CancelTransactionResponse(m);

                if (cancelResponse.Success) return;

                _log.Warn("Failed to cancel transaction: reason=" + cancelResponse.GetErrorReason() + ", detail=" + cancelResponse.GetErrorDetail());

                txState.CancelFailed("Failed to cancel transaction: " + cancelResponse.GetErrorDetail() + ". Check EFTPOS.");
            }

            _txFlowStateChanged(this, CurrentTxFlowState);
        }

        private void _handleSetPosInfoResponse(Message m)
        {
            lock (_txLock)
            {
                var response = new SetPosInfoResponse(m);
                if (response.isSuccess())
                {
                    _hasSetInfo = true;
                    _log.Info("Setting POS info successful");
                }
                else
                {
                    _log.Warn("Setting POS info failed: reason=" + response.getErrorReason() + ", detail=" + response.getErrorDetail());
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
                                _log.Info($"Been too long waiting for transaction to cancel.");
                                txState.UnknownCompleted("Waited long enough for Cancel Transaction result. Check EFTPOS. ");
                                needsPublishing = true;
                            }
                            else if (state.RequestSent && DateTime.Now > state.LastStateRequestTime.Add(_checkOnTxFrequency))
                            {
                                // TH-1T, TH-4T - It's been a while since we received an update, let's call a GLT
                                _log.Info($"Checking on our transaction. Last we asked was at {state.LastStateRequestTime}...");
                                _callGetLastTransaction();
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
                    _log.Info($"I'm Connecting to the Eftpos at {_eftposAddress}...");
                    break;

                case ConnectionState.Connected:
                    _retriesSinceLastDeviceAddressResolution = 0;

                    if (CurrentFlow == SpiFlow.Pairing && CurrentStatus == SpiStatus.Unpaired)
                    {
                        CurrentPairingFlowState.Message = "Requesting to Pair...";
                        _pairingFlowStateChanged(this, CurrentPairingFlowState);
                        var pr = PairingHelper.NewPairequest();
                        _send(pr.ToMessage());
                    }
                    else
                    {
                        _log.Info($"I'm Connected to {_eftposAddress}...");
                        _spiMessageStamp.Secrets = _secrets;
                        _startPeriodicPing();
                    }
                    break;

                case ConnectionState.Disconnected:
                    // Let's reset some lifecycle related to connection state, ready for next connection
                    _log.Info($"I'm disconnected from {_eftposAddress}...");
                    _mostRecentPingSent = null;
                    _mostRecentPongReceived = null;
                    _missedPongsCount = 0;
                    _stopPeriodicPing();

                    if (CurrentStatus != SpiStatus.Unpaired)
                    {
                        CurrentStatus = SpiStatus.PairedConnecting;

                        lock (_txLock)
                        {
                            if (CurrentFlow == SpiFlow.Transaction && !CurrentTxFlowState.Finished)
                            {
                                // we're in the middle of a transaction, just so you know!
                                // TH-1D
                                _log.Warn($"Lost connection in the middle of a transaction...");
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

                            _log.Info($"Will try to reconnect in {_sleepBeforeReconnectMs}ms ...");
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
                        _log.Warn("Lost Connection during pairing.");
                        CurrentPairingFlowState.Message = "Could not Connect to Pair. Check Network and Try Again...";
                        _onPairingFailed();
                        _pairingFlowStateChanged(this, CurrentPairingFlowState);
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
                    _doPing();

                    Thread.Sleep(_pongTimeout);
                    if (_mostRecentPingSent != null &&
                        (_mostRecentPongReceived == null || _mostRecentPongReceived.Id != _mostRecentPingSent.Id))
                    {
                        _missedPongsCount += 1;
                        _log.Info($"Eftpos didn't reply to my Ping. Missed Count: {_missedPongsCount}/{_missedPongsToDisconnect}. ");

                        if (_missedPongsCount < _missedPongsToDisconnect)
                        {
                            _log.Info("Trying another ping...");
                            continue;
                        }

                        // This means that we have reached missed pong limit.
                        // We consider this connection as broken.
                        // Let's Disconnect.
                        _log.Info("Disconnecting...");
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
            _log.Info("On Ready To Transact!");

            // So, we have just made a connection and pinged successfully.
            CurrentStatus = SpiStatus.PairedConnected;

            lock (_txLock)
            {
                if (CurrentFlow == SpiFlow.Transaction && !CurrentTxFlowState.Finished)
                {
                    if (CurrentTxFlowState.RequestSent)
                    {
                        // TH-3A - We've just reconnected and were in the middle of Tx.
                        // Let's get the last transaction to check what we might have missed out on.
                        _callGetLastTransaction();
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
                    if (!_hasSetInfo) { _callSetPosInfo(); }

                    // let's also tell the eftpos our latest table configuration.
                    _spiPat?.PushPayAtTableConfig();
                }
            }
        }

        private void _callSetPosInfo()
        {
            SetPosInfoRequest setPosInfoRequest = new SetPosInfoRequest(_posVersion, _posVendorId, ".net", GetVersion(), DeviceInfo.GetAppDeviceInfo());
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
            // We need to maintain this time delta otherwise the server will not accept our messages.
            _spiMessageStamp.ServerTimeDelta = m.GetServerTimeDelta();

            if (_mostRecentPongReceived == null)
            {
                // First pong received after a connection, and after the pairing process is fully finalised.
                if (CurrentStatus != SpiStatus.Unpaired)
                {
                    _log.Info("First pong of connection and in paired state.");
                    _onReadyToTransact();
                }
                else
                {
                    _log.Info("First pong of connection but pairing process not finalised yet.");
                }
            }

            _mostRecentPongReceived = m;
            _log.Debug($"PongLatency:{DateTime.Now.Subtract(_mostRecentPingSentTime)}");
        }

        /// <summary>
        /// The server will also send us pings. We need to reply with a pong so it doesn't disconnect us.
        /// </summary>
        /// <param name="m"></param>
        private void _handleIncomingPing(Message m)
        {
            var pong = PongHelper.GeneratePongRessponse(m);
            _send(pong);
        }

        /// <summary>
        /// Ask the PinPad to tell us what the Most Recent Transaction was
        /// </summary>
        private void _callGetLastTransaction()
        {
            var gltRequestMsg = new GetLastTransactionRequest().ToMessage();
            CurrentTxFlowState.CallingGlt(gltRequestMsg.Id);
            _send(gltRequestMsg);
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
            _log.Debug("Received:" + m.DecryptedJson);

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
                case Events.GetLastTransactionResponse:
                    _handleGetLastTransactionResponse(m);
                    break;
                case Events.SettleResponse:
                    _handleSettleResponse(m);
                    break;
                case Events.SettlementEnquiryResponse:
                    _handleSettlementEnquiryResponse(m);
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
                case Events.Error:
                    _handleErrorEvent(m);
                    break;
                case Events.InvalidHmacSignature:
                    _log.Info("I could not verify message from Eftpos. You might have to Un-pair Eftpos and then reconnect.");
                    break;
                default:
                    _log.Info($"I don't Understand Event: {m.EventName}, {m.Data}. Perhaps I have not implemented it yet.");
                    break;
            }
        }

        private void _onWsErrorReceived(object sender, MessageEventArgs error)
        {
            _log.Warn("Received WS Error: " + error.Message);
        }

        internal bool _send(Message message)
        {
            var json = message.ToJson(_spiMessageStamp);
            if (_conn.Connected)
            {
                _log.Debug("Sending: " + message.DecryptedJson);
                _conn.Send(json);
                return true;
            }
            else
            {
                _log.Debug("Asked to send, but not connected: " + message.DecryptedJson);
                return false;
            }
        }
        #endregion

        #region Device Management 

        private bool HasSerialNumberChanged(string updatedSerialNumber)
        {
            return _serialNumber != updatedSerialNumber;
        }

        private bool HasEftposAddressChanged(string updatedEftposAddress)
        {
            return _eftposAddress != updatedEftposAddress;
        }

        private async void _autoResolveEftposAddress()
        {
            if (!_autoAddressResolutionEnabled)
                return;

            if (string.IsNullOrWhiteSpace(_serialNumber) || string.IsNullOrWhiteSpace(_deviceApiKey))
            {
                _log.Warn("Missing serialNumber and/or deviceApiKey. Need to set them before for Auto Address to work.");
                return;
            }

            var service = new DeviceAddressService();
            var addressResponse = await service.RetrieveService(_serialNumber, _deviceApiKey, _acquirerCode, _inTestMode);

            if (addressResponse?.Address == null)
                return;

            if (!HasEftposAddressChanged(addressResponse.Address))
                return;

            // update device and connection address
            _eftposAddress = "ws://" + addressResponse.Address;
            _conn.Address = _eftposAddress;

            CurrentDeviceStatus = new DeviceAddressStatus
            {
                Address = addressResponse.Address,
                LastUpdated = addressResponse.LastUpdated
            };

            _deviceAddressChanged(this, CurrentDeviceStatus);
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
            _log.Info("Disposing...");
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
        private bool _inTestMode;
        private bool _autoAddressResolutionEnabled;
        private Secrets _secrets;
        private MessageStamp _spiMessageStamp;
        private string _posVendorId;
        private string _posVersion;
        private bool _hasSetInfo;

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
        private readonly int _sleepBeforeReconnectMs = 5000;
        private readonly int _missedPongsToDisconnect = 2;
        private readonly int _retriesBeforeResolvingDeviceAddress = 5;

        private SpiPayAtTable _spiPat;

        private SpiPreauth _spiPreauth;

        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger("spi");

        private static readonly string _version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        #endregion        
    }
}