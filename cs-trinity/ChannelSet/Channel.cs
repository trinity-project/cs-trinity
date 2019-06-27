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
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;

using Neo.IO.Data.LevelDB;
using Trinity.TrinityDB;
using Trinity.TrinityDB.Definitions;
using Trinity.Wallets;
using Trinity.Exceptions.DBError;

namespace Trinity.ChannelSet
{
    public class Channel        // : IDisposable
    {
        private string uri;
        private string peerUri;
        private string assetType;
        private string channelName;
        //private string pubKey;
        //private string address;
        //private string peerPubKey;
        //private string peerAddress;
        //private Dictionary<string, double> Deposit;
        //private Dictionary<string, double> Balance;
        
        private readonly ChannelModel TableChannel = null;
        private readonly TransactionModel TableTransaction = null;
        private readonly BlockModel TableBlock = null;

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="uri"></param>
        /// <param name="peerUri"></param>
        /// <param name="channel"></param>
        public Channel(string channel, string asset, string uri, string peerUri = null)
        {
            this.uri = uri;
            this.peerUri = peerUri;
            this.channelName = channel;
            this.assetType = asset;
            
            this.TableChannel = new ChannelModel(this.dbPath(), uri, peerUri);
            this.TableBlock = new BlockModel(this.dbPath(), uri);
            this.TableTransaction = new TransactionModel(this.dbPath(), channel);
            
        }

        public ChannelTableContent GetChannel(string channel)
        {
            try
            {
                Slice channelContent = this.TableChannel.Db.Get(this.TableChannel.bothKeyword.Add(channel.ToBytesUtf8()), channel);
                return channelContent.ToString().Deserialize<ChannelTableContent>();
            }
            catch (Exception ExpInfo)
            {
                Console.WriteLine("Failed to get the channel: {0}. Exception: {1}", channel, ExpInfo);
            }

            return null;
        }

        public ChannelTableContent TryGetChannel(string channel)
        {
            if (this.TableChannel.Db.TryGet(this.TableChannel.bothKeyword.Add(channel.ToBytesUtf8()), channel, out Slice chContent))
            {
                return chContent.ToString().Deserialize<ChannelTableContent>();
            }

            return null;
        }

        public List<ChannelTableContent> GetChannelListOfThisWallet()
        {
            // Fuzzy get
            return this.TableChannel.Db.FuzzyGet<ChannelTableContent>(this.TableChannel.keyword);
        }

        public void AddChannel(string channel, ChannelTableContent value)
        {
            this.TableChannel.Db.Add(this.TableChannel.bothKeyword.Add(channel.ToBytesUtf8()), channel, value);
        }

        public void UpdateChannel(string channel, ChannelTableContent value)
        {
            this.TableChannel.Db.Update(this.TableChannel.bothKeyword.Add(channel.ToBytesUtf8()), channel, value);


        }

        public void DeleteChannel(string channel)
        {
            this.TableChannel.Db.Delete(this.TableChannel.bothKeyword.Add(channel.ToBytesUtf8()), channel);
        }

        // channel summary info
        public ChannelSummaryContents GetChannelSummary(string channel)
        {
            try
            {
                Slice channelSummaryContent = this.TableChannel.Db.Get(this.TableChannel.summary.Add(channel.ToBytesUtf8()), channel);
                return channelSummaryContent.ToString().Deserialize<ChannelSummaryContents>();
            }
            catch (Exception ExpInfo)
            {
                Log.Fatal("Failed to get summary of the channel: {0}. Exception: {1}", channel, ExpInfo);
            }

            return null;
        }

        public ChannelSummaryContents TryGetChannelSummary(string channel)
        {
            if (this.TableChannel.Db.TryGet(this.TableChannel.summary.Add(channel.ToBytesUtf8()), channel, out Slice summary))
            {
                return summary.ToString().Deserialize<ChannelSummaryContents>();
            }

            return null;
        }

        public void AddChannelSummary(string channel, ChannelSummaryContents value)
        {
            this.TableChannel.Db.Add(this.TableChannel.summary.Add(channel.ToBytesUtf8()), channel, value);
        }

        public void UpdateChannelSummary(string channel, ChannelSummaryContents value)
        {
            this.TableChannel.Db.Update(this.TableChannel.summary.Add(channel.ToBytesUtf8()), channel, value);
        }

        public void DeleteChannelSummary(string channel)
        {
            this.TableChannel.Db.Delete(this.TableChannel.summary.Add(channel.ToBytesUtf8()), channel);
        }

        // transaction info
        public TItemContent GetTransaction<TItemContent>(UInt64 nonce)
        {
            try
            {
                Slice txContent = this.TableTransaction.Db.Get(this.TableTransaction.record?.Add(nonce.ToString().ToBytesUtf8()), nonce.ToString());
                return txContent.ToString().Deserialize<TItemContent>();
            }
            catch (Exception ExpInfo)
            {
                Console.WriteLine("Failed to get transaction with nonce: {0}. Exception: {1}", nonce, ExpInfo);
            }

            return default;
        }

        public TransactionTabelSummary GetTransaction(string txid)
        {
            Slice txContent = this.TableTransaction.Db.Get(this.TableTransaction.txIdGroup.Add(txid.ToBytesUtf8()), txid);
            if (null != txContent.ToString())
            {
                return txContent.ToString().Deserialize<TransactionTabelSummary>();
            }

            return null;
        }

        public TItemContent TryGetTransaction<TItemContent>(UInt64 nonce)
        {
            if (this.TableTransaction.Db.TryGet(this.TableTransaction.record?.Add(nonce.ToString().ToBytesUtf8()),
                nonce.ToString(), out Slice txContent))
            {
                return txContent.ToString().Deserialize<TItemContent>();
            }

            throw new TrinityLevelDBException(
                    EnumLevelDBErrorCode.Transaction_Contents_Not_Found,
                    string.Format("Transaction information with nonce-{0} not found!", nonce),
                    EnumLevelDBErrorBase.TRANSACTION_TABLE_DETAIL.ToString()
                );
        }

        public TransactionTabelSummary TryGetTransaction(string txid)
        {
            if (this.TableTransaction.Db.TryGet(this.TableTransaction.txIdGroup.Add(txid.ToBytesUtf8()), txid, out Slice txContent))
            {
                return txContent.ToString().Deserialize<TransactionTabelSummary>();
            }

            return null;
        }

        public List<TItemContent> GetTransactionList<TItemContent>()
        {
            // Fuzzy get
            return this.TableTransaction.Db.FuzzyGet<TItemContent>(this.TableTransaction.record);
        }

        public void AddTransaction<TItemContent>(UInt64 nonce, TItemContent value)
        {
            this.TableTransaction.Db.Add(this.TableTransaction.record?.Add(nonce.ToString().ToBytesUtf8()), nonce.ToString(), value);
        }

        public void AddTransaction(string txid, TransactionTabelSummary value)
        {
            this.TableTransaction.Db.Add(this.TableTransaction.txIdGroup.Add(txid.ToBytesUtf8()), txid, value);
        }

        public void UpdateTransaction<TItemContent>(UInt64 nonce, TItemContent value)
        {
            this.TableTransaction.Db.Update(this.TableTransaction.record?.Add(nonce.ToString().ToBytesUtf8()), nonce.ToString(), value);
        }

        public void DeleteTransaction(UInt64 nonce)
        {
            this.TableTransaction.Db.Delete(this.TableTransaction.record?.Add(nonce.ToString().ToBytesUtf8()), nonce.ToString());
        }

        public void DeleteTransaction(string txid)
        {
            this.TableTransaction.Db.Delete(this.TableTransaction.txIdGroup.Add(txid.ToBytesUtf8()), txid);
        }

        /// =============================================================================================
        /// record the HashR-R pair
        public TransactionTabelHLockPair TryGetTransactionHLockPair(string hashcode)
        {
            if (this.TableTransaction.Db.TryGet(
                this.TableTransaction.txHashPairGroup.Add(hashcode.ToBytesUtf8()), hashcode, out Slice txContent))
            {
                return txContent.ToString().Deserialize<TransactionTabelHLockPair>();
            }

            return null;
        }

        public void AddTransactionHLockPair(string hashcode, TransactionTabelHLockPair value)
        {
            this.TableTransaction.Db.Add(this.TableTransaction.txHashPairGroup.Add(hashcode.ToBytesUtf8()), hashcode, value);
        }

        public void UpdateTransactionHLockPair(string hashcode, TransactionTabelHLockPair value)
        {
            if (null != this.TryGetTransactionHLockPair(hashcode))
            {
                this.TableTransaction.Db.Update(this.TableTransaction.txHashPairGroup.Add(hashcode.ToBytesUtf8()), hashcode, value);
            }
            else
            {
                Log.Error("Failed to update HTLC Lock pair with HashR: {0} since HLockPair not found!", hashcode);
            }
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="value"></param>
        public void AddBlockHeight(string uri, uint value)
        {
            this.TableBlock.Db.Update(this.TableBlock.keyword, uri, value);
        }

        public uint TryGetBlockHeight(string uri)
        {
            if (this.TableBlock.Db.TryGet(this.TableBlock.keyword, uri, out Slice height))
            {
                return  Convert.ToUInt32(height.ToString());
            }

            return 0;
        }

        public virtual string dbPath()
        {
            return "./trinity/leveldb";
        }

        //public void Dispose()
        //{
        //}

        // Static Method Sets are implemented as below.
        /// <summary>
        /// Get all channels between founder and partner.
        /// </summary>
        /// <param name="founder"></param>
        /// <param name="partner"></param>
        /// <returns></returns>
        public static string NewChannel(string founder, string partner)
        {
            string encodeStr = founder + DateTime.Now.Ticks.ToString() + partner + DateTime.Now.ToString();
            byte[] sourcebytes = Encoding.Default.GetBytes(encodeStr);
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] hashedbytes = md5.ComputeHash(sourcebytes);
            return BitConverter.ToString(hashedbytes).Replace("-", "");
        }
    }
}