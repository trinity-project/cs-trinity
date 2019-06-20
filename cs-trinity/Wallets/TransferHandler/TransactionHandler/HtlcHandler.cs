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
        private bool isFounder = false;

        // current transaction by nonce
        private TransactionHtlcContent currentTransaction = null;

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

            this.isFounder = this.IsRole0(this.Request.MessageBody.RoleIndex);
        }

        public HtlcHandler(string message) : base(message)
        {
            this.isFounder = this.IsRole1(this.Request.MessageBody.RoleIndex);

            if (this.IsRole1(this.Request.MessageBody.RoleIndex))
            {
                this.currentTransaction = this.GetCurrentTransaction<TransactionHtlcContent>();
            }
        }

        public HtlcHandler(Htlc message, int role=0) : base(message, role)
        {
            this.Request.Router = this.onGoingRequest.Router;
            this.Request.Next = this.onGoingRequest.Next;
            this.isFounder = this.IsRole0(role);

            if (this.IsRole1(role))
            {
                this.currentTransaction = this.GetCurrentTransaction<TransactionHtlcContent>();
            }
        }
        
        public override bool SucceedStep()
        {
            if (base.SucceedStep())
            {
                Log.Info("Succeed handling Htlc. Channel: {0}, AssetType: {1}, Payment: {2}. Balance: {3}. PeerBalance: {4}, RoleIndex: {5}.",
                    this.Request.ChannelName, this.Request.AssetType, this.Request.MessageBody.Count,
                    this.SelfBalance(), this.PeerBalance(), this.Request.MessageBody.RoleIndex);

                return true;
            }

            return false;
        }

        public override bool FailStep(string errorCode)
        {
            Log.Error("Failed handling Htlc. Channel: {0}, AssetType: {1}, Payment: {2}. Balance: {3}. PeerBalance: {4}, RoleIndex: {5}. Error: {6}",
                    this.Request.ChannelName, this.Request.AssetType, this.Request.MessageBody.Count,
                    this.SelfBalance(), this.PeerBalance(), this.Request.MessageBody.RoleIndex, errorCode);
            return base.FailStep(errorCode);
        }

        public override bool MakeTransaction()
        {
            if (base.MakeTransaction())
            {
                Log.Info("Send Htlc. Channel: {0}, AssetType: {1}, Payment: {2}. Balance: {3}. PeerBalance: {4}, RoleIndex: {5}.",
                    this.Request.ChannelName, this.Request.AssetType, this.Request.MessageBody.Count,
                    this.SelfBalance(), this.PeerBalance(), this.Request.MessageBody.RoleIndex);

                return true;
            }

            return false;
        }

        public override bool Verify()
        {
            this.VerifyUri();
            this.VerifyRoleIndex();
            this.VerifyAssetType(this.Request.MessageBody.AssetType);

            if (this.IsRole0(this.Request.MessageBody.RoleIndex))
            {
                this.VerifyNonce(this.NextNonce(this.Request.ChannelName));
            }
            else
            {
                this.VerifyNonce(this.CurrentNonce(this.Request.ChannelName));
            }

            return true;
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
            }
            else
            {
                // TODO: maybe we need do some exception info to show in the message box???
            }

            return true;
        }

        private void AddHLockPair(bool isFounder)
        {
            // add new hlock pair for this transaction
            TransactionTabelHLockPair hLockPair = new TransactionTabelHLockPair
            {
                transactionType = EnumTransactionType.HTLC.ToString(),
                state = EnumTransactionState.initial.ToString(),
                htlcNonce = this.Request.TxNonce,
                router = this.Request.Router
            };

            // record the below information according to the transaction play role
            if (isFounder)
            {
                hLockPair.paymentChannel = this.Request.ChannelName;
                hLockPair.payment = this.Request.MessageBody.Count;
            }
            else
            {
                hLockPair.incomeChannel = this.Request.ChannelName;
                hLockPair.income = this.Request.MessageBody.Count;
            }
        }

        #region Htlc_OVERRIDE_VIRUAL_SETS_OF_DIFFERENT_TRANSACTION_HANDLER
        public override void InitializeBlockChainApi() { this.GetBlockChainAdaptorApi(false); }

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
            this.Request.AssetType = this.Request.MessageBody.AssetType;
        }

        public override void SetTransactionValid()
        {
            this.isHtlcValid = true;
        }

        public override void CalculateBalance()
        {
            base.CalculateBalance(this.Request.MessageBody.Count, this.isFounder, true);
        }

        public override HtlcHandler CreateRequestHndl(int role)
        {
            return new HtlcHandler(this.Request, role);
        }

        public override HtlcSignHandler CreateResponseHndl(string errorCode = "Ok")
        {
            return new HtlcSignHandler(this.onGoingRequest, errorCode);
        }

        public override void AddTransaction(bool isFounder = false)
        {
            // Just add the transaction when role index is zero
            if (!IsRole0(this.Request.MessageBody.RoleIndex))
            {
                return;
            }

            // Add transaction to leveldb
            TransactionHtlcContent txContent = this.NewTransactionContent<TransactionHtlcContent>(isFounder);
            // HCTX = self.hctx, HEDTX=self.hedtx, HTTX= self.httx
            txContent.type = EnumTransactionType.HTLC.ToString();
            txContent.payment = this.Request.MessageBody.Count;
            txContent.hashcode = this.Request.MessageBody.HashR;
            txContent.HEDTX = this.Request.MessageBody.HEDTX;
            txContent.HCTX = new TxContentsSignGeneric<HtlcCommitTx>();
            txContent.RDTX = new TxContentsSignGeneric<HtlcRevocableDeliveryTx>();

            // record self transaction contents, wait for signature.
            if (isFounder)
            {
                txContent.HCTX.originalData = this.Request.MessageBody.HCTX;
                txContent.RDTX.originalData = this.Request.MessageBody.RDTX;

                txContent.HTTX = new TxContentsSignGeneric<HtlcTimoutTx>
                {
                    originalData = this.Request.MessageBody.HTTX
                };

                txContent.HTRDTX = new TxContentsSignGeneric<HtlcTimeoutRevocableDelivertyTx>
                {
                    originalData = this.Request.MessageBody.HTRDTX
                };
            }

            // record the transaction to levelDB
            this.GetChannelLevelDbEntry()?.AddTransaction(this.Request.TxNonce, txContent);

            // reord the htlc lock pair
            this.AddHLockPair(isFounder);
        }

        public override void UpdateTransaction()
        {
            // when role 0, just add the transaction
            if (IsRole0(this.Request.MessageBody.RoleIndex))
            {
                return;
            }

            this.currentTransaction.state = EnumTransactionState.confirming.ToString();
            this.currentTransaction.HETX = this.Request.MessageBody.HETX;
            this.currentTransaction.HERDTX = this.Request.MessageBody.HERDTX;

            // update the transaction
            if (!this.currentTransaction.isFounder)
            {
                this.currentTransaction.HCTX.originalData = this.Request.MessageBody.HCTX;
                this.currentTransaction.RDTX.originalData = this.Request.MessageBody.RDTX;

                this.currentTransaction.HTDTX = new TxContentsSignGeneric<HtlcTimeoutDeliveryTx>
                {
                    originalData = this.Request.MessageBody.HTDTX,
                };
            }

            // update the transaction
            this.GetChannelLevelDbEntry()?.UpdateTransaction(this.Request.TxNonce, this.currentTransaction);
        }
        #endregion //Htlc_OVERRIDE_VIRUAL_SETS_OF_DIFFERENT_TRANSACTION_HANDLER
    }


    ///////////////////////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////////////////////////
    /// HtlcSignHandler start
    /// ///////////////////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Class Handler for handling HtlcSign Message
    /// </summary>
    public class HtlcSignHandler : TransactionHandler<HtlcSign, Htlc, HtlcHandler, HtlcFailHandler>
    {
        private bool isFounder = false;

        private TransactionHtlcContent currentTransaction = null;
        private TransactionTabelHLockPair currentHLockTransaction = null;

        public HtlcSignHandler(string message) : base(message)
        {
            this.isFounder = this.IsRole0(this.Request.MessageBody.RoleIndex);

            // Get the current transaction
            this.currentTransaction = this.GetCurrentTransaction<TransactionHtlcContent>();
            this.currentHLockTransaction = this.GetHLockPair();
        }

        public HtlcSignHandler(Htlc message, string errorCode="Ok"):base(message)
        {
            this.Request.Router = this.onGoingRequest.Router;
            this.Request.Next = this.onGoingRequest.Next;
            this.Request.Error = errorCode;
            this.isFounder = this.IsRole1(this.Request.MessageBody.RoleIndex);

            if (this.IsRole1(this.Request.MessageBody.RoleIndex))
            {   
                // Get the current transaction
                this.currentTransaction = this.GetCurrentTransaction<TransactionHtlcContent>();
                this.currentHLockTransaction = this.GetHLockPair();
            }
        }

        public override bool Handle()
        {
            if (!base.Handle())
            {
                return false;
            }

            // Trigger htlc to next peer
            if (this.IsRole1(this.Request.MessageBody.RoleIndex))
            {
                this.TriggerHtlcToNextPeer();
            }

            return true;
        }
        
        public override bool SucceedStep()
        {
            if (base.SucceedStep())
            {
                Log.Info("Succeed handling HtlcSign. Channel: {0}, AssetType: {1}, Payment: {2}. Balance: {3}. PeerBalance: {4}, RoleIndex: {5}.",
                    this.Request.ChannelName, this.Request.AssetType, this.Request.MessageBody.Count,
                    this.SelfBalance(), this.PeerBalance(), this.Request.MessageBody.RoleIndex);
                return true;
            }

            return false;
        }

        public override bool FailStep(string errorCode)
        {
            Log.Error("Succeed handling HtlcSign. Channel: {0}, AssetType: {1}, Payment: {2}. Balance: {3}. PeerBalance: {4}, RoleIndex: {5}. Error: {6}.",
                    this.Request.ChannelName, this.Request.AssetType, this.Request.MessageBody.Count,
                    this.SelfBalance(), this.PeerBalance(), this.Request.MessageBody.RoleIndex, errorCode);
            return base.FailStep(errorCode);
        }

        public override bool MakeTransaction()
        {
            if (base.MakeTransaction())
            {
                Log.Info("Send HtlcSign. Channel: {0}, AssetType: {1}, Payment: {2}. Balance: {3}. PeerBalance: {4}, RoleIndex: {5}.",
                    this.Request.ChannelName, this.Request.AssetType, this.Request.MessageBody.Count,
                    this.SelfBalance(), this.PeerBalance(), this.Request.MessageBody.RoleIndex);
            }
            
            return false;
        }

        public override bool Verify()
        {
            this.VerifyUri();
            this.VerifyRoleIndex();
            this.VerifyAssetType(this.Request.MessageBody.AssetType);
            this.VerifyNonce(this.CurrentNonce(this.Request.ChannelName));

            return true;
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

        // ToDo: used in future
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

        private void TriggerHtlcToNextPeer()
        {
            if (!this.IsRole1(this.Request.MessageBody.RoleIndex))
            {
                return;
            }

            // trigger htlc to next peer
            if (this.IsPayee(out int currentUriIndex))
            {
                Log.Info("Htlc with HashR<{0}> has been finished since the payee has received the payment: {1}.",
                    this.Request.MessageBody.HashR, this.Request.MessageBody.Count);
                return;
            }

            string nextPeerUri = this.Request.Router[currentUriIndex + 1];
            long payment = this.CalculatePayment();

            // Get the channel for next htlc
            string channelName = this.ChooseChannel(nextPeerUri, payment);
            if (null != channelName)
            {
                // trigger new htlc
                HtlcHandler htlcHndl = new HtlcHandler(
                    this.GetUri(), nextPeerUri, channelName, this.Request.MessageBody.AssetType,
                    this.Request.NetMagic, 0, payment, this.Request.MessageBody.HashR, this.Request.Router);
            }
            else
            {
                Log.Error("Could not find the channel for HTLC with HashR: {0}", this.Request.MessageBody.HashR);
            }

            return;
        }

        private bool IsPayee(out int currentUriIndex)
        {
            // for adapting the old trinity, here we have to use complicated logics
            currentUriIndex = this.Request.Router.IndexOf(this.GetUri());
            if (2 >= this.Request.Router.Count - currentUriIndex)
            {
                Log.Info("HTLC with HashR<{0}> finished since the payee has received the payment");
                return true;
            }

            return false;
        }
        
        private long CalculatePayment()
        {
            // use Hardcode Fee this time
            long fee = 100000; // 0.01
            return this.Request.MessageBody.Count - fee;
        }

        #region HtlcSign_OVERRIDE_VIRUAL_SETS_OF_DIFFERENT_TRANSACTION_HANDLER
        public override void InitializeBlockChainApi() { this.GetBlockChainAdaptorApi(false); }
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
            this.Request.AssetType = this.Request.MessageBody.AssetType;
        }

        public override void CalculateBalance()
        {
            base.CalculateBalance(this.Request.MessageBody.Count, this.isFounder, true);
        }

        public override void UpdateTransaction()
        {
            if ((this.IsRole0(this.Request.MessageBody.RoleIndex) && this.currentTransaction.isFounder) ||
                (this.IsRole1(this.Request.MessageBody.RoleIndex) && !this.currentTransaction.isFounder))
            {
                this.currentTransaction.HCTX.txDataSign = this.Request.MessageBody.HCTX.txDataSign;
                this.currentTransaction.RDTX.txDataSign = this.Request.MessageBody.RDTX.txDataSign;
                this.currentTransaction.state = EnumTransactionState.confirmed.ToString();

                if (IsRole0(this.Request.MessageBody.RoleIndex))
                {
                    this.currentTransaction.HTTX.txDataSign = this.Request.MessageBody.HTTX.txDataSign;
                    this.currentTransaction.HTRDTX.txDataSign = this.Request.MessageBody.HTRDTX.txDataSign;
                }
                else if (IsRole1(this.Request.MessageBody.RoleIndex))
                {
                    this.currentTransaction.HTDTX.txDataSign = this.Request.MessageBody.HTDTX.txDataSign;
                }

                // update the HLockPair if needed
                if (null != this.currentHLockTransaction)
                {
                    this.UpdateHLockPair(EnumTransactionState.confirming);
                }

                this.GetChannelLevelDbEntry().UpdateTransaction(this.Request.TxNonce, this.currentTransaction);

                // update the channel balance
                this.UpdateChannelBalance();
            }
        }

        public override void UpdateChannelSummary()
        {
            this.RecordChannelSummary();
        }

        public override void AddTransactionSummary()
        {
            if (this.IsRole0(this.Request.MessageBody.RoleIndex) || this.IsRole1(this.Request.MessageBody.RoleIndex))
            {
                // Add transaction summary for monitoring
                this.AddTransactionSummary(this.Request.TxNonce, this.Request.MessageBody.HCTX.originalData.txId,
                    this.Request.ChannelName, EnumTransactionType.COMMITMENT);

                this.AddTransactionSummary(this.Request.TxNonce, this.Request.MessageBody.RDTX.originalData.txId,
                    this.Request.ChannelName, EnumTransactionType.REVOCABLE);
            }
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
