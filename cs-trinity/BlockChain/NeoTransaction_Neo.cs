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
using Neo.Wallets;
using Neo.SmartContract;
using Neo.IO.Json;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;

using Trinity;
using Trinity.BlockChain;
using Trinity.Wallets.Templates.Definitions;
using MessagePack;
using Trinity.Wallets;
using static Trinity.BlockChain.NeoInterface;




namespace Trinity.BlockChain
{

    /// <summary>
    /// This class is used to adapt to NEO smartconcact.
    /// It helps Trinity broadcast the transaction to the NEO blockchain. 
    /// </summary>
    public sealed class NeoTransaction_Neo
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
        private string fundingTxId;

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

        // remote wallet trade information of the channel
        private readonly string peerBalance;
        private readonly string peerPubkey;
        private UInt160 peerScriptHash => (null != this.peerPubkey) ? this.peerPubkey.ToHash160() : null;
        private string peerAddress => (null != this.peerPubkey) ? this.peerPubkey.ToAddress() : null;

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
        public NeoTransaction_Neo(string assetId, string pubKey, string balance, string peerPubKey, string peerBalance,
            string addressFunding = null, string scriptFunding = null)
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
            Log.Debug("Assembly contract: {0}.\r\n", contract);
            // Assembly transaction with vout for both wallets
#if DEBUG_LOCAL
            List<string> vouts = new List<string>();
            Vin vout = new Vin
            {
                n = 0,
                txid = "d12cd540f9298fd07a4f70ff02581dd1fb2414947a0da4a550b06ec6f0c0eba9",
                value = 2
            };
            string voutData = MessagePackSerializer.ToJson(MessagePackSerializer.Serialize(vout));
            vouts.Add(voutData);
#else
            List<string> vouts = NeoInterface.getGloablAssetVout(UInt160.Parse(pubKey), assetId, uint.Parse(balance));
#endif
            Log.Debug("Assembly vouts. vouts: {0}.\r\n", vouts);

#if DEBUG_LOCAL
            List<string> peerVouts = new List<string>();
            Vin peerVout = new Vin
            {
                n = 0,
                txid = "a2af30d58b2e90275f5251378eccbaa8fe8eff76d4016df5337d6b2f6608dace",
                value = 2
            };
            string peervoutData = MessagePackSerializer.ToJson(MessagePackSerializer.Serialize(peerVout));
            peerVouts.Add(peervoutData);
#else
            List<string> peerVouts = NeoInterface.getGloablAssetVout(UInt160.Parse(pubKey), assetId, uint.Parse(peerBalance));
#endif
            Log.Debug("Assembly vouts. peerVouts: {0}.\r\n", peerVouts);

            // Assembly transaction with input for both wallets
            CoinReference[] self_inputs = getInputFormVout(vouts);
            CoinReference[] other_inputs = getInputFormVout(peerVouts);
            CoinReference[] inputsData = self_inputs.Concat(other_inputs).ToArray();

            string amount = (uint.Parse(balance) + uint.Parse(peerBalance)).ToString();
            TransactionOutput[] output_to_fundingaddress = createOutput(assetId, amount, contract.Address);

            // Assembly transaction with output for both wallets
            long totalInputs = getTotalInputs(vouts);
            long peerTotalInputs = getTotalInputs(peerVouts);

            TransactionOutput[] output_to_self = new TransactionOutput[] { };
            TransactionOutput[] output_to_other = new TransactionOutput[] { };
            if (totalInputs > long.Parse(balance))
            {
                string selfBalance = (totalInputs - long.Parse(balance)).ToString();
                output_to_self = createOutput(assetId, selfBalance, address);
            }
            if (peerTotalInputs > long.Parse(balance))
            {
                string otherBalance = (peerTotalInputs - long.Parse(balance)).ToString();
                output_to_other = createOutput(assetId, otherBalance, peerAddress);
            }
            TransactionOutput[] outputsData = output_to_fundingaddress.Concat(output_to_self).Concat(output_to_other).ToArray();

            // Assembly transaction with attributes for both wallets
            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
#if DEBUG_LOCAL
            new NeoInterface.TransactionAttributeLong(TransactionAttributeUsage.Remark, timestampLong, attributes).MakeAttribute(out attributes);
#else
            new NeoInterface.TransactionAttributeDouble(TransactionAttributeUsage.Remark, this.timestamp, attributes).MakeAttribute(out attributes);
#endif

            // Start to makeup the transaction body
            this.GetContractTransaction(out ContractTransaction tx, inputsData, outputsData, attributes);

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
                witness = witness
            };

            this.SetFundingTxId(fundingTx.txId);
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
            string RSMCContractAddress = this.addressRsmc;

            // Assembly transaction with input for both wallets
            CoinReference[] inputsData = createInputsData(this.fundingTxId, 0);

            // Assembly transaction with output for both wallets
            TransactionOutput[] outputToRSMC = createOutput(assetId, this.balance, RSMCContractAddress);
            TransactionOutput[] outputToOther = createOutput(assetId, this.peerBalance, peerAddress);
            TransactionOutput[] outputsData = outputToRSMC.Concat(outputToOther).ToArray();

            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
#if DEBUG_LOCAL
            new NeoInterface.TransactionAttributeLong(TransactionAttributeUsage.Remark, timestampLong, attributes).MakeAttribute(out attributes);
#else
            new NeoInterface.TransactionAttributeDouble(TransactionAttributeUsage.Remark, this.timestamp, attributes).MakeAttribute(out attributes);
#endif

            this.GetContractTransaction(out ContractTransaction tx, inputsData, outputsData, attributes);

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
        public bool CreateRDTX(out RevocableDeliveryTx revocableDeliveryTx, string txId)
        {
            // Assembly transaction with input for both wallets
            CoinReference[] inputsData = createInputsData(txId, 0);

            // Assembly transaction with output for both wallets
            TransactionOutput[] outputsData = createOutput(this.assetId, this.balance, this.address);

            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            UInt160 addressHash = this.address.ToScriptHash();
            string preTxId = txId.NeoStrip().HexToBytes().Reverse().ToArray().ToHexString().Strip("\"");   //preTxId

#if DEBUG_LOCAL
            new NeoInterface.TransactionAttributeLong(TransactionAttributeUsage.Remark, timestampLong, attributes).MakeAttribute(out attributes);
#else
            new NeoInterface.TransactionAttributeDouble(TransactionAttributeUsage.Remark, this.timestamp, attributes).MakeAttribute(out attributes);
#endif
            new NeoInterface.TransactionAttributeString(TransactionAttributeUsage.Remark1, preTxId, attributes).MakeAttribute(out attributes);
            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Remark2, addressHash, attributes).MakeAttribute(out attributes);

            this.GetContractTransaction(out ContractTransaction tx, inputsData, outputsData, attributes);

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
            // Assembly transaction with input for both wallets
            CoinReference[] inputsData = createInputsData(txId, 0);

            // Assembly transaction with output for both wallets
            TransactionOutput[] outputsData = createOutput(assetId, this.balance, this.peerAddress);

            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            UInt160 peerAddressHash = this.peerAddress.ToScriptHash();

            string preTxID = txId.NeoStrip().HexToBytes().Reverse().ToArray().ToHexString().Strip("\"");            //preTxId
            UInt160 ScriptHashSelf = this.peerAddress.ToScriptHash();                              //outputTo

#if DEBUG_LOCAL
            new NeoInterface.TransactionAttributeLong(TransactionAttributeUsage.Remark, timestampLong, attributes).MakeAttribute(out attributes);
#else
            new NeoInterface.TransactionAttributeDouble(TransactionAttributeUsage.Remark, this.timestamp, attributes).MakeAttribute(out attributes);
#endif
            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Remark1, peerAddressHash, attributes).MakeAttribute(out attributes);

            this.GetContractTransaction(out ContractTransaction tx, inputsData, outputsData, attributes);

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
            Log.Debug("addressFunding: {0}.\r\n", this.addressFunding);
            UInt160 fundingScriptHash = this.addressFunding.ToScriptHash();
            uint amount = uint.Parse(this.balance) + uint.Parse(this.peerBalance);

            // Assembly transaction with input for both wallets
#if DEBUG_LOCAL
            List<string> vouts = new List<string>();
            Vin vout = new Vin
            {
                n = 0,
                txid = "101c5e988e72bdabb121350f168bef8965f0fd2891c387b5384e9718c32c2c7d",
                value = 2
            };
            string voutData = MessagePackSerializer.ToJson(MessagePackSerializer.Serialize(vout));
            vouts.Add(voutData);
#else
            List<string> vouts = getGloablAssetVout(fundingScriptHash, assetId, amount);
#endif
            CoinReference[] inputsData = getInputFormVout(vouts);
            Log.Debug("Assembly vouts. vouts: {0}.\r\n", vouts);

            // Assembly transaction with output for both wallets
            TransactionOutput[] outputToSelf = createOutput(assetId, this.balance, this.address, true);
            TransactionOutput[] outputToOther = createOutput(assetId, this.peerBalance, peerAddress, true);
            TransactionOutput[] outputsData = outputToSelf.Concat(outputToOther).ToArray();

            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
#if DEBUG_LOCAL
            new NeoInterface.TransactionAttributeLong(TransactionAttributeUsage.Remark, timestampLong, attributes).MakeAttribute(out attributes);
#else
            new NeoInterface.TransactionAttributeDouble(TransactionAttributeUsage.Remark, this.timestamp, attributes).MakeAttribute(out attributes);
#endif

            this.GetContractTransaction(out ContractTransaction tx, inputsData, outputsData, attributes);

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
        public bool CreateSenderHCTX(out HtlcCommitTx HCTX, string HtlcValue, string HashR)
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
#if DEBUG_LOCAL
            new NeoInterface.TransactionAttributeLong(TransactionAttributeUsage.Remark, this.timestampLong, attributes).MakeAttribute(out attributes);
#else
            new NeoInterface.TransactionAttributeDouble(TransactionAttributeUsage.Remark, this.timestamp, attributes).MakeAttribute(out attributes);
#endif

            this.SetAddressRSMC(RSMCContract["address"].ToString());
            this.SetScripRSMC(RSMCContract["script"].ToString());
            this.SetAddressHTLC(HTLCContract["address"].ToString());
            this.SetScripHTLC(HTLCContract["script"].ToString());

            UInt160 fundingScriptHash = this.addressFunding.ToScriptHash();
            uint amount = uint.Parse(this.balance) + uint.Parse(this.peerBalance) + uint.Parse(HtlcValue);

            // Assembly transaction with input for both wallets
#if DEBUG_LOCAL
            List<string> vouts = new List<string>();
            Vin vout = new Vin
            {
                n = 0,
                txid = "0x577fb4c3ca37a5eab7243478f3eae2b011800a10d5c4c0cd85e71bab52e76a79",
                value = 2
            };
            string voutData = MessagePackSerializer.ToJson(MessagePackSerializer.Serialize(vout));
            vouts.Add(voutData);
#else
            List<string> vouts = getGloablAssetVout(fundingScriptHash, assetId, amount);
#endif
            CoinReference[] inputsData = getInputFormVout(vouts);

            // Assembly transaction with output for both wallets
            TransactionOutput[] outputToRMSC = createOutput(assetId, this.balance, this.addressRsmc, true);
            TransactionOutput[] outputToReceiver = createOutput(assetId, this.peerBalance, peerAddress, true);
            TransactionOutput[] outputToHtlc = createOutput(assetId, HtlcValue, this.addressHtlc, true);
            TransactionOutput[] outputsData = outputToRMSC.Concat(outputToReceiver).Concat(outputToHtlc).ToArray();

            this.GetContractTransaction(out ContractTransaction tx, inputsData, outputsData, attributes);

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
        public bool CreateSenderRDTX(out HtlcRevocableDeliveryTx revocableDeliveryTx, string txId)
        {
            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            string preTxId = txId.Substring(2).HexToBytes().Reverse().ToArray().ToHexString().Strip("\"");   //preTxId  ???
#if DEBUG_LOCAL
            new NeoInterface.TransactionAttributeLong(TransactionAttributeUsage.Remark, this.timestampLong, attributes).MakeAttribute(out attributes);
#else
            new NeoInterface.TransactionAttributeDouble(TransactionAttributeUsage.Remark, this.timestamp, attributes).MakeAttribute(out attributes);
#endif
            new NeoInterface.TransactionAttributeString(TransactionAttributeUsage.Remark1, preTxId, attributes).MakeAttribute(out attributes);
            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Remark2, this.scriptHash, attributes).MakeAttribute(out attributes);                                         //outPutTo

            // Assembly transaction with input for both wallets
            CoinReference[] inputsData = createInputsData(txId, 0);

            // Assembly transaction with output for both wallets
            TransactionOutput[] outputsData = createOutput(assetId, this.balance, this.address);

            this.GetContractTransaction(out ContractTransaction tx, inputsData, outputsData, attributes);

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
        public bool CreateHEDTX(out HtlcExecutionDeliveryTx HEDTX, string txId, string HtlcValue)
        {
            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
#if DEBUG_LOCAL
            new NeoInterface.TransactionAttributeLong(TransactionAttributeUsage.Remark, this.timestampLong, attributes).MakeAttribute(out attributes);
#else
            new NeoInterface.TransactionAttributeDouble(TransactionAttributeUsage.Remark, this.timestamp, attributes).MakeAttribute(out attributes);
#endif

            string opdata = NeoInterface.CreateOpdata(this.addressHtlc, this.peerAddress, HtlcValue, this.assetId);
            Log.Debug("createRDTX opdata_to_receiver: {0}", opdata);

            // Assembly transaction with input for both wallets
            CoinReference[] inputsData = createInputsData(txId, 2);

            // Assembly transaction with output for both wallets
            TransactionOutput[] outputsData = createOutput(assetId, HtlcValue, this.peerAddress);

            this.GetContractTransaction(out ContractTransaction tx, inputsData, outputsData, attributes);

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
        public bool CreateHTTX(out HtlcTimoutTx HTTX, string txId, string HtlcValue)
        {
            JObject RSMCContract = NeoInterface.CreateRSMCContract(this.scriptHash, this.pubKey, this.peerScriptHash, this.peerPubkey, this.timestampString);
            Log.Debug("RSMCContract: {0}", RSMCContract);
            this.SetAddressRSMC(RSMCContract["address"].ToString());
            this.SetScripRSMC(RSMCContract["script"].ToString());

            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
#if DEBUG_LOCAL
            new NeoInterface.TransactionAttributeLong(TransactionAttributeUsage.Remark, this.timestampLong, attributes).MakeAttribute(out attributes);
#else
            new NeoInterface.TransactionAttributeDouble(TransactionAttributeUsage.Remark, this.timestamp, attributes).MakeAttribute(out attributes);
#endif

            // Assembly transaction with input for both wallets
            CoinReference[] inputsData = createInputsData(txId, 2);

            // Assembly transaction with output for both wallets
            TransactionOutput[] outputsData = createOutput(assetId, HtlcValue, this.addressRsmc);

            this.GetContractTransaction(out ContractTransaction tx, inputsData, outputsData, attributes);

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
            string preTxId = txId.Substring(2).HexToBytes().Reverse().ToArray().ToHexString().Strip("\"");   //preTxId ???
#if DEBUG_LOCAL
            new NeoInterface.TransactionAttributeLong(TransactionAttributeUsage.Remark, this.timestampLong, attributes).MakeAttribute(out attributes);
#else
            new NeoInterface.TransactionAttributeDouble(TransactionAttributeUsage.Remark, this.timestamp, attributes).MakeAttribute(out attributes);
#endif
            new NeoInterface.TransactionAttributeString(TransactionAttributeUsage.Remark1, preTxId, attributes).MakeAttribute(out attributes);
            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Remark2, this.scriptHash, attributes).MakeAttribute(out attributes);                                         //outPutTo

            string opdata = NeoInterface.CreateOpdata(this.addressRsmc, this.address, HtlcValue, this.assetId);
            Log.Debug("CreateHTRDTX opdata: {0}", opdata);

            // Assembly transaction with input for both wallets
            CoinReference[] inputsData = createInputsData(txId, 0);

            // Assembly transaction with output for both wallets
            TransactionOutput[] outputsData = createOutput(assetId, HtlcValue, this.address);

            this.GetContractTransaction(out ContractTransaction tx, inputsData, outputsData, attributes);

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
        /// attention: balance is Receiver balance, peerBalance is Sender balance
        public bool CreateReceiverHCTX(out HtlcCommitTx HCTX, string HtlcValue, string HashR)
        {
            JObject RSMCContract = NeoInterface.CreateRSMCContract(this.scriptHash, this.pubKey, this.peerScriptHash, this.peerPubkey, this.timestampString);
            Log.Debug("timestamp: {0}", this.timestampString);
            Log.Debug("RSMCContract: {0}", RSMCContract);
#if DEBUG_LOCAL
            JObject HTLCContract = NeoInterface.CreateHTLCContract((this.timestampLong + 600).ToString(), this.pubKey, this.peerPubkey, HashR);
#else
            JObject HTLCContract = NeoInterface.CreateHTLCContract(this.timestampString, this.peerPubkey, this.pubKey, HashR);
#endif
            Log.Debug("HTLCContract: {0}", HTLCContract);

            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
#if DEBUG_LOCAL
            new NeoInterface.TransactionAttributeLong(TransactionAttributeUsage.Remark, this.timestampLong, attributes).MakeAttribute(out attributes);
#else
            new NeoInterface.TransactionAttributeDouble(TransactionAttributeUsage.Remark, this.timestamp, attributes).MakeAttribute(out attributes);
#endif

            this.SetAddressRSMC(RSMCContract["address"].ToString());
            this.SetScripRSMC(RSMCContract["script"].ToString());
            this.SetAddressHTLC(HTLCContract["address"].ToString());
            this.SetScripHTLC(HTLCContract["script"].ToString());

            UInt160 fundingScriptHash = this.addressFunding.ToScriptHash();
            uint amount = uint.Parse(this.balance) + uint.Parse(this.peerBalance) + uint.Parse(HtlcValue);

            // Assembly transaction with input for both wallets
#if DEBUG_LOCAL
            List<string> vouts = new List<string>();
            Vin vout = new Vin
            {
                n = 0,
                txid = "0x577fb4c3ca37a5eab7243478f3eae2b011800a10d5c4c0cd85e71bab52e76a79",
                value = 2
            };
            string voutData = MessagePackSerializer.ToJson(MessagePackSerializer.Serialize(vout));
            vouts.Add(voutData);
#else
            List<string> vouts = getGloablAssetVout(fundingScriptHash, assetId, amount);
#endif
            CoinReference[] inputsData = getInputFormVout(vouts);

            // Assembly transaction with output for both wallets
            TransactionOutput[] outputToRMSC = createOutput(assetId, this.balance, this.addressRsmc, true);
            TransactionOutput[] outputToSender = createOutput(assetId, this.peerBalance, this.peerAddress, true);
            TransactionOutput[] outputToHtlc = createOutput(assetId, HtlcValue, this.addressHtlc, true);
            TransactionOutput[] outputsData = outputToRMSC.Concat(outputToSender).Concat(outputToHtlc).ToArray();

            this.GetContractTransaction(out ContractTransaction tx, inputsData, outputsData, attributes);

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
        /// attention: balance is Receiver balance, peerBalance is Sender balance
        public bool CreateReceiverRDTX(out HtlcRevocableDeliveryTx revocableDeliveryTx, string txId)
        {
            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            string preTxId = txId.Substring(2).HexToBytes().Reverse().ToArray().ToHexString().Strip("\"");   //preTxId  ???
#if DEBUG_LOCAL
            new NeoInterface.TransactionAttributeLong(TransactionAttributeUsage.Remark, this.timestampLong, attributes).MakeAttribute(out attributes);
#else
            new NeoInterface.TransactionAttributeDouble(TransactionAttributeUsage.Remark, this.timestamp, attributes).MakeAttribute(out attributes);
#endif
            new NeoInterface.TransactionAttributeString(TransactionAttributeUsage.Remark1, preTxId, attributes).MakeAttribute(out attributes);
            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Remark2, this.scriptHash, attributes).MakeAttribute(out attributes);                    //outPutTo

            // Assembly transaction with input for both wallets
            CoinReference[] inputsData = createInputsData(txId, 0);

            // Assembly transaction with output for both wallets
            TransactionOutput[] outputsData = createOutput(assetId, this.balance, this.address);

            this.GetContractTransaction(out ContractTransaction tx, inputsData, outputsData, attributes);

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
        public bool CreateHTDTX(out HtlcTimeoutDeliveryTx HTDTX, string txId, string HtlcValue)
        {
            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
#if DEBUG_LOCAL
            new NeoInterface.TransactionAttributeLong(TransactionAttributeUsage.Remark, this.timestampLong, attributes).MakeAttribute(out attributes);
#else
            new NeoInterface.TransactionAttributeDouble(TransactionAttributeUsage.Remark, this.timestamp, attributes).MakeAttribute(out attributes);
#endif

            // Assembly transaction with input for both wallets
            CoinReference[] inputsData = createInputsData(txId, 2);

            // Assembly transaction with output for both wallets
            TransactionOutput[] outputsData = createOutput(assetId, HtlcValue, this.peerAddress);

            this.GetContractTransaction(out ContractTransaction tx, inputsData, outputsData, attributes);

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
        public bool CreateHETX(out HtlcExecutionTx HETX, string txId, string HtlcValue)
        {
            JObject RSMCContract = NeoInterface.CreateRSMCContract(this.scriptHash, this.pubKey, this.peerScriptHash, this.peerPubkey, this.timestampString);
            Log.Debug("RSMCContract: {0}", RSMCContract);
            this.SetAddressRSMC(RSMCContract["address"].ToString());
            this.SetScripRSMC(RSMCContract["script"].ToString());

            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
#if DEBUG_LOCAL
            new NeoInterface.TransactionAttributeLong(TransactionAttributeUsage.Remark, this.timestampLong, attributes).MakeAttribute(out attributes);
#else
            new NeoInterface.TransactionAttributeDouble(TransactionAttributeUsage.Remark, this.timestamp, attributes).MakeAttribute(out attributes);
#endif

            // Assembly transaction with input for both wallets
            CoinReference[] inputsData = createInputsData(txId, 2);

            // Assembly transaction with output for both wallets
            TransactionOutput[] outputsData = createOutput(assetId, HtlcValue, this.addressRsmc);

            this.GetContractTransaction(out ContractTransaction tx, inputsData, outputsData, attributes);

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
        public bool CreateHERDTX(out HtlcExecutionRevocableDeliveryTx revocableDeliveryTx, string txId, string HtlcValue)
        {
            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            string preTxId = txId.Substring(2).HexToBytes().Reverse().ToArray().ToHexString().Strip("\"");   //preTxId ???

#if DEBUG_LOCAL
            new NeoInterface.TransactionAttributeLong(TransactionAttributeUsage.Remark, this.timestampLong, attributes).MakeAttribute(out attributes);
#else
            new NeoInterface.TransactionAttributeDouble(TransactionAttributeUsage.Remark, this.timestamp, attributes).MakeAttribute(out attributes);
#endif
            new NeoInterface.TransactionAttributeString(TransactionAttributeUsage.Remark1, preTxId, attributes).MakeAttribute(out attributes);
            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Remark2, this.scriptHash, attributes).MakeAttribute(out attributes);                                         //outPutTo

            // Assembly transaction with input for both wallets
            CoinReference[] inputsData = createInputsData(txId, 0);

            // Assembly transaction with output for both wallets
            TransactionOutput[] outputsData = createOutput(assetId, HtlcValue, this.address);

            this.GetContractTransaction(out ContractTransaction tx, inputsData, outputsData, attributes);

            revocableDeliveryTx = new HtlcExecutionRevocableDeliveryTx
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

        /// <summary>
        /// It helps Trinity to count total inputs from Vouts.
        /// </summary>
        /// <param name="vouts"> vouts that needs to be counted </param>
        /// <returns></returns>
        public long getTotalInputs(List<string> vouts)
        {
            long inputs_total = 0;
            if (null == vouts)
            {
                return 0;
            }
            foreach (string item in vouts)
            {
                Vin vin = item.Deserialize<Vin>();
                inputs_total += vin.value;
            }

            return inputs_total;
        }

        public void SetAddressFunding(string addressFunding)
        {
            this.addressFunding = addressFunding.NeoStrip();
        }

        public void SetScripFunding(string scriptFunding)
        {
            this.scriptFunding = scriptFunding.NeoStrip();
        }

        public void SetFundingTxId(string fundingTxId)
        {
            this.fundingTxId = fundingTxId.NeoStrip();
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
        private void GetContractTransaction(out ContractTransaction transaction, CoinReference[] inputsData, TransactionOutput[] OutputsData, List<TransactionAttribute> attributes, byte version = 1)
        {
            // return null if no assembly data is input
            if (null == inputsData)
            {
                transaction = null;
                return;
            }

            transaction = new ContractTransaction
            {
                Attributes = attributes.ToArray(),
                Inputs = inputsData,
                Outputs = OutputsData
            };

            return;
        }
    }
}
