using System.Collections.Generic;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;

namespace SPIClient
{
    /// <summary>
    /// These attributes work for COM interop.
    /// </summary>
    [ComVisible(true)]
    [Guid("8A139471-1D4B-41CA-B9AE-726562108C2A")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class SpiPreauth
    {
        private readonly Spi _spi;
        private readonly object _txLock;
        public readonly SpiConfig Config = new SpiConfig();

        /// <summary>
        /// This default stucture works for COM interop.
        /// </summary>
        public SpiPreauth() { }

        internal SpiPreauth(Spi spi, object txLock)
        {
            _spi = spi;
            _txLock = txLock;
        }

        public InitiateTxResult InitiateAccountVerifyTx(string posRefId)
        {
            var verifyMsg = new AccountVerifyRequest(posRefId).ToMessage();

            var tfs = new TransactionFlowState(
                posRefId, TransactionType.AccountVerify, 0, verifyMsg,
                $"Waiting for EFTPOS connection to make account verify request");
            var sentMsg = $"Asked EFTPOS to verify account";
            return _initiatePreauthTx(tfs, sentMsg);
        }

        public InitiateTxResult InitiateOpenTx(string posRefId, int amountCents)
        {
            return InitiateOpenTx(posRefId, amountCents, new TransactionOptions());
        }

        public InitiateTxResult InitiateOpenTx(string posRefId, int amountCents, TransactionOptions options)
        {
            var msg = new PreauthOpenRequest(amountCents, posRefId)
            {
                Config = Config,
                Options = options
            }.ToMessage();

            var tfs = new TransactionFlowState(
                posRefId, TransactionType.Preauth, amountCents, msg,
                $"Waiting for EFTPOS connection to make preauth request for ${amountCents / 100.0:.00}");
            var sentMsg = $"Asked EFTPOS to create preauth for ${amountCents / 100.0:.00}";
            return _initiatePreauthTx(tfs, sentMsg);
        }

        public InitiateTxResult InitiateTopupTx(string posRefId, string preauthId, int amountCents)
        {
            return InitiateTopupTx(posRefId, preauthId, amountCents, new TransactionOptions());
        }

        public InitiateTxResult InitiateTopupTx(string posRefId, string preauthId, int amountCents, TransactionOptions options)
        {
            var msg = new PreauthTopupRequest(preauthId, amountCents, posRefId)
            {
                Config = Config,
                Options = options
            }.ToMessage();

            var tfs = new TransactionFlowState(
                posRefId, TransactionType.Preauth, amountCents, msg,
                $"Waiting for EFTPOS connection to make preauth topup request for ${amountCents / 100.0:.00}");
            var sentMsg = $"Asked EFTPOS to make preauth topup for ${amountCents / 100.0:.00}";
            return _initiatePreauthTx(tfs, sentMsg);
        }

        public InitiateTxResult InitiatePartialCancellationTx(string posRefId, string preauthId, int amountCents)
        {
            return InitiatePartialCancellationTx(posRefId, preauthId, amountCents, new TransactionOptions());
        }

        public InitiateTxResult InitiatePartialCancellationTx(string posRefId, string preauthId, int amountCents, TransactionOptions options)
        {
            var msg = new PreauthPartialCancellationRequest(preauthId, amountCents, posRefId)
            {
                Config = Config,
                Options = options
            }.ToMessage();

            var tfs = new TransactionFlowState(
                posRefId, TransactionType.Preauth, amountCents, msg,
                $"Waiting for EFTPOS connection to make preauth partial cancellation request for ${amountCents / 100.0:.00}");
            var sentMsg = $"Asked EFTPOS to make preauth partial cancellation for ${amountCents / 100.0:.00}";
            return _initiatePreauthTx(tfs, sentMsg);
        }

        public InitiateTxResult InitiateExtendTx(string posRefId, string preauthId)
        {
            return InitiateExtendTx(posRefId, preauthId, new TransactionOptions());
        }

        public InitiateTxResult InitiateExtendTx(string posRefId, string preauthId, TransactionOptions options)
        {
            var msg = new PreauthExtendRequest(preauthId, posRefId)
            {
                Config = Config,
                Options = options
            }.ToMessage();

            var tfs = new TransactionFlowState(
                posRefId, TransactionType.Preauth, 0, msg,
                $"Waiting for EFTPOS connection to make preauth Extend request");
            var sentMsg = $"Asked EFTPOS to make preauth Extend request";
            return _initiatePreauthTx(tfs, sentMsg);
        }

        public InitiateTxResult InitiateCompletionTx(string posRefId, string preauthId, int amountCents)
        {
            return InitiateCompletionTx(posRefId, preauthId, amountCents, 0);
        }

        public InitiateTxResult InitiateCompletionTx(string posRefId, string preauthId, int amountCents, int surchargeAmount)
        {
            return InitiateCompletionTx(posRefId, preauthId, amountCents, surchargeAmount, new TransactionOptions());
        }

        public InitiateTxResult InitiateCompletionTx(string posRefId, string preauthId, int amountCents, int surchargeAmount, TransactionOptions options)
        {
            var msg = new PreauthCompletionRequest(preauthId, amountCents, posRefId)
            {
                Config = Config,
                SurchargeAmount = surchargeAmount,
                Options = options
            }.ToMessage();

            var tfs = new TransactionFlowState(
                posRefId, TransactionType.Preauth, amountCents, msg,
                $"Waiting for EFTPOS connection to make preauth completion request for ${amountCents / 100.0:.00}");
            var sentMsg = $"Asked EFTPOS to make preauth completion for ${amountCents / 100.0:.00}";
            return _initiatePreauthTx(tfs, sentMsg);
        }

        public InitiateTxResult InitiateCancelTx(string posRefId, string preauthId)
        {
            return InitiateCancelTx(posRefId, preauthId, new TransactionOptions());
        }

        public InitiateTxResult InitiateCancelTx(string posRefId, string preauthId, TransactionOptions options)
        {
            var msg = new PreauthCancelRequest(preauthId, posRefId)
            {
                Config = Config,
                Options = options
            }.ToMessage();

            var tfs = new TransactionFlowState(
                posRefId, TransactionType.Preauth, 0, msg,
                $"Waiting for EFTPOS connection to make preauth cancellation request");
            var sentMsg = $"Asked EFTPOS to make preauth cancellation request";
            return _initiatePreauthTx(tfs, sentMsg);
        }


        private InitiateTxResult _initiatePreauthTx(TransactionFlowState tfs, string sentMsg)
        {
            if (_spi.CurrentStatus == SpiStatus.Unpaired) return new InitiateTxResult(false, "Not Paired");

            lock (_txLock)
            {
                if (_spi.CurrentFlow != SpiFlow.Idle) return new InitiateTxResult(false, "Not Idle");

                _spi.CurrentFlow = SpiFlow.Transaction;
                _spi.CurrentTxFlowState = tfs;
                if (_spi._send(tfs.Request))
                {
                    _spi.CurrentTxFlowState.Sent(sentMsg);
                }
            }
            _spi._txFlowStateChanged(this, _spi.CurrentTxFlowState);
            return new InitiateTxResult(true, "Preauth Initiated");
        }

        internal void _handlePreauthMessage(Message m)
        {
            switch (m.EventName)
            {
                case PreauthEvents.AccountVerifyResponse:
                    _handleAccountVerifyResponse(m);
                    break;
                case PreauthEvents.PreauthOpenResponse:
                case PreauthEvents.PreauthTopupResponse:
                case PreauthEvents.PreauthPartialCancellationResponse:
                case PreauthEvents.PreauthExtendResponse:
                case PreauthEvents.PreauthCompleteResponse:
                case PreauthEvents.PreauthCancellationResponse:
                    _handlePreauthResponse(m);
                    break;
                default:
                    _log.Info($"I don't Understand Preauth Event: {m.EventName}, {m.Data}. Perhaps I have not implemented it yet.");
                    break;
            }
        }

        private void _handleAccountVerifyResponse(Message m)
        {
            lock (_txLock)
            {
                var incomingPosRefId = m.GetDataStringValue("pos_ref_id");
                var currentTxFlowState = _spi.CurrentTxFlowState;
                if (_spi.CurrentFlow != SpiFlow.Transaction || currentTxFlowState.Finished || !currentTxFlowState.PosRefId.Equals(incomingPosRefId))
                {
                    _log.Info($"Received Account Verify response but I was not waiting for one. Incoming Pos Ref ID: {incomingPosRefId}");
                    return;
                }
                // TH-1A, TH-2A

                currentTxFlowState.Completed(m.GetSuccessState(), m, "Account Verify Transaction Ended.");
                // TH-6A, TH-6E
            }
            _spi._txFlowStateChanged(this, _spi.CurrentTxFlowState);
        }

        private void _handlePreauthResponse(Message m)
        {
            lock (_txLock)
            {
                var incomingPosRefId = m.GetDataStringValue("pos_ref_id");
                var currentTxFlowState = _spi.CurrentTxFlowState;
                if (_spi.CurrentFlow != SpiFlow.Transaction || currentTxFlowState.Finished || !currentTxFlowState.PosRefId.Equals(incomingPosRefId))
                {
                    _log.Info($"Received Preauth response but I was not waiting for one. Incoming Pos Ref ID: {incomingPosRefId}");
                    return;
                }
                // TH-1A, TH-2A

                currentTxFlowState.Completed(m.GetSuccessState(), m, "Preauth Transaction Ended.");
                // TH-6A, TH-6E
            }
            _spi._txFlowStateChanged(this, _spi.CurrentTxFlowState);
        }

        internal static bool IsPreauthEvent(string eventName)
        {
            return eventName.StartsWith("preauth")
                   || eventName == PreauthEvents.PreauthCompleteResponse
                   || eventName == PreauthEvents.PreauthCompleteRequest
                   || eventName == PreauthEvents.AccountVerifyRequest
                   || eventName == PreauthEvents.AccountVerifyResponse;
        }

        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger("spipreauth");
    }
}