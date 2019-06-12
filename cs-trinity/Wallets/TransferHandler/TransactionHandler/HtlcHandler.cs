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
        private readonly TransactionFundingContent fundingTrade = null;
        private readonly long balance = 0;
        private readonly long peerBalance = 0;

        private readonly bool isHtlcValid = false;

        private HtlcCommitTx hcTx;
        private HtlcRevocableDeliveryTx rdTx;
        private HtlcExecutionTx heTx;
        private HtlcExecutionDeliveryTx hedTx;
        private HtlcExecutionRevocableDeliveryTx heRdTx;
        private HtlcTimoutTx htTx;
        private HtlcTimeoutDeliveryTx htdTx;
        private HtlcTimeoutRevocableDelivertyTx htRdTx;

        public HtlcHandler(string sender, string receiver, string channel, string asset,
            string magic, UInt64 nonce, long payment, string hashcode, List<string> router, int role = 0) : base()
        {
            this.RoleMax = 1;

            // Allocate Htlc request.
            this.Request = new Htlc
            {
                Sender = sender,
                Receiver = receiver,
                ChannelName = channel,
                AssetType = asset,
                NetMagic = magic,
                Router = router,
                Next = router?[router.Count - 1],

                MessageBody = new HtlcBody
                {
                    AssetType = asset,
                    Count = payment,
                    RoleIndex = role,
                    HashR = hashcode,
                }
            };

            this.ParsePubkeyPair(sender, receiver);
            this.SetChannelInterface(sender, receiver, channel, asset);

            // create Htlc request if role is 0
            if (IsRole0(role))
            {
                this.Request.TxNonce = this.NextNonce(channel);
            }
            else
            {
                this.Request.TxNonce = nonce;
            }

            this.currentChannel = new Channel(channel, asset, sender, receiver);
            this.currentChannelInfo = this.currentChannel.TryGetChannel(channel);
            this.fundingTrade = this.currentChannel.TryGetTransaction<TransactionFundingContent>(fundingTradeNonce);

            if (null != this.currentChannelInfo
                && null != this.fundingTrade
                && null != this.fundingTrade.founder.originalData.scriptFunding
                && null != this.fundingTrade.founder.originalData.addressFunding)
            {
                this.balance = this.currentChannelInfo.balance;
                this.peerBalance = this.currentChannelInfo.peerBalance;
                long[] balanceOfPeers = this.CalculateBalanceForHtlc(role, this.balance, this.peerBalance, payment);
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

            this.currentChannel = new Channel(this.Request.ChannelName, this.Request.MessageBody.AssetType,
                this.Request.Receiver, this.Request.Sender);
            this.currentChannelInfo = this.currentChannel.TryGetChannel(this.Request.ChannelName);
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
            this.FHandler = new HtlcSignHandler(this.Request, TransactionErrorCode.Fail);
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
            this.SHandler = new HtlcSignHandler(this.Request);
            // TODO : add some sign functions
            this.SHandler.MakeTransaction();
            #endregion

            #region New_Htlchandler
            if (this.IsRole0(this.Request.MessageBody.RoleIndex))
            {
                // record the peer data to the database ??? update ???
                this.AddTransaction(true);

                // Send Htlc to peer
                HtlcHandler htlcHandler = new HtlcHandler(this.Request.Receiver, this.Request.Sender, this.Request.ChannelName,
                        this.Request.MessageBody.AssetType, this.Request.NetMagic, this.Request.TxNonce, this.Request.MessageBody.Count,
                        this.Request.MessageBody.HashR, this.Request.Router, this.Request.MessageBody.RoleIndex + 1);
                htlcHandler.MakeTransaction();

                // Send Htlc to next peer
                int currentPeerIndex = this.Request.Router.IndexOf(this.Request.Receiver);
                if (0 < currentPeerIndex && currentPeerIndex < this.Request.Router.Count - 1)
                {
                    string nextPeer = this.Request.Router[currentPeerIndex + 1];
                    string nextChannel = this.ChooseChannel(nextPeer, this.Request.MessageBody.Count);
                    if (null != nextChannel)
                    {
                        HtlcHandler nextHtlcHandler = new HtlcHandler(this.Request.Receiver, nextPeer, nextChannel,
                            this.Request.MessageBody.AssetType, this.Request.NetMagic, this.Request.TxNonce, this.Request.MessageBody.Count,
                            this.Request.MessageBody.HashR, this.Request.Router);
                        nextHtlcHandler.MakeTransaction();
                    }

                }
            }
            #endregion
            else if (this.IsRole1(this.Request.MessageBody.RoleIndex))
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

            string lockedPayment = this.Request?.MessageBody?.Count.ToString();

            // record the item to database
            if (IsRole0(this.Request.MessageBody.RoleIndex))
            {
                // Create HCTX
                this.neoTransaction.CreateSenderHCTX(out this.hcTx, lockedPayment, this.Request.MessageBody.HashR);

                // create RDTX
                this.neoTransaction.CreateSenderRDTX(out this.rdTx, this.hcTx.txId);

                // create HEDTX
                this.neoTransaction.CreateHEDTX(out this.hedTx, lockedPayment);

                // create HTTX
                this.neoTransaction.CreateHTTX(out this.htTx, lockedPayment);

                // create HTRDTX
                this.neoTransaction.CreateHTRDTX(out this.htRdTx, this.htTx.txId, lockedPayment);

                // makeup message body
                this.Request.MessageBody.HCTX = this.hcTx;
                this.Request.MessageBody.RDTX = this.rdTx;
                this.Request.MessageBody.HTTX = this.htTx;
                this.Request.MessageBody.HTRDTX = this.htRdTx;
                this.Request.MessageBody.HEDTX = this.MakeupSignature(this.hedTx);

                this.AddTransaction();
            }
            else if (IsRole1(this.Request.MessageBody.RoleIndex))
            {
                // Create HCTX
                this.neoTransaction.CreateReceiverHCTX(out this.hcTx, lockedPayment, this.Request.MessageBody.HashR);

                // create RDTX
                this.neoTransaction.CreateReceiverRDTX(out this.rdTx, this.hcTx.txId);

                // create HETX
                this.neoTransaction.CreateHETX(out this.heTx, lockedPayment);

                // create HERDTX
                this.neoTransaction.CreateHERDTX(out this.heRdTx, this.heTx.txId, lockedPayment);

                // create HTDTX
                this.neoTransaction.CreateHTDTX(out this.htdTx, lockedPayment);

                // makeup message body
                this.Request.MessageBody.HCTX = this.hcTx;
                this.Request.MessageBody.RDTX = this.rdTx;
                this.Request.MessageBody.HETX = this.MakeupSignature(this.heTx);
                this.Request.MessageBody.HERDTX = this.MakeupSignature(this.heRdTx);
                this.Request.MessageBody.HTDTX = this.htdTx;

                // TODO: record to db ???
            }
            else
            {
                // TODO: maybe we need do some exception info to show in the message box???
            }

            return true;
        }

        private void AddTransaction(bool isPeer = false)
        {
            // TODO: add transaction to dabase
        }

        private string ChooseChannel(string peer, long payment)
        {
            foreach (ChannelTableContent channel in this.GetChannelInterface()?.GetChannelListOfThisWallet())
            {
                if (channel.peer.Equals(peer)
                    && channel.state.Equals(EnumChannelState.OPENED.ToString())
                    && payment <= channel.balance)
                {
                    return channel.channel;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Class Handler for handling HtlcSign Message
    /// </summary>
    public class HtlcSignHandler : TransferHandler<HtlcSign, HtlcHandler, HtlcFailHandler>
    {
        private readonly NeoTransaction neoTransaction = null;
        private readonly Htlc htlcRequest = null;
        private readonly bool isErrorResponse = true;

        private readonly Channel currentChannel = null;
        private readonly ChannelTableContent currentChannelInfo = null;
        private readonly TransactionFundingContent fundingTrade = null;
        private readonly long balance = 0;
        private readonly long peerBalance = 0;

        public HtlcSignHandler(Htlc htlcMessage, TransactionErrorCode errorCode=TransactionErrorCode.Ok) : base()
        {
            this.RoleMax = 1;
            this.htlcRequest = htlcMessage;

            string sender = htlcMessage.Receiver;
            string receiver = htlcMessage.Sender;
            string channel = htlcMessage.ChannelName;
            string asset = htlcMessage.MessageBody.AssetType;
            int role = htlcMessage.MessageBody.RoleIndex;
            long payment = htlcMessage.MessageBody.Count;

            this.Request = new HtlcSign
            {
                Sender = sender,
                Receiver = receiver,
                ChannelName = channel,
                AssetType = asset,
                NetMagic = htlcMessage.NetMagic,
                TxNonce = htlcMessage.TxNonce,

                MessageBody = new HtlcSignBody
                {
                    AssetType = htlcMessage.AssetType,
                    Count = payment,
                    RoleIndex = role,
                    HashR = htlcMessage.MessageBody.HashR,
                },

                Error = TransactionErrorCode.Ok.ToString(),
            };

            this.ParsePubkeyPair(sender, receiver);
            this.SetChannelInterface(sender, receiver, channel, asset);

            this.currentChannel = new Channel(channel, asset, sender, receiver);
            this.currentChannelInfo = this.currentChannel.TryGetChannel(channel);
            this.fundingTrade = this.currentChannel.TryGetTransaction<TransactionFundingContent>(fundingTradeNonce);

            if (null != this.currentChannelInfo
                && null != this.fundingTrade
                && null != this.fundingTrade.founder.originalData.scriptFunding
                && null != this.fundingTrade.founder.originalData.addressFunding)
            {
                this.balance = this.currentChannelInfo.balance;
                this.peerBalance = this.currentChannelInfo.peerBalance;
                long[] balanceOfPeers = this.CalculateBalanceForRsmc(role, this.balance, this.peerBalance, payment);
                this.neoTransaction = new NeoTransaction(asset.ToAssetId(), this.GetPubKey(), balanceOfPeers[0].ToString(),
                            this.GetPeerPubKey(), balanceOfPeers[1].ToString(),
                            this.fundingTrade.founder.originalData.addressFunding,
                            this.fundingTrade.founder.originalData.scriptFunding);
            }

            this.SetTransactionErrorCode(errorCode);
            this.isErrorResponse = errorCode != TransactionErrorCode.Ok;
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
            if (this.IsRole0(this.Request.MessageBody.RoleIndex))
            {
                // Todo: update level db
            }
            else if (this.IsRole1(this.Request.MessageBody.RoleIndex))
            {
                // Todo: update level db
            }
            else
            {
                return false;
            }
            return base.SucceedStep();
        }

        public override bool MakeupMessage()
        {
            if (isErrorResponse)
            {
                // currently: do nothing
            }
            else
            {
                this.Request.MessageBody.HCTX = this.MakeupSignature(this.htlcRequest.MessageBody.HCTX);
                this.Request.MessageBody.RDTX = this.MakeupSignature(this.htlcRequest.MessageBody.RDTX);

                // Start to sign the messages
                if (IsRole0(this.Request.MessageBody.RoleIndex))
                {
                    this.Request.MessageBody.HTTX = this.MakeupSignature(this.htlcRequest.MessageBody.HTTX);
                    this.Request.MessageBody.HTRDTX = this.MakeupSignature(this.htlcRequest.MessageBody.HTRDTX);
                }
                else if (IsRole1(this.Request.MessageBody.RoleIndex))
                {
                    this.Request.MessageBody.HTDTX = this.MakeupSignature(this.htlcRequest.MessageBody.HTDTX);
                }
                else
                {
                    Log.Error("Error Role: {0} for htlc transaction:", this.Request.MessageBody.RoleIndex);
                    this.SetTransactionErrorCode(TransactionErrorCode.Invalid_Role);
                }
            }
            return base.MakeupMessage();
        }

        public void MakeupCommitmentSignTx(CommitmentTx txContent)
        {
            // TODO: add method body
        }

        public void MakeupRevocableDeliverySignTx(RevocableDeliveryTx txContent)
        {
            // TODO: add method body
        }

        private void SetTransactionErrorCode(TransactionErrorCode errCode)
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
