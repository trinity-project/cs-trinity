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
using Trinity.TrinityWallet;

namespace Trinity.ChannelSet
{
    class Channel
    {
        private string uri;
        private string peerUri;
        private string pubKey;
        private string adress;
        private string peerPubKey;
        private string peerAddress;
        private string assetType;
        private string channelName;
        private Dictionary<string, double> Deposit;
        private Dictionary<string, double> Balance;
        
        private ChannelModel TableChannel;
        private TransactionModel TableTransaction;

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="uri"></param>
        /// <param name="peerUri"></param>
        /// <param name="channel"></param>
        public Channel(string asset, string uri, string peerUri=null, string channel=null)
        {
            this.uri = uri;
            this.peerUri = peerUri;
            this.channelName = channel;
            this.assetType = asset;

            string dbPath = "./trinity/leveldb";
            this.TableChannel = new ChannelModel(dbPath, uri, peerUri);
            this.TableTransaction = new TransactionModel(dbPath, channel);
        }

        public Slice GetChannel(string channel)
        {
            return this.TableChannel.Db.Get(this.TableChannel.bothKeyword, channel);
        }

        public List<ChannelTableContents> GetChannelListOfThisWallet()
        {
            // Fuzzy get
            return this.TableChannel.Db.FuzzyGet<ChannelTableContents>(this.TableChannel.bothKeyword);
        }

        public void AddChannel(string channel, ChannelTableContents value)
        {
            this.TableChannel.Db.Add(this.TableChannel.bothKeyword, channel, value);
        }

        public void UpdateChannel(string channel, ChannelTableContents value)
        {
            this.TableChannel.Db.Update(this.TableChannel.bothKeyword, channel, value);
        }

        public void DeleteChannel(string channel)
        {
            this.TableChannel.Db.Delete(this.TableChannel.bothKeyword, channel);
        }

        // channel summary info
        public Slice GetChannelSummary(string channel)
        {
            return this.TableChannel.Db.Get(this.TableChannel.summary, channel);
        }

        public void AddChannelSummary(string channel, ChannelSummaryContents value)
        {
            this.TableChannel.Db.Add(this.TableChannel.summary, channel, value);
        }

        public void UpdateChannelSummary(string channel, ChannelSummaryContents value)
        {
            this.TableChannel.Db.Update(this.TableChannel.summary, channel, value);
        }

        private void DeleteChannelSummary(string channel)
        {
            this.TableChannel.Db.Delete(this.TableChannel.summary, channel);
        }

        // transaction info
        public Slice GetTransaction(UInt64 nonce)
        {
            return this.TableTransaction.Db.Get(this.TableTransaction.record, nonce.ToString());
        }

        public Slice GetTransaction(string txid)
        {
            return this.TableTransaction.Db.Get(this.TableTransaction.txid, txid);
        }

        public List<ChannelTableContents> GetTransactionList()
        {
            // Fuzzy get
            return this.TableTransaction.Db.FuzzyGet<ChannelTableContents>(this.TableTransaction.record);
        }

        public void AddTransaction(UInt64 nonce, ChannelTableContents value)
        {
            this.TableTransaction.Db.Add(this.TableTransaction.record, nonce.ToString(), value);
        }

        public void UpdateTransaction(UInt64 nonce, ChannelTableContents value)
        {
            this.TableTransaction.Db.Update(this.TableTransaction.record, nonce.ToString(), value);
        }

        private void DeleteTransaction(UInt64 nonce)
        {
            this.TableTransaction.Db.Delete(this.TableTransaction.record, nonce.ToString());
        }

        //public bool IsFounder(string sender)
        //{
        //    return sender.Equals(this.uri);
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