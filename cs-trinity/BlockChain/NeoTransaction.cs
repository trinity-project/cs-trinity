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
using Neo.SmartContract;
using Neo.IO.Json;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;

using Trinity;
using Trinity.Wallets.Templates.Definitions;

namespace Trinity.BlockChain
{

    /// <summary>
    /// This class is used to adapt to NEO smartconcact.
    /// It helps Trinity broadcast the transaction to the NEO blockchain. 
    /// </summary>
    public sealed class NeoTransaction
    {
        // Timestamp attribute for contract
#if DEBUG
        private readonly double timestamp = 1554866712.123456; // for test use;
        private readonly string timestampString = "1554866712.123456";
#else
        private double timestamp => (DateTime.Now - TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1))).TotalSeconds;
        private string timestampString => this.timestamp.ToString("F6");  // 6-precision, but the last number is aways 0(caused by C#)
#endif

        // which type of asset is used
        private readonly string assetId;

        // founder trade information
        private string addressFunding;
        private string scriptFunding;

        // Commitment trade information
        private string addressRsmc;
        private string scriptRsmc;

        // Peers of the trade
        // self wallet trade information of the channel
        private readonly string balance;
        private readonly string pubKey;
        private UInt160 scriptHash => this.scriptHash ?? this.pubKey.ToHash160();
        private string address => this.address ?? this.pubKey.ToAddress();

        // peer wallet trade information of the channel
        private readonly string peerBalance;
        private readonly string peerPubkey;
        private UInt160 peerScriptHash => this.peerScriptHash ?? this.peerPubkey.ToHash160();
        private string peerAddress => this.peerAddress ?? this.peerPubkey.ToAddress();

        /// <summary>
        /// Constructor for this class
        /// </summary>
        /// <param name="asset"> Asset ID</param>
        /// <param name="pubKey"> Self Wallet's PublicKey </param>
        /// <param name="balance"> Self Wallet's Balance in specified channel. Also it's used as deposit Just only when creating channel between 2 Wallets </param>
        /// <param name="peerPubKey"> Peer Wallet's PublicKey </param>
        /// <param name="peerBalance"> Peer Wallet's Balance in specified channel. Refer to comments for balance. </param>
        /// <param name="addressFunding"> Contract address for storing 2 wallets' Deposit. It's JUST created when Founder Message is triggerred. </param>
        /// <param name="scriptFunding"> Contract script </param>
        public NeoTransaction(string assetId, string pubKey, string balance, string peerPubKey, string peerBalance,
            string addressFunding=null, string scriptFunding=null)
        {
            this.assetId = assetId;

            this.pubKey = pubKey.NeoStrip();
            this.balance = balance.NeoStrip();

            this.peerPubkey = peerPubKey.NeoStrip();
            this.peerBalance = peerBalance.NeoStrip();

            this.addressFunding = addressFunding?.NeoStrip();
            this.scriptFunding = scriptFunding?.NeoStrip();
        }

        /// <summary>
        /// It helps Trinity to create Funding transaction for adapting the Neo contract.
        /// </summary>
        /// <param name="fundingTx"> output the FundingTx body </param>
        /// <returns></returns>
        public bool CreateFundingTx(out FundingTx fundingTx)
        {
            // Create multi-signarture contract address to store deposit
            Contract contract = NeoInterface.CreateMultiSigContract(pubKey, peerPubkey);

            // Assembly transaction with Opcode for both wallets
            string opdata = NeoInterface.CreateOpdata(address, contract.Address, balance, assetId);
            string peerOpdata = NeoInterface.CreateOpdata(peerAddress, contract.Address, peerBalance, assetId);
            Log.Debug("Assembly opdata. opdata: {0}.\r\n peerOpdata: {1}", opdata, peerOpdata);
            
            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Script, this.scriptHash, attributes).MakeAttribute(out attributes);
            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Script, this.peerScriptHash, attributes).MakeAttribute(out attributes);
            new NeoInterface.TransactionAttributeDouble(TransactionAttributeUsage.Remark, this.timestamp, attributes).MakeAttribute(out attributes);

            // Start to makeup the transaction body
            this.GetInvocationTransaction(out Transaction tx, opdata + peerOpdata, attributes);

            // get the witness 
            string witness;
            if (this.scriptHash > this.peerScriptHash)
            {
                witness = "024140{signOther}2321" + this.peerPubkey + "ac" + "4140{signSelf}2321" + this.pubKey + "ac";
            }
            else
            {
                witness = "024140{signSelf}2321" + this.pubKey + "ac" + "4140{signOther}2321" + this.peerPubkey + "ac";
            }

            fundingTx = new FundingTx
            {
                txData = tx.GetHashData().ToHexString().NeoStrip(),
                addressFunding = contract.Address.NeoStrip(),
                txId = tx.Hash.ToString().Strip("\""),
                scriptFunding = contract.Script.ToHexString().NeoStrip(),
                witness = witness,
            };

            this.SetAddressFunding(fundingTx.addressFunding);
            this.SetScripFunding(fundingTx.scriptFunding);

            return true;
        }

        /// <summary>
        /// It helps Trinity to create Commitment transaction for adapting the Neo contract.
        /// </summary>
        /// <param name="commitmentTx"></param>
        /// <returns></returns>
        public bool CreateCTX(out CommitmentTx commitmentTx)
        {
            JObject RSMCContract = NeoInterface.CreateRSMCContract(this.scriptHash, this.pubKey, this.peerScriptHash, this.peerPubkey, this.timestampString);
            Log.Debug("RSMCContract: {0}", RSMCContract);

            this.SetAddressRSMC(RSMCContract["address"].ToString());
            this.SetScripRSMC(RSMCContract["script"].ToString());
            string RSMCContractAddress = NeoInterface.FormatJObject(this.addressRsmc);                                                   //‘›”√

            string opdataRsmc = NeoInterface.CreateOpdata(this.addressFunding, RSMCContractAddress, this.balance, this.assetId);
            Log.Debug("opdataRsmc: {0}", opdataRsmc);

            string peerOpdataRsmc = NeoInterface.CreateOpdata(this.addressFunding, this.peerAddress, this.peerBalance, this.assetId);
            Log.Debug("peerOpdataRsmc: {0}", peerOpdataRsmc);

            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            UInt160 address_hash_funding = NeoInterface.ToScriptHash1(this.addressFunding);
            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Script, address_hash_funding, attributes).MakeAttribute(out attributes);
            new NeoInterface.TransactionAttributeDouble(TransactionAttributeUsage.Remark, this.timestamp, attributes).MakeAttribute(out attributes);

            // 
            this.GetInvocationTransaction(out Transaction tx, opdataRsmc + peerOpdataRsmc, attributes);

            commitmentTx = new CommitmentTx
            {
                txData = tx.GetHashData().ToHexString().NeoStrip(),
                addressRSMC = this.addressRsmc,
                scriptRSMC = this.scriptRsmc,
                txId = tx.Hash.ToString().NeoStrip(),
                witness = "018240{signSelf}40{signOther}da" + this.scriptFunding
            };

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="revocableDeliveryTx"></param>
        /// <param name="txId"></param>
        /// <returns></returns>
        public bool createRDTX(out RevocableDeliveryTx revocableDeliveryTx, string txId)
        {
            string opdata = NeoInterface.CreateOpdata(this.addressRsmc, this.address, this.balance, this.assetId);
            Log.Debug("createRDTX opdata: {0}", opdata);

            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            UInt160 address_hash_RSMC = NeoInterface.ToScriptHash1(this.addressRsmc);
            string preTxId = txId.HexToBytes().Reverse().ToArray().ToHexString().Strip("\"");   //preTxId

            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Script, address_hash_RSMC, attributes).MakeAttribute(out attributes);
            new NeoInterface.TransactionAttributeDouble(TransactionAttributeUsage.Remark, this.timestamp, attributes).MakeAttribute(out attributes);
            new NeoInterface.TransactionAttributeString(TransactionAttributeUsage.Remark1, preTxId, attributes).MakeAttribute(out attributes);
            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Remark2, this.scriptHash, attributes).MakeAttribute(out attributes);

            this.GetInvocationTransaction(out Transaction tx, opdata, attributes);

            revocableDeliveryTx = new RevocableDeliveryTx
            {
                txData = tx.GetHashData().ToHexString().NeoStrip(),
                txId = tx.Hash.ToString().Strip("\""),
                witness = "01{blockheight_script}40{signOther}40{signSelf}fd" + NeoInterface.CreateVerifyScript(this.scriptRsmc)
            };

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="txId"></param>
        /// <returns></returns>
        public bool CreateBRTX(string txId)
        {
            string opdata = NeoInterface.CreateOpdata(this.addressRsmc, this.peerAddress, this.balance, this.assetId);
            Console.WriteLine("createBRTX: opdata: {0}", opdata);

            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            UInt160 address_hash_RSMC = NeoInterface.ToScriptHash1(this.addressRsmc);

            string preTxID = txId.HexToBytes().Reverse().ToArray().ToHexString().Strip("\"");            //preTxId
            UInt160 ScriptHashSelf = NeoInterface.ToScriptHash1(this.peerAddress);                              //outputTo

            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Script, address_hash_RSMC, attributes).MakeAttribute(out attributes);
            new NeoInterface.TransactionAttributeDouble(TransactionAttributeUsage.Remark, this.timestamp, attributes).MakeAttribute(out attributes);
            new NeoInterface.TransactionAttributeString(TransactionAttributeUsage.Remark1, preTxID, attributes).MakeAttribute(out attributes);
            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Remark2, ScriptHashSelf, attributes).MakeAttribute(out attributes);

            this.GetInvocationTransaction(out Transaction tx, opdata, attributes);

            JObject result = new JObject();
            result["txData"] = tx.GetHashData().ToHexString();
            result["txId"] = tx.Hash.ToString();
            result["witness"] = "01{blockheight_script}40{signOther}40{signSelf}fd" + NeoInterface.CreateVerifyScript(this.scriptRsmc);

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settleTx"></param>
        /// <returns></returns>
        public bool CreateSettle(out TxContents settleTx)
        {
            string opdata = NeoInterface.CreateOpdata(this.addressFunding, this.address, this.balance, this.assetId);
            Log.Debug("CreateSettle: opdata: {0}", opdata);

            string peerOpdata = NeoInterface.CreateOpdata(this.addressFunding, this.peerAddress, this.peerBalance, this.assetId);
            Log.Debug("CreateSettle: peerOpdata: {0}", peerOpdata);

            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            UInt160 address_hash_funding = NeoInterface.ToScriptHash1(this.addressFunding);
            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Script, address_hash_funding, attributes).MakeAttribute(out attributes);
            new NeoInterface.TransactionAttributeDouble(TransactionAttributeUsage.Remark, this.timestamp, attributes).MakeAttribute(out attributes);

            this.GetInvocationTransaction(out Transaction tx, opdata + peerOpdata, attributes);

            settleTx = new TxContents
            {
                txData = tx.GetHashData().ToHexString().NeoStrip(),
                txId = tx.Hash.ToString().Strip("\""),
                witness = "018240{signSelf}40{signOther}da" + this.scriptFunding
            };

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        /// Private Method Sets                                                                 ///
        /////////////////////////////////////////////////////////////////////////////////////////// 
        /// <summary>
        /// 
        /// </summary>
        /// <param name="opdata"> Assembly Data of Transaction </param>
        /// <param name="version"> Version of InvocationTransaction </param>
        /// <param name="transaction"> </param>
        private void GetInvocationTransaction(out Transaction transaction, string opdata, List<TransactionAttribute> attributes, byte version=1)
        {
            // return null if no assembly data is input
            if (null == opdata)
            {
                transaction = null;
                return;
            }

            transaction = new InvocationTransaction
            {
                Version = version,
                Script = opdata.NeoStrip().HexToBytes()
            };
            transaction.Attributes = attributes.ToArray();
            transaction.Inputs = new CoinReference[0];
            transaction.Outputs = new TransactionOutput[0];

            return;
        }

        private void SetAddressFunding(string addressFunding)
        {
            this.addressFunding = addressFunding.NeoStrip();
        }

        private void SetScripFunding(string scriptFunding)
        {
            this.scriptFunding = scriptFunding.NeoStrip();
        }

        private void SetAddressRSMC(string addressRsmc)
        {
            this.addressRsmc = addressRsmc.NeoStrip();
        }

        private void SetScripRSMC(string scriptRsmc)
        {
            this.scriptRsmc = scriptRsmc.NeoStrip();
        }
    }
}
