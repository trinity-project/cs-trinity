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
    public class SettleHandler : TransferHandler<Settle, SettleSignHandler, SettleSignHandler>
    {
        private readonly TransactionFundingContent fundingTrade;
        private readonly ChannelTableContent channelContent;
        private readonly NeoTransaction neoTransaction;

        /// <summary>
        /// Constructors
        /// </summary>
        /// <param name="message"></param>
        public SettleHandler(string message) : base(message)
        {
            this.ParsePubkeyPair(this.Request.Receiver, this.Request.Sender);
            this.SetChannelInterface(this.Request.Receiver, this.Request.Sender,
                this.Request.ChannelName, this.Request.MessageBody.AssetType);
            this.fundingTrade = this.GetChannelInterface().TryGetTransaction<TransactionFundingContent>(fundingTradeNonce);
            this.channelContent = this.GetChannelInterface().TryGetChannel(this.Request.ChannelName);

            // Whatever happens, we set the channel settling when call this class
            this.UpdateChannelState(this.Request.Receiver, this.Request.Sender,
                this.Request.ChannelName, EnumChannelState.SETTLING);
        }

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="receiver"></param>
        /// <param name="channel"></param>
        /// <param name="asset"></param>
        /// <param name="magic"></param>
        public SettleHandler(string sender, string receiver, string channel, string asset, string magic) : base()
        {
            
            this.Request = new Settle
            {
                Sender = sender,
                Receiver = receiver,
                ChannelName = channel,
                AssetType = asset,
                NetMagic = magic ?? this.GetNetMagic(),
                MessageBody = new SettleBody(),
            };

            this.ParsePubkeyPair(sender, receiver);
            this.SetChannelInterface(sender, receiver, channel, asset);
            this.Request.TxNonce = this.NextNonce(channel);
            this.fundingTrade = this.GetChannelInterface().TryGetTransaction<TransactionFundingContent>(fundingTradeNonce);
            this.channelContent = this.GetChannelInterface().TryGetChannel(channel);

            long balance = this.channelContent.balance;
            long peerBalance = this.channelContent.peerBalance;


            this.neoTransaction = new NeoTransaction(asset.ToAssetId(), this.GetPubKey(), balance.ToString(),
                this.GetPeerPubKey(), peerBalance.ToString(), this.fundingTrade.founder.originalData.addressFunding,
                this.fundingTrade.founder.originalData.scriptFunding);

            // Whatever happens, we set the channel settling when call this class
            this.UpdateChannelState(this.Request.Receiver, this.Request.Sender,
                this.Request.ChannelName, EnumChannelState.SETTLING);

        }

        public override bool Handle()
        {
            Log.Debug("Handle Settle Message. Channel name: {0}, Balance {1}",
                this.Request.ChannelName, this.Request.MessageBody.Balance);
            if (!base.Handle())
            {
                return false;
            }

            // Add txid for monitor
            this.AddTransactionSummary(this.Request.TxNonce, this.Request.MessageBody.Settlement.txId,
                this.Request.ChannelName, EnumTxType.SETTLE);

            return true;
        }

        public override bool FailStep()
        {
            this.FHandler = new SettleSignHandler(this.Request.Receiver, this.Request.Sender, this.Request.ChannelName,
                    this.Request.MessageBody.AssetType, this.Request.NetMagic);

            this.FHandler.MakeTransaction();

            return true;
        }

        public override bool SucceedStep()
        {

            // create SettleSign handler for send response to peer
            this.SHandler = new SettleSignHandler(this.Request.Receiver, this.Request.Sender, this.Request.ChannelName,
                    this.Request.MessageBody.AssetType, this.Request.NetMagic);
            this.SHandler.MakeupRefundTxSign(this.Request.MessageBody.Settlement);

            // send SettleSign to peer
            this.SHandler.MakeTransaction();

            return true;
        }

        public override bool MakeTransaction()
        {
            // makeup the message
            if (this.MakeupRefundTx())
            {
                bool ret = base.MakeTransaction();
                
                Log.Debug("{0} to send Settle Message.", ret?"Succeed":"Fail");
                return ret;
            }

            return false;
        }

        public override bool MakeupMessage()
        {
            return this.MakeupRefundTx();
        }

        private bool MakeupRefundTx()
        {
            // makeup refund trade
            if (null == this.fundingTrade || null == this.channelContent)
            {
                Log.Error("No funding trade is found for channel: {0}", this.Request.ChannelName);
                return false;
            }

            // Start to create refund trade
            if (!this.neoTransaction.CreateSettle(out TxContents refundTx))
            {
                Log.Error("Failed to create refunding trade for channel: {0}", this.Request.ChannelName);
                return false;
            }

            // set the message body
            this.Request.MessageBody.Settlement = refundTx;
            this.Request.MessageBody.Balance = new Dictionary<string, long> {
                { this.Request.Sender, this.channelContent.balance}, { this.Request.Receiver, this.channelContent.peerBalance }
            };
            this.Request.MessageBody.AssetType = this.channelContent.asset;

            // Add txid for monitor
            this.AddTransactionSummary(this.Request.TxNonce, refundTx.txId,
                this.Request.ChannelName, EnumTxType.SETTLE);

            return true;
        }
    }


    ///////////////////////////////////////////////////////////////////////////////////////////
    /// SettleSignHandler Prototype
    ///////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Class Handler for handling SettleSign Message
    /// </summary>
    public class SettleSignHandler : TransferHandler<SettleSign, VoidHandler, VoidHandler>
    {
        private readonly TransactionFundingContent fundingTrade;
        private readonly ChannelTableContent channelContent;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public SettleSignHandler(string message) : base(message)
        {
            this.ParsePubkeyPair(this.Request.Receiver, this.Request.Sender);
            this.SetChannelInterface(this.Request.Receiver, this.Request.Sender,
                this.Request.ChannelName, this.Request.MessageBody.AssetType);
            this.fundingTrade = this.GetChannelInterface().TryGetTransaction<TransactionFundingContent>(fundingTradeNonce);
            this.channelContent = this.GetChannelInterface().TryGetChannel(this.Request.ChannelName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="receiver"></param>
        /// <param name="channel"></param>
        /// <param name="asset"></param>
        /// <param name="magic"></param>
        public SettleSignHandler(string sender, string receiver, string channel, string asset, string magic) : base()
        {
            this.RoleMax = 1;

            this.Request = new SettleSign
            {
                Sender = sender,
                Receiver = receiver,
                ChannelName = channel,
                AssetType = asset,
                NetMagic = magic,
                
                MessageBody = new SettleSignBody()
            };

            this.ParsePubkeyPair(sender, receiver);
            this.SetChannelInterface(sender, receiver, channel, asset);
            this.fundingTrade = this.GetChannelInterface().TryGetTransaction<TransactionFundingContent>(fundingTradeNonce);
            this.channelContent = this.GetChannelInterface().TryGetChannel(channel);
        }

        public override bool Handle()
        {
            Log.Debug("Handle SettleSign Message. Channel name: {0}.", this.Request.ChannelName);
            return base.Handle();
        }

        public override bool FailStep()
        {
            Log.Error(this.Request.Error);
            return true;
        }

        public override bool SucceedStep()
        {
            // Broadcast this transaction
            this.BroadcastTransaction();
            return true;
        }

        private void BroadcastTransaction()
        {
            string peerSettleSignarture = this.Request.MessageBody.Settlement.txDataSign;
            string settleSignarture = this.Sign(this.Request.MessageBody.Settlement.originalData.txData);
            string witness = this.Request.MessageBody.Settlement.originalData.witness
                .Replace("{signOther}", peerSettleSignarture)
                .Replace("{signSelf}", settleSignarture);

            JObject ret = NeoInterface.SendRawTransaction(this.Request.MessageBody.Settlement.originalData.txData + witness);
            Log.Debug("Broadcast Settle transaction result is {0}. txId: {1}", ret, this.Request.MessageBody.Settlement.originalData.txId);
        }

        public bool MakeupRefundTxSign(TxContents contents)
        {
            this.Request.MessageBody.Settlement = this.MakeupSignature(contents);
            return true;
        }

        public void SetErrorCode(TransactionErrorCode error)
        {
            this.Request.Error = error.ToString();
        }
    }
}
