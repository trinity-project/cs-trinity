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
        private TransactionTabelHLockPair currentHLockTransaction = null;

        private HtlcCommitTx hcTx;
        private HtlcRevocableDeliveryTx rdTx;
        private HtlcExecutionTx heTx;
        private HtlcExecutionDeliveryTx hedTx;
        private HtlcExecutionRevocableDeliveryTx heRdTx;
        private HtlcTimoutTx htTx;
        private HtlcTimeoutDeliveryTx htdTx;
        private HtlcTimeoutRevocableDelivertyTx htRdTx;

        public HtlcHandler(string sender, string receiver, string channel, string asset,
            string magic, UInt64 nonce, long payment, string hashcode, List<PathInfo> router, int role = 0)
            :base(sender, receiver, channel, asset, magic, nonce, payment, role, hashcode)
        {
            // check the channel is opened firstly
            this.CheckChannelIsOpened();

            this.isFounder = this.IsRole0(this.Request.MessageBody.RoleIndex);

            // Set Htlc header or body
            this.Request.TxNonce = this.NextNonce(channel);
            this.Request.Router = router;
            int currentIndex = this.IndexOfRouter(router, receiver);
            if (0 < currentIndex && currentIndex + 1 < router.Count)
            {
                this.Request.Next = router[currentIndex + 1].uri;
            }

            // Get record of htlc locked payment
            this.currentHLockTransaction = this.GetHLockPair();
        }

        public HtlcHandler(Htlc message, int role = 0) : base(message, role)
        {
            this.isFounder = this.IsRole0(role);

            // Set Htlc header or body
            this.Request.Router = this.onGoingRequest.Router;
            this.Request.Next = this.onGoingRequest.Next;

            // Get current transaction and locked payment record
            this.currentTransaction = this.GetCurrentTransaction<TransactionHtlcContent>();
            this.currentHLockTransaction = this.GetHLockPair();
        }

        public HtlcHandler(string message) : base(message)
        {
            // check the channel is opened firstly
            this.CheckChannelIsOpened();

            this.isFounder = this.IsRole1(this.Request.MessageBody.RoleIndex);

            this.currentTransaction = this.GetCurrentTransaction<TransactionHtlcContent>();
            this.currentHLockTransaction = this.GetHLockPair();

            // calculate balance after payment according to the flag : isFounder
            this.CalculateBalance();
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
                this.VerifyBalance(this.Request.MessageBody.Count, this.isFounder);
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

        private void AddOrUpdateHLockPair()
        {
            // Just add the locked payment with not null hashcode
            if (null == this.Request.MessageBody.HashR)
            {
                return;
            }

            // update the current lock pair
            if (null != this.currentHLockTransaction)
            {
                // update the HLockPair
                if (this.isFounder) // means this is a router point
                {
                    this.currentHLockTransaction.paymentChannel = this.Request.ChannelName;
                    this.currentHLockTransaction.payment = this.Request.MessageBody.Count;
                }
                else // means this is it's payee, it will trigger htlc to rsmc later by RResponse
                {
                    this.currentHLockTransaction.transactionType = EnumTransactionType.HTLC.ToString();
                    this.currentHLockTransaction.state = EnumTransactionState.initial.ToString();
                    this.currentHLockTransaction.router = this.Request.Router;
                    this.currentHLockTransaction.incomeChannel = this.Request.ChannelName;
                    this.currentHLockTransaction.income = this.Request.MessageBody.Count;
                    this.currentHLockTransaction.htlcNonce = this.Request.TxNonce;
                }

                this.GetChannelLevelDbEntry()?.UpdateTransactionHLockPair(this.Request.MessageBody.HashR, this.currentHLockTransaction);
            }
            else if (this.IsRole0(this.Request.MessageBody.RoleIndex))
            {
                // Just add the locked payment with hashcode when role index is zero
                // add new hlock pair for this transaction
                TransactionTabelHLockPair hLockPair = new TransactionTabelHLockPair
                {
                    transactionType = EnumTransactionType.HTLC.ToString(),
                    asset = this.Request.MessageBody.AssetType,
                    state = EnumTransactionState.initial.ToString(),
                    htlcNonce = this.Request.TxNonce,
                    router = this.Request.Router
                };

                // record the below information according to the transaction play role
                if (this.isFounder)
                {
                    hLockPair.paymentChannel = this.Request.ChannelName;
                    hLockPair.payment = this.Request.MessageBody.Count;
                }
                else
                {
                    hLockPair.incomeChannel = this.Request.ChannelName;
                    hLockPair.income = this.Request.MessageBody.Count;
                }

                this.GetChannelLevelDbEntry()?.AddTransactionHLockPair(this.Request.MessageBody.HashR, hLockPair);
            }
            else
            {

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
            this.HashR = this.Request.MessageBody.HashR;

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
            return new HtlcSignHandler(this.Request, errorCode);
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
            this.AddTransaction(this.Request.TxNonce, txContent);

            // reord the htlc lock pair
            this.AddOrUpdateHLockPair();
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
            this.UpdateTransaction(this.Request.TxNonce, this.currentTransaction);
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
            // check the channel is opened firstly
            this.CheckChannelIsOpened();

            this.isFounder = this.IsRole0(this.Request.MessageBody.RoleIndex);

            // Get the current transaction
            this.currentTransaction =
                this.GetChannelLevelDbEntry().TryGetTransaction<TransactionHtlcContent>(this.Request.TxNonce);
            this.currentHLockTransaction = this.GetHLockPair();

            // calculate balance after payment according to the flag : isFounder
            this.CalculateBalance();
        }

        public HtlcSignHandler(Htlc message, string errorCode="Ok"):base(message)
        {
            this.isFounder = this.IsRole1(this.Request.MessageBody.RoleIndex);

            // set HtlcSign header or body
            this.Request.Router = this.onGoingRequest.Router;
            this.Request.Next = this.onGoingRequest.Next;
            this.Request.Error = errorCode;
            
            // Get the current transaction
            this.currentTransaction = this.GetCurrentTransaction<TransactionHtlcContent>();
            this.currentHLockTransaction = this.GetHLockPair();
        }

        public override bool Handle()
        {
            if (!base.Handle())
            {
                return false;
            }

            // Trigger htlc to next peer
            if (this.IsRole0(this.Request.MessageBody.RoleIndex))
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

            if (IsRole0(this.Request.MessageBody.RoleIndex))
            {
                this.VerifyBalance(this.Request.MessageBody.Count, this.isFounder);
                this.VerifyNonce(this.CurrentNonce(this.Request.ChannelName));
            }
            else
            {
                this.VerifyNonce(this.NextNonce(this.Request.ChannelName));
            }

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
        private ChannelTableContent ChooseChannel(string peer, long payment)
        {
            return this.GetChannelLevelDbEntry()?.GetChannel(peer, payment, EnumChannelState.OPENED.ToString());
        }

        private void TriggerHtlcToNextPeer()
        {
            // trigger htlc to next peer
            if (this.IsReachedPayee(this.Request.Router, out int currentUriIndex))
            {
                Log.Info("Htlc with HashR<{0}> has been finished since the payee has received the payment: {1}.",
                    this.Request.MessageBody.HashR, this.Request.MessageBody.Count);

                // Trigger RResponse to peer wallet
                RResponseHandler RResponseHndl = new RResponseHandler(this.Request.Receiver, this.Request.Sender, this.Request.ChannelName,
                    this.Request.MessageBody.AssetType, this.Request.NetMagic, this.Request.TxNonce, this.Request.MessageBody.Count,
                    this.Request.MessageBody.HashR, this.currentHLockTransaction.rcode);
                RResponseHndl.MakeTransaction();
                return;
            }
            else if (0 > currentUriIndex)
            {
                Log.Error("Current wallet uri not found. Uri: {0}, Router: {1}", this.GetUri(), this.Request.Router);
                return;
            }

            string nextPeerUri = this.Request.Router[currentUriIndex + 1].uri;
            long payment = this.CalculatePayment(Fixed8.Parse(this.Request.Router[currentUriIndex].fee.ToString()).GetData()) ;

            // Get the channel for next htlc
            ChannelTableContent nextChannel = this.ChooseChannel(nextPeerUri, payment);
            if (null != nextChannel)
            {
                // trigger new htlc
                HtlcHandler htlcHndl = new HtlcHandler(
                    this.GetUri(), nextPeerUri, nextChannel.channel, this.Request.MessageBody.AssetType,
                    this.Request.NetMagic, 0, payment, this.Request.MessageBody.HashR, this.Request.Router);
                htlcHndl.MakeTransaction();
            }
            else
            {
                Log.Error("Could not find the channel for HTLC with HashR: {0}", this.Request.MessageBody.HashR);
            }

            return;
        }

        private long CalculatePayment(long fee = 1000000)
        {
            // default fee is 0.01
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
                RoleIndex = this.onGoingRequest.MessageBody.RoleIndex,
                HashR = this.onGoingRequest.MessageBody.HashR,
            };
        }

        public override void SetLocalsFromBody()
        {
            // RoleIndex Related
            this.RoleMax = 1;
            this.currentRole = this.Request.MessageBody.RoleIndex; // record current role Index
            this.HashR = this.Request.MessageBody.HashR;

            // Asset type from message body for adaptor old version trinity
            this.Request.AssetType = this.Request.MessageBody.AssetType;
        }

        public override void CalculateBalance()
        {
            base.CalculateBalance(this.Request.MessageBody.Count, this.isFounder, true);
        }

        public override void UpdateTransaction()
        {
            if ((this.IsRole0(this.Request.MessageBody.RoleIndex) && this.isFounder) ||
                (this.IsRole1(this.Request.MessageBody.RoleIndex) && !this.isFounder))
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
