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
    public class HtlcHandler : TransactionHandler<Htlc, Htlc, HtlcHandler, HtlcSignHandler>
    {
        // Locals for Htlc trade
        private bool isHtlcValid = false;

        private HtlcCommitTx hcTx;
        private HtlcRevocableDeliveryTx rdTx;
        private HtlcExecutionTx heTx;
        private HtlcExecutionDeliveryTx hedTx;
        private HtlcExecutionRevocableDeliveryTx heRdTx;
        private HtlcTimoutTx htTx;
        private HtlcTimeoutDeliveryTx htdTx;
        private HtlcTimeoutRevocableDelivertyTx htRdTx;

        public HtlcHandler(string sender, string receiver, string channel, string asset,
            string magic, UInt64 nonce, long payment, string hashcode, List<string> router, int role = 0)
            :base(sender, receiver, channel, asset, magic, nonce, payment, role, hashcode)
        {
            this.Request.Router = router;
            this.Request.Next = router?[router.IndexOf(receiver) + 1];
        }

        public HtlcHandler(string message) : base(message) { }

        public HtlcHandler(Htlc message, int role=0) : base(message, role)
        {
            this.Request.Router = this.onGoingRequest.Router;
            this.Request.Next = this.onGoingRequest.Next;
        }

        public override bool Handle()
        {
            Log.Debug("Handle Message {0}. Channel name {1}, Asset Type: {2}, Payment: {3}. Balance: {4}. PeerBalance: {5}",
                this.Request.MessageType,
                this.Request.ChannelName,
                this.Request.MessageBody.AssetType,
                this.Request.MessageBody.Count,
                this.GetCurrentChannel().balance, this.GetCurrentChannel().peerBalance);

            if (!base.Handle())
            {
                return false;
            }

            // Add txid for monitor
            this.AddTransactionSummary(this.Request.TxNonce, this.Request.MessageBody.HCTX.txId,
                this.Request.ChannelName, EnumTransactionType.FUNDING);

            this.AddTransactionSummary(this.Request.TxNonce, this.Request.MessageBody.RDTX.txId,
                this.Request.ChannelName, EnumTransactionType.COMMITMENT);

            return true;
        }

        public override bool SucceedStep()
        {
            bool ret = base.SucceedStep();
            if (ret && this.IsRole1(this.Request.MessageBody.RoleIndex))
            {
                // update 
                this.AddTransaction(true);
            };

            return ret;
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
                    this.Request.MessageBody.Count, 
                    this.GetCurrentChannel().balance, this.GetCurrentChannel().peerBalance);
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
            foreach (ChannelTableContent channel in this.GetChannelLevelDbEntry()?.GetChannelListOfThisWallet())
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

        #region Htlc_OVERRIDE_VIRUAL_SETS_OF_DIFFERENT_TRANSACTION_HANDLER
        public override void InitializeMessageBody(string asset, long payment, int role = 0, string hashcode = null, string rcode = null)
        {
            this.Request.MessageBody = new HtlcBody
            {
                AssetType = asset,
                Count = payment,
                RoleIndex = role,
                HashR = hashcode,
            };
        }

        public override void InitializeMessageBody(int role = 0)
        {
            this.Request.MessageBody = new HtlcBody
            {
                AssetType = this.onGoingRequest.MessageBody.AssetType,
                Count = this.onGoingRequest.MessageBody.Count,
                RoleIndex = role,
                HashR = this.onGoingRequest.MessageBody.HashR,
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

        public override void SetTransactionValid()
        {
            this.isHtlcValid = true;
        }

        public override long[] CalculateBalance(long balance, long peerBalance)
        {
            return this.CalculateBalance(this.Request.MessageBody.RoleIndex, balance, peerBalance, 
                this.Request.MessageBody.Count, false, false);
        }

        public override HtlcHandler CreateRequestHndl(int role)
        {
            return new HtlcHandler(this.Request, role);
        }

        public override HtlcSignHandler CreateResponseHndl(string errorCode = "Ok")
        {
            return new HtlcSignHandler(this.onGoingRequest, errorCode);
        }
        #endregion //Htlc_OVERRIDE_VIRUAL_SETS_OF_DIFFERENT_TRANSACTION_HANDLER
    }

    /// <summary>
    /// Class Handler for handling HtlcSign Message
    /// </summary>
    public class HtlcSignHandler : TransactionHandler<HtlcSign, Htlc, HtlcHandler, HtlcFailHandler>
    {
        public HtlcSignHandler(string message) : base(message) { }

        public HtlcSignHandler(Htlc message, string errorCode="Ok"):base(message)
        {
            this.Request.Error = errorCode;
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
            this.Request.MessageBody.HCTX = this.MakeupSignature(this.onGoingRequest.MessageBody.HCTX);
            this.Request.MessageBody.RDTX = this.MakeupSignature(this.onGoingRequest.MessageBody.RDTX);

            // Start to sign the messages
            if (IsRole0(this.Request.MessageBody.RoleIndex))
            {
                this.Request.MessageBody.HTTX = this.MakeupSignature(this.onGoingRequest.MessageBody.HTTX);
                this.Request.MessageBody.HTRDTX = this.MakeupSignature(this.onGoingRequest.MessageBody.HTRDTX);
            }
            else if (IsRole1(this.Request.MessageBody.RoleIndex))
            {
                this.Request.MessageBody.HTDTX = this.MakeupSignature(this.onGoingRequest.MessageBody.HTDTX);
            }
            else
            {
                Log.Error("Error Role: {0} for htlc transaction:", this.Request.MessageBody.RoleIndex);
            }

            return base.MakeupMessage();
        }

        #region Htlc_OVERRIDE_VIRUAL_SETS_OF_DIFFERENT_TRANSACTION_HANDLER
        public override void InitializeMessageBody(int role = 0)
        {
            this.Request.MessageBody = new HtlcSignBody
            {
                AssetType = this.onGoingRequest.MessageBody.AssetType,
                Count = this.onGoingRequest.MessageBody.Count,
                RoleIndex = role,
                HashR = this.onGoingRequest.MessageBody.HashR,
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

        public override long[] CalculateBalance(long balance, long peerBalance)
        {
            return this.CalculateBalance(this.Request.MessageBody.RoleIndex, balance, peerBalance,
                this.Request.MessageBody.Count, false, false);
        }
        #endregion //HtlcSign_OVERRIDE_VIRUAL_SETS_OF_DIFFERENT_TRANSACTION_HANDLER
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////
    /// HtlcFailHandler start
    ///////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Class Handler for handling HtlcFail Message
    /// </summary>
    public class HtlcFailHandler : TransactionHandler<HtlcFail, VoidTransactionMessage, VoidHandler, VoidHandler>
    {
        public HtlcFailHandler(string message) : base(message)
        {
        }

        public override bool Handle()
        {
            Log.Error("{0}: failed to make htlc transaction. Channel: {1}, AssetType: {2}. Error: {3}",
                this.Request.MessageType, this.Request.ChannelName, this.Request.MessageBody.AssetType, this.Request.Error);

            return true;
        }

        
        #region HtlcFail_OVERRIDE_VIRUAL_SETS_OF_DIFFERENT_TRANSACTION_HANDLER
        // Not need initialize the BlockChain API
        public override void InitializeBlockChainApi() { }
        #endregion
    }
}
