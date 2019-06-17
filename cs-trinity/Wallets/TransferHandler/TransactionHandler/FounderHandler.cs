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
        private const UInt64 fundingNonce = 0;
        // Transaction body for Founder message
        private FundingTx fundingTx;
        private CommitmentTx commTx;
        private RevocableDeliveryTx rdTx;

        public FounderHandler(string sender, string receiver, string channel, string asset,
            string magic, long deposit) : base(sender, receiver, channel, asset, magic, fundingNonce, deposit)
        {
            this.Request.TxNonce = fundingNonce; // To avoid rewrite outside.
        }

        public FounderHandler(string message) : base(message) { }

        public FounderHandler(Founder request, int role = 0) : base(request, role) { }

        public override bool Handle()
        {
            if (!base.Handle())
            {
                return false;
            }

            // Add txid for monitor
            this.AddTransactionSummary(this.Request.TxNonce, this.Request.MessageBody.Founder.txId,
                this.Request.ChannelName, EnumTransactionType.FUNDING);

            this.AddTransactionSummary(this.Request.TxNonce, this.Request.MessageBody.Commitment.txId,
                this.Request.ChannelName, EnumTransactionType.COMMITMENT);

            this.AddTransactionSummary(this.Request.TxNonce, this.Request.MessageBody.RevocableDelivery.txId,
                this.Request.ChannelName, EnumTransactionType.REVOCABLE);

            return true;
        }

        public override bool SucceedStep()
        {
            return base.SucceedStep();
        }

        public override bool MakeTransaction()
        {
            bool ret = base.MakeTransaction();
            Log.Info("{0} to send {1}. Channel name {2}, Asset Type: {3}, Deposit: {4}.",
                ret ? "Succeed" : "Fail",
                this.Request.MessageType,
                this.Request.ChannelName,
                this.Request.MessageBody.AssetType,
                this.Request.MessageBody.Deposit);
            return ret;
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
                // TODO: Read from the database
                TransactionFundingContent transactionContent = this.GetChannelLevelDbEntry().GetTransaction<TransactionFundingContent>(this.Request.TxNonce);
                if (null != transactionContent)
                {
                    this.fundingTx = new FundingTx
                    {
                        txData = transactionContent.founder.originalData.txData,
                        txId = transactionContent.founder.originalData.txId,
                        addressFunding = transactionContent.founder.originalData.addressFunding,
                        scriptFunding = transactionContent.founder.originalData.scriptFunding,
                        witness = transactionContent.founder.originalData.witness
                    };

                    // set the neo transaction handler attribute
                    this.neoTransaction.SetAddressFunding(this.fundingTx.addressFunding);
                    this.neoTransaction.SetScripFunding(this.fundingTx.scriptFunding);
                    return true;
                }
                else
                {
                    this.fundingTx = null;
                    Log.Error("Founder: Should never go here. Channel: {0}, RoleIndex: {1}.",
                        this.Request.ChannelName, this.Request.MessageBody.RoleIndex);
                }
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
            if (IsRole0(this.Request.MessageBody.RoleIndex) || IsRole1(this.Request.MessageBody.RoleIndex))
            {
                this.AddOrUpdateTransaction();
                return true;
            }

            return false;
        }

        public override void AddOrUpdateTransaction(bool isPeer = false)
        {
            TransactionFundingContent txContent = null;
            bool isNeedAdd = false;

            // Get the transaction firstly
            try
            {
                txContent = this.GetChannelLevelDbEntry().TryGetTransaction<TransactionFundingContent>(this.Request.TxNonce);
            }
            catch(TrinityLevelDBException)
            {
                txContent = new TransactionFundingContent
                {
                    nonce = this.Request.TxNonce,
                    founder = new TxContentsSignGeneric<FundingTx>(),
                    commitment = new TxContentsSignGeneric<CommitmentTx>(),
                    revocableDelivery = new TxContentsSignGeneric<RevocableDeliveryTx>(),

                    state = EnumTransactionState.initial.ToString()
                };

                // need add the transaction item to leveldb
                isNeedAdd = true;
            }

            // Same founder info exists in both sides
            txContent.founder.originalData = this.Request.MessageBody.Founder;

            // add peer information from the message
            if (isPeer)
            {
                // add monitor tx id
                txContent.monitorTxId = this.Request.MessageBody.Commitment.txId;
            }
            else
            {
                // add commitment and revocable delivery transaction info.
                txContent.commitment.originalData = this.Request.MessageBody.Commitment;
                txContent.revocableDelivery.originalData = this.Request.MessageBody.RevocableDelivery;
            }

            if (isNeedAdd)
            {
                this.GetChannelLevelDbEntry().AddTransaction(this.Request.TxNonce, txContent);
            }
            else
            {
                this.GetChannelLevelDbEntry().UpdateTransaction(this.Request.TxNonce, txContent);
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

        public override NeoTransaction GetBlockChainAdaptorApi()
        {
            this.neoTransaction = new NeoTransaction(
                this.Request.MessageBody.AssetType.ToAssetId(),
                this.GetPubKey(), this.Request.MessageBody.Deposit.ToString(),
                this.GetPeerPubKey(), this.Request.MessageBody.Deposit.ToString());
            return base.GetBlockChainAdaptorApi();
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
        //private FundingSignTx fstContent;
        //private CommitmentSignTx cstContent;
        //private RevocableDeliverySignTx rdstContent;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="receiver"></param>
        /// <param name="channel"></param>
        /// <param name="asset"></param>
        /// <param name="magic"></param>
        /// <param name="nonce"></param>
        /// <param name="deposit"></param>
        /// <param name="role"></param>

        public FounderSignHandler(string message) : base(message)
        {
        }

        public FounderSignHandler(Founder message, string errorCode = "Ok") : base(message)
        {
            this.Request.AssetType = message.MessageBody.AssetType;
            this.Request.Error = errorCode;
        }

        public override bool Handle()
        {
            Log.Debug("Handle Message {0}. Channel name {1}, Asset Type: {2}, Deposit: {3}.",
                this.Request.MessageType,
                this.Request.ChannelName,
                this.Request.MessageBody.AssetType,
                this.Request.MessageBody.Deposit);
            return base.Handle();
        }

        public override bool MakeTransaction()
        {
            bool ret = base.MakeTransaction();
            Log.Debug("{0} to send {1}. Channel name {2}, Asset Type: {3}, Deposit: {4}.",
                ret ? "Succeed" : "Fail",
                this.Request.MessageType,
                this.Request.ChannelName,
                this.Request.MessageBody.AssetType,
                this.Request.MessageBody.Deposit);

            if (ret)
            {
                this.ExtraSucceedAction();
            }
            return ret;
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
            return true;
        }

        private void BroadcastTransaction()
        {
            string peerFundSign = this.Request.MessageBody.Founder.txDataSign;
            string fundSign = this.Sign(this.Request.MessageBody.Founder.originalData.txData);
            string witness = this.Request.MessageBody.Founder.originalData.witness
                .Replace("{signOther}", peerFundSign)
                .Replace("{signSelf}", fundSign);

            JObject ret = NeoInterface.SendRawTransaction(this.Request.MessageBody.Founder.originalData.txData + witness);
            Log.Debug("Broadcast Founder transaction result is {0}. txId: {1}", ret, this.Request.MessageBody.Founder.originalData.txId);
        }

        private void UpdateTransaction()
        {
            TransactionFundingContent content = this.GetChannelLevelDbEntry().TryGetTransaction<TransactionFundingContent>(this.Request.TxNonce);

            if (null == content)
            {
                return;
            }

            // start update the transaction
            content.founder.txDataSign = this.Request.MessageBody.Founder.txDataSign;
            content.commitment.txDataSign = this.Request.MessageBody.Commitment.txDataSign;
            content.revocableDelivery.txDataSign = this.Request.MessageBody.RevocableDelivery.txDataSign;

            this.GetChannelLevelDbEntry().UpdateTransaction(this.Request.TxNonce, content);
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
            if (IsRole1(this.Request.MessageBody.RoleIndex))
            {
                // Update the channel to opening state
                this.UpdateChannelState(this.GetUri(), this.GetPeerUri(),
                    this.Request.ChannelName, EnumChannelState.OPENING);

                // Add channel summary information
                this.UpdateChannelSummaryContent(this.Request.ChannelName, this.Request.TxNonce, this.GetPeerUri());
            }
        }

        public override NeoTransaction GetBlockChainAdaptorApi()
        {
            this.neoTransaction = new NeoTransaction(
                this.Request.MessageBody.AssetType.ToAssetId(),
                this.GetPubKey(), this.Request.MessageBody.Deposit.ToString(),
                this.GetPeerPubKey(), this.Request.MessageBody.Deposit.ToString());
            return base.GetBlockChainAdaptorApi();
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
            Log.Debug("{0}: Failed to create channel {1}. AssetType: {2}, Deposit {3}",
                this.Request.MessageType, this.Request.ChannelName,
                this.Request.MessageBody.AssetType, this.Request.MessageBody.Deposit
                );

            return true;
        }

        #region FounderFail_OVERRIDE_VIRUAL_SETS_OF_DIFFERENT_TRANSACTION_HANDLER
        /// Not need Initialize the level db interface for this handler
        public override void InitializeLevelDBApi() { }
        #endregion
    }
}
