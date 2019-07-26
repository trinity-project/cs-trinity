/*
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
using System.Collections.Generic;

using Neo;
using Neo.IO.Json;
using Trinity;
using Trinity.BlockChain;
using Trinity.Wallets.Templates.Definitions;
using Trinity.Wallets.Templates.Messages;
using Trinity.ChannelSet;
using Trinity.ChannelSet.Definitions;
using Trinity.TrinityDB.Definitions;
using Trinity.Exceptions.WalletError;
using Trinity.Wallets.TransferHandler.ControlHandler;


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
        protected string channelName = null;
        protected string assetId = null;

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
            this.InitializeAssetType(true);

            // LevelDB API & channel related intialiazation
            this.InitializeLocals(true); // Common local variables intializtion
            this.InitializeLevelDBApi();    // LevelDB API & channel related intialization
        }

        public TransactionHandler(TRMessage request, int role=0) : base()
        {
            this.onGoingRequest = request;

            this.InitializeAssetType();

            // Allocate new request and intialize the header & body
            this.InitializeMessage(request.Receiver, request.Sender, request.ChannelName,
                this.assetId, request.NetMagic, request.TxNonce);
            this.InitializeMessageBody(role);

            // Initialize the components for this class
            this.InitializeLocals(); // Common local variables intializtion
            this.InitializeLevelDBApi();    // LevelDB API & channel related intialization
            this.InitializeBlockChainApi(); // BlockChain API initialization
        }

        public TransactionHandler(string sender, string receiver, string channel, string asset,
            string magic, UInt64 nonce, long payment, int role = 0, string hashcode=null, string rcode=null) : base()
        {
            this.assetId = asset?.ToAssetId(this.GetAssetMap());

            // Allocate new request and intialize the header & body
            this.InitializeMessage(sender, receiver, channel, this.assetId, magic, nonce);
            this.InitializeMessageBody(this.assetId, payment, role, hashcode, rcode);

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
            if (base.MakeTransaction())
            {
                this.AddOrUpdateTransaction(true);
                return true;
            }

            return false;
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
        public virtual bool VerifyNonce(UInt64 expectedNonce, bool isFunding=false)
        {
            return true;

#if CouldBeUsedInFuture
            if ((this.Request.TxNonce == expectedNonce && fundingNonce < this.Request.TxNonce) 
                || (isFunding && this.Request.TxNonce == fundingNonce))
            {
                return true;
            }

            throw new TransactionException(
                    EnumTransactionErrorCode.Incompatible_Nonce,
                    string.Format("{0} : Nonce of peers are incompatible. Nonce: {1}, ExpectedNonce: {2}.",
                            this.Request.MessageType, this.Request.TxNonce, expectedNonce),
                    this.Request.MessageType);
#endif
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

        public virtual bool VerifyBalance(long payment, bool isFounder=true)
        {
            if ((isFounder && this.currentChannel.balance >= payment) 
                || (!isFounder && this.currentChannel.peerBalance >= payment))
            {
                return true;
            }
            else if (this.IsHtlcToRsmc())
            {
                return true;
            }

            throw new TransactionException(EnumTransactionErrorCode.Channel_Without_Enough_Balance_For_Payment,
                string.Format("Not enough balance for payment. Balance: {0}, Payment: {1}.", 
                isFounder ? this.currentChannel.balance : this.currentChannel.peerBalance, payment),
                this.Request.MessageType
                );
        }

        public bool CheckChannelIsOpened()
        {
            return this.CheckChannelState(EnumChannelState.OPENED, EnumTransactionErrorCode.Channel_Not_Opened);
        }

        private bool CheckChannelState(EnumChannelState state, EnumTransactionErrorCode errorcode)
        {
            if (state.ToString().Equals(this.currentChannel.state))
            {
                return true;
            }

            throw new TransactionException(errorcode,
                string.Format("Incompatible Channel state. State: {0}, ExpectedState: {1}.", this.currentChannel.state, state),
                this.Request.MessageType
                );
        }

        public bool CheckChannelSupportedAsset(string asset)
        {
            string assetId = asset.Trim()?.ToAssetId(this.GetAssetMap());
            string supportedAssetId = this.currentChannel?.asset.Trim().ToAssetId(this.GetAssetMap());

            if (null != assetId && assetId.Equals(supportedAssetId))
            {
                return true;
            }

            throw new TransactionException(EnumTransactionErrorCode.Channel_Not_Support_Such_Asset_Type,
                string.Format("Channel does not support this asset type. Support Asset: {0}, Trade Asset: {1}.", supportedAssetId, assetId),
                this.Request.MessageType);
        }

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

            // notify gateway to delete graph
            if (EnumChannelState.SETTLING.Equals(state))
            {
                SyncNetTopologyHandler.DeleteNetworkTopology(channelContent);
            }
        }

        public void UpdateChannelBalance()
        {
            // Update just only when the current transaction is confirmed state
            ChannelTableContent channelContent = this.GetCurrentChannel();

            // Update the channel balance of peers
            channelContent.balance = this.balance;
            channelContent.peerBalance = this.peerBalance;
            this.channelDBEntry?.UpdateChannel(channelName, channelContent);

            // notify gateway to update the balance saved in gateway
            if (EnumChannelState.OPENED.ToString().Equals(channelContent.state))
            {
                SyncNetTopologyHandler.UpdateNetworkTopology(channelContent);
            }
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

        protected bool IsReachedPayee(List<PathInfo> router, out int currentUriIndex)
        {
            // for adapting the old trinity, here we have to use complicated logics
            currentUriIndex = this.IndexOfRouter(router, this.GetUri());
            if (0 <= currentUriIndex && router.Count <= currentUriIndex + 1)
            {
                return true;
            }

            return false;
        }

        protected int IndexOfRouter(List<PathInfo> router, string uri)
        {
            if (null == uri || null == router)
            {
                return -1;
            }

            foreach(PathInfo currentPoint in router)
            {
                if (currentPoint.uri == uri)
                {
                    return router.IndexOf(currentPoint);
                }
            }

            return -1;
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
                channel = this.Request.ChannelName,
                timestamp = DateTime.Now.ToLocalTime().ToString(),
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

        // The 2 method are used to Avoid the leveldb error
        public void AddTransaction<TItemContent>(UInt64 nonce, TItemContent content, bool isFunding = false)
        {
            if (!isFunding && fundingNonce == nonce)
            {
                throw new TransactionException(EnumTransactionErrorCode.Transaction_Nonce_Should_Be_Larger_Than_Zero,
                    "AddTransaction Error: Nonce<0> is only be used as funding transaction for creating channel.",
                    this.Request.MessageType
                    );
            }

            // update the transaction
            this.GetChannelLevelDbEntry()?.UpdateTransaction(this.Request.TxNonce, content);
            //this.GetChannelLevelDbEntry()?.AddTransaction(this.Request.TxNonce, content);
        }

        public void UpdateTransaction<TItemContent>(UInt64 nonce, TItemContent content, bool isFunding=false)
        {
            if (!isFunding && nonce == fundingNonce)
            {
                throw new TransactionException(EnumTransactionErrorCode.Transaction_Nonce_Should_Be_Larger_Than_Zero,
                    "UpdateTransaction Error: Nonce<0> is only be used as funding transaction for creating channel.",
                    this.Request.MessageType
                    );
            }

            // update the transaction
            this.GetChannelLevelDbEntry()?.UpdateTransaction(this.Request.TxNonce, content);
        }


        #region VIRUAL_SETS_OF_DIFFERENT_TRANSACTION_HANDLER
        //////////////////////////////////////////////////////////////////////////////////
        /// Start of virtual methods for different actions:
        /// 
        /// One set of methods are used to be overwritten for initialize transaction handler
        /// instances with different actions.
        //////////////////////////////////////////////////////////////////////////////////
        public virtual void InitializeAssetType(bool useCurrentRequest = false) { }

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
                TxNonce = nonce
            };
        }

        public virtual void InitializeMessageBody(string asset, long payment, int role = 0, 
            string hashcode = null, string rcode = null) { }
        public virtual void InitializeMessageBody(int role = 0) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="isFromPeer"> indicates that the message is received from remote </param>
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
            if (null != this.GetCurrentChannel())
            {
                this.balance = this.currentChannel.balance;
                this.peerBalance = this.currentChannel.peerBalance;
            }
        }

        /// <summary>
        /// Initialize the blockchain API for makeup the transaction body. It 
        /// </summary>
        public virtual void InitializeBlockChainApi()
        {
            if (null == this.currentChannel)
            {
                throw new TransactionException( EnumTransactionErrorCode.Channel_Not_Found,
                    string.Format("Channel: {0} not found in database", this.Request.ChannelName),
                    this.Request.MessageType.ToUpper());
            }

            // default is for Founder transaction
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
            this.channelDBEntry = this.channelDBEntry ?? new Channel(this.channelName, this.assetId, this.selfUri, this.peerUri);
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
            this.neoTransaction = new NeoTransaction(this.assetId,
                this.GetPubKey(), this.balance.ToString(), this.GetPeerPubKey(), this.peerBalance.ToString());

            this.SetTransactionValid();

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
            this.neoTransaction = new NeoTransaction(this.assetId, 
                this.GetPubKey(), this.balance.ToString(), this.GetPeerPubKey(), this.peerBalance.ToString(),
                this.fundingTrade?.founder.originalData.addressFunding, this.fundingTrade?.founder.originalData.scriptFunding);
            this.neoTransaction.SetFundingTxId(this.fundingTrade?.founder.originalData.txId);

            this.SetTransactionValid();

            return this.neoTransaction;
        }

        public virtual void AddTransaction(bool isFounder = false) { }
        public virtual void UpdateTransaction() { }
        public virtual void AddTransactionSummary() { }
        public virtual void UpdateChannelSummary() { }

        ////////////////////////////////////////////////////////////////////////////
#endregion // VIRUAL_SETS_OF_DIFFERENT_TRANSACTION_HANDLER
    }

    public sealed class TransactionHandler
    {
        // public static methods
        public static void MakeTransaction(string sender, string receiver, string channel, string asset,
            string magic, UInt64 nonce, long payment, string hashcode = null)
        {
            asset = asset.ToAssetId(startTrinity.GetAssetMap());

            // GetCurrent Channel
            Channel channelLevelDbApi = new Channel(channel, asset, sender, receiver);
            ChannelTableContent currentChannel = channelLevelDbApi.GetChannel(channel);
            
            // to decide which transaction is used
            if (null != currentChannel && currentChannel.peer.Equals(receiver))
            {
                RsmcHandler rsmcHndl = new RsmcHandler(sender, receiver, channel, asset,
                            magic, nonce, payment);
                rsmcHndl.MakeTransaction();
            }
            else
            {
                // Get route info here
                AckRouterInfo routerInfo = GetRouterInfoHandler.GetRouter(sender, receiver, asset, TrinityWallet.GetMagic(), payment);
                List<PathInfo> router = routerInfo?.RouterInfo?.FullPath;

                if (2 > router.Count)
                {
                    Log.Error("Failed to make Htlc transaction since wrong router. Router: {0}", router);
                    return;
                }

                // Calculate the payment with fee
                for (int routerIndex=1; routerIndex < router.Count-1; routerIndex++)
                {
                    payment += Fixed8.Parse(router[routerIndex].fee.ToString()).GetData();
                }

                currentChannel = channelLevelDbApi.GetChannel(router[1].uri, payment, EnumChannelState.OPENED.ToString(), asset);
                // Htlc transaction
                if (null != currentChannel)
                {
                    // start HTLC transaction
                    HtlcHandler htlcHndl = new HtlcHandler(
                        sender, currentChannel.peer, currentChannel.channel, asset, magic, nonce, payment, hashcode, router);
                    htlcHndl.MakeTransaction();
                }
                else
                {
                    Log.Error("No satisfied Channel is found between {0} and {1}. Payment: {2}", sender, router[1].uri, payment);
                }
            }
        }
    }
}
