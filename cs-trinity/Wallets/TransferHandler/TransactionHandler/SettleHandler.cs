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
        private readonly TransactionTabelContent fundingTrade;
        private readonly ChannelTableContent channelContent;

        /// <summary>
        /// Constructors
        /// </summary>
        /// <param name="message"></param>
        public SettleHandler(string message) : base(message)
        {
            this.ParsePubkeyPair(this.Request.Receiver, this.Request.Sender);
            this.SetChannelInterface(this.Request.Receiver, this.Request.Sender,
                this.Request.ChannelName, null);
            this.fundingTrade = this.GetChannelInterface().TryGetTransaction(0);
            this.channelContent = this.GetChannelInterface().TryGetChannel(this.Request.ChannelName);
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
            this.fundingTrade = this.GetChannelInterface().TryGetTransaction(0);
            this.channelContent = this.GetChannelInterface().TryGetChannel(channel);
        }

        public override bool Handle()
        {
            base.Handle();

            return true;
        }

        public override bool FailStep()
        {
            this.FHandler = new SettleSignHandler(this.Request.Receiver, this.Request.Sender, this.Request.ChannelName,
                    this.Request.MessageBody.AssetType, this.Request.NetMagic);

            this.FHandler.MakeTransaction(this.GetClient());

            return true;
        }

        public override bool SucceedStep()
        {

            // create SettleSign handler for send response to peer
            this.SHandler = new SettleSignHandler(this.Request.Receiver, this.Request.Sender, this.Request.ChannelName,
                    this.Request.MessageBody.AssetType, this.Request.NetMagic);
            this.SHandler.MakeupRefundTxSign(this.Request.MessageBody.Settlement);

            // send SettleSign to peer
            this.SHandler.MakeTransaction(this.GetClient());

            return true;
        }

        public override void MakeTransaction(TrinityTcpClient client)
        {
            // makeup the message
            if (this.MakeupRefundTx())
            {
                base.MakeTransaction(client);
            }
        }

        private bool MakeupRefundTx()
        {
            // makeup refund trade
            if (null == this.fundingTrade || null == this.channelContent)
            {
                Console.WriteLine("No funding trade is found for channel: {}", this.Request.ChannelName);
                return false;
            }

            // Start to create refund trade
            this.channelContent.balance.TryGetValue(this.Request.Sender, out double balance);
            this.channelContent.balance.TryGetValue(this.Request.Sender, out double peerBalance);
            JObject refundTx = Funding.createSettle(
                this.fundingTrade.founder.originalData.addressFunding,
                balance.ToString(), peerBalance.ToString(),
                this.GetPubKey(), this.GetPeerPubKey(),
                this.fundingTrade.founder.originalData.scriptFunding,
                this.channelContent.asset.ToAssetId()
                );

            if (null == refundTx)
            {
                Console.WriteLine("Failed to create refunding trade for channel: {}", this.Request.ChannelName);
                return false;
            }

            // set the message body
            this.Request.MessageBody.Settlement = refundTx.ToString().Deserialize<TxContents>();
            this.Request.MessageBody.Balance = this.channelContent.balance;
            this.Request.MessageBody.AssetType = this.channelContent.asset;

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
        private readonly TransactionTabelContent fundingTrade;
        private readonly ChannelTableContent channelContent;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public SettleSignHandler(string message) : base(message)
        {
            this.ParsePubkeyPair(this.Request.Receiver, this.Request.Sender);
            this.SetChannelInterface(this.Request.Receiver, this.Request.Sender,
                this.Request.ChannelName, null);
            this.fundingTrade = this.GetChannelInterface().TryGetTransaction(0);
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
            this.fundingTrade = this.GetChannelInterface().TryGetTransaction(0);
            this.channelContent = this.GetChannelInterface().TryGetChannel(channel);
        }

        public override bool Handle()
        {
            return false;
        }

        public override bool FailStep()
        {
            Console.WriteLine(this.Request.Error);
            return true;
        }

        public override bool SucceedStep()
        {
            // Broadcast this transaction
            throw new NotImplementedException();
        }

        public bool MakeupRefundTxSign(TxContents settlement)
        {
            return true;
        }

        public void SetErrorCode(TransactionErrorCode error)
        {
            this.Request.Error = error.ToString();
        }
    }
}
