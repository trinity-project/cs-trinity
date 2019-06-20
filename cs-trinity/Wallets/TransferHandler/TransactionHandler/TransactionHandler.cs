﻿/*
Author: Trinity Core Team

MIT License

Copyright (c) 2018 Trinity

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
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
using Trinity;
using Trinity.BlockChain;
using Trinity.Wallets.Templates.Definitions;
using Trinity.Wallets.Templates.Messages;
using Trinity.ChannelSet;
using Trinity.ChannelSet.Definitions;
using Trinity.TrinityDB.Definitions;
using Trinity.Exceptions.WalletError;


namespace Trinity.Wallets.TransferHandler.TransactionHandler
{
    /// <summary>
    /// Generic Class for handling the Transaction plane messages.
    /// Parameters details refer to the description in TransferHandler 
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    /// <typeparam name="TRMessage"></typeparam>
    /// <typeparam name="TRQHandler"></typeparam>
    /// <typeparam name="TRSPHanlder"></typeparam>
    public abstract class TransactionHandler<TMessage, TRMessage, TRQHandler, TRSPHanlder>
        : TransferHandler<TMessage, TRMessage, TRQHandler, TRSPHanlder>
        where TMessage : TransactionHeader, new()
        where TRMessage : TransactionHeader
        where TRQHandler : TransferHandlerBase
        where TRSPHanlder : TransferHandlerBase
    {
        // LevelDB entry
        private TransactionFundingContent fundingTrade = null;
        private TransactionTabelHLockPair hlockTrade = null;
        private Channel channelDBEntry = null;
        private ChannelSummaryContents currentChannelSummary = null;
        private TransactionTabelHLockPair currentHLockTransaction = null;
        protected ChannelTableContent currentChannel = null;


        // BlockChain transaction api
        protected NeoTransaction neoTransaction = null;

        // nonce for transaction
        // funding transaction nonce should be always zero.
        public const ulong fundingNonce = 0;

        // Local variables members for initialization steps
        private string selfUri = null;
        private string peerUri = null;
        private long balance = 0;
        private long peerBalance = 0;
        private UInt64 latestNonce = 0;
        protected string channelName = null;

        // record current role
        protected int currentRole = -1;
        protected string HashR = null;

        /// <summary>
        /// Default Constructor
        /// </summary>
        public TransactionHandler() : base()
        {
        }

        public TransactionHandler(string message) : base(message)
        {
            // LevelDB API & channel related intialiazation
            this.InitializeLocals(true); // Common local variables intializtion
            this.InitializeLevelDBApi();    // LevelDB API & channel related intialization
        }

        public TransactionHandler(TRMessage request, int role=0) : base()
        {
            this.onGoingRequest = request;

            // Allocate new request and intialize the header & body
            this.InitializeMessage(request.Receiver, request.Sender, request.ChannelName,
                request.AssetType, request.NetMagic, request.TxNonce);
            this.InitializeMessageBody(role);

            // Initialize the components for this class
            this.InitializeLocals(); // Common local variables intializtion
            this.InitializeLevelDBApi();    // LevelDB API & channel related intialization
            this.InitializeBlockChainApi(); // BlockChain API initialization
        }

        public TransactionHandler(string sender, string receiver, string channel, string asset,
            string magic, UInt64 nonce, long payment, int role = 0, string hashcode=null, string rcode=null) : base()
        {
            // Allocate new request and intialize the header & body
            this.InitializeMessage(sender, receiver, channel, asset, magic, nonce);
            this.InitializeMessageBody(asset, payment, role, hashcode, rcode);

            // Initialize the components for this class
            this.InitializeLocals(); // Common local variables intializtion
            this.InitializeLevelDBApi();    // LevelDB API & channel related intialization
            this.InitializeBlockChainApi(); // BlockChain API initialization
        }

        public override bool SucceedStep()
        {
            #region New_TRSPHandler_If_Needed
            if (this.IsRole0(this.currentRole) || this.IsRole1(this.currentRole))
            {
                this.MakeResponse();
            }
            #endregion New_TRSPHandler_If_Needed

            // Add transaction to database only when role is 0
            this.AddOrUpdateTransaction(false);

            // Terminate Transaction when current role index exceeds ths RoleMax
            if (this.IsTerminatedRole(this.currentRole, out int newRole))
            {
                Log.Info("{0} Terminated. RoleIndex: {1}", this.Request.MessageType, this.currentRole);
                return true;
            }
            #region Trigger_Request_New_Step
            else
            {
                this.MakeRequest(newRole);
            }
            #endregion // Trigger_Request_New_Step

            return true;
        }

        public override bool MakeTransaction()
        {
            this.AddOrUpdateTransaction(true);
            return base.MakeTransaction();
        }

        //
        private void AddOrUpdateTransaction(bool isFounder)
        {
            if (this.IsRole0(this.currentRole))
            {
                this.AddTransaction(isFounder);
                // update channel to latest nonce
                this.UpdateChannelSummary();
            }
            
            this.UpdateTransaction();
        }

        /// <summary>
        /// Virtual Method sets. Should be overwritten in child classes.
        /// </summary>
        /// <returns></returns>
        /// Verification method sets
        public virtual bool VerifyNonce(UInt64 expectedNonce)
        {
            if (this.Request.TxNonce == expectedNonce)
            {
                return true;
            }

            throw new TransactionException(
                    EnumTransactionErrorCode.Incompatible_Nonce,
                    string.Format("{0} : Nonce of peers are incompatible. Nonce: {1}, PeerNonce: {2}.",
                            this.Request.MessageType, expectedNonce, this.Request.TxNonce),
                    this.Request.MessageType);
        }

        public virtual bool VerifyDeposit(long deposit)
        {
            // TODO: add max limitation
            if (0 < deposit)
            {
                return true;
            }

            throw new TransactionException(
                    EnumTransactionErrorCode.Incompatible_Nonce,
                    string.Format("{0} : Deposit exceeds the limitition. Channel: {1}, Deposit: {2}.",
                            this.Request.MessageType, this.Request.ChannelName, deposit),
                    this.Request.MessageType);
        }

        public virtual bool VerifyBalance() { return true; }
        public virtual bool VerifyRoleIndex()
        { 
            if (IsIllegalRole(this.currentRole))
            {
                throw new TransactionException(
                    EnumTransactionErrorCode.Role_Index_Out_Of_Range,
                    string.Format("{0} : current RoleIndex<{0}> is out of range.", this.Request.MessageType, this.currentRole),
                    this.Request.MessageType) ;
            }

            return true;
        }

        public virtual bool VerifySignarture(string content, string contentSign)
        {
            if (!NeoInterface.VerifySignature(content, contentSign, this.GetPeerPubKey()))
            {
                throw new TransactionException(
                    EnumTransactionErrorCode.Transaction_With_Wrong_Signature,
                    string.Format("{0} : Transaction signature is error. Channel: {1}, nonce: {2}", 
                        this.Request.MessageType, this.Request.ChannelName, this.Request.TxNonce),
                    this.Request.MessageType);
            }

            return true;
        }

        public override bool VerifyUri()
        {
            if (!this.selfUri.Equals(this.currentChannel.uri))
            {
                throw new TransactionException(EnumTransactionErrorCode.Invalid_Url,
                    String.Format("{0}: the receiver is not me! uri: {1}", this.Request.ChannelName, this.selfUri),
                    EnumTransactionErrorBase.COMMON.ToString());
            }

            return true;
        }

        public void UpdateChannelState(EnumChannelState state)
        {
            ChannelTableContent channelContent = this.GetCurrentChannel();

            // Update the channel state
            channelContent.state = state.ToString();
            this.channelDBEntry?.UpdateChannel(channelName, channelContent);
        }

        public void UpdateChannelBalance()
        {
            // Update just only when the current transaction is confirmed state
            ChannelTableContent channelContent = this.GetCurrentChannel();

            // Update the channel balance of peers
            channelContent.balance = this.balance;
            channelContent.peerBalance = this.peerBalance;
            this.channelDBEntry?.UpdateChannel(channelName, channelContent);
        }

        public void RecordChannelSummary()
        {
            ChannelSummaryContents txContent = new ChannelSummaryContents
            {
                nonce = this.Request.TxNonce,
                peer = this.peerUri,
                type = null
            };

            this.channelDBEntry?.UpdateChannelSummary(this.Request.ChannelName, txContent);
        }

        public void AddTransactionSummary(UInt64 nonce, string txId, string channel, EnumTransactionType type)
        {
            TransactionTabelSummary txContent = new TransactionTabelSummary
            {
                nonce = nonce,
                channel = channel,
                txType = type.ToString()
            };

            this.channelDBEntry?.AddTransaction(txId, txContent);
        }

        public UInt64 CurrentNonce(string channel)
        {
            this.currentChannelSummary = this.currentChannelSummary ?? this.GetChannelLevelDbEntry()?.TryGetChannelSummary(channel);
            return this.currentChannelSummary.nonce;
        }

        public UInt64 NextNonce(string channel)
        {
            return this.CurrentNonce(channel) + 1;
        }

        protected JObject BroadcastTransaction(string txData, string peerTxDataSignarture, string witness)
        {
            string txDataSignarture = this.Sign(txData);
            witness = witness.Replace("{signOther}", txDataSignarture).Replace("{signSelf}", peerTxDataSignarture);

            // Broadcast the transaction by calling rpc interface
            return NeoInterface.SendRawTransaction(txData + witness);
        }

        public long SelfBalance() { return this.balance; }
        public long PeerBalance() { return this.peerBalance; }

        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="payment"></param>
        /// <param name="isFounder"></param>
        /// <param name="isHtlc"></param>
        protected void CalculateBalance(long payment, bool isFounder, bool isHtlc=false)
        {
            if (isFounder)
            {
                if (isHtlc)
                {
                    this.balance -= payment;
                }
                else if (this.IsHtlcToRsmc())
                {
                    this.peerBalance += payment;
                }
                else
                {
                    this.balance -= payment;
                    this.peerBalance += payment;
                }
            }
            else
            {
                if (isHtlc)
                {
                    this.peerBalance -= payment;
                }
                else if (this.IsHtlcToRsmc())
                {
                    this.balance += payment;
                }
                else
                {
                    this.peerBalance -= payment;
                    this.balance += payment;
                }
            }
        }

        private bool IsHtlcToRsmc()
        {
            return EnumTransactionType.HTLC.ToString().Equals(this.currentHLockTransaction?.transactionType);
        }

        /// <summary>
        /// Trinity Transaction Role define here.
        /// </summary>
        private readonly int Role0 = 0;
        private readonly int Role1 = 1;
        private readonly int Role2 = 2;
        private readonly int Role3 = 3;
        protected int RoleMax = 3;

        public bool IsIllegalRole(int role)
        {
            return Role0 > role || RoleMax < role;
        }

        public bool IsRole0(int role)
        {
            return role.Equals(Role0);
        }

        public bool IsRole1(int role)
        {
            return role.Equals(Role1);
        }

        public bool IsRole2(int role)
        {
            return role.Equals(Role2);
        }

        public bool IsRole3(int role)
        {
            return role.Equals(Role3);
        }

        public bool IsTerminatedRole(int role, out int newRole)
        {
            newRole = role + 1;
            return this.IsIllegalRole(newRole);
        }

        public string GetUri()
        {
            return this.selfUri;
        }

        public string GetPeerUri()
        {
            return this.peerUri;
        }

        public TTransactionContent NewTransactionContent<TTransactionContent>(bool isFounder = false)
            where TTransactionContent : TransactionTabelContent, new()
        {
            return new TTransactionContent
            {
                nonce = this.Request.TxNonce,
                balance = this.balance,
                peerBalance = this.peerBalance,
                role = this.currentRole,
                isFounder = isFounder,
                state = EnumTransactionState.initial.ToString(),
            };
        }

        public TransactionTabelHLockPair GetHLockPair()
        {
            if (null != this.HashR)
            {
                this.currentHLockTransaction = this.currentHLockTransaction ??
                    this.GetChannelLevelDbEntry()?.TryGetTransactionHLockPair(this.HashR);
            }
            return this.currentHLockTransaction;
        }

        public void UpdateHLockPair(EnumTransactionState state, UInt64 nonce=0)
        {
            if (null == this.currentHLockTransaction)
            {
                return;
            }

            // update the rsmc nonce and state
            this.currentHLockTransaction.rsmcNonce = nonce;
            this.currentHLockTransaction.state = state.ToString();
            this.GetChannelLevelDbEntry()?.AddTransactionHLockPair(this.HashR, this.currentHLockTransaction);

        }

        #region VIRUAL_SETS_OF_DIFFERENT_TRANSACTION_HANDLER
        //////////////////////////////////////////////////////////////////////////////////
        /// Start of virtual methods for different actions:
        /// 
        /// One set of methods are used to be overwritten for initialize transaction handler
        /// instances with different actions.
        //////////////////////////////////////////////////////////////////////////////////

        public virtual void InitializeMessage(string sender, string receiver, string channel, string asset,
            string magic, ulong nonce)
        {
            this.Request = new TMessage
            {
                Sender = sender,
                Receiver = receiver,
                ChannelName = channel,
                AssetType = asset,
                NetMagic = magic ?? this.GetNetMagic(),
                TxNonce = this.latestNonce
            };
        }

        public virtual void InitializeMessageBody(string asset, long payment, int role = 0, 
            string hashcode = null, string rcode = null) { }
        public virtual void InitializeMessageBody(int role = 0) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="isFromPeer"> indicates that the message is received from peer </param>
        public virtual void InitializeLocals(bool isFromPeer = false)
        {
            // set common variables from header
            this.SetLocalsFromHeader(isFromPeer);

            // set common variables from message body
            this.SetLocalsFromBody();

            // Parse the public key of peers
            this.ParsePubkeyPair(selfUri, peerUri);
        }

        public virtual void InitializeLevelDBApi()
        {
            // New instances for visiting the channel api
            this.GetChannelLevelDbEntry();

            // Get current channel information
            this.GetCurrentChannel();
        }

        /// <summary>
        /// Initialize the blockchain API for makeup the transaction body. It 
        /// </summary>
        public virtual void InitializeBlockChainApi()
        {
            // default is for Founder
            this.GetBlockChainAdaptorApi();

            return;
        }

        public virtual void SetLocalsFromHeader(bool isFromPeer = false)
        {
            // Set the uri info of peers
            this.selfUri = this.Request.Sender;
            this.peerUri = this.Request.Receiver;
            if (isFromPeer)
            {
                this.selfUri = this.Request.Receiver;
                this.peerUri = this.Request.Sender;
            }

            // set the channel name
            this.channelName = this.Request.ChannelName;
        }

        public virtual void SetLocalsFromBody() { }

        public virtual void SetTransactionValid() { }
        public virtual void CalculateBalance() { }

        public virtual Channel GetChannelLevelDbEntry()
        {
            this.channelDBEntry = this.channelDBEntry ?? new Channel(this.channelName, this.Request.AssetType, this.selfUri, this.peerUri);
            return this.channelDBEntry;
        }

        public virtual ChannelTableContent GetCurrentChannel()
        {
            this.currentChannel = this.channelDBEntry?.TryGetChannel(this.channelName);
            return this.currentChannel;
        }

        public virtual TTransactionContent GetCurrentTransaction<TTransactionContent>() 
            where TTransactionContent : TransactionTabelContent
        {
            if (IsRole0(currentRole))
            {
                return null;
            }

            return this.GetChannelLevelDbEntry().TryGetTransaction<TTransactionContent>(this.Request.TxNonce);
        }

        public virtual TransactionFundingContent GetFundingTrade()
        {
            this.fundingTrade = this.fundingTrade ?? this.channelDBEntry?.TryGetTransaction<TransactionFundingContent>(fundingNonce);
            return this.fundingTrade;
        }

        public virtual TransactionTabelHLockPair GetHtlcLockTrade()
        {
            if(null != this.HashR)
            {
                this.hlockTrade = this.hlockTrade ?? this.channelDBEntry?.TryGetTransactionHLockPair(this.HashR);
            }
            return this.hlockTrade;
        }

        public NeoTransaction GetBlockChainAdaptorApi()
        {
            // Use the current channel balance to initialize some locals here
            this.balance = this.GetCurrentChannel().balance;
            this.peerBalance = this.GetCurrentChannel().peerBalance;

            this.neoTransaction = new NeoTransaction(this.Request.AssetType.ToAssetId(),
                this.GetPubKey(), this.balance.ToString(), this.GetPeerPubKey(), this.peerBalance.ToString());
            return this.neoTransaction;
        }

        public NeoTransaction GetBlockChainAdaptorApi(bool isSettle)
        {
            // Use the current channel balance to initialize some locals here
            this.balance = this.GetCurrentChannel().balance;
            this.peerBalance = this.GetCurrentChannel().peerBalance;

            // get funding trade
            this.GetFundingTrade();
            if (!isSettle)
            {
                this.CalculateBalance();
            }

            // generate the neotransaction
            this.neoTransaction = new NeoTransaction(this.Request.AssetType.ToAssetId(), 
                this.GetPubKey(), this.balance.ToString(), this.GetPeerPubKey(), this.peerBalance.ToString(),
                this.fundingTrade?.founder.originalData.addressFunding, this.fundingTrade?.founder.originalData.scriptFunding);
            return this.neoTransaction;
        }

        public virtual void AddTransaction(bool isFounder = false) { }
        public virtual void UpdateTransaction() { }
        public virtual void AddTransactionSummary() { }
        public virtual void UpdateChannelSummary() { }

        ////////////////////////////////////////////////////////////////////////////
        #endregion // VIRUAL_SETS_OF_DIFFERENT_TRANSACTION_HANDLER
    }
}
