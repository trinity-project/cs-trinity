/*
Author: Trinity Core Team

MIT License

Copyright (c) 2018 Trinity

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using System;

using Trinity.ChannelSet.Definitions;
using Trinity.TrinityDB.Definitions;
using Trinity.Wallets.Templates.Definitions;
using Trinity.Wallets.Templates.Messages;

namespace Trinity.Wallets.TransferHandler.TransactionHandler
{
    /// <summary>
    /// Class Handler for handling Rsmc Message
    /// </summary>
    public class RsmcHandler : TransactionHandler<Rsmc, Rsmc, RsmcHandler, RsmcSignHandler>
    {
        // Locals
        private bool isRsmcValid = false;
        private bool isFounder = false;

        // record the transaction
        private TransactionRsmcContent currentTransaction = null;
        private TransactionTabelHLockPair currentHLockTransaction = null;

        private CommitmentTx commTx;
        private RevocableDeliveryTx rdTx;
        private BreachRemedyTx brTx;

        /// <summary>
        /// This constructor is used by UI to trigger a new RSMC Transaction or HTLC to RSMC
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="receiver"></param>
        /// <param name="channel"></param>
        /// <param name="asset"></param>
        /// <param name="magic"></param>
        /// <param name="nonce"></param>
        /// <param name="payment"></param>
        /// <param name="role"></param>
        public RsmcHandler(string sender, string receiver, string channel, string asset,
            string magic, UInt64 nonce, long payment, string hashcode=null)
            : base(sender, receiver, channel, asset, magic, nonce, payment, 0, hashcode)
        {
            this.Request.TxNonce = this.NextNonce(channel);
            this.currentHLockTransaction = this.GetHLockPair();
            this.isFounder = this.IsRole0(this.Request.MessageBody.RoleIndex)
                || this.IsRole2(this.Request.MessageBody.RoleIndex);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"> Previous Rsmc message </param>
        /// <param name="role"></param>
        public RsmcHandler(Rsmc message, int role) : base(message, role)
        {
            // Get the current transaction
            this.currentTransaction = this.GetCurrentTransaction<TransactionRsmcContent>();
            this.currentHLockTransaction = this.GetHLockPair();

            this.isFounder = this.IsRole0(this.Request.MessageBody.RoleIndex)
                || this.IsRole2(this.Request.MessageBody.RoleIndex);
        }

        public RsmcHandler(string message) : base(message)
        {
            // Get the current transaction
            this.currentTransaction = this.GetCurrentTransaction<TransactionRsmcContent>();
            this.currentHLockTransaction = this.GetHLockPair();

            this.isFounder = this.IsRole1(this.Request.MessageBody.RoleIndex)
                || this.IsRole3(this.Request.MessageBody.RoleIndex);
        }

        public override bool SucceedStep()
        {
            if (base.SucceedStep())
            {
                Log.Info(
                "Succeed handling Rsmc. Channel: {0}, AssetType: {1}, Balance: {2}. PeerBalance: {3}, Payment: {4}, RoleIndex: {5}.",
                    this.Request.ChannelName, this.Request.AssetType, this.SelfBalance(), this.PeerBalance(),
                    this.Request.MessageBody.Value, this.Request.MessageBody.RoleIndex, this.Request.MessageBody.RoleIndex);

                // udpate the channel balance
                if (this.IsRole2(this.Request.MessageBody.RoleIndex) || this.IsRole3(this.Request.MessageBody.RoleIndex))
                {
                    this.UpdateChannelBalance();
                }
                
                return true;
            }

            return false;
        }

        public override bool FailStep(string errorCode)
        {
            Log.Error(
                "Failed handling Rsmc. Channel: {0}, AssetType: {1}, Balance: {2}. PeerBalance: {3}, Payment: {4}, RoleIndex: {5}. Error: {6}",
                    this.Request.ChannelName, this.Request.AssetType, this.SelfBalance(), this.PeerBalance(),
                    this.Request.MessageBody.Value, this.Request.MessageBody.RoleIndex, this.Request.MessageBody.RoleIndex, errorCode);

            return base.FailStep(errorCode);
        }

        public override bool MakeTransaction()
        {
            if (base.MakeTransaction())
            { 
                Log.Info("Send Rsmc. Channel: {0}, AssetType: {1}, Balance: {2}. PeerBalance: {3}, Payment: {4}, RoleIndex: {5}",
                    this.Request.ChannelName, this.Request.AssetType, this.SelfBalance(), this.PeerBalance(),
                    this.Request.MessageBody.Value, this.Request.MessageBody.RoleIndex);
            }

            return false;
        }

        public override bool Verify()
        {
            this.VerifyUri();
            this.VerifyRoleIndex();
            this.VerifyAssetType(this.Request.MessageBody.AssetType);

            if (this.IsRole0(this.Request.MessageBody.RoleIndex)) {
                this.VerifyNonce(this.NextNonce(this.Request.ChannelName));
            }
            else { 
                this.VerifyNonce(this.CurrentNonce(this.Request.ChannelName));
            }

            if (this.IsRole2(this.Request.MessageBody.RoleIndex) || this.IsRole2(this.Request.MessageBody.RoleIndex))
            {
                this.VerifySignarture(this.Request.MessageBody.BreachRemedy.originalData.txData,
                    this.Request.MessageBody.BreachRemedy.txDataSign);
            }

            return true;
        }

        public override bool MakeupMessage()
        {
            return this.MakeupTransactionBody();
        }

        public bool MakeupTransactionBody()
        {
            if (!this.isRsmcValid)
            {
                Log.Error("Error to makeup the RSMC. Channel: {0}, AssetType: {1}, Balance: {2}. PeerBalance: {3}, Payment: {4}, RoleIndex: {5}",
                    this.Request.ChannelName, this.Request.AssetType, this.SelfBalance(), this.PeerBalance(), 
                    this.Request.MessageBody.Value, this.Request.MessageBody.RoleIndex);
                return false;
            }

            // record the item to database
            if (IsRole0(this.Request.MessageBody.RoleIndex) || IsRole1(this.Request.MessageBody.RoleIndex))
            {
                // Create Commitment transaction
                this.neoTransaction.CreateCTX(out this.commTx);

                // create Revocable commitment transaction
                this.neoTransaction.createRDTX(out this.rdTx, this.commTx.txId);

                // message body
                this.Request.MessageBody.Commitment = this.commTx;
                this.Request.MessageBody.RevocableDelivery = this.rdTx;
            }
            else if (IsRole2(this.Request.MessageBody.RoleIndex) || IsRole3(this.Request.MessageBody.RoleIndex))
            {
                this.neoTransaction?.SetAddressRSMC(this.currentTransaction.commitment.originalData.addressRSMC);
                this.neoTransaction?.SetScripRSMC(this.currentTransaction.commitment.originalData.scriptRSMC);
                this.neoTransaction.CreateBRTX(out this.brTx, this.currentTransaction.commitment.originalData.txId);
                this.Request.MessageBody.BreachRemedy = this.MakeupSignature(this.brTx);
            }

            return true;
        }

        #region RsmcHandler_OVERRIDE_VIRUAL_SETS_OF_DIFFERENT_TRANSACTION_HANDLER
        public override void InitializeBlockChainApi() { this.GetBlockChainAdaptorApi(false); }

        public override void InitializeMessageBody(string asset, long payment, int role = 0, string hashcode = null, string rcode = null)
        {
            this.Request.MessageBody = new RsmcBody
            {
                AssetType = asset,
                Value = payment,
                RoleIndex = role,
                Comments = hashcode,
                HashR = hashcode,
            };
        }

        public override void InitializeMessageBody(int role = 0)
        {
            this.Request.MessageBody = new RsmcBody
            {
                AssetType = this.onGoingRequest.MessageBody.AssetType,
                Value = this.onGoingRequest.MessageBody.Value,
                RoleIndex = role,
                Comments = this.onGoingRequest.MessageBody.Comments,
                HashR = this.onGoingRequest.MessageBody.HashR ?? this.onGoingRequest.MessageBody.Comments
            };
        }

        public override void SetLocalsFromBody()
        {
            // RoleIndex Related
            this.RoleMax = 3;
            this.currentRole = this.Request.MessageBody.RoleIndex; // record current role Index
            this.HashR = this.Request.MessageBody.HashR ?? this.Request.MessageBody.Comments;

            // Asset type from message body for adaptor old version trinity
            this.Request.AssetType = this.Request.MessageBody.AssetType;
        }

        public override void SetTransactionValid()
        {
            this.isRsmcValid = true;
        }

        public override void CalculateBalance()
        {
            this.CalculateBalance(this.Request.MessageBody.Value, this.isFounder);
        }

        public override void AddTransaction(bool isFounder = false)
        {
            // Just add the transaction when role index is zero
            if (!IsRole0(this.currentRole))
            {
                return;
            }

            // Add transaction to leveldb
            TransactionRsmcContent txContent = this.NewTransactionContent<TransactionRsmcContent>(isFounder);

            txContent.type = EnumTransactionType.RSMC.ToString();
            txContent.payment = this.Request.MessageBody.Value;

            txContent.commitment = new TxContentsSignGeneric<CommitmentTx>();
            txContent.revocableDelivery = new TxContentsSignGeneric<RevocableDeliveryTx>();

            if (isFounder)
            {
                // add commitment and revocable delivery transaction info for later verification
                txContent.commitment.originalData = this.Request.MessageBody.Commitment;
                txContent.revocableDelivery.originalData = this.Request.MessageBody.RevocableDelivery;
            }
            else
            {
                // Monitor founder commitment txid in partner side
                txContent.monitorTxId = this.Request.MessageBody.Commitment?.txId;
            }

            // record the transaction to levelDB
            this.GetChannelLevelDbEntry()?.AddTransaction(this.Request.TxNonce, txContent);
        }

        public override void UpdateTransaction()
        {
            // when role 0, just add the transaction
            if (IsRole0(this.Request.MessageBody.RoleIndex))
            {
                return;
            }

            // update the transaction
            if (IsRole1(this.Request.MessageBody.RoleIndex))
            {
                // update current transaction
                if (this.currentTransaction.isFounder)
                {
                    this.currentTransaction.monitorTxId = this.Request.MessageBody.Commitment?.txId;
                }
                else
                {
                    this.currentTransaction.commitment
                        .originalData = this.Request.MessageBody.Commitment;
                    this.currentTransaction.revocableDelivery.originalData = this.Request.MessageBody.RevocableDelivery;
                }
            }
            else if ((IsRole2(this.Request.MessageBody.RoleIndex) && !this.currentTransaction.isFounder)
                || (IsRole3(this.Request.MessageBody.RoleIndex) && this.currentTransaction.isFounder))
            {
                this.currentTransaction.breachRemedy = this.Request.MessageBody.BreachRemedy;
                this.currentTransaction.state = EnumTransactionState.confirmed.ToString();

                // update the HLockPair if needed
                if (null != this.currentHLockTransaction)
                {
                    this.UpdateHLockPair(EnumTransactionState.confirmed, this.Request.TxNonce);
                }
            }
            else
            {
                return;
            }

            // update the transaction
            this.GetChannelLevelDbEntry()?.UpdateTransaction(this.Request.TxNonce, this.currentTransaction);
        }

        public override RsmcSignHandler CreateResponseHndl(string errorCode)
        {
            return new RsmcSignHandler(this.onGoingRequest, errorCode);
        }

        public override RsmcHandler CreateRequestHndl(int role)
        {
            return new RsmcHandler(this.Request, role);
            
        }
        #endregion // RsmcHandler_OVERRIDE_VIRUAL_SETS_OF_DIFFERENT_TRANSACTION_HANDLER
    }

    /// <summary>
    /// Class Handler for handling RsmcSign Message
    /// </summary>
    public class RsmcSignHandler : TransactionHandler<RsmcSign, Rsmc, RsmcHandler, RsmcSignHandler>
    {
        private bool isFounder = false;

        private TransactionRsmcContent currentTransaction;
        private TransactionTabelHLockPair currentHLockTransaction = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="receiver"></param>
        /// <param name="channel"></param>
        /// <param name="asset"></param>
        /// <param name="magic"></param>
        /// <param name="nonce"></param>
        /// <param name="payment"></param>
        /// <param name="role"></param>
        public RsmcSignHandler(Rsmc message, string errorCode="Ok") : base(message)
        {
            this.Request.AssetType = message.MessageBody.AssetType;
            this.Request.Error = errorCode;
            // Get the current transaction
            this.currentTransaction = this.GetCurrentTransaction<TransactionRsmcContent>();
            this.currentHLockTransaction = this.GetHLockPair();

            this.isFounder = this.IsRole1(this.Request.MessageBody.RoleIndex);
        }

        public RsmcSignHandler(string message) : base(message)
        {
            // Get the current transaction
            this.currentTransaction = this.GetCurrentTransaction<TransactionRsmcContent>();
            this.currentHLockTransaction = this.GetHLockPair();

            this.isFounder = this.IsRole0(this.Request.MessageBody.RoleIndex);
        }

        public override bool FailStep(string errorCode)
        {
            Log.Error(
                "Failed handling RsmcSign. Channel: {0}, AssetType: {1}, Balance: {2}. PeerBalance: {3}, Payment: {4}, RoleIndex: {5}. Error: {6}",
                this.Request.ChannelName, this.Request.AssetType, this.SelfBalance(), this.PeerBalance(),
                this.Request.MessageBody.Value, this.Request.MessageBody.RoleIndex, this.Request.MessageBody.RoleIndex, errorCode);
            return true;
        }

        public override bool SucceedStep()
        {
            if (base.SucceedStep())
            {
                Log.Info(
                    "Succeed handling RsmcSign. Channel: {0}, AssetType: {1}, Balance: {2}. PeerBalance: {3}, Payment: {4}, RoleIndex: {5}",
                    this.Request.ChannelName, this.Request.AssetType, this.SelfBalance(), this.PeerBalance(),
                    this.Request.MessageBody.Value, this.Request.MessageBody.RoleIndex, this.Request.MessageBody.RoleIndex);
                return false;
            }

            return false;
        }

        public override bool MakeTransaction()
        {
            if (base.MakeTransaction())
            {
                Log.Info(
                    "Send RsmcSign. Channel: {0}, AssetType: {1}, Balance: {2}. PeerBalance: {3}, Payment: {4}, RoleIndex: {5}",
                    this.Request.ChannelName, this.Request.AssetType, this.SelfBalance(), this.PeerBalance(),
                    this.Request.MessageBody.Value, this.Request.MessageBody.RoleIndex, this.Request.MessageBody.RoleIndex);

                // 
                return true;
            }

            return false;
        }

        public override void CalculateBalance()
        {
            this.CalculateBalance(this.Request.MessageBody.Value, this.isFounder);
        }

        public override bool MakeupMessage()
        {
            // Signature the commitment transaction
            this.Request.MessageBody.Commitment = this.MakeupSignature(this.onGoingRequest.MessageBody.Commitment);

            // Signature the Revocable Delivery Transaction body
            this.Request.MessageBody.RevocableDelivery = this.MakeupSignature(this.onGoingRequest.MessageBody.RevocableDelivery);

            // Add txid for monitoring
            this.AddTransactionSummary();

            return true;
        }

        public override bool Verify()
        {
            this.VerifyUri();
            this.VerifyRoleIndex();
            this.VerifyAssetType(this.Request.MessageBody.AssetType);
            this.VerifyNonce(this.CurrentNonce(this.Request.ChannelName));

            if (this.IsRole0(this.Request.MessageBody.RoleIndex) || this.IsRole1(this.Request.MessageBody.RoleIndex))
            {
                this.VerifySignarture(this.currentTransaction.commitment.originalData.txData,
                    this.Request.MessageBody.Commitment.txDataSign);

                this.VerifySignarture(this.currentTransaction.revocableDelivery.originalData.txData,
                    this.Request.MessageBody.RevocableDelivery.txDataSign);
            }

            return true;
        }

        #region RsmcSign_OVERRIDE_VIRUAL_SETS_OF_DIFFERENT_TRANSACTION_HANDLER
        public override void InitializeBlockChainApi() { this.GetBlockChainAdaptorApi(false); }

        public override void InitializeMessageBody(int role = 0)
        {
            this.Request.MessageBody = new RsmcSignBody
            {
                AssetType = this.onGoingRequest.MessageBody.AssetType,
                Value = this.onGoingRequest.MessageBody.Value,
                RoleIndex = this.onGoingRequest.MessageBody.RoleIndex,
                HashR = this.onGoingRequest.MessageBody.HashR ?? this.onGoingRequest.MessageBody.Comments,
                Comments = this.onGoingRequest.MessageBody.Comments,
            };
        }

        public override void SetLocalsFromBody()
        {
            // RoleIndex Related
            this.RoleMax = 1;
            this.currentRole = this.Request.MessageBody.RoleIndex; // record current role Index
            this.HashR = this.Request.MessageBody.HashR ?? this.Request.MessageBody.Comments;

            // Asset type from message body for adaptor old version trinity
            this.Request.AssetType = this.onGoingRequest.MessageBody.AssetType;
        }

        public override void UpdateTransaction()
        {
            if ((this.IsRole0(this.Request.MessageBody.RoleIndex) && this.currentTransaction.isFounder) ||
                (this.IsRole1(this.Request.MessageBody.RoleIndex) && !this.currentTransaction.isFounder))
            {
                this.currentTransaction.commitment.txDataSign = this.Request.MessageBody.Commitment.txDataSign;
                this.currentTransaction.revocableDelivery.txDataSign = this.Request.MessageBody.RevocableDelivery.txDataSign;
                this.currentTransaction.state = EnumTransactionState.confirming.ToString();

                // update the HLockPair if needed
                if (null != this.currentHLockTransaction)
                {
                    this.UpdateHLockPair(EnumTransactionState.confirming, this.Request.TxNonce);
                }

                this.GetChannelLevelDbEntry().UpdateTransaction(this.Request.TxNonce, this.currentTransaction);
            }
        }

        public override void UpdateChannelSummary()
        {
            this.RecordChannelSummary();
        }

        public override void AddTransactionSummary()
        {
            if (this.IsRole0(this.Request.MessageBody.RoleIndex) || this.IsRole1(this.Request.MessageBody.RoleIndex))
            {
                // Add transaction summary for monitoring
                this.AddTransactionSummary(this.Request.TxNonce, this.Request.MessageBody.Commitment.originalData.txId,
                    this.Request.ChannelName, EnumTransactionType.COMMITMENT);

                this.AddTransactionSummary(this.Request.TxNonce, this.Request.MessageBody.RevocableDelivery.originalData.txId,
                    this.Request.ChannelName, EnumTransactionType.REVOCABLE);
            }
        }
        #endregion
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////
    /// RsmcFailHandler start
    ///////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Class Handler for handling RsmcFail Message
    /// </summary>
    public class RsmcFailHandler : TransactionHandler<RsmcFail, VoidTransactionMessage, VoidHandler, VoidHandler>
    {
        public RsmcFailHandler(string message) : base(message)
        {
        }

        public override bool Handle()
        {
            Log.Debug("Handle Message {0}. Failed to make RSMC with channel {1}.",
                this.Request.MessageType,
                this.Request.ChannelName);

            return true;
        }

        #region RsmcFail_OVERRIDE_VIRUAL_SETS_OF_DIFFERENT_TRANSACTION_HANDLER
        /// Not need Initialize the level db interface for this handler
        public override void InitializeLevelDBApi() { }
        #endregion
    }
}
