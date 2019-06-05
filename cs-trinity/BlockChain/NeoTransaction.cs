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
#if DEBUG_LOCAL
        private readonly double timestamp = 1554866712.123456; // for test use;
        private readonly string timestampString = "1554866712.123456";
        private readonly long timestampLong = 1554866712;
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

        // HTLC trade information
        private string addressHtlc;
        private string scriptHtlc;

        // Peers of the trade
        // self wallet trade information of the channel
        private readonly string balance;
        private readonly string pubKey;
        private UInt160 scriptHash => (null != this.pubKey) ? this.pubKey.ToHash160() : null;
        private string address => (null != this.pubKey) ? this.pubKey.ToAddress() : null;
        //private string address
        //{
        //    get
        //    {
        //        if (null != this.pubKey)
        //        {
        //            return this.pubKey.ToAddress();
        //        }
        //        return null;
        //    }
        //}

        // peer wallet trade information of the channel
        private readonly string peerBalance;
        private readonly string peerPubkey;
        private UInt160 peerScriptHash => (null != this.peerPubkey) ? this.peerPubkey.ToHash160() : null;
        private string peerAddress => (null != this.peerPubkey) ? this.peerPubkey.ToAddress() : null;
        //private string peerAddress
        //{
        //    get
        //    {
        //        if (null != this.peerPubkey)
        //        {
        //            return this.peerPubkey.ToAddress();
        //        }
        //        return null;
        //    }
        //}

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
            string RSMCContractAddress =this.addressRsmc;                                                   //‘›”√

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
                txId = tx.Hash.ToString().Strip("\""),
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
            string preTxId = txId.NeoStrip().HexToBytes().Reverse().ToArray().ToHexString().Strip("\"");   //preTxId

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
        public bool CreateBRTX(out BreachRemedyTx breachRemedyTx, string txId)
        {
            string opdata = NeoInterface.CreateOpdata(this.addressRsmc, this.peerAddress, this.balance, this.assetId);
            Console.WriteLine("createBRTX: opdata: {0}", opdata);

            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            UInt160 address_hash_RSMC = NeoInterface.ToScriptHash1(this.addressRsmc);

            string preTxID = txId.NeoStrip().HexToBytes().Reverse().ToArray().ToHexString().Strip("\"");            //preTxId
            UInt160 ScriptHashSelf = NeoInterface.ToScriptHash1(this.peerAddress);                              //outputTo

            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Script, address_hash_RSMC, attributes).MakeAttribute(out attributes);
            new NeoInterface.TransactionAttributeDouble(TransactionAttributeUsage.Remark, this.timestamp, attributes).MakeAttribute(out attributes);
            new NeoInterface.TransactionAttributeString(TransactionAttributeUsage.Remark1, preTxID, attributes).MakeAttribute(out attributes);
            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Remark2, ScriptHashSelf, attributes).MakeAttribute(out attributes);

            this.GetInvocationTransaction(out Transaction tx, opdata, attributes);

            breachRemedyTx = new BreachRemedyTx
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

        /// <summary>
        /// It helps Trinity to create Sender HC transaction for adapting the Neo contract.
        /// </summary>
        /// <param name="HCTX"> output the HCTX body </param>
        /// <returns></returns>
        public bool CreateSenderHCTX(out HtlcCommitTx HCTX, string HtlcValue, string balance, string peerBalance, string HashR)
        {
            JObject RSMCContract = NeoInterface.CreateRSMCContract(this.scriptHash, this.pubKey, this.peerScriptHash, this.peerPubkey, this.timestampString);
            Log.Debug("timestamp: {0}", this.timestampString);
            Log.Debug("RSMCContract: {0}", RSMCContract);
#if DEBUG_LOCAL
            JObject HTLCContract = NeoInterface.CreateHTLCContract((this.timestampLong + 600).ToString(), this.pubKey, this.peerPubkey, HashR);
#else
            JObject HTLCContract = NeoInterface.CreateHTLCContract(timestampString, this.pubKey, this.peerPubkey, HashR);
#endif
            Log.Debug("HTLCContract: {0}", HTLCContract);

            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            UInt160 address_hash_funding = NeoInterface.ToScriptHash1(this.addressFunding);
            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Script, address_hash_funding, attributes).MakeAttribute(out attributes);
#if DEBUG_LOCAL
            new NeoInterface.TransactionAttributeLong(TransactionAttributeUsage.Remark, this.timestampLong, attributes).MakeAttribute(out attributes);
#else
            new NeoInterface.TransactionAttributeDouble(TransactionAttributeUsage.Remark, this.timestamp, attributes).MakeAttribute(out attributes);
#endif

            this.SetAddressRSMC(RSMCContract["address"].ToString());
            this.SetScripRSMC(RSMCContract["script"].ToString());
            this.SetAddressHTLC(HTLCContract["address"].ToString());
            this.SetScripHTLC(HTLCContract["script"].ToString());

            string opdataToHTLC = NeoInterface.CreateOpdata(this.addressFunding, this.addressHtlc, HtlcValue, this.assetId);
            Log.Debug("opdataToHTLC: {0}", opdataToHTLC);
            string opdataToRsmc = NeoInterface.CreateOpdata(this.addressFunding, this.addressRsmc, balance, this.assetId);
            Log.Debug("opdataToRsmc: {0}", opdataToRsmc);
            string OpdataToPeer = NeoInterface.CreateOpdata(this.addressFunding, this.peerAddress, peerBalance, this.assetId);
            Log.Debug("OpdataToPeer: {0}", OpdataToPeer);

            this.GetInvocationTransaction(out Transaction tx, opdataToRsmc + OpdataToPeer + opdataToHTLC, attributes);

            HCTX = new HtlcCommitTx
            {
                txData = tx.GetHashData().ToHexString().NeoStrip(),
                addressRSMC = this.addressRsmc,
                addressHTLC = this.addressHtlc,
                scriptRSMC = this.scriptRsmc,
                scriptHTLC = this.scriptHtlc,
                txId = tx.Hash.ToString().Strip("\""),
                witness = "018240{signSelf}40{signOther}da" + this.scriptFunding
            };

            return true;
        }

        /// <summary>
        /// It helps Trinity to create Sender RD transaction for adapting the Neo contract.
        /// </summary>
        /// <param name="RDTX"> output the RDTX body </param>
        /// <returns></returns>
        public bool CreateSenderRDTX(out HtlcRevocableDeliveryTx revocableDeliveryTx, string balance, string txId)
        {
            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            UInt160 address_hash_RSMC = NeoInterface.ToScriptHash1(this.addressRsmc);
            string preTxId = txId.Substring(2).HexToBytes().Reverse().ToArray().ToHexString().Strip("\"");   //preTxId  ???

            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Script, address_hash_RSMC, attributes).MakeAttribute(out attributes);
#if DEBUG_LOCAL
            new NeoInterface.TransactionAttributeLong(TransactionAttributeUsage.Remark, this.timestampLong, attributes).MakeAttribute(out attributes);
#else
            new NeoInterface.TransactionAttributeDouble(TransactionAttributeUsage.Remark, this.timestamp, attributes).MakeAttribute(out attributes);
#endif
            new NeoInterface.TransactionAttributeString(TransactionAttributeUsage.Remark1, preTxId, attributes).MakeAttribute(out attributes);
            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Remark2, this.scriptHash, attributes).MakeAttribute(out attributes);                                         //outPutTo

            string opdata = NeoInterface.CreateOpdata(this.addressRsmc, this.address, balance, this.assetId);
            Log.Debug("createRDTX opdata: {0}", opdata);

            this.GetInvocationTransaction(out Transaction tx, opdata, attributes);

            revocableDeliveryTx = new HtlcRevocableDeliveryTx
            {
                txData = tx.GetHashData().ToHexString().NeoStrip(),
                txId = tx.Hash.ToString().Strip("\""),
                witness = "01{blockheight_script}40{signOther}40{signSelf}fd" + NeoInterface.CreateVerifyScript(this.scriptRsmc)
            };

            return true;
        }

        /// <summary>
        /// It helps Trinity to create Sender HED transaction for adapting the Neo contract.
        /// </summary>
        /// <param name="HEDTX"> output the HEDTX body </param>
        /// <returns></returns>
        public bool CreateHEDTX(out HtlcExecutionDeliveryTx HEDTX, string HtlcValue)
        {
            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            UInt160 address_hash_HTLC = NeoInterface.ToScriptHash1(this.addressHtlc);
            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Script, address_hash_HTLC, attributes).MakeAttribute(out attributes);
#if DEBUG_LOCAL
            new NeoInterface.TransactionAttributeLong(TransactionAttributeUsage.Remark, this.timestampLong, attributes).MakeAttribute(out attributes);
#else
            new NeoInterface.TransactionAttributeDouble(TransactionAttributeUsage.Remark, this.timestamp, attributes).MakeAttribute(out attributes);
#endif

            string opdata = NeoInterface.CreateOpdata(this.addressHtlc, this.peerAddress, HtlcValue, this.assetId);
            Log.Debug("createRDTX opdata_to_receiver: {0}", opdata);

            this.GetInvocationTransaction(out Transaction tx, opdata, attributes);

            HEDTX = new HtlcExecutionDeliveryTx
            {
                txData = tx.GetHashData().ToHexString().NeoStrip(),
                txId = tx.Hash.ToString().Strip("\""),
                witness = "01{blockheight_script}40{signOther}40{signSelf}fd" + NeoInterface.CreateVerifyScript(this.scriptHtlc)
            };

            return true;
        }

        /// <summary>
        /// It helps Trinity to create Sender HT transaction for adapting the Neo contract.
        /// </summary>
        /// <param name="HTTX"> output the HTTX body </param>
        /// <returns></returns>
        public bool CreateHTTX(out HtlcTimoutTx HTTX, string HtlcValue)
        {
            JObject RSMCContract = NeoInterface.CreateRSMCContract(this.scriptHash, this.pubKey, this.peerScriptHash, this.peerPubkey, this.timestampString);
            Log.Debug("RSMCContract: {0}", RSMCContract);
            this.SetAddressRSMC(RSMCContract["address"].ToString());
            this.SetScripRSMC(RSMCContract["script"].ToString());

            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            UInt160 address_hash_HTLC = NeoInterface.ToScriptHash1(this.addressHtlc);
            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Script, address_hash_HTLC, attributes).MakeAttribute(out attributes);
#if DEBUG_LOCAL
            new NeoInterface.TransactionAttributeLong(TransactionAttributeUsage.Remark, this.timestampLong, attributes).MakeAttribute(out attributes);
#else
            new NeoInterface.TransactionAttributeDouble(TransactionAttributeUsage.Remark, this.timestamp, attributes).MakeAttribute(out attributes);
#endif

            string opdata = NeoInterface.CreateOpdata(this.addressHtlc, this.addressRsmc, HtlcValue, this.assetId);
            Log.Debug("createRDTX opdata_to_receiver: {0}", opdata);

            this.GetInvocationTransaction(out Transaction tx, opdata, attributes);

            HTTX = new HtlcTimoutTx
            {
                txData = tx.GetHashData().ToHexString().NeoStrip(),
                txId = tx.Hash.ToString().Strip("\""),
                addressRSMC = this.addressRsmc,
                scriptRSMC = this.scriptRsmc,
                witness = "01{blockheight_script}40{signOther}40{signSelf}fd" + NeoInterface.CreateVerifyScript(this.scriptHtlc)
            };

            return true;
        }

        /// <summary>
        /// It helps Trinity to create Sender HT transaction for adapting the Neo contract.
        /// </summary>
        /// <param name="HTRDTX"> output the HTTX body </param>
        /// <returns></returns>
        public bool CreateHTRDTX(out HtlcTimeoutRevocableDelivertyTx revocableDeliveryTx, string txId, string HtlcValue)
        {
            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            UInt160 address_hash_RSMC = NeoInterface.ToScriptHash1(this.addressRsmc);
            string preTxId = txId.Substring(2).HexToBytes().Reverse().ToArray().ToHexString().Strip("\"");   //preTxId ???

            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Script, address_hash_RSMC, attributes).MakeAttribute(out attributes);
#if DEBUG_LOCAL
            new NeoInterface.TransactionAttributeLong(TransactionAttributeUsage.Remark, this.timestampLong, attributes).MakeAttribute(out attributes);
#else
            new NeoInterface.TransactionAttributeDouble(TransactionAttributeUsage.Remark, this.timestamp, attributes).MakeAttribute(out attributes);
#endif
            new NeoInterface.TransactionAttributeString(TransactionAttributeUsage.Remark1, preTxId, attributes).MakeAttribute(out attributes);
            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Remark2, this.scriptHash, attributes).MakeAttribute(out attributes);                                         //outPutTo

            string opdata = NeoInterface.CreateOpdata(this.addressRsmc, this.address, HtlcValue, this.assetId);
            Log.Debug("CreateHTRDTX opdata: {0}", opdata);

            this.GetInvocationTransaction(out Transaction tx, opdata, attributes);

            revocableDeliveryTx = new HtlcTimeoutRevocableDelivertyTx
            {
                txData = tx.GetHashData().ToHexString().NeoStrip(),
                txId = tx.Hash.ToString().Strip("\""),
                witness = "01{blockheight_script}40{signOther}40{signSelf}fd" + NeoInterface.CreateVerifyScript(this.scriptRsmc)
            };

            return true;
        }

        /// <summary>
        /// It helps Trinity to create Sender HC transaction for adapting the Neo contract.
        /// </summary>
        /// <param name="HCTX"> output the HCTX body </param>
        /// <returns></returns>
        public bool CreateReceiverHCTX(out HtlcCommitTx HCTX, string HtlcValue, string balance, string peerBalance, string HashR)
        {
            JObject RSMCContract = NeoInterface.CreateRSMCContract(this.peerScriptHash, this.peerPubkey, this.scriptHash, this.pubKey, this.timestampString);
            Log.Debug("timestamp: {0}", this.timestampString);
            Log.Debug("RSMCContract: {0}", RSMCContract);
#if DEBUG_LOCAL
            JObject HTLCContract = NeoInterface.CreateHTLCContract((this.timestampLong + 600).ToString(), this.peerPubkey, this.pubKey, HashR);
#else
            JObject HTLCContract = NeoInterface.CreateHTLCContract(this.timestampString, this.peerPubkey, this.pubKey, HashR);
#endif
            Log.Debug("HTLCContract: {0}", HTLCContract);

            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            UInt160 address_hash_funding = NeoInterface.ToScriptHash1(this.addressFunding);
            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Script, address_hash_funding, attributes).MakeAttribute(out attributes);
#if DEBUG_LOCAL
            new NeoInterface.TransactionAttributeLong(TransactionAttributeUsage.Remark, this.timestampLong, attributes).MakeAttribute(out attributes);
#else
            new NeoInterface.TransactionAttributeDouble(TransactionAttributeUsage.Remark, this.timestamp, attributes).MakeAttribute(out attributes);
#endif

            this.SetAddressRSMC(RSMCContract["address"].ToString());
            this.SetScripRSMC(RSMCContract["script"].ToString());
            this.SetAddressHTLC(HTLCContract["address"].ToString());
            this.SetScripHTLC(HTLCContract["script"].ToString());

            string opdataToHTLC = NeoInterface.CreateOpdata(this.addressFunding, this.addressHtlc, HtlcValue, this.assetId);
            Log.Debug("opdataToHTLC: {0}", opdataToHTLC);
            string opdataToRsmc = NeoInterface.CreateOpdata(this.addressFunding, this.addressRsmc, peerBalance, this.assetId);
            Log.Debug("opdataToRsmc: {0}", opdataToRsmc);
            string OpdataToSender = NeoInterface.CreateOpdata(this.addressFunding, this.address, balance, this.assetId);
            Log.Debug("OpdataToSender: {0}", OpdataToSender);

            this.GetInvocationTransaction(out Transaction tx, opdataToRsmc + OpdataToSender + opdataToHTLC, attributes);

            HCTX = new HtlcCommitTx
            {
                txData = tx.GetHashData().ToHexString().NeoStrip(),
                addressRSMC = this.addressRsmc,
                addressHTLC = this.addressHtlc,
                scriptRSMC = this.scriptRsmc,
                scriptHTLC = this.scriptHtlc,
                txId = tx.Hash.ToString().Strip("\""),
                witness = "018240{signSelf}40{signOther}da" + this.scriptFunding
            };

            return true;
        }

        /// <summary>
        /// It helps Trinity to create Sender RD transaction for adapting the Neo contract.
        /// </summary>
        /// <param name="RDTX"> output the RDTX body </param>
        /// <returns></returns>
        public bool CreateReceiverRDTX(out HtlcRevocableDeliveryTx revocableDeliveryTx, string balance, string txId)
        {
            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            UInt160 address_hash_RSMC = NeoInterface.ToScriptHash1(this.addressRsmc);
            string preTxId = txId.Substring(2).HexToBytes().Reverse().ToArray().ToHexString().Strip("\"");   //preTxId  ???

            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Script, address_hash_RSMC, attributes).MakeAttribute(out attributes);
#if DEBUG_LOCAL
            new NeoInterface.TransactionAttributeLong(TransactionAttributeUsage.Remark, this.timestampLong, attributes).MakeAttribute(out attributes);
#else
            new NeoInterface.TransactionAttributeDouble(TransactionAttributeUsage.Remark, this.timestamp, attributes).MakeAttribute(out attributes);
#endif
            new NeoInterface.TransactionAttributeString(TransactionAttributeUsage.Remark1, preTxId, attributes).MakeAttribute(out attributes);
            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Remark2, this.peerScriptHash, attributes).MakeAttribute(out attributes);                                         //outPutTo

            string opdata = NeoInterface.CreateOpdata(this.addressRsmc, this.peerAddress, balance, this.assetId);
            Log.Debug("createRDTX opdata: {0}", opdata);

            this.GetInvocationTransaction(out Transaction tx, opdata, attributes);

            revocableDeliveryTx = new HtlcRevocableDeliveryTx
            {
                txData = tx.GetHashData().ToHexString().NeoStrip(),
                txId = tx.Hash.ToString().Strip("\""),
                witness = "01{blockheight_script}40{signOther}40{signSelf}fd" + NeoInterface.CreateVerifyScript(this.scriptRsmc)
            };

            return true;
        }

        //createHTDTX(addressHTLC, pubkeySender, HTLCValue, HTLCScript, asset_id)
        /// <summary>
        /// It helps Trinity to create Sender HTD transaction for adapting the Neo contract.
        /// </summary>
        /// <param name="HTDTX"> output the HTDTX body </param>
        /// <returns></returns>
        public bool CreateHTDTX(out HtlcTimeoutDeliveryTx HTDTX, string HtlcValue)
        {
            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            UInt160 address_hash_HTLC = NeoInterface.ToScriptHash1(this.addressHtlc);
            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Script, address_hash_HTLC, attributes).MakeAttribute(out attributes);
#if DEBUG_LOCAL
            new NeoInterface.TransactionAttributeLong(TransactionAttributeUsage.Remark, this.timestampLong, attributes).MakeAttribute(out attributes);
#else
            new NeoInterface.TransactionAttributeDouble(TransactionAttributeUsage.Remark, this.timestamp, attributes).MakeAttribute(out attributes);
#endif

            string opdata = NeoInterface.CreateOpdata(this.addressHtlc, this.peerAddress, HtlcValue, this.assetId);
            Log.Debug("createRDTX opdata_to_receiver: {0}", opdata);

            this.GetInvocationTransaction(out Transaction tx, opdata, attributes);

            HTDTX = new HtlcTimeoutDeliveryTx
            {
                txData = tx.GetHashData().ToHexString().NeoStrip(),
                txId = tx.Hash.ToString().Strip("\""),
                witness = "01{blockheight_script}40{signOther}40{signSelf}fd" + NeoInterface.CreateVerifyScript(this.scriptHtlc)
            };

            return true;
        }

        /// <summary>
        /// It helps Trinity to create Sender HT transaction for adapting the Neo contract.
        /// </summary>
        /// <param name="HETX"> output the HTTX body </param>
        /// <returns></returns>
        public bool CreateHETX(out HtlcExecutionTx HETX, string HtlcValue)
        {
            JObject RSMCContract = NeoInterface.CreateRSMCContract(this.peerScriptHash, this.peerPubkey, this.scriptHash, this.pubKey, this.timestampString);
            Log.Debug("RSMCContract: {0}", RSMCContract);
            this.SetAddressRSMC(RSMCContract["address"].ToString());
            this.SetScripRSMC(RSMCContract["script"].ToString());

            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            UInt160 address_hash_HTLC = NeoInterface.ToScriptHash1(this.addressHtlc);
            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Script, address_hash_HTLC, attributes).MakeAttribute(out attributes);
#if DEBUG_LOCAL
            new NeoInterface.TransactionAttributeLong(TransactionAttributeUsage.Remark, this.timestampLong, attributes).MakeAttribute(out attributes);
#else
            new NeoInterface.TransactionAttributeDouble(TransactionAttributeUsage.Remark, this.timestamp, attributes).MakeAttribute(out attributes);
#endif

            string opdata = NeoInterface.CreateOpdata(this.addressHtlc, this.addressRsmc, HtlcValue, this.assetId);
            Log.Debug("createRDTX opdata_to_receiver: {0}", opdata);

            this.GetInvocationTransaction(out Transaction tx, opdata, attributes);

            HETX = new HtlcExecutionTx
            {
                txData = tx.GetHashData().ToHexString().NeoStrip(),
                txId = tx.Hash.ToString().Strip("\""),
                addressRSMC = this.addressRsmc,
                scriptRSMC = this.scriptRsmc,
                witness = "01{blockheight_script}40{signOther}40{signSelf}fd" + NeoInterface.CreateVerifyScript(this.scriptHtlc)
            };

            return true;
        }

        /// <summary>
        /// It helps Trinity to create Sender HT transaction for adapting the Neo contract.
        /// </summary>
        /// <param name="HERDTX"> output the HTTX body </param>
        /// <returns></returns>
        public bool CreateHERDTX(out HtlcTimeoutRevocableDelivertyTx revocableDeliveryTx, string txId, string HtlcValue)
        {
            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            UInt160 address_hash_RSMC = NeoInterface.ToScriptHash1(this.addressRsmc);
            string preTxId = txId.Substring(2).HexToBytes().Reverse().ToArray().ToHexString().Strip("\"");   //preTxId ???

            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Script, address_hash_RSMC, attributes).MakeAttribute(out attributes);
#if DEBUG_LOCAL
            new NeoInterface.TransactionAttributeLong(TransactionAttributeUsage.Remark, this.timestampLong, attributes).MakeAttribute(out attributes);
#else
            new NeoInterface.TransactionAttributeDouble(TransactionAttributeUsage.Remark, this.timestamp, attributes).MakeAttribute(out attributes);
#endif
            new NeoInterface.TransactionAttributeString(TransactionAttributeUsage.Remark1, preTxId, attributes).MakeAttribute(out attributes);
            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Remark2, this.scriptHash, attributes).MakeAttribute(out attributes);                                         //outPutTo

            string opdata = NeoInterface.CreateOpdata(this.addressRsmc, this.address, HtlcValue, this.assetId);
            Log.Debug("CreateHTRDTX opdata: {0}", opdata);

            this.GetInvocationTransaction(out Transaction tx, opdata, attributes);

            revocableDeliveryTx = new HtlcTimeoutRevocableDelivertyTx
            {
                txData = tx.GetHashData().ToHexString().NeoStrip(),
                txId = tx.Hash.ToString().Strip("\""),
                witness = "01{blockheight_script}40{signOther}40{signSelf}fd" + NeoInterface.CreateVerifyScript(this.scriptRsmc)
            };

            return true;
        }

        /// <summary>
        /// It helps Trinity to create random for adapting the Neo contract.
        /// </summary>
        /// <param name="Length"> output the length of R </param>
        /// <returns></returns>
        public string CreateR(int Length)
        {
            char[] constant ={
                '0','1','2','3','4','5','6','7','8','9','a','b','c','d','e','f'
            };
            System.Text.StringBuilder newRandom = new System.Text.StringBuilder(16);
            Random rd = new Random();
            for (int i = 0; i < Length; i++)
            {
                newRandom.Append(constant[rd.Next(16)]);
            }
            return newRandom.ToString();
        }

        public void SetAddressFunding(string addressFunding)
        {
            this.addressFunding = addressFunding.NeoStrip();
        }

        public void SetScripFunding(string scriptFunding)
        {
            this.scriptFunding = scriptFunding.NeoStrip();
        }

        public void SetAddressRSMC(string addressRsmc)
        {
            this.addressRsmc = addressRsmc.NeoStrip();
        }

        public void SetAddressHTLC(string addressHtlc)
        {
            this.addressHtlc = addressHtlc.NeoStrip();
        }

        public void SetScripRSMC(string scriptRsmc)
        {
            this.scriptRsmc = scriptRsmc.NeoStrip();
        }

        public void SetScripHTLC(string scriptHtlc)
        {
            this.scriptHtlc = scriptHtlc.NeoStrip();
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
        private void GetInvocationTransaction(out Transaction transaction, string opdata, List<TransactionAttribute> attributes, byte version = 1)
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
    }
}
