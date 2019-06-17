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
        protected ChannelTableContent currentChannel = null;

        // BlockChain transaction api
        protected NeoTransaction neoTransaction = null;

        // nonce for transaction
        // funding transaction nonce should be always zero.
        public const ulong fundingTradeNonce = 0;
        // Record the last nonce value
        private UInt64 latestNonce = 0;

        // Local variables members for initialization steps
        private string selfUri = null;
        private string peerUri = null;
        protected string channelName = null;

        protected string AssetType = null;
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

            // Add or update the data to the database
            this.AddOrUpdateTransaction(true);
            this.AddOrUpdateTransactionSummary();

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

        /// <summary>
        /// Virtual Method sets. Should be overwritten in child classes.
        /// </summary>
        /// <returns></returns>
        /// Verification method sets
        public virtual bool VerifyNonce() { return true; }
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

        public virtual void UpdateChannelState(string uri, string peerUri, string channelName, EnumChannelState state)
        {
            ChannelTableContent channelContent = this.GetChannelLevelDbEntry().TryGetChannel(channelName);
            if (null == channelContent)
            {
                Log.Fatal("Could not find channel -- {0} in Database.", channelName);
                return;
            }

            // Update the channel state
            channelContent.state = state.ToString();
            this.channelDBEntry?.UpdateChannel(channelName, channelContent);
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

        public void UpdateChannelSummaryContent(string channel, UInt64 nonce, string peerUri)
        {
            ChannelSummaryContents txContent = new ChannelSummaryContents
            {
                nonce = nonce,
                peer = peerUri,
                type = null
            };

            this.channelDBEntry?.UpdateChannelSummary(channel, txContent);
        }

        public UInt64 CurrentNonce(string channel)
        {
            if (0 != this.latestNonce)
            {
                return this.latestNonce;
            }

            ChannelSummaryContents content = this.channelDBEntry?.TryGetChannelSummary(channel);
            if (null == content)
            {
                throw new Exception(string.Format("Not found summary information for channel: {0}", channel));
            }
            this.latestNonce = content.nonce;

            return this.latestNonce;
        }

        public UInt64 NextNonce(string channel)
        {
            return this.CurrentNonce(channel) + 1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="balance"></param>
        /// <param name="peerBalance"></param>
        /// <param name="payment"></param>
        /// <param name="isPeer"></param>
        /// <param name="isH2R"></param>
        /// <returns></returns>
        private long[] CalculateBalance(long balance, long peerBalance, long payment, bool isPeer = false, bool isH2R = false)
        {
            if (isPeer)
            {
                if (isH2R)
                {
                    return new long[2] { balance + payment, peerBalance };
                }
                else
                {
                    return new long[2] { balance + payment, peerBalance - payment };
                }
            }
            else
            {
                if (isH2R)
                {
                    return new long[2] { balance, peerBalance + payment };
                }
                else
                {
                    return new long[2] { balance - payment, peerBalance + payment };
                }
            }
        }

        public long[] CalculateBalance(int role, long balance, long peerBalance, long payment, bool isPeer = false, bool isH2R = false)
        {
            if (this.IsRole0(role) || this.IsRole2(role))
            {
                return this.CalculateBalance(balance, peerBalance, payment, isPeer, isH2R);
            }
            else if (this.IsRole1(role) || this.IsRole3(role))
            {
                return this.CalculateBalance(balance, peerBalance, payment, !isPeer, isH2R);
            }
            else
            {
                throw new Exception(string.Format("Invalid role: {0} for RSMC transaction", role));
            }
        }

        private long[] CalculateBalance(long balance, long peerBalance, long payment, bool isPeer = false)
        {
            if (isPeer)
            {
                return new long[2] { balance, peerBalance - payment };
            }
            else
            {
                return new long[2] { balance - payment, peerBalance };
            }
        }

        public long[] CalculateBalance(int role, long balance, long peerBalance, long payment, bool isPeer = false)
        {
            if (this.IsRole0(role))
            {
                return this.CalculateBalance(balance, peerBalance, payment, isPeer);
            }
            else if (this.IsRole1(role))
            {
                return this.CalculateBalance(balance, peerBalance, payment, !isPeer);
            }
            else
            {
                throw new Exception(string.Format("Invalid role: {0} for RSMC transaction", role));
            }
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
                TxNonce = nonce
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
        /// Initialize the blockchain API for makeup the transaction body
        /// </summary>
        public virtual void InitializeBlockChainApi()
        {
            // if is creating channel
            if (fundingTradeNonce == this.Request.TxNonce)
            {
                this.GetBlockChainAdaptorApi();
            }
            else
            {
                // Get funding trade
                this.GetFundingTrade();

                // initialize the BlockChain Api
                if (null != this.currentChannel
                    && null != this.fundingTrade
                    && null != this.fundingTrade.founder.originalData.scriptFunding
                    && null != this.fundingTrade.founder.originalData.addressFunding)
                {
                    long[] balanceOfPeers = this.CalculateBalance(this.currentChannel.balance, this.currentChannel.peerBalance);
                    this.GetBlockChainAdaptorApi(balanceOfPeers[0], balanceOfPeers[1]);
                    this.SetTransactionValid();
                }
            }

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
        public virtual long[] CalculateBalance(long balance, long peerBalance) { return null; }

        public virtual Channel GetChannelLevelDbEntry()
        {
            this.channelDBEntry = this.channelDBEntry ?? new Channel(this.channelName, this.AssetType, this.selfUri, this.peerUri);
            return this.channelDBEntry;
        }

        public virtual ChannelTableContent GetCurrentChannel()
        {
            this.currentChannel = this.channelDBEntry?.TryGetChannel(this.channelName);
            return this.currentChannel;
        }

        public virtual TransactionFundingContent GetFundingTrade()
        {
            this.fundingTrade = this.fundingTrade ?? this.channelDBEntry?.TryGetTransaction<TransactionFundingContent>(fundingTradeNonce);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="balance"> deposit vaule for creating channel or self balance after payment </param>
        /// <param name="peerBalance"> deposit vaule for creating channel or peer balance after payment </param>
        /// <returns></returns>
        public virtual NeoTransaction GetBlockChainAdaptorApi(long balance, long peerBalance)
        {
            this.neoTransaction = new NeoTransaction(this.AssetType.ToAssetId(), this.GetPubKey(), balance.ToString(),
                                this.GetPeerPubKey(), peerBalance.ToString(),
                                this.fundingTrade?.founder.originalData.addressFunding,
                                this.fundingTrade?.founder.originalData.scriptFunding);
            return this.neoTransaction;
        }

        public virtual NeoTransaction GetBlockChainAdaptorApi()
        {
            return this.neoTransaction;
        }

        public virtual void AddOrUpdateTransaction(bool isPeer = false) { }
        public virtual void AddOrUpdateTransactionSummary(bool isPeer = false) { }

        ////////////////////////////////////////////////////////////////////////////
        #endregion // VIRUAL_SETS_OF_DIFFERENT_TRANSACTION_HANDLER
    }
}
