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
using Neo.SmartContract;

namespace Trinity.Wallets.TransferHandler.TransactionHandler
{
    /// <summary>
    /// Class Handler for handling Founder Message
    /// </summary>
    public class FounderHandler : TransactionHandler<Founder, Founder, FounderHandler, FounderSignHandler>
    {
        private bool isFounder = false;

        // Transaction body for Founder message
        private FundingTx fundingTx = null;
        private CommitmentTx commTx = null;
        private RevocableDeliveryTx rdTx = null;
        private TransactionFundingContent currentTransaction = null;
        private List<string> peerVout = null;

        public FounderHandler(string sender, string receiver, string channel, string asset,
            string magic, long deposit, List<string> vout=null) : base(sender, receiver, channel, asset, magic, fundingNonce, deposit)
        {
            this.isFounder = this.IsRole0(this.Request.MessageBody.RoleIndex);

            // set message header or message body
            this.Request.TxNonce = fundingNonce; // To avoid rewrite outside.
            this.peerVout = vout;
        }

        public FounderHandler(Founder request, int role = 0) : base(request, role)
        {
            this.isFounder = this.IsRole0(this.Request.MessageBody.RoleIndex);

            // Get current transaction with nonce 0
            this.currentTransaction = this.GetCurrentTransaction<TransactionFundingContent>();
        }

        public FounderHandler(string message) : base(message)
        {
            this.isFounder = this.IsRole1(this.Request.MessageBody.RoleIndex);

            // Get current transaction with nonce 0
            this.currentTransaction = this.GetCurrentTransaction<TransactionFundingContent>();
        }

        public override bool SucceedStep()
        {
            if(base.SucceedStep())
            {
                Log.Info("Succeed handling Founder. Channel: {0}, AssetType: {1}, Deposit: {2}, , RoleIndex: {3}. txId(funding): {4}.",
                this.Request.ChannelName, this.Request.AssetType, this.Request.MessageBody.Deposit,
                this.Request.MessageBody.RoleIndex, this.Request.MessageBody.Founder.txId);
                return true;
            }

            return false;
        }

        public override bool FailStep(string errorCode)
        {
            Log.Info("Failed handling Founder. Channel: {0}, AssetType: {1}, Deposit: {2}, , RoleIndex: {3}. txId(funding): {4}. Error: {5}",
                this.Request.ChannelName, this.Request.AssetType, this.Request.MessageBody.Deposit,
                this.Request.MessageBody.RoleIndex, this.Request.MessageBody.Founder.txId, errorCode);
            return base.FailStep(errorCode);
        }

        public override bool MakeTransaction()
        {
            if (base.MakeTransaction())
            {
                Log.Info("Succeed to send Founder. Channel: {0}, AssetType: {1}, Deposit: {2}, RoleIndex: {3}.",
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
                this.neoTransaction.CreateFundingTx(out this.fundingTx, this.peerVout);
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
                this.neoTransaction.SetFundingTxId(this.fundingTx.txId);
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
            this.VerifyNonce(fundingNonce, true);
            this.VerifyDeposit(this.Request.MessageBody.Deposit);
            this.VerifyRoleIndex();

            return true;
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
            this.neoTransaction.CreateRDTX(out this.rdTx, this.commTx.txId);

            this.Request.MessageBody.Founder = this.fundingTx;
            this.Request.MessageBody.Commitment = this.commTx;
            this.Request.MessageBody.RevocableDelivery = this.rdTx;

            return true;
        }

        #region Founder_Override_VIRUAL_SETS_OF_DIFFERENT_TRANSACTION_HANDLER
        public override void InitializeAssetType(bool useCurrentRequest = false)
        {
            if (useCurrentRequest)
            {
                this.assetId = this.Request.MessageBody.AssetType.ToAssetId(this.GetAssetMap());
            }
            else
            {
                this.assetId = this.onGoingRequest.MessageBody.AssetType.ToAssetId(this.GetAssetMap());
            }
        }

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
                AssetType = this.assetId,
                Deposit = this.onGoingRequest.MessageBody.Deposit,
                RoleIndex = role,
            };
        }

        public override void SetLocalsFromBody()
        {
            // RoleIndex Related
            this.RoleMax = 1;
            this.currentRole = this.Request.MessageBody.RoleIndex; // record current role Index
        }

        public override void AddTransaction(bool isFounder = false)
        {
            // Just add the transaction when role index is zero
            if (IsRole0(this.currentRole))
            {
                TransactionFundingContent txContent = this.NewTransactionContent<TransactionFundingContent>(isFounder);

                txContent.founder = new TxContentsSignGeneric<FundingTx>();
                txContent.commitment = new TxContentsSignGeneric<CommitmentTx>();
                txContent.revocableDelivery = new TxContentsSignGeneric<RevocableDeliveryTx>();

                txContent.type = EnumTransactionType.FUNDING.ToString();
                txContent.payment = this.Request.MessageBody.Deposit;
                txContent.founder.originalData = this.Request.MessageBody.Founder;

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
                this.AddTransaction(this.Request.TxNonce, txContent, true);
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

                this.currentTransaction.state = EnumTransactionState.confirming.ToString();

                // update the transaction
                this.UpdateTransaction(this.Request.TxNonce, this.currentTransaction, true);
            }
        }

        public override FounderHandler CreateRequestHndl(int role)
        {
            return new FounderHandler(this.Request, role);
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
        private bool isFounder = false;
        private TransactionFundingContent currentTransaction = null;

        /// <summary>
        /// Create new FounderSign message after received Founder.
        /// </summary>
        /// <param name="message">Received Founder message</param>
        /// <param name="errorCode"></param>
        public FounderSignHandler(Founder message, string errorCode = "Ok") : base(message)
        {
            this.isFounder = this.IsRole1(this.Request.MessageBody.RoleIndex);
            this.Request.AssetType = this.assetId;
            this.Request.Error = errorCode;

            // Get current transaction
            this.currentTransaction = this.GetCurrentTransaction<TransactionFundingContent>();
            
        }

        /// <summary>
        /// Handle received FounderSign message
        /// </summary>
        /// <param name="message"></param>
        public FounderSignHandler(string message) : base(message)
        {
            this.isFounder = this.IsRole0(this.Request.MessageBody.RoleIndex);

            // Get current transaction with nonce zero
            this.currentTransaction =
                this.GetChannelLevelDbEntry().TryGetTransaction<TransactionFundingContent>(this.Request.TxNonce);
        }

        public override bool SucceedStep()
        {
            if (!base.SucceedStep())
            {
                return false;
            }

            Log.Info("Succeed handling FounderSign. Channel: {0}, AssetType: {1}, Deposit: {2}, RoleIndex: {3}.",
                this.Request.ChannelName, this.Request.MessageBody.AssetType,
                this.Request.MessageBody.Deposit, this.Request.MessageBody.RoleIndex);

            // Add contract address to the account to monitor it
            NeoInterface.addContractToAccount(this.Request.Sender, this.Request.Receiver);

            // Add self txid to monitor
            this.AddTransactionSummary();

            // broadcast this transaction
            if (IsRole1(this.Request.MessageBody.RoleIndex))
            {
                this.BroadcastTransaction();
                this.ExtraSucceedAction();
            }

            return true;
        }

        public override bool FailStep(string errorCode)
        {
            Log.Info("Failed handling FounderSign. Channel: {0}, AssetType: {1}, Deposit: {2}, RoleIndex: {3}. Error: {4}",
                this.Request.ChannelName, this.Request.MessageBody.AssetType,
                this.Request.MessageBody.Deposit, this.Request.MessageBody.RoleIndex, errorCode);
            return base.FailStep(errorCode);
        }

        public override bool MakeTransaction()
        {
            if (base.MakeTransaction())
            {
                Log.Info("Succeed to send FounderSign. Channel: {0}, AssetType: {1}, Deposit: {2}, RoleIndex: {3}.",
                    this.Request.ChannelName, this.Request.MessageBody.AssetType,
                    this.Request.MessageBody.Deposit, this.Request.MessageBody.RoleIndex);

                this.ExtraSucceedAction();
                return true;
            }

            return false;
        }

        public override bool MakeupMessage()
        {
            this.Request.MessageBody.Founder = this.MakeupSignature(this.onGoingRequest.MessageBody.Founder);
            this.Request.MessageBody.Commitment = this.MakeupSignature(this.onGoingRequest.MessageBody.Commitment);
            this.Request.MessageBody.RevocableDelivery = this.MakeupSignature(this.onGoingRequest.MessageBody.RevocableDelivery);

            // Add txid for monitor
            this.AddTransactionSummary(this.Request.TxNonce, this.Request.MessageBody.Founder.originalData.txId,
                this.Request.ChannelName, EnumTransactionType.FUNDING);
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

        public override bool Verify()
        {
            this.VerifyNonce(fundingNonce, true);
            this.VerifyDeposit(this.Request.MessageBody.Deposit);
            this.VerifyRoleIndex();

            this.VerifySignarture(this.currentTransaction.founder.originalData.txData, 
                this.Request.MessageBody.Founder.txDataSign);
            this.VerifySignarture(this.currentTransaction.commitment.originalData.txData, 
                this.Request.MessageBody.Commitment.txDataSign);
            this.VerifySignarture(this.currentTransaction.revocableDelivery.originalData.txData,
                this.Request.MessageBody.RevocableDelivery.txDataSign);

            return true;
        }

        #region FounderSign_Override_VIRUAL_SETS_OF_DIFFERENT_TRANSACTION_HANDLER
        public override void InitializeAssetType(bool useCurrentRequest = false)
        {
            if (useCurrentRequest)
            {
                this.assetId = this.Request.MessageBody.AssetType.ToAssetId(this.GetAssetMap());
            }
            else
            {
                this.assetId = this.onGoingRequest.MessageBody.AssetType.ToAssetId(this.GetAssetMap());
            }
        }

        public override void InitializeMessageBody(int role = 0)
        {
            this.Request.MessageBody = new FounderSignBody
            {
                AssetType = this.assetId,
                Deposit = this.onGoingRequest.MessageBody.Deposit,
                RoleIndex = this.onGoingRequest.MessageBody.RoleIndex,
            };
        }

        public override void SetLocalsFromBody()
        {
            // RoleIndex Related
            this.RoleMax = 1;
            this.currentRole = this.Request.MessageBody.RoleIndex; // record current role Index
        }

        public override void ExtraSucceedAction()
        {
            if (IsRole1(this.Request.MessageBody.RoleIndex))
            {
                // Update the channel to opening state
                this.UpdateChannelState(EnumChannelState.OPENING);
            }
        }

        public override void UpdateTransaction()
        {
            if ((this.IsRole0(this.Request.MessageBody.RoleIndex) && this.isFounder) ||
                (this.IsRole1(this.Request.MessageBody.RoleIndex) && !this.isFounder))
            {
                // start update the transaction
                this.currentTransaction.founder.txDataSign = this.Request.MessageBody.Founder.txDataSign;
                this.currentTransaction.commitment.txDataSign = this.Request.MessageBody.Commitment.txDataSign;
                this.currentTransaction.revocableDelivery.txDataSign = this.Request.MessageBody.RevocableDelivery.txDataSign;

                this.currentTransaction.state = EnumTransactionState.confirmed.ToString();

                this.GetChannelLevelDbEntry().UpdateTransaction(this.Request.TxNonce, this.currentTransaction);
            }
        }

        public override void AddTransactionSummary()
        {
            // Add txid for monitor
            this.AddTransactionSummary(this.Request.TxNonce, this.Request.MessageBody.Commitment.originalData.txId,
                this.Request.ChannelName, EnumTransactionType.COMMITMENT);

            this.AddTransactionSummary(this.Request.TxNonce, this.Request.MessageBody.RevocableDelivery.originalData.txId,
                this.Request.ChannelName, EnumTransactionType.REVOCABLE);
        }

        public override void UpdateChannelSummary()
        {
            this.RecordChannelSummary();
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
            Log.Info("Handle FounderFail. Failed to create channel {0}. AssetType: {1}, Deposit {2}",
                this.Request.ChannelName, this.Request.MessageBody.AssetType, this.Request.MessageBody.Deposit);

            return true;
        }

        #region FounderFail_OVERRIDE_VIRUAL_SETS_OF_DIFFERENT_TRANSACTION_HANDLER
        /// Not need Initialize the level db interface for this handler
        public override void InitializeLevelDBApi() { }
        #endregion
    }
}
