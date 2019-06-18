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
using System.Collections.Generic;

using Neo;
using Neo.IO.Json;
using Neo.Wallets;

using Trinity.ChannelSet.Definitions;
using Trinity.TrinityDB.Definitions;
using Trinity.BlockChain;
using Trinity.Wallets.Templates.Definitions;
using Trinity.Wallets.Templates.Messages;
using Trinity.Network.TCP;

namespace Trinity.Wallets.TransferHandler.TransactionHandler
{
    /// <summary>
    /// Class Handler for handling Founder Message
    /// </summary>
    public class FounderHandler : TransactionHandler<Founder, Founder, FounderHandler, FounderSignHandler>
    {
        // Transaction body for Founder message
        private FundingTx fundingTx = null;
        private CommitmentTx commTx = null;
        private RevocableDeliveryTx rdTx = null;
        private TransactionFundingContent currentTransaction = null;

        public FounderHandler(string sender, string receiver, string channel, string asset,
            string magic, long deposit) : base(sender, receiver, channel, asset, magic, fundingNonce, deposit)
        {
            this.Request.TxNonce = fundingNonce; // To avoid rewrite outside.
        }

        public FounderHandler(string message) : base(message)
        {
            this.currentTransaction = this.GetCurrentTransaction<TransactionFundingContent>();
        }

        public FounderHandler(Founder request, int role = 0) : base(request, role)
        {
            this.currentTransaction = this.GetCurrentTransaction<TransactionFundingContent>();
        }

        public override bool Handle()
        {
            Log.Info("Handle Founder message. Channel: {0}, AssetType: {1}, Deposit: {2}, txId(funding): {3}.",
                this.Request.ChannelName, this.Request.MessageBody.AssetType, this.Request.MessageBody.Deposit,
                this.Request.MessageBody.Founder.txId);
            return base.Handle();
        }

        public override bool MakeTransaction()
        {
            if (base.MakeTransaction())
            {
                Log.Info("Succed to send Founder message. Channel: {0}, AssetType: {1}, Deposit: {2}, RoleIndex: {3}.",
                    this.Request.ChannelName, this.Request.MessageBody.AssetType,
                    this.Request.MessageBody.Deposit, this.Request.MessageBody.RoleIndex);
                return true;
            }

            return false;
        }

        private bool SignFundingTx()
        {
            // Sign C1A / C1B by role index
            if (this.IsRole0(this.Request.MessageBody.RoleIndex))
            {
                // Because this is triggered by the RegisterChannel, the founder of this channel is value of Receiver;
                this.neoTransaction.CreateFundingTx(out this.fundingTx);
                return true;
            }
            else if (this.IsRole1(this.Request.MessageBody.RoleIndex))
            {
                // Read from the database
                this.fundingTx = new FundingTx
                {
                    txData = this.currentTransaction.founder.originalData.txData,
                    txId = this.currentTransaction.founder.originalData.txId,
                    addressFunding = this.currentTransaction.founder.originalData.addressFunding,
                    scriptFunding = this.currentTransaction.founder.originalData.scriptFunding,
                    witness = this.currentTransaction.founder.originalData.witness
                };

                // set the neo transaction handler attribute
                this.neoTransaction.SetAddressFunding(this.fundingTx.addressFunding);
                this.neoTransaction.SetScripFunding(this.fundingTx.scriptFunding);
                return true;
            }
            else
            {
                // TODO: error LOG
            }

            return false;
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
            if (!this.SignFundingTx())
            {
                return false;
            }

            // Create Commitment transaction
            this.neoTransaction.CreateCTX(out this.commTx);

            // create Revocable commitment transaction
            this.neoTransaction.createRDTX(out this.rdTx, this.commTx.txId);

            this.Request.MessageBody.Founder = this.fundingTx;
            this.Request.MessageBody.Commitment = this.commTx;
            this.Request.MessageBody.RevocableDelivery = this.rdTx;

            // record the item to database
            if (IsRole0(this.Request.MessageBody.RoleIndex))
            {
                this.AddTransaction(true);
                return true;
            }
            else if (IsRole1(this.Request.MessageBody.RoleIndex))
            {
                this.UpdateTransaction();
                return true;
            }

            return false;
        }

        public override void AddTransaction(bool isFounder = false)
        {
            // Just add the transaction when role index is zero
            if (IsRole0(this.currentRole))
            {
                TransactionFundingContent txContent = new TransactionFundingContent
                {
                    nonce = this.Request.TxNonce,
                    balance = this.Request.MessageBody.Deposit,
                    peerBalance = this.Request.MessageBody.Deposit,
                    role = this.currentRole,
                    isFounder = isFounder,
                    state = EnumTransactionState.initial.ToString(),

                    // transaction body
                    founder = new TxContentsSignGeneric<FundingTx>(),
                    commitment = new TxContentsSignGeneric<CommitmentTx>(),
                    revocableDelivery = new TxContentsSignGeneric<RevocableDeliveryTx>()
                };

                // Add related information according to the value of isFounder
                if (isFounder)
                {
                    // add commitment and revocable delivery transaction info.
                    txContent.commitment.originalData = this.Request.MessageBody.Commitment;
                    txContent.revocableDelivery.originalData = this.Request.MessageBody.RevocableDelivery;
                }
                else
                {
                    // add monitor tx id
                    txContent.monitorTxId = this.Request.MessageBody.Commitment?.txId;
                }

                // record the transaction to levelDB
                this.GetChannelLevelDbEntry()?.AddTransaction(this.Request.TxNonce, txContent);
            }
        }

        public override void UpdateTransaction()
        {
            // update the transaction just when RoleIndex equal to 1
            if (IsRole1(this.Request.MessageBody.RoleIndex))
            {
                // update current transaction
                if (this.currentTransaction.isFounder)
                {
                    this.currentTransaction.monitorTxId = this.Request.MessageBody.Commitment?.txId;
                }
                else
                {
                    this.currentTransaction.commitment.originalData = this.Request.MessageBody.Commitment;
                    this.currentTransaction.revocableDelivery.originalData = this.Request.MessageBody.RevocableDelivery;
                }

                // update the transaction
                this.GetChannelLevelDbEntry()?.UpdateTransaction(this.Request.TxNonce, this.currentTransaction);
            }
        }

        #region Founder_Override_VIRUAL_SETS_OF_DIFFERENT_TRANSACTION_HANDLER
        public override void InitializeMessageBody(string asset, long payment, int role = 0, string hashcode = null, string rcode = null)
        {
            this.Request.MessageBody = new FounderBody
            {
                AssetType = asset,
                Deposit = payment,
                RoleIndex = role,
            };
        }
        public override void InitializeMessageBody(int role = 0)
        {
            this.Request.MessageBody = new FounderBody
            {
                AssetType = this.onGoingRequest.MessageBody.AssetType,
                Deposit = this.onGoingRequest.MessageBody.Deposit,
                RoleIndex = role,
            };
        }

        public override void SetLocalsFromBody()
        {
            // RoleIndex Related
            this.RoleMax = 1;
            this.currentRole = this.Request.MessageBody.RoleIndex; // record current role Index

            // Asset type from message body for adaptor old version trinity
            this.AssetType = this.Request.MessageBody.AssetType;
        }

        public override FounderHandler CreateRequestHndl(int role)
        {
            return new FounderHandler(this.onGoingRequest, role);
        }

        public override FounderSignHandler CreateResponseHndl(string errorCode = "Ok")
        {
            return new FounderSignHandler(this.Request, errorCode);
        }
        #endregion //Founder_Override_VIRUAL_SETS_OF_DIFFERENT_TRANSACTION_HANDLER
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////
    /// FounderSignHandler start
    ///////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Class Handler for handling FounderSign Message
    /// </summary>
    public class FounderSignHandler : TransactionHandler<FounderSign, Founder, FounderHandler, FounderSignHandler>
    {
        private TransactionFundingContent currentTransaction = null;
        /// <summary>
        /// Handle received FounderSign message
        /// </summary>
        /// <param name="message"></param>
        public FounderSignHandler(string message) : base(message)
        {
            this.currentTransaction =
                this.GetChannelLevelDbEntry().TryGetTransaction<TransactionFundingContent>(this.Request.TxNonce);
        }

        /// <summary>
        /// Create new FounderSign message after received Founder.
        /// </summary>
        /// <param name="message">Received Founder message</param>
        /// <param name="errorCode"></param>
        public FounderSignHandler(Founder message, string errorCode = "Ok") : base(message)
        {
            this.Request.AssetType = message.MessageBody.AssetType;
            this.Request.Error = errorCode;

            // Get current transaction
            this.currentTransaction =
                this.GetChannelLevelDbEntry().TryGetTransaction<TransactionFundingContent>(this.Request.TxNonce);
        }

        public override bool Handle()
        {
            Log.Info("Handle FounderSign message. Channel: {0}, AssetType: {1}, Deposit: {2}.",
                this.Request.ChannelName, this.Request.MessageBody.AssetType, this.Request.MessageBody.Deposit);
            return base.Handle();
        }

        public override bool MakeTransaction()
        {
            if (base.MakeTransaction())
            {
                Log.Info("Succeed to send FounderSign message. Channel: {0}, AssetType: {1}, Deposit: {2}, RoleIndex: {3}.",
                    this.Request.ChannelName, this.Request.MessageBody.AssetType,
                    this.Request.MessageBody.Deposit, this.Request.MessageBody.RoleIndex);

                this.ExtraSucceedAction();
                return true;
            }

            return false;
        }

        public override bool SucceedStep()
        {
            // update the transaction history
            this.UpdateTransaction();

            // broadcast this transaction
            if (IsRole1(this.Request.MessageBody.RoleIndex))
            {
                this.BroadcastTransaction();

                this.ExtraSucceedAction();
            }
            return true;
        }

        public override bool MakeupMessage()
        {
            this.Request.MessageBody.Founder = this.MakeupSignature(this.onGoingRequest.MessageBody.Founder);
            this.Request.MessageBody.Commitment = this.MakeupSignature(this.onGoingRequest.MessageBody.Commitment);
            this.Request.MessageBody.RevocableDelivery = this.MakeupSignature(this.onGoingRequest.MessageBody.RevocableDelivery);

            // Add txid for monitor
            this.AddTransactionSummary();
            return true;
        }

        private void BroadcastTransaction()
        {
            JObject ret = this.BroadcastTransaction(
                this.Request.MessageBody.Founder.originalData.txData,
                this.Request.MessageBody.Founder.txDataSign,
                this.Request.MessageBody.Founder.originalData.witness);
            Log.Debug("Broadcast Founder transaction result: {0}. txId: {1}", 
                ret, this.Request.MessageBody.Founder.originalData.txId);
        }
        
        public override void UpdateTransaction()
        {
            // start update the transaction
            this.currentTransaction.founder.txDataSign = this.Request.MessageBody.Founder.txDataSign;
            this.currentTransaction.commitment.txDataSign = this.Request.MessageBody.Commitment.txDataSign;
            this.currentTransaction.revocableDelivery.txDataSign = this.Request.MessageBody.RevocableDelivery.txDataSign;

            this.GetChannelLevelDbEntry().UpdateTransaction(this.Request.TxNonce, this.currentTransaction);
        }

        public override bool Verify()
        {
            return this.VerifyRoleIndex();
        }

        #region FounderSign_Override_VIRUAL_SETS_OF_DIFFERENT_TRANSACTION_HANDLER
        public override void InitializeMessageBody(int role = 0)
        {
            this.Request.MessageBody = new FounderSignBody
            {
                AssetType = this.onGoingRequest.MessageBody.AssetType,
                Deposit = this.onGoingRequest.MessageBody.Deposit,
                RoleIndex = role,
            };
        }

        public override void SetLocalsFromBody()
        {
            // RoleIndex Related
            this.RoleMax = 1;
            this.currentRole = this.Request.MessageBody.RoleIndex; // record current role Index

            // Asset type from message body for adaptor old version trinity
            this.AssetType = this.Request.MessageBody.AssetType;
        }

        public override void ExtraSucceedAction()
        {
            if (IsRole1(this.currentRole))
            {
                // Update the channel to opening state
                this.UpdateChannelState(EnumChannelState.OPENING);

                // Add channel summary information
                this.UpdateChannelSummary(this.Request.ChannelName, this.Request.TxNonce, this.GetPeerUri());
            }
        }

        public override void AddTransactionSummary()
        {
            // Add txid for monitor
            this.AddTransactionSummary(this.Request.TxNonce, this.Request.MessageBody.Founder.originalData.txId,
                this.Request.ChannelName, EnumTransactionType.FUNDING);

            this.AddTransactionSummary(this.Request.TxNonce, this.Request.MessageBody.Commitment.originalData.txId,
                this.Request.ChannelName, EnumTransactionType.COMMITMENT);

            this.AddTransactionSummary(this.Request.TxNonce, this.Request.MessageBody.RevocableDelivery.originalData.txId,
                this.Request.ChannelName, EnumTransactionType.REVOCABLE);
        }
        #endregion //FounderSign_Override_VIRUAL_SETS_OF_DIFFERENT_TRANSACTION_HANDLER
    }


    ///////////////////////////////////////////////////////////////////////////////////////////////
    /// FounderFailHandler start
    ///////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Class Handler for handling FounderFail Message
    /// </summary>
    public class FounderFailHandler : TransactionHandler<FounderFail, VoidTransactionMessage, VoidHandler, VoidHandler>
    {
        public FounderFailHandler(string message) : base(message) { }

        public override bool Handle()
        {
            Log.Info("Handle FounderFail message. Failed to create channel {0}. AssetType: {1}, Deposit {2}",
                this.Request.ChannelName, this.Request.MessageBody.AssetType, this.Request.MessageBody.Deposit);

            return true;
        }

        #region FounderFail_OVERRIDE_VIRUAL_SETS_OF_DIFFERENT_TRANSACTION_HANDLER
        /// Not need Initialize the level db interface for this handler
        public override void InitializeLevelDBApi() { }
        #endregion
    }
}
