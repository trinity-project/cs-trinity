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
using Trinity.BlockChain;
using Trinity.ChannelSet;
using Trinity.Wallets.Templates.Definitions;
using Trinity.Wallets.Templates.Messages;

namespace Trinity.Wallets.TransferHandler.TransactionHandler
{
    /// <summary>
    /// Class Handler for handling Htlc Message
    /// </summary>
    public class HtlcHandler : TransferHandler<Htlc, HtlcSignHandler, HtlcSignHandler>
    {
        private readonly NeoTransaction neoTransaction = null;

        private readonly Channel currentChannel = null;
        private readonly ChannelTableContent currentChannelInfo = null;
        private readonly TransactionTabelContent fundingTrade = null;
        private readonly long balance = 0;
        private readonly long peerBalance = 0;

        private readonly bool isHtlcValid = false;

        private CommitmentTx commTx;
        private RevocableDeliveryTx rdTx;
        private BreachRemedyTx brTx;

        public HtlcHandler(string sender, string receiver, string channel, string asset,
            string magic, UInt64 nonce, long payment, string hashcode, int role = 0) : base()
        {
            this.RoleMax = 3;

            this.Request = new Htlc
            {
                Sender = sender,
                Receiver = receiver,
                ChannelName = channel,
                AssetType = asset,
                NetMagic = magic,
                TxNonce = nonce,

                MessageBody = new HtlcBody
                {
                    AssetType = asset,
                    Count = payment,
                    RoleIndex = role,
                    HashR = hashcode
                }
            };

            this.ParsePubkeyPair(sender, receiver);
            this.SetChannelInterface(sender, receiver, channel, asset);

            this.currentChannel = new Channel(channel, asset, sender, receiver);
            this.currentChannelInfo = this.currentChannel.TryGetChannel(channel);
            this.fundingTrade = this.currentChannel.TryGetTransaction(fundingTradeNonce);

            if (null != this.currentChannelInfo
                && this.currentChannelInfo.balance.TryGetValue(sender, out this.balance)
                && this.currentChannelInfo.balance.TryGetValue(receiver, out this.peerBalance)
                && null != this.fundingTrade
                && null != this.fundingTrade.founder.originalData.scriptFunding
                && null != this.fundingTrade.founder.originalData.addressFunding)
            {
                long[] balanceOfPeers = this.CalculateBalance(role, this.balance, this.peerBalance, payment);
                this.neoTransaction = new NeoTransaction(asset.ToAssetId(), this.GetPubKey(), balanceOfPeers[0].ToString(),
                            this.GetPeerPubKey(), balanceOfPeers[1].ToString(),
                            this.fundingTrade.founder.originalData.addressFunding,
                            this.fundingTrade.founder.originalData.scriptFunding);

                this.isHtlcValid = true;
            }
        }

        public HtlcHandler(string msg) : base(msg)
        {
            this.ParsePubkeyPair(this.Request.Receiver, this.Request.Sender);
            this.SetChannelInterface(this.Request.Receiver, this.Request.Sender,
                this.Request.ChannelName, this.Request.MessageBody.AssetType);
        }

        public override bool Handle()
        {
            Log.Debug("Handle Message {0}. Channel name {1}, Asset Type: {2}, Payment: {3}. Balance: {4}. PeerBalance: {5}",
                this.Request.MessageType,
                this.Request.ChannelName,
                this.Request.MessageBody.AssetType,
                this.Request.MessageBody.Count,
                this.balance, this.peerBalance);

            if (!base.Handle())
            {
                return false;
            }

            // Add txid for monitor
            this.AddTransactionSummary(this.Request.TxNonce, this.Request.MessageBody.HCTX.txId,
                this.Request.ChannelName, EnumTxType.FUNDING);

            this.AddTransactionSummary(this.Request.TxNonce, this.Request.MessageBody.RDTX.txId,
                this.Request.ChannelName, EnumTxType.COMMITMENT);

            return true;
        }

        public override bool FailStep()
        {
            this.FHandler = new HtlcSignHandler(this.Request.Receiver, this.Request.Sender, this.Request.ChannelName,
                    this.Request.MessageBody.AssetType, this.Request.NetMagic, this.Request.TxNonce, this.Request.MessageBody.Count,
                    this.Request.MessageBody.HashR, this.Request.MessageBody.RoleIndex);
            this.FHandler.SetTransactionErrorCode(TransactionErrorCode.Fail);
            this.FHandler.MakeTransaction();

            return true;
        }

        public override bool SucceedStep()
        {
            if (this.IsIllegalRole(this.Request.MessageBody.RoleIndex))
            {
                this.FailStep();
                Console.WriteLine("Invalid nonce for Htlc. Nonce: {0}", this.Request.MessageBody.RoleIndex);
                return false;
            }

            // Send HtlcSign to peer
            #region New_HtlcSignHandler
            this.SHandler = new HtlcSignHandler(this.Request.Receiver, this.Request.Sender, this.Request.ChannelName,
                    this.Request.MessageBody.AssetType, this.Request.NetMagic, this.Request.TxNonce, this.Request.MessageBody.Count,
                    this.Request.MessageBody.HashR, this.Request.MessageBody.RoleIndex);
            // TODO : add some sign functions
            this.SHandler.MakeTransaction();
            #endregion

            #region New_Htlchandler
            if (this.IsRole0(this.Request.MessageBody.RoleIndex)
                || this.IsRole1(this.Request.MessageBody.RoleIndex)
                || this.IsRole2(this.Request.MessageBody.RoleIndex))
            {
                // record the peer data to the database ??? update ???
                this.AddTransaction(true);

                // Send Htlc to peer
                HtlcHandler htlcHandler = new HtlcHandler(this.Request.Receiver, this.Request.Sender, this.Request.ChannelName,
                        this.Request.MessageBody.AssetType, this.Request.NetMagic, this.Request.TxNonce, this.Request.MessageBody.Count,
                        this.Request.MessageBody.HashR, this.Request.MessageBody.RoleIndex + 1);
                htlcHandler.MakeTransaction();
            }
            #endregion
            else if (this.IsRole3(this.Request.MessageBody.RoleIndex))
            {
                // update 
                this.AddTransaction(true);
            }
            else
            {
                Log.Error("Unkown Role index: {0}", this.Request.MessageBody.RoleIndex);
            }

            return true;
        }

        public override bool MakeTransaction()
        {
            bool ret = base.MakeTransaction();
            Log.Debug("{0} to send {1}. Channel name {2}, Asset Type: {3}, Payment: {4}.",
                ret ? "Succeed" : "Fail",
                this.Request.MessageType,
                this.Request.ChannelName,
                this.Request.MessageBody.AssetType,
                this.Request.MessageBody.Count);
            return ret;
        }

        // Todo: impmentation this method in the base class in future
        public override bool VerifyRoleIndex()
        {
            if (this.IsIllegalRole(this.Request.MessageBody.RoleIndex))
            {
                Console.WriteLine("Invalid nonce for Htlc. Nonce: {0}", this.Request.TxNonce);
                return false;
            }

            return true;
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
            if (!this.isHtlcValid)
            {
                Log.Error("Error to makeup the HTLC transaction. Channel name {2}, Asset Type: {3}, Payment: {4}, Balance: {4}. PeerBalance: {5}",
                    this.Request.ChannelName,
                    this.Request.MessageBody.AssetType,
                    this.Request.MessageBody.Count, this.balance, this.peerBalance);
                return false;
            }

            // record the item to database
            if (IsRole0(this.Request.MessageBody.RoleIndex) || IsRole1(this.Request.MessageBody.RoleIndex))
            {
                // Create Commitment transaction
                this.neoTransaction.CreateCTX(out this.commTx);

                // create Revocable commitment transaction
                this.neoTransaction.createRDTX(out this.rdTx, this.commTx.txId);

                // TODO: makeup message body

                this.AddTransaction();
            }
            else if (IsRole2(this.Request.MessageBody.RoleIndex) || IsRole3(this.Request.MessageBody.RoleIndex))
            {
                // TODO: update the transaction data to the database
            }

            return true;
        }

        private void AddTransaction(bool isPeer = false)
        {
            // TODO: add transaction to dabase
        }
    }

    /// <summary>
    /// Class Handler for handling HtlcSign Message
    /// </summary>
    public class HtlcSignHandler : TransferHandler<HtlcSign, HtlcHandler, HtlcFailHandler>
    {
        private readonly NeoTransaction neoTransaction = null;

        private readonly Channel currentChannel = null;
        private readonly ChannelTableContent currentChannelInfo = null;
        private readonly TransactionTabelContent fundingTrade = null;
        private readonly long balance = 0;
        private readonly long peerBalance = 0;

        public HtlcSignHandler(string sender, string receiver, string channel, string asset,
            string magic, UInt64 nonce, long payment, string hashcode, int role = 0) : base()
        {
            this.RoleMax = 3;

            this.Request = new HtlcSign
            {
                Sender = sender,
                Receiver = receiver,
                ChannelName = channel,
                AssetType = asset,
                NetMagic = magic,
                TxNonce = nonce,

                MessageBody = new HtlcBody
                {
                    AssetType = asset,
                    Count = payment,
                    RoleIndex = role,
                    HashR = hashcode,
                },

                Error = TransactionErrorCode.Ok.ToString(),
            };

            this.ParsePubkeyPair(sender, receiver);
            this.SetChannelInterface(sender, receiver, channel, asset);

            this.currentChannel = new Channel(channel, asset, sender, receiver);
            this.currentChannelInfo = this.currentChannel.TryGetChannel(channel);
            this.fundingTrade = this.currentChannel.TryGetTransaction(fundingTradeNonce);

            if (null != this.currentChannelInfo
                && this.currentChannelInfo.balance.TryGetValue(sender, out this.balance)
                && this.currentChannelInfo.balance.TryGetValue(receiver, out this.peerBalance)
                && null != this.fundingTrade
                && null != this.fundingTrade.founder.originalData.scriptFunding
                && null != this.fundingTrade.founder.originalData.addressFunding)
            {
                long[] balanceOfPeers = this.CalculateBalance(role, this.balance, this.peerBalance, payment);
                this.neoTransaction = new NeoTransaction(asset.ToAssetId(), this.GetPubKey(), balanceOfPeers[0].ToString(),
                            this.GetPeerPubKey(), balanceOfPeers[1].ToString(),
                            this.fundingTrade.founder.originalData.addressFunding,
                            this.fundingTrade.founder.originalData.scriptFunding);
            }
        }

        public HtlcSignHandler(string msg) : base(msg)
        {
            this.ParsePubkeyPair(this.Request.Receiver, this.Request.Sender);
            this.SetChannelInterface(this.Request.Receiver, this.Request.Sender,
                this.Request.ChannelName, this.Request.MessageBody.AssetType);
        }

        public override bool Handle()
        {
            Log.Debug("Handle Message {0}. Channel name {1}, Asset Type: {2}, Value: {3}.",
                this.Request.MessageType,
                this.Request.ChannelName,
                this.Request.MessageBody.AssetType,
                this.Request.MessageBody.Count);
            return base.Handle();
        }

        public override bool FailStep()
        {
            return base.FailStep();
        }

        public override bool SucceedStep()
        {
            return base.SucceedStep();
        }

        public void MakeupCommitmentSignTx(CommitmentTx txContent)
        {
                // TODO: add method body
            }

            public void MakeupRevocableDeliverySignTx(RevocableDeliveryTx txContent)
        {
            // TODO: add method body
        }

        public void SetTransactionErrorCode(TransactionErrorCode errCode)
        {
            this.Request.Error = errCode.ToString();
        }
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////
    /// HtlcFailHandler start
    ///////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Class Handler for handling HtlcFail Message
    /// </summary>
    public class HtlcFailHandler : TransferHandler<HtlcFail, VoidHandler, VoidHandler>
    {
        public HtlcFailHandler(string message) : base(message)
        {
            this.ParsePubkeyPair(this.Request.Receiver, this.Request.Sender);
            this.SetChannelInterface(this.Request.Receiver, this.Request.Sender,
                this.Request.ChannelName, this.Request.MessageBody.AssetType);
        }

        public override bool Handle()
        {
            Log.Debug("Handle Message {0}. Failed to make Htlc with channel {1}.",
                this.Request.MessageType,
                this.Request.ChannelName);

            return base.Handle();
        }

        public override bool FailStep()
        {
            return base.FailStep();
        }

        public override bool SucceedStep()
        {
            return base.SucceedStep();
        }
    }
}
