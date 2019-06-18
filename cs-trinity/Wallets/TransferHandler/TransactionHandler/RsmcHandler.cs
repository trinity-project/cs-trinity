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
using Trinity.Exceptions.WalletError;

namespace Trinity.Wallets.TransferHandler.TransactionHandler
{
    /// <summary>
    /// Class Handler for handling Rsmc Message
    /// </summary>
    public class RsmcHandler : TransactionHandler<Rsmc, Rsmc, RsmcHandler, RsmcSignHandler>
    {
        //
        private readonly long balance = 0;
        private readonly long peerBalance = 0;

        // 
        private bool isRsmcValid = false;
        private bool isHtlc2Rsmc = false;

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
            string magic, UInt64 nonce, long payment)
            : base(sender, receiver, channel, asset, magic, nonce, payment)
        {
            this.Request.TxNonce = this.NextNonce(channel);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"> Previous Rsmc message </param>
        /// <param name="role"></param>
        public RsmcHandler(Rsmc message, int role) : base(message, role)
        {
            this.Request.AssetType = message.MessageBody.AssetType;
        }

        public RsmcHandler(string message) : base(message)
        {
        }

        public override bool Handle()
        {
            Log.Debug("Handle Message {0}. Channel name {1}, Asset Type: {2}, Payment: {3}. Balance: {4}. PeerBalance: {5}",
                this.Request.MessageType,
                this.Request.ChannelName,
                this.Request.MessageBody.AssetType,
                this.Request.MessageBody.Value,
                this.balance, this.peerBalance);

            if (!base.Handle())
            {
                return false;
            }

            return true;
        }

        public override bool FailStep(string errorCode)
        {
            Log.Error("Failed to handle Rsmc Transaction");
            return base.FailStep(errorCode);
        }

        public override bool MakeTransaction()
        {
            bool ret = base.MakeTransaction();
            Log.Debug("{0} to send {1}. Channel name {2}, Asset Type: {3}, Payment: {4}.",
                ret ? "Succeed" : "Fail",
                this.Request.MessageType,
                this.Request.ChannelName,
                this.Request.MessageBody.AssetType,
                this.Request.MessageBody.Value);
            return ret;
        }


        public override bool Verify()
        {
            return this.VerifyRoleIndex();
        }

        public override bool MakeupMessage()
        {
            return this.MakeupTransactionBody();
        }

        public bool MakeupTransactionBody()
        {
            if (!this.isRsmcValid)
            {
                Log.Error("Error to makeup the RSMC transaction. Channel name {2}, Asset Type: {3}, Payment: {4}, Balance: {4}. PeerBalance: {5}",
                    this.Request.ChannelName,
                    this.Request.MessageBody.AssetType,
                    this.Request.MessageBody.Value, this.balance, this.peerBalance);
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

                this.AddOrUpdateTransaction();

                if (this.IsRole1(this.Request.MessageBody.RoleIndex))
                {
                    // Add channel summary information
                    this.UpdateChannelSummary(this.Request.ChannelName, this.Request.TxNonce, this.Request.Receiver);
                }
            }
            else if (IsRole2(this.Request.MessageBody.RoleIndex) || IsRole3(this.Request.MessageBody.RoleIndex))
            {
                TransactionRsmcContent txContent = this.GetChannelLevelDbEntry().TryGetTransaction<TransactionRsmcContent>(this.Request.TxNonce-1);
                if (null == txContent)
                {
                    Log.Error("Why no RSMC content is found with nonce: {0}", this.Request.TxNonce-1);
                    return false;
                }

                this.neoTransaction?.SetAddressRSMC(txContent.commitment.originalData.addressRSMC);
                this.neoTransaction?.SetScripRSMC(txContent.commitment.originalData.scriptRSMC);
                this.neoTransaction.CreateBRTX(out this.brTx, txContent.commitment.originalData.txId);
                this.Request.MessageBody.BreachRemedy = this.MakeupSignature(this.brTx);
            }

            return true;
        }

        public override void AddOrUpdateTransactionSummary(bool isFounder = false)
        {
            TransactionRsmcContent txContent = this.GetChannelLevelDbEntry().TryGetTransaction<TransactionRsmcContent>(this.Request.TxNonce);
            bool isAdd = false;

            if (null == txContent)
            {
                isAdd = true;
                txContent = new TransactionRsmcContent
                {
                    nonce = this.Request.TxNonce,
                    commitment = new TxContentsSignGeneric<CommitmentTx>(),
                    revocableDelivery = new TxContentsSignGeneric<RevocableDeliveryTx>(),
                    breachRemedy = new TxContentsSignGeneric<BreachRemedyTx>(),
                    state = EnumTransactionState.initial.ToString()
                };
            }
            
            // add peer information from the message
            if (isFounder)
            {
                // Add commitment Tx id for monitoring
                if (this.IsRole0(this.Request.MessageBody.RoleIndex) || this.IsRole1(this.Request.MessageBody.RoleIndex))
                {
                    txContent.monitorTxId = this.Request.MessageBody.Commitment.txId;
                    txContent.state = EnumTransactionState.confirming.ToString();
                }
                else
                {
                    txContent.breachRemedy = this.Request.MessageBody.BreachRemedy;
                    txContent.state = EnumTransactionState.confirmed.ToString();

                    // update the channel balance
                    long[] balanceOfPeers = this.CalculateBalance(this.Request.MessageBody.RoleIndex,
                        this.currentChannel.balance, this.currentChannel.peerBalance, this.Request.MessageBody.Value, true, this.isHtlc2Rsmc);
                    this.currentChannel.balance = balanceOfPeers[0];
                    this.currentChannel.peerBalance = balanceOfPeers[1];
                    this.GetChannelLevelDbEntry().UpdateChannel(this.Request.ChannelName, currentChannel);
                }
            }
            else
            {
                if (this.IsRole0(this.Request.MessageBody.RoleIndex) || this.IsRole1(this.Request.MessageBody.RoleIndex))
                {
                    long[] balanceOfPeers = this.CalculateBalance(this.Request.MessageBody.RoleIndex,
                            this.currentChannel.balance, this.currentChannel.peerBalance, this.Request.MessageBody.Value, false, this.isHtlc2Rsmc);

                    txContent.balance = balanceOfPeers[0];
                    txContent.peerBalance = balanceOfPeers[1];
                    txContent.commitment.originalData = this.Request.MessageBody.Commitment;
                    txContent.revocableDelivery.originalData = this.Request.MessageBody.RevocableDelivery;
                }
            }

            // Add transaction if no item exsited in leveldb
            if (isAdd)
            {
                this.GetChannelLevelDbEntry().AddTransaction(this.Request.TxNonce, txContent);
            }
            else
            {
                this.GetChannelLevelDbEntry().UpdateTransaction(this.Request.TxNonce, txContent);
            }
        }

        private void AddTransactionSummaryForRsmc()
        {
            if (this.IsRole0(this.Request.MessageBody.RoleIndex) || this.IsRole1(this.Request.MessageBody.RoleIndex))
            {
                // Add transaction summary for monitoring
                this.AddTransactionSummary(this.Request.TxNonce, this.Request.MessageBody.Commitment.txId,
                    this.Request.ChannelName, EnumTransactionType.COMMITMENT);

                this.AddTransactionSummary(this.Request.TxNonce, this.Request.MessageBody.RevocableDelivery.txId,
                    this.Request.ChannelName, EnumTransactionType.REVOCABLE);
            }
            else
            {
                // Add BreachRemedy TxId for setting the current channel closed status.
                this.AddTransactionSummary(this.Request.TxNonce, this.Request.MessageBody.BreachRemedy.originalData.txId,
                    this.Request.ChannelName, EnumTransactionType.BREACHREMEDY);
            }
        }

        #region RsmcHandler_OVERRIDE_VIRUAL_SETS_OF_DIFFERENT_TRANSACTION_HANDLER
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
                HashR = this.onGoingRequest.MessageBody.Comments
            };
        }

        public override void SetLocalsFromBody()
        {
            // RoleIndex Related
            this.RoleMax = 3;
            this.currentRole = this.Request.MessageBody.RoleIndex; // record current role Index

            // Asset type from message body for adaptor old version trinity
            this.AssetType = this.Request.MessageBody.AssetType;
        }

        public override void SetTransactionValid()
        {
            this.isRsmcValid = true;
        }

        public override long[] CalculateBalance(long balance, long peerBalance)
        {
            return this.CalculateBalance(this.Request.MessageBody.RoleIndex, balance, peerBalance, this.Request.MessageBody.Value, false, this.isHtlc2Rsmc);
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
        private bool isHtlc2Rsmc = false;

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
        }

        public RsmcSignHandler(string message) : base(message)
        {
        }

        public override bool Handle()
        {
            Log.Debug("Handle Message {0}. Channel name {1}, Asset Type: {2}, Value: {3}.",
                this.Request.MessageType,
                this.Request.ChannelName,
                this.Request.MessageBody.AssetType,
                this.Request.MessageBody.Value);
            return base.Handle();
        }

        public override bool FailStep(string errorCode)
        {
            Log.Error("Fail to handle RsmcSign. Nonce: {0}, role: {1}. ErrorCode: {2}",
                this.Request.TxNonce, this.Request.MessageBody.RoleIndex, errorCode);
            return true;
        }

        public override bool SucceedStep()
        {
            if (this.IsRole0(this.Request.MessageBody.RoleIndex) || this.IsRole1(this.Request.MessageBody.RoleIndex))
            {
                // TODO: Verify the message body in future.

                // update the transaction info
                this.UpdateTransactionForRsmc();
                
                if (this.IsRole1(this.Request.MessageBody.RoleIndex))
                {
                    // Add channel summary information
                    this.UpdateChannelSummary(this.Request.ChannelName, this.Request.TxNonce, this.Request.Sender);
                }
            }
            return base.SucceedStep();
        }

        public override long[] CalculateBalance(long balance, long peerBalance)
        {
            return this.CalculateBalance(this.Request.MessageBody.RoleIndex, balance, peerBalance, this.Request.MessageBody.Value, true, this.isHtlc2Rsmc);
        }

        public override bool MakeupMessage()
        {
            // Signature the commitment transaction
            this.Request.MessageBody.Commitment = this.MakeupSignature(this.onGoingRequest.MessageBody.Commitment);

            // Signature the Revocable Delivery Transaction body
            this.Request.MessageBody.RevocableDelivery = this.MakeupSignature(this.onGoingRequest.MessageBody.RevocableDelivery);

            return true;
        }

        private bool UpdateTransactionForRsmc()
        {
            TransactionRsmcContent txContent = this.GetChannelLevelDbEntry().TryGetTransaction<TransactionRsmcContent>(this.Request.TxNonce);
            
            if (null == txContent)
            {
                Log.Error("Fail to update RSMC transaction with nonce: {0}", this.Request.TxNonce);
                return false;
            }

            txContent.commitment.txDataSign = this.Request.MessageBody.Commitment.txDataSign;
            txContent.revocableDelivery.txDataSign = this.Request.MessageBody.RevocableDelivery.txDataSign;

            this.GetChannelLevelDbEntry().UpdateTransaction(this.Request.TxNonce, txContent);

            return true;
        }

        #region RsmcHandler_OVERRIDE_VIRUAL_SETS_OF_DIFFERENT_TRANSACTION_HANDLER
        public override void InitializeMessageBody(int role = 0)
        {
            this.Request.MessageBody = new RsmcSignBody
            {
                AssetType = this.onGoingRequest.MessageBody.AssetType,
                Value = this.onGoingRequest.MessageBody.Value,
                RoleIndex = this.onGoingRequest.MessageBody.RoleIndex,
            };
        }

        public override void SetLocalsFromBody()
        {
            // RoleIndex Related
            this.RoleMax = 1;
            this.currentRole = this.Request.MessageBody.RoleIndex; // record current role Index

            // Asset type from message body for adaptor old version trinity
            this.AssetType = this.onGoingRequest.MessageBody.AssetType;
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
