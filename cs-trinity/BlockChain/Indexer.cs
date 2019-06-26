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
using Neo.IO;

namespace Trinity.BlockChain
{
    public class Indexer        // : IDisposable
    {
        private string address;
        private string assetType;
        //private string uri;
        //private string peerUri;
        //private string pubKey;
        //private string address;
        //private string peerPubKey;
        //private string peerAddress;
        //private Dictionary<string, double> Deposit;
        //private Dictionary<string, double> Balance;

        private readonly IndexerModel TableIndexer;

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="address"></param>
        /// <param name="assetId"></param>
        public Indexer(string address)
        {
            this.address = address;
            //this.assetType = assetId;

            this.TableIndexer = new IndexerModel(this.dbPath(), address);
            // this.TableIndexer = new IndexerModel(this.dbPath(), uri, peerUri);
        }

        public IndexerTableContent[] GetIndexer(string address, string assetId)
        {
            try
            {
                Slice IndexerContent = this.TableIndexer.Db.Get(this.TableIndexer.bothKeyword.Add(address.ToBytesUtf8()), assetId);
                return IndexerContent.ToString().DeserializeObject<List<IndexerTableContent>>().ToArray();
            }
            catch (Exception ExpInfo)
            {
                Console.WriteLine("Failed to get the address: {0}. Exception: {1}", address, ExpInfo);
            }

            return null;
        }

        public IndexerTableContent TryGetIndexer(string address, string assetId)
        {
            if (this.TableIndexer.Db.TryGet(this.TableIndexer.bothKeyword.Add(address.ToBytesUtf8()), assetId, out Slice chContent))
            {
                return chContent.ToString().Deserialize<IndexerTableContent>();
            }

            return null;
        }

        public List<IndexerTableContent> GetIndexerListOfThisWallet()
        {
            // Fuzzy get
            return this.TableIndexer.Db.FuzzyGet<IndexerTableContent>(this.TableIndexer.keyword);
        }

        public void AddIndexer(string address, string assetId, IndexerTableContent[] value)
        {
            this.TableIndexer.Db.Add(this.TableIndexer.bothKeyword.Add(address.ToBytesUtf8()), assetId, value);
        }

        public void UpdateIndexer(string address, string assetId, IndexerTableContent[] value)
        {
            this.TableIndexer.Db.Update(this.TableIndexer.bothKeyword.Add(address.ToBytesUtf8()), assetId, value);
        }

        public void DeleteIndexer(string address, string assetId)
        {
            this.TableIndexer.Db.Delete(this.TableIndexer.bothKeyword.Add(address.ToBytesUtf8()), assetId);
        }

        public virtual string dbPath()
        {
            return "./trinity/leveldbTest";
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
        public static string NewIndexer(string founder, string partner)
        {
            string encodeStr = founder + DateTime.Now.Ticks.ToString() + partner + DateTime.Now.ToString();
            byte[] sourcebytes = Encoding.Default.GetBytes(encodeStr);
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] hashedbytes = md5.ComputeHash(sourcebytes);
            return BitConverter.ToString(hashedbytes).Replace("-", "");
        }
    }
}