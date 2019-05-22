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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Neo;
using Trinity.Network.TCP;
using Trinity.Wallets.Templates.Definitions;
using Trinity.Wallets.Templates.Messages;
using Trinity.ChannelSet;
using Trinity.ChannelSet.Definitions;
using Trinity.TrinityDB.Definitions;


namespace Trinity.Wallets.TransferHandler
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    /// <typeparam name="TSHandler"></typeparam>
    /// <typeparam name="TFHandler"></typeparam>
    public abstract class TransferHandler<TMessage, TSHandler, TFHandler> : IDisposable
    {
        private TrinityWallet wallet;

        protected TMessage Request;
        private string MessageName => typeof(TMessage).Name;
        protected TSHandler SHandler;
        protected TFHandler FHandler;

        private TrinityTcpClient client;
        public Channel channelDbInterface;

        //private string priKey;
        private string pubKey;
        private string peerPubKey;

        // Record current Header
        public TransactionHeader header;

        // 
        public const ulong fundingTradeNonce = 0;

        /// <summary>
        /// Virtual Method sets. Should be overwritten in child classes.
        /// </summary>
        /// <returns></returns>
        /// Verification method sets
        public virtual bool Verify() { return true; }
        public virtual bool VerifyNonce() { return true; }
        public virtual bool VerifySignature() { return true; }
        public virtual bool VerifyBalance() { return true; }
        public virtual bool VerifyNetMagic() { return true; }
        public virtual bool VerifyRoleIndex() { return true; }

        public virtual bool SucceedStep() { return true; }
        public virtual bool FailStep() { return true; }

        /// <summary>
        /// Dispose method to release memory
        /// </summary>
        public virtual void Dispose()
        {
            
        }

        /// <summary>
        /// Default Constructor
        /// </summary>
        public TransferHandler()
        {
            this.wallet = startTrinity.trinityWallet;
        }

        public TransferHandler(string message)
        {
            this.wallet = startTrinity.trinityWallet;
            this.Request = message.Deserialize<TMessage>();
        }

        public virtual TValue GetHeaderValue<TValue>(string name)
        {
            return this.Request.GetAttribute<TMessage, TValue>(name);
        }

        public virtual void SetBodyAttribute<TValue>(string name, TValue value) {}
        public virtual void GetBodyAttribute<TContext>(string name) { }

        public virtual bool MakeTransaction()
        {
            if (this.MakeupMessage())
            {
                this.wallet?.GetClient()?.SendData(this.Request.Serialize());
                return true;
            }
            else
            {
                Console.WriteLine("Void Message is found");
            }
            return false;
        }

        public virtual bool MakeupMessage()
        {
            return true;
        }

        public string ToJson()
        {
            return this.Request.Serialize();
        }

        public TMessage GetTMessage()
        {
            return this.Request;
        }

        //public virtual bool Handle(string msg)
        //{
        //    if (null != msg) {
        //        this.Request = msg.Deserialize<TMessage>();
        //        this.header = msg.Deserialize<TransactionHeader>();
        //        this.ParsePubkeyPair(this.header.Receiver, this.header.Sender);
        //        return true;
        //    }

        //    return false;
        //}

        public virtual bool Handle()
        {
            // MessageType is not Founder
            if (!(this.Request is TMessage))
            {
                return false;
            }

            // lack of verification steps
            if (!this.Verify())
            {
                this.FailStep();
                return false;
            }

            this.SucceedStep();

            return true;
        }

        public void SetWallet(TrinityWallet wallet)
        {
            this.wallet = wallet;
        }

        public void SetClient(TrinityTcpClient client)
        {
            this.client = client;
        }

        public TrinityTcpClient GetClient()
        {
            return this.wallet?.GetClient();
        }

        public string GetNetMagic()
        {
            return this.wallet?.GetNetMagic();
        }

        public void ParsePubkeyPair(string uri, string peerUri)
        {
            this.pubKey = uri?.Split('@').First();
            this.peerPubKey = peerUri?.Split('@').First();
        }

        public string GetPubKey()
        {
            return this.pubKey;
        }

        public UInt160 GetPublicKeyHash()
        {
            return this.wallet?.GetPublicKeyHash();
        }

        public string GetPeerPubKey()
        {
            return this.peerPubKey;
        }

        public void SetChannelInterface(string uri, string peerUri, string channel, string asset)
        {
            if (null == this.channelDbInterface)
            {
                this.channelDbInterface = new Channel(channel, asset, uri, peerUri);
            } 
        }

        public Channel GetChannelInterface()
        {
            return this.channelDbInterface;
        }

        public virtual void UpdateChannelState(string uri, string peerUri, string channelName, EnumChannelState state)
        {
            ChannelTableContent channelContent = this.GetChannelInterface().TryGetChannel(channelName);
            if (null == channelContent)
            {
                Log.Fatal("Could not find channel -- {0} in Database.", channelName);
                return;
            }

            // Update the channel state
            channelContent.state = state.ToString();
            this.channelDbInterface?.UpdateChannel(channelName, channelContent);
        }

        public void AddTransactionSummary(UInt64 nonce, string txId, string channel, EnumTxType type)
        {
            TransactionTabelSummary txContent = new TransactionTabelSummary
            {
                nonce = nonce,
                channel = channel,
                txType = type.ToString()
            };

            this.channelDbInterface?.AddTransaction(txId, txContent);
        }

        public bool CurrentNonce(string channel, out UInt64 nonce)
        {
            nonce = 0;

            ChannelSummaryContents content = this.channelDbInterface?.TryGetChannelSummary(channel);

            if (null == content)
            {
                Log.Error("Not found summary information for channel: {0}", channel);
                return false;
            }

            nonce = content.nonce;
            return true;
        }

        public bool NextNonce(string channel, out UInt64 nonce)
        {
            nonce = 0;

            if (this.CurrentNonce(channel, out UInt64 currentNonce))
            {
                nonce = currentNonce + 1;
                return true;
            }

            return false;
        }

        public long[] CalculateBalance(int role, long balance, long peerBalance, long payment)
        {
            if (this.IsRole0(role) || this.IsRole2(role))
            {
                return new long[2] { balance - payment, peerBalance + payment };
            }
            else if (this.IsRole1(role) || this.IsRole3(role))
            {
                return new long[2] { balance + payment, peerBalance - payment };
            }
            else
            {
                throw new Exception(string.Format("Invalid role: {0}", role));
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

        public string Sign(string content)
        {
            return this.wallet?.Sign(content);
        }

        public virtual FundingSignTx MakeupSignature(FundingTx txContent)
        {
            string txDataSign = this.Sign(txContent.txData);

            return new FundingSignTx
            {
                txDataSign = txDataSign,
                originalData = txContent
            };
        }
        
        public virtual CommitmentSignTx MakeupSignature(CommitmentTx txContent)
        {
            string txDataSign = this.Sign(txContent.txData);

            return new CommitmentSignTx
            {
                txDataSign = txDataSign,
                originalData = txContent
            };
        }

        public virtual RevocableDeliverySignTx MakeupSignature(RevocableDeliveryTx txContent)
        {
            string txDataSign = this.Sign(txContent.txData);

            return new RevocableDeliverySignTx
            {
                txDataSign = txDataSign,
                originalData = txContent
            };
        }

        public virtual BreachRemedySignTx MakeupSignature(BreachRemedyTx txContent)
        {
            string txDataSign = this.Sign(txContent.txData);

            return new BreachRemedySignTx
            {
                txDataSign = txDataSign,
                originalData = txContent
            };
        }

        public virtual TxContentsSign MakeupSignature(TxContents txContent)
        {
            string txDataSign = this.Sign(txContent.txData);

            return new TxContentsSign
            {
                txDataSign = txDataSign,
                originalData = txContent
            };
        }

        public bool VerifySignarture(string content, string contentSign)
        {
            return (null == this.wallet) ? false : this.wallet.VerifySignarture(content, contentSign);
        }
    }
}
