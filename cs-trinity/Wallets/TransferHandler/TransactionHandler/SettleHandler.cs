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
    /// Class Handler for handling Settle Message
    /// </summary>
    public class SettleHandler : TransactionHandler<Settle, SettleSign, SettleHandler, SettleSignHandler>
    {
        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="receiver"></param>
        /// <param name="channel"></param>
        /// <param name="asset"></param>
        /// <param name="magic"></param>
        public SettleHandler(string sender, string receiver, string channel, string asset, string magic)
            : base(sender, receiver, channel, asset, magic, 0, 0)
        {
            this.Request.TxNonce = this.NextNonce(channel);

            // Whatever happens, we set the channel settling when Settle message is being to send
            this.UpdateChannelState(EnumChannelState.SETTLING);
        }

        /// <summary>
        /// Constructors
        /// </summary>
        /// <param name="message"></param>
        public SettleHandler(string message) : base(message)
        {
            // Whatever happens, we set the channel settling when the Settle message was received
            this.UpdateChannelState(EnumChannelState.SETTLING);
        }

        public override bool Handle()
        {
            Log.Debug("Handle Settle Message. Channel name: {0}, Balance: {1}, txId: {2}",
                this.Request.ChannelName, this.Request.MessageBody.Balance, this.Request.MessageBody.Settlement.txId);
            return base.Handle();
        }

        public override bool SucceedStep()
        {
            // TODO: remove the transaction ID in future... IMPROVE the efficiency ???
            return new SettleSignHandler(this.Request).MakeTransaction();
        }

        public override bool MakeTransaction()
        {
            if (base.MakeTransaction())
            {
                Log.Info("Succeed to send Settle Message. Channel: {0}, AssetType: {1}, Balance: {2}.",
                    this.Request.ChannelName, this.Request.MessageBody.AssetType, this. Request.MessageBody.Balance);
                return true;
            }

            return false;
        }

        public override bool MakeupMessage()
        {
            return this.MakeupRefundTx();
        }

        private bool MakeupRefundTx()
        {
            // Start to create refund trade
            if (!this.neoTransaction.CreateSettle(out TxContents refundTx))
            {
                Log.Error("Failed to create refunding trade for Channel: {0}", this.Request.ChannelName);
                return false;
            }

            // set the message body
            this.Request.MessageBody.Settlement = refundTx;
            this.Request.MessageBody.Balance = new Dictionary<string, long> {
                { this.Request.Sender, this.GetCurrentChannel().balance}, { this.Request.Receiver, this.GetCurrentChannel().peerBalance }
            };
            this.Request.MessageBody.AssetType = this.GetCurrentChannel().asset;

            return true;
        }

        #region Settle_OVERRIDE_VIRUAL_SETS_OF_DIFFERENT_TRANSACTION_HANDLER
        public override void InitializeBlockChainApi() { this.GetBlockChainAdaptorApi(true); }

        public override void InitializeMessageBody(string asset, long payment, int role = 0, string hashcode = null, string rcode = null)
        {
            this.Request.MessageBody = new SettleBody
            {
                AssetType = asset,
            };
        }

        public override void SetLocalsFromBody()
        {
            // RoleIndex Related
            this.RoleMax = 0;
            this.currentRole = 0; // record current role Index

            // Asset type from message body for adaptor old version trinity
            this.AssetType = this.Request.MessageBody.AssetType;
        }

        public override SettleSignHandler CreateResponseHndl(string errorCode = "Ok")
        {
            return new SettleSignHandler(this.Request, errorCode);
        }
        #endregion
    }


    ///////////////////////////////////////////////////////////////////////////////////////////
    /// SettleSignHandler Prototype
    ///////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Class Handler for handling SettleSign Message
    /// </summary>
    public class SettleSignHandler : TransactionHandler<SettleSign, Settle, SettleHandler, SettleSignHandler>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public SettleSignHandler(string message) : base(message) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="receiver"></param>
        /// <param name="channel"></param>
        /// <param name="asset"></param>
        /// <param name="magic"></param>
        public SettleSignHandler(Settle message, string errorCode = "Ok") : base(message, 0)
        {
            this.Request.Error = errorCode;
        }

        public override bool Handle()
        {
            Log.Debug("Handle SettleSign Message. Channel name: {0}.", this.Request.ChannelName);
            return base.Handle();
        }

        public override bool FailStep(string errorCode)
        {
            Log.Error("{0}: Failed to close channel: {1}. Error: {2}", 
                this.Request.MessageType, this.Request.ChannelName, this.Request.Error);
            return true;
        }

        public override bool SucceedStep()
        {
            // Broadcast this transaction
            this.BroadcastTransaction();

            // add monitor txid
            this.AddTransactionSummary();
            return true;
        }

        private void BroadcastTransaction()
        {
            JObject ret = this.BroadcastTransaction(
                this.Request.MessageBody.Settlement.originalData.txData,
                this.Request.MessageBody.Settlement.txDataSign,
                this.Request.MessageBody.Settlement.originalData.witness );
            Log.Info("Broadcast Settle transaction result: {0}. txId: {1}", ret, this.Request.MessageBody.Settlement.originalData.txId);
        }

        public bool MakeupRefundTxSign(TxContents contents)
        {
            this.Request.MessageBody.Settlement = this.MakeupSignature(contents);
            this.Request.MessageBody.Balance = new Dictionary<string, long> {
                { this.Request.Sender, this.GetCurrentChannel().balance}, { this.Request.Receiver, this.GetCurrentChannel().peerBalance } };

            return true;
        }

        public override bool MakeTransaction()
        {
            if (base.MakeTransaction())
            {
                Log.Info("Succeed to send SettleSign message. Channel: {0}, AssetType: {1}, Balance: {2}.",
                    this.Request.ChannelName, this.Request.MessageBody.AssetType, this.Request.MessageBody.Balance);
                this.AddTransactionSummary();
                return true;
            }

            return false;
        }

        public override bool MakeupMessage()
        {
            this.Request.MessageBody.Balance = new Dictionary<string, long> {
                { this.Request.Sender, this.GetCurrentChannel().balance}, { this.Request.Receiver, this.GetCurrentChannel().peerBalance } };
            this.Request.MessageBody.Settlement = this.MakeupSignature(this.onGoingRequest.MessageBody.Settlement);

            return base.MakeupMessage();
        }

        public override bool Verify()
        {
            this.VerifySignarture(this.Request.MessageBody.Settlement.originalData.txData,
                this.Request.MessageBody.Settlement.txDataSign);

            return true;
        }

        #region SettleSign_OVERRIDE_VIRUAL_SETS_OF_DIFFERENT_TRANSACTION_HANDLER
        public override void InitializeBlockChainApi() { this.GetBlockChainAdaptorApi(true); }

        public override void InitializeMessageBody(int role = 0)
        {
            this.Request.MessageBody = new SettleSignBody
            {
                AssetType = this.onGoingRequest.MessageBody.AssetType
            };
        }

        public override void SetLocalsFromBody()
        {
            // RoleIndex Related
            this.RoleMax = 0;
            this.currentRole = 0; // record current role Index

            // Asset type from message body for adaptor old version trinity
            this.AssetType = this.Request.MessageBody.AssetType;
        }

        public override void AddTransactionSummary()
        {
            // Add txid for monitor
            this.AddTransactionSummary(this.Request.TxNonce, this.Request.MessageBody.Settlement.originalData.txId,
                this.Request.ChannelName, EnumTransactionType.SETTLE);
        }
        #endregion
    }
}
