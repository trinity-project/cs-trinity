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

using Trinity.Wallets;
using Trinity.Wallets.Templates.Definitions;

namespace Trinity.BlockChain.Interface
{
    public class NeoTransactionBase
    {
        // Timestamp attribute for contract
#if DEBUG_LOCAL
        protected readonly double timestamp = 1554866712.123456; // for test use;
        protected readonly string timestampString = "1554866712.123456";
        protected readonly long timestampLong = 1554866712;
#else
        protected double timestamp => (DateTime.Now - TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1))).TotalSeconds;
#endif
        // Convert the double to string with with accuracy of 6 decimal points.
        protected delegate TResult DoubleFixed6<in T, out TResult>(T value);
        // 6-precision, but the last number is aways 0
        protected DoubleFixed6<double, string> ToFixed6 = currentTimestamp => currentTimestamp.ToString("F6");

        // which type of asset is used
        protected readonly string assetId;

        // founder trade information
        protected string addressFunding;
        protected string scriptFunding;
        protected string fundingTxId;

        // Commitment trade information
        protected string addressRsmc;
        protected string scriptRsmc;

        // HTLC trade information
        protected string addressHtlc;
        protected string scriptHtlc;

        // peers of transaction traders of the channel
        protected ChannelTrader local;
        protected ChannelTrader remote;

        public NeoTransactionBase() { }

        /// <summary>
        /// Default constructor for create NeoTransaction instances
        /// </summary>
        /// <param name="asset"> Asset ID</param>
        /// <param name="pubKey"> Self Wallet's PublicKey </param>
        /// <param name="balance"> Self Wallet's Balance in specified channel. Also it's used as deposit Just only when creating channel between 2 Wallets </param>
        /// <param name="peerPubKey"> Peer Wallet's PublicKey </param>
        /// <param name="peerBalance"> Peer Wallet's Balance in specified channel. Refer to comments for balance. </param>
        /// <param name="addressFunding"> Contract address for storing 2 wallets' Deposit. It's JUST created when Founder Message is triggerred. </param>
        /// <param name="scriptFunding"> Contract script </param>
        public NeoTransactionBase(string assetId, string pubKey, string balance, string peerPubKey, string peerBalance,
            string addressFunding = null, string scriptFunding = null)
        {
            this.assetId = assetId;

            this.local = new ChannelTrader(pubKey, balance);
            this.remote = new ChannelTrader(peerPubKey, peerBalance);

            this.addressFunding = addressFunding?.NeoStrip();
            this.scriptFunding = scriptFunding?.NeoStrip();
        }

        public void InitializeFundingTx()
        {
            // Create multi-signarture contract address to store deposit
            Contract contract = NeoInterface.CreateMultiSigContract(this.local.publicKey, this.remote.publicKey);

            this.SetAddressFunding(contract.Address);
            this.SetScripFunding(contract.Script.ToHexString());
        }

        public void FinalizeFundingTx<TTransaction>(out FundingTx fundingTx, TTransaction transaction, double timestamp)
            where TTransaction : Transaction
        {
            string witness;
            if (this.local.scriptHash > this.remote.scriptHash)
            {
                witness = "024140{signOther}2321" + this.remote.publicKey + "ac" + "4140{signSelf}2321" + this.local.publicKey + "ac";
            }
            else
            {
                witness = "024140{signSelf}2321" + this.local.publicKey + "ac" + "4140{signOther}2321" + this.remote.publicKey + "ac";
            }

            fundingTx = new FundingTx
            {
                txData = transaction.GetHashData().ToHexString().NeoStrip(),
                addressFunding = this.addressFunding,
                txId = transaction.Hash.ToString().Strip("\""),
                scriptFunding = this.scriptFunding,
                witness = witness,
                timeAttribute = timestamp
            };

            this.SetFundingTxId(fundingTx.txId);
        }

        public void InitializeCTX(double timestamp)
        {
#if DEBUG_LOCAL
            JObject RSMCContract = NeoInterface.CreateRSMCContract(this.local.scriptHash, this.local.publicKey, this.remote.scriptHash,
                this.remote.publicKey, this.ToFixed6(timestampLong));
#else
            JObject RSMCContract = NeoInterface.CreateRSMCContract(this.local.scriptHash, this.local.publicKey, this.remote.scriptHash,
                this.remote.publicKey, this.ToFixed6(timestamp));
#endif

            Log.Debug("RSMCContract: {0}", RSMCContract);

            this.SetAddressRSMC(RSMCContract["address"].ToString());
            this.SetScripRSMC(RSMCContract["script"].ToString());
        }

        public void FinalizeCTX<TTransaction>(out CommitmentTx commitmentTx, TTransaction transaction, double timestamp)
            where TTransaction : Transaction
        {
            string witness = "018240{signSelf}40{signOther}da" + this.scriptFunding;

            commitmentTx = new CommitmentTx
            {
                txData = transaction.GetHashData().ToHexString().NeoStrip(),
                addressRSMC = this.addressRsmc,
                scriptRSMC = this.scriptRsmc,
                txId = transaction.Hash.ToString().Strip("\""),
                witness = witness,
                timeAttribute = timestamp
            };
        }

        public void FinalizeRDTX<TTransaction>(out RevocableDeliveryTx revocableDeliveryTx, TTransaction transaction, double timestamp)
            where TTransaction : Transaction
        {
            string witness = "01{blockheight_script}40{signOther}40{signSelf}fd" + NeoInterface.CreateVerifyScript(this.scriptRsmc);

            revocableDeliveryTx = new RevocableDeliveryTx
            {
                txData = transaction.GetHashData().ToHexString().NeoStrip(),
                txId = transaction.Hash.ToString().Strip("\""),
                witness = witness,
                timeAttribute = timestamp
            };
        }

        public void FinalizeBRTX<TTransaction>(out BreachRemedyTx breachRemedyTx, TTransaction transaction, double timestamp)
            where TTransaction : Transaction
        {
            string witness = "01{blockheight_script}40{signOther}40{signSelf}fd" + NeoInterface.CreateVerifyScript(this.scriptRsmc);

            breachRemedyTx = new BreachRemedyTx
            {
                txData = transaction.GetHashData().ToHexString().NeoStrip(),
                txId = transaction.Hash.ToString().Strip("\""),
                witness = witness,
                timeAttribute = timestamp
            };
        }

        public void FinalizeSettleTX<TTransaction>(out TxContents settleTx, TTransaction transaction, double timestamp)
            where TTransaction : Transaction
        {
            string witness = "018240{signSelf}40{signOther}da" + this.scriptFunding;

            settleTx = new TxContents
            {
                txData = transaction.GetHashData().ToHexString().NeoStrip(),
                txId = transaction.Hash.ToString().Strip("\""),
                witness = witness,
                timeAttribute = timestamp,
            };
        }

        public void InitializeHCTX(string HashR, double timestamp, bool isPayer =true)
        {
            string currentTimeString = ToFixed6(timestamp);
            string debugInfo = isPayer ? "Payer" : "Payee";
            string payerPublicKey = isPayer ? this.local.publicKey : this.remote.publicKey;
            string payeePublicKey = isPayer ? this.remote.publicKey : this.local.publicKey;

#if DEBUG_LOCAL
            JObject RSMCContract = NeoInterface.CreateRSMCContract(this.local.scriptHash, this.local.publicKey,
                this.remote.scriptHash, this.remote.publicKey, this.timestampString);
            Console.WriteLine(this.local.publicKey);
            Console.WriteLine(this.remote.publicKey);
            JObject HTLCContract = NeoInterface.CreateHTLCContract((this.timestampLong + 600).ToString(), this.local.publicKey, this.remote.publicKey, HashR);
#else
            // Create RSMC contract address to store HTLC Payment
            JObject RSMCContract = NeoInterface.CreateRSMCContract(this.local.scriptHash, this.local.publicKey,
                this.remote.scriptHash, this.remote.publicKey, currentTimeString);
            // Create HTLC contract address to lock HTLC Payment
            JObject HTLCContract = NeoInterface.CreateHTLCContract(currentTimeString, this.local.publicKey, this.remote.publicKey, HashR);
#endif
            Log.Debug("timestamp: {0}", currentTimeString);
            Log.Debug("create {0} RSMCContract: {1}", debugInfo, RSMCContract);
            Log.Debug("create {0} HTLCContract: {1}", debugInfo, HTLCContract);

            // Initialize the address and script for HTLC transaction
            this.SetAddressRSMC(RSMCContract["address"].ToString());
            this.SetScripRSMC(RSMCContract["script"].ToString());
            this.SetAddressHTLC(HTLCContract["address"].ToString());
            this.SetScripHTLC(HTLCContract["script"].ToString());
        }

        public void FinalizeHCTX<TTransaction>(out HtlcCommitTx HCTX, TTransaction transaction, double timestamp)
            where TTransaction : Transaction
        {
            string witness = "018240{signSelf}40{signOther}da" + this.scriptFunding;

            HCTX = new HtlcCommitTx
            {
                txData = transaction.GetHashData().ToHexString().NeoStrip(),
                addressRSMC = this.addressRsmc,
                addressHTLC = this.addressHtlc,
                scriptRSMC = this.scriptRsmc,
                scriptHTLC = this.scriptHtlc,
                txId = transaction.Hash.ToString().Strip("\""),
                witness = witness,
                timeAttribute = timestamp
            };
        }

        public void FinalizeHRDTX<TTransaction>(out HtlcRevocableDeliveryTx HRDTX, TTransaction transaction, double timestamp)
            where TTransaction : Transaction
        {
            string witness = "01{blockheight_script}40{signOther}40{signSelf}fd" + NeoInterface.CreateVerifyScript(this.scriptRsmc);

            HRDTX = new HtlcRevocableDeliveryTx
            {
                txData = transaction.GetHashData().ToHexString().NeoStrip(),
                txId = transaction.Hash.ToString().Strip("\""),
                witness = witness,
                timeAttribute = timestamp
            };
        }

        public void InitializeHETX(double timestamp)
        {
            string currentTimeString = ToFixed6(timestamp);

#if DEBUG_LOCAL
            JObject RSMCContract = NeoInterface.CreateRSMCContract(this.local.scriptHash, this.local.publicKey,
                this.remote.scriptHash, this.remote.publicKey, this.timestampString);
#else
            JObject RSMCContract = NeoInterface.CreateRSMCContract(this.local.scriptHash, this.local.publicKey,
                this.remote.scriptHash, this.remote.publicKey, currentTimeString);
#endif

            Log.Debug("HETX RSMCContract: {0}", RSMCContract);
            this.SetAddressRSMC(RSMCContract["address"].ToString());
            this.SetScripRSMC(RSMCContract["script"].ToString());
        }

        public void FinalizeHETX<TTransaction>(out HtlcExecutionTx HETX, TTransaction transaction, double timestamp)
            where TTransaction : Transaction
        {
            string witness = "01{blockheight_script}40{signOther}40{signSelf}fd" + NeoInterface.CreateVerifyScript(this.scriptHtlc);

            HETX = new HtlcExecutionTx
            {
                txData = transaction.GetHashData().ToHexString().NeoStrip(),
                txId = transaction.Hash.ToString().Strip("\""),
                addressRSMC = this.addressRsmc,
                scriptRSMC = this.scriptRsmc,
                witness = witness,
                timeAttribute = timestamp
            };
        }

        public void FinalizeHEDTX<TTransaction>(out HtlcExecutionDeliveryTx HEDTX, TTransaction transaction, double timestamp)
            where TTransaction : Transaction
        {
            string witness = "01{blockheight_script}40{signOther}40{signSelf}fd" + NeoInterface.CreateVerifyScript(this.scriptHtlc);

            HEDTX = new HtlcExecutionDeliveryTx
            {
                txData = transaction.GetHashData().ToHexString().NeoStrip(),
                txId = transaction.Hash.ToString().Strip("\""),
                witness = witness,
                timeAttribute = timestamp
            };
        }

        public void FinalizeHERDTX<TTransaction>(out HtlcExecutionRevocableDeliveryTx HERDTX, TTransaction transaction, double timestamp)
            where TTransaction : Transaction
        {
            string witness = "01{blockheight_script}40{signOther}40{signSelf}fd" + NeoInterface.CreateVerifyScript(this.scriptRsmc);

            HERDTX = new HtlcExecutionRevocableDeliveryTx
            {
                txData = transaction.GetHashData().ToHexString().NeoStrip(),
                txId = transaction.Hash.ToString().Strip("\""),
                witness = witness,
                timeAttribute = timestamp
            };
        }

        public void InitializeHTTX(double timestamp)
        {
            string currentTimeString = ToFixed6(timestamp);

#if DEBUG_LOCAL
            JObject RSMCContract = NeoInterface.CreateRSMCContract(this.local.scriptHash, this.local.publicKey,
                this.remote.scriptHash, this.remote.publicKey, this.timestampString);
#else
            JObject RSMCContract = NeoInterface.CreateRSMCContract(this.local.scriptHash, this.local.publicKey, 
                this.remote.scriptHash, this.remote.publicKey, currentTimeString);
#endif
            Log.Debug("HTTX RSMCContract: {0}", RSMCContract);
            this.SetAddressRSMC(RSMCContract["address"].ToString());
            this.SetScripRSMC(RSMCContract["script"].ToString());
        }

        public void FinalizeHTTX<TTransaction>(out HtlcTimoutTx HTTX, TTransaction transaction, double timestamp)
            where TTransaction : Transaction
        {
            string witness = "01{blockheight_script}40{signOther}40{signSelf}fd" + NeoInterface.CreateVerifyScript(this.scriptHtlc);

            HTTX = new HtlcTimoutTx
            {
                txData = transaction.GetHashData().ToHexString().NeoStrip(),
                txId = transaction.Hash.ToString().Strip("\""),
                addressRSMC = this.addressRsmc,
                scriptRSMC = this.scriptRsmc,
                witness = witness,
                timeAttribute = timestamp
            };
        }

        public void FinalizeHTDTX<TTransaction>(out HtlcTimeoutDeliveryTx HTDTX, TTransaction transaction, double timestamp)
            where TTransaction : Transaction
        {
            string witness = "01{blockheight_script}40{signOther}40{signSelf}fd" + NeoInterface.CreateVerifyScript(this.scriptHtlc);

            HTDTX = new HtlcTimeoutDeliveryTx
            {
                txData = transaction.GetHashData().ToHexString().NeoStrip(),
                txId = transaction.Hash.ToString().Strip("\""),
                witness = witness,
                timeAttribute = timestamp
            };
        }

        public void FinalizeHTRDTX<TTransaction>(out HtlcTimeoutRevocableDelivertyTx HTRDTX, TTransaction transaction, double timestamp)
            where TTransaction : Transaction
        {
            string witness = "01{blockheight_script}40{signOther}40{signSelf}fd" + NeoInterface.CreateVerifyScript(this.scriptRsmc);

            HTRDTX = new HtlcTimeoutRevocableDelivertyTx
            {
                txData = transaction.GetHashData().ToHexString().NeoStrip(),
                txId = transaction.Hash.ToString().Strip("\""),
                witness = witness,
                timeAttribute = timestamp
            };
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
        /// Create Assebly Data of Invocation Transaction for NEO nep-5 coin
        /// </summary>
        /// <param name="opdata"> Assembly Data of Transaction </param>
        /// <param name="attributes"> Multi-Attributes are set to the Invocation Transaction </param>
        /// <param name="version"> Version of InvocationTransaction </param>
        /// <returns> Neo Invocation Transaction Content </returns>
        protected Transaction AllocateTransaction(string opdata, List<TransactionAttribute> attributes, byte version = 1)
        {
            // return null if no assembly data is input
            if (null == opdata)
            {
                return null;
            }

            return new InvocationTransaction
            {
                Version = version,
                Script = opdata.NeoStrip().HexToBytes(),
                Attributes = attributes.ToArray(),
                Inputs = new CoinReference[0],
                Outputs = new TransactionOutput[0]
            };
        }

        private long getTotalInputs(List<string> vouts)
        {
            long inputs_total = 0;
            if (null == vouts)
            {
                return 0;
            }
            foreach (string item in vouts)
            {
                NeoInterface.Vin vin = item.Deserialize<NeoInterface.Vin>();
                inputs_total += vin.value;
            }

            return inputs_total;
        }

        /// <summary>
        /// Create Assebly Data of Contract Transaction for Neo or NeoGas.
        /// </summary>
        /// <param name="inputsData"></param>
        /// <param name="OutputsData"></param>
        /// <param name="attributes"> Multi-Attributes are set to the Contract Transaction </param>
        /// <returns> Neo Contract Transaction content </returns>
        protected ContractTransaction AllocateTransaction(CoinReference[] inputsData, 
            TransactionOutput[] OutputsData, List<TransactionAttribute> attributes)
        {
            // return null if no assembly data is input
            if (null == inputsData)
            {
                return null;
            }

            return new ContractTransaction
            {
                Attributes = attributes.ToArray(),
                Inputs = inputsData,
                Outputs = OutputsData
            };
        }

        /* ==============================================================================
         * Sets of Override methods
         * Default is for neo blockchain (neo, neogas or nep-5 coin).
         * ==============================================================================
         */
        protected virtual void MakeUpFundingTransaction(out Transaction transaction, double timestamp)
        {
            // Assembly transaction with Opcode for both wallets
            string opdata = NeoInterface.CreateOpdata(this.local.address, this.addressFunding, this.local.balance, this.assetId);
            Log.Debug("FUNDING opdata: {0}", opdata);

            string peerOpdata = NeoInterface.CreateOpdata(this.remote.address, this.addressFunding, this.remote.balance, this.assetId);
            Log.Debug("FUNDING peerOpdata: {0}", peerOpdata);

            List<TransactionAttribute> attributes = this.MakeTransactionAttributes(timestamp);

            // Allocate a new InvocationTransaction for funding transaction
            transaction = this.AllocateTransaction(opdata + peerOpdata, attributes);
        }

        protected virtual void MakeUpFundingTransaction(out ContractTransaction transaction, string contractAddress,
            ChannelTrader localTrader, ChannelTrader remoteTrader, double timestamp, List<string> peerVouts)
        {
#if DEBUG_LOCAL
            List<string> vouts = new List<string>();
            NeoInterface.Vin vout = new NeoInterface.Vin
            {
                n = 0,
                txid = "577fb4c3ca37a5eab7243478f3eae2b011800a10d5c4c0cd85e71bab52e76a78",
                value = 239
            };
            string voutData = MessagePack.MessagePackSerializer.ToJson(MessagePack.MessagePackSerializer.Serialize(vout));
            vouts.Add(voutData);
#else
            List<string> vouts = NeoInterface.getGlobalAssetVout(localTrader.scriptHash, assetId, uint.Parse(localTrader.balance));
#endif
            Log.Debug("Assembly vouts. vouts: {0}.\r\n", vouts);

#if DEBUG_LOCAL
            List<string> peerVouts = new List<string>();
            NeoInterface.Vin peerVout = new NeoInterface.Vin
            {
                n = 0,
                txid = "47b590b1d0765b90e0f6c762ddff905ee4d8c3c81ef7ff35397942b87f8ba31b",
                value = 64
            };
            string peervoutData = MessagePack.MessagePackSerializer.ToJson(MessagePack.MessagePackSerializer.Serialize(peerVout));
            peerVouts.Add(peervoutData);
#else
            //List<string> peerVouts = NeoInterface.getGlobalAssetVout(localTrader.scriptHash, assetId, uint.Parse(remoteTrader.balance));
#endif
            //Log.Debug("Assembly vouts. peerVouts: {0}.\r\n", peerVouts);

            // Assembly transaction with input for both wallets
            CoinReference[] self_inputs = NeoInterface.getInputFormVout(vouts);
            CoinReference[] other_inputs = NeoInterface.getInputFormVout(peerVouts);
            CoinReference[] inputsData = self_inputs.Concat(other_inputs).ToArray();

            string amount = (uint.Parse(localTrader.balance) + uint.Parse(remoteTrader.balance)).ToString();
            TransactionOutput[] output_to_fundingaddress = NeoInterface.createOutput(assetId, amount, this.addressFunding);

            // Assembly transaction with output for both wallets
            long totalInputs = this.getTotalInputs(vouts);
            long peerTotalInputs = this.getTotalInputs(peerVouts);

            TransactionOutput[] output_to_self = new TransactionOutput[] { };
            TransactionOutput[] output_to_other = new TransactionOutput[] { };
            if (totalInputs > long.Parse(localTrader.balance))
            {
                string selfBalance = (totalInputs - long.Parse(localTrader.balance)).ToString();
                output_to_self = NeoInterface.createOutput(assetId, selfBalance, localTrader.address);
            }
            if (peerTotalInputs > long.Parse(localTrader.balance))
            {
                string otherBalance = (peerTotalInputs - long.Parse(localTrader.balance)).ToString();
                output_to_other = NeoInterface.createOutput(assetId, otherBalance, remoteTrader.address);
            }
            TransactionOutput[] outputsData = output_to_fundingaddress.Concat(output_to_self).Concat(output_to_other).ToArray();

            // Assembly transaction with attributes for both wallets
            List<TransactionAttribute> attributes = this.MakeTransactionAttributes(timestamp);
            
            // Start to makeup the transaction body
            transaction = this.AllocateTransaction(inputsData, outputsData, attributes);
        }

        protected virtual void MakeUpCTXTransaction(out Transaction transaction, string contractAddress, 
            ChannelTrader localTrader, ChannelTrader remoteTrader, double timestamp)
        {
            this.MakeUpCommitmentTransaction(out transaction, contractAddress, localTrader, remoteTrader, timestamp);
        }

        protected virtual void MakeUpCTXTransaction(out ContractTransaction transaction, string contractAddress,
            ChannelTrader localTrader, ChannelTrader remoteTrader, double timestamp)
        {
            // Assembly transaction with input for both wallets
            CoinReference[] inputsData = NeoInterface.createInputsData(this.fundingTxId, 0);

            // Assembly transaction with output for both wallets
            TransactionOutput[] outputToRSMC = NeoInterface.createOutput(assetId, localTrader.balance, contractAddress);
            TransactionOutput[] outputToOther = NeoInterface.createOutput(assetId, remoteTrader.balance, remoteTrader.address);
            TransactionOutput[] outputsData = outputToRSMC.Concat(outputToOther).ToArray();

            List<TransactionAttribute> attributes = this.MakeTransactionAttributes(this.addressFunding, timestamp);

            transaction = this.AllocateTransaction(inputsData, outputsData, attributes);
        }

        protected virtual void MakeUpRDTXTransaction(out Transaction transaction, string contractAddress,
            ChannelTrader trader, string txId, double timestamp)
        {
            this.MakeUpRevocableOrBreachTransaction(out transaction, contractAddress, trader, txId, trader.balance, timestamp);
        }

        protected virtual void MakeUpRDTXTransaction(out ContractTransaction transaction, string contractAddress,
            ChannelTrader trader, string txId, double timestamp)
        {
            // Assembly transaction with input for both wallets
            CoinReference[] inputsData = NeoInterface.createInputsData(txId, 0);

            // Assembly transaction with output for both wallets
            TransactionOutput[] outputsData = NeoInterface.createOutput(this.assetId, trader.balance, trader.address);

            List<TransactionAttribute> attributes = this.MakeTransactionAttributes(
                contractAddress, txId, trader.address.ToScriptHash(), timestamp);

            transaction = this.AllocateTransaction(inputsData, outputsData, attributes);
        }

        protected virtual void MakeUpBRTXTransaction(out Transaction transaction, string contractAddress,
            ChannelTrader trader, string txId, string balance, double timestamp)
        {
            this.MakeUpRevocableOrBreachTransaction(out transaction, contractAddress, trader, txId, balance, timestamp);
        }

        protected virtual void MakeUpBRTXTransaction(out ContractTransaction transaction, string contractAddress,
            ChannelTrader trader, string txId, string balance, double timestamp)
        {
            // Assembly transaction with input for both wallets
            CoinReference[] inputsData = NeoInterface.createInputsData(txId, 0);

            // Assembly transaction with output for both wallets
            TransactionOutput[] outputsData = NeoInterface.createOutput(assetId, balance, trader.address);

            List<TransactionAttribute> attributes = this.MakeTransactionAttributes(
                contractAddress, txId, trader.address.ToScriptHash(), timestamp);

            transaction = this.AllocateTransaction(inputsData, outputsData, attributes);
        }

        protected virtual void MakeUpSettleTransaction(out Transaction transaction,
            ChannelTrader localTrader, ChannelTrader remoteTrader, double timestamp)
        {
            this.MakeUpCommitmentTransaction(out transaction, localTrader.address, localTrader, remoteTrader, timestamp);
        }

        protected virtual void MakeUpSettleTransaction(out ContractTransaction transaction, double timestamp)
        {
            uint amount = uint.Parse(this.local.balance) + uint.Parse(this.remote.balance);

            // Assembly transaction with input for both wallets
#if DEBUG_LOCAL
            List<string> vouts = new List<string>();
            NeoInterface.Vin vout = new NeoInterface.Vin
            {
                n = 0,
                txid = "101c5e988e72bdabb121350f168bef8965f0fd2891c387b5384e9718c32c2c7d",
                value = 2
            };
            string voutData = MessagePack.MessagePackSerializer.ToJson(MessagePack.MessagePackSerializer.Serialize(vout));
            vouts.Add(voutData);
#else
            List<string> vouts = NeoInterface.getGlobalAssetVout(this.addressFunding.ToScriptHash(), assetId, amount);
#endif
            CoinReference[] inputsData = NeoInterface.getInputFormVout(vouts);
            Log.Debug("Assembly vouts. vouts: {0}.\r\n", vouts);

            // Assembly transaction with output for both wallets
            TransactionOutput[] outputToSelf = NeoInterface.createOutput(assetId, this.local.balance, this.local.address, true);
            TransactionOutput[] outputToOther = NeoInterface.createOutput(assetId, this.remote.balance, this.remote.address, true);
            TransactionOutput[] outputsData = outputToSelf.Concat(outputToOther).ToArray();

            List<TransactionAttribute> attributes = this.MakeTransactionAttributes(this.addressFunding, timestamp);

            transaction = this.AllocateTransaction(inputsData, outputsData, attributes);
        }

        protected virtual void MakeUpHCTXTransaction(out Transaction transaction, string HtlcPay, double timestamp, bool isPayer=true)
        {
            string debugInfo = isPayer ? "Payer" : "Payee";

            // Create assembly data for HCTX
            string opdataToHTLC = NeoInterface.CreateOpdata(this.addressFunding, this.addressHtlc, HtlcPay, this.assetId);
            Log.Debug("opdataToHTLC of {0}: {1}", debugInfo, opdataToHTLC);
            string opdataToRsmc = NeoInterface.CreateOpdata(this.addressFunding, this.addressRsmc, this.local.balance, this.assetId);
            Log.Debug("opdataToRsmc of {0}: {1}", debugInfo, opdataToRsmc);
            string OpdataToRemote = NeoInterface.CreateOpdata(this.addressFunding, this.remote.address, this.remote.balance, this.assetId);
            Log.Debug("OpdataToRemote of {0}: {1}", debugInfo, OpdataToRemote);

            // create attributes for HCTX
            List<TransactionAttribute> attributes = this.MakeTransactionAttributes(this.addressFunding, timestamp);

            // Allocate new InvocationTransaction for HCTX
            transaction = this.AllocateTransaction(opdataToRsmc + OpdataToRemote + opdataToHTLC, attributes);
        }

        protected virtual void MakeUpHCTXTransaction(out ContractTransaction transaction, string HtlcPay,
            double timestamp, bool isPayer = true)
        {
            uint amount = uint.Parse(this.local.balance) + uint.Parse(this.remote.balance) + uint.Parse(HtlcPay);

            // Assembly transaction with input for both wallets
#if DEBUG_LOCAL
            List<string> vouts = new List<string>();
            NeoInterface.Vin vout = new NeoInterface.Vin
            {
                n = 0,
                txid = "0x577fb4c3ca37a5eab7243478f3eae2b011800a10d5c4c0cd85e71bab52e76a79",
                value = 4
            };
            string voutData = MessagePack.MessagePackSerializer.ToJson(MessagePack.MessagePackSerializer.Serialize(vout));
            vouts.Add(voutData);
#else
            List<string> vouts = NeoInterface.getGlobalAssetVout(this.addressFunding.ToScriptHash(), this.assetId, amount);
#endif
            CoinReference[] inputsData = NeoInterface.getInputFormVout(vouts);

            // Assembly transaction with output for both wallets
            TransactionOutput[] outputToRMSC = NeoInterface.createOutput(assetId, this.local.balance, this.addressRsmc, true);
            TransactionOutput[] outputToRemote = NeoInterface.createOutput(assetId, this.remote.balance, this.remote.address, true);
            TransactionOutput[] outputToHtlc = NeoInterface.createOutput(assetId, HtlcPay, this.addressHtlc, true);
            TransactionOutput[] outputsData = outputToRMSC.Concat(outputToRemote).Concat(outputToHtlc).ToArray();

            // create attributes for HCTX
            List<TransactionAttribute> attributes = this.MakeTransactionAttributes(this.addressFunding, timestamp);

            // Allocate new InvocationTransaction for HCTX
            transaction = this.AllocateTransaction(inputsData, outputsData, attributes);
        }

        protected virtual void MakeUpHRDTXTransaction(out Transaction transaction, ChannelTrader trader, string txId,
            double timestamp, bool isPayer = true)
        {
            string debugInfo = isPayer ? "Payer" : "Payee";

            string opdata = NeoInterface.CreateOpdata(this.addressRsmc, trader.address, trader.balance, this.assetId);
            Log.Debug("HRDTX opdata of {0}: {1}", debugInfo, opdata);

            // create attributes
            List<TransactionAttribute> attributes = this.MakeTransactionAttributes(this.addressRsmc, txId, trader.scriptHash, timestamp);

            transaction = this.AllocateTransaction(opdata, attributes);
        }

        protected virtual void MakeUpHRDTXTransaction(out ContractTransaction transaction, ChannelTrader trader, string txId,
            double timestamp, bool isPayer = true)
        {
            // Assembly transaction with input for both wallets
            CoinReference[] inputsData = NeoInterface.createInputsData(txId, 0);

            // Assembly transaction with output for both wallets
            TransactionOutput[] outputsData = NeoInterface.createOutput(assetId, trader.balance, trader.address);

            // create attributes
            List<TransactionAttribute> attributes = this.MakeTransactionAttributes(this.addressRsmc, txId, trader.scriptHash, timestamp);

            // Allocate new InvocationTransaction for HRDTX
            transaction = this.AllocateTransaction(inputsData, outputsData, attributes);
        }

        protected virtual void MakeUpHEDTXTransaction(out Transaction transaction, ChannelTrader trader, string HtlcPay, double timestamp)
        {
            string opdata = NeoInterface.CreateOpdata(this.addressHtlc, trader.address, HtlcPay, this.assetId);
            Log.Debug("HEDTX opdata: {0}", opdata);

            List<TransactionAttribute> attributes = this.MakeTransactionAttributes(this.addressHtlc, timestamp);

            transaction = this.AllocateTransaction(opdata, attributes);
        }

        protected virtual void MakeUpHEDTXTransaction(out ContractTransaction transaction, ChannelTrader trader, 
            string HtlcPay, string txId, double timestamp)
        {
            // Assembly transaction with input for both wallets
            CoinReference[] inputsData = NeoInterface.createInputsData(txId, 2);

            // Assembly transaction with output for both wallets
            TransactionOutput[] outputsData = NeoInterface.createOutput(this.assetId, HtlcPay, trader.address);

            List<TransactionAttribute> attributes = this.MakeTransactionAttributes(this.addressHtlc, timestamp);

            transaction = this.AllocateTransaction(inputsData, outputsData, attributes);
        }

        protected virtual void MakeUpHERDTXTransaction(out Transaction transaction, ChannelTrader trader, string HtlcPay, string txId, double timestamp)
        {
            string opdata = NeoInterface.CreateOpdata(this.addressRsmc, trader.address, HtlcPay, this.assetId);
            Log.Debug("HERDTX opdata: {0}", opdata);

            // create the attributes for HERDTX
            List<TransactionAttribute> attributes = this.MakeTransactionAttributes(this.addressRsmc, txId, trader.scriptHash, timestamp);
            
            // allocate Transaction for HERDTX
            transaction = this.AllocateTransaction(opdata, attributes);
        }

        protected virtual void MakeUpHERDTXTransaction(out ContractTransaction transaction, ChannelTrader trader, string HtlcPay, string txId, double timestamp)
        {
            // Assembly transaction data for both wallets in input and output directions
            CoinReference[] inputsData = NeoInterface.createInputsData(txId, 0);
            TransactionOutput[] outputsData = NeoInterface.createOutput(this.assetId, HtlcPay, trader.address);

            // create the attributes for HERDTX
            List<TransactionAttribute> attributes = this.MakeTransactionAttributes(this.addressRsmc, txId, trader.scriptHash, timestamp);

            // allocate Transaction for HERDTX
            transaction = this.AllocateTransaction(inputsData, outputsData, attributes);
        }

        protected virtual void MakeUpHETXTransaction(out Transaction transaction, string HtlcPay, double timestamp)
        {
            string opdata = NeoInterface.CreateOpdata(this.addressHtlc, this.addressRsmc, HtlcPay, this.assetId);
            Log.Debug("HETX opdata: {0}", opdata);

            List<TransactionAttribute> attributes = this.MakeTransactionAttributes(this.addressHtlc, timestamp);

            transaction = this.AllocateTransaction(opdata, attributes);
        }

        protected virtual void MakeUpHETXTransaction(out ContractTransaction transaction, string HtlcPay, string txId, double timestamp)
        {
            // Assembly transaction with input for both wallets
            CoinReference[] inputsData = NeoInterface.createInputsData(txId, 2);

            // Assembly transaction with output for both wallets
            TransactionOutput[] outputsData = NeoInterface.createOutput(assetId, HtlcPay, this.addressRsmc);

            List<TransactionAttribute> attributes = this.MakeTransactionAttributes(this.addressHtlc, timestamp);

            transaction = this.AllocateTransaction(inputsData, outputsData, attributes);
        }

        protected virtual void MakeUpHTDTXTransaction(out Transaction transaction, ChannelTrader trader, string HtlcPay, double timestamp)
        {
            string opdata = NeoInterface.CreateOpdata(this.addressHtlc, trader.address, HtlcPay, this.assetId);
            Log.Debug("HTDTX opdata: {0}", opdata);

            List<TransactionAttribute> attributes = this.MakeTransactionAttributes(this.addressHtlc, timestamp);

            transaction = this.AllocateTransaction(opdata, attributes);
        }

        protected virtual void MakeUpHTDTXTransaction(out ContractTransaction transaction, ChannelTrader trader, 
            string HtlcPay, string txId, double timestamp)
        {
            // Assembly transaction with input for both wallets
            CoinReference[] inputsData = NeoInterface.createInputsData(txId, 2);

            // Assembly transaction with output for both wallets
            TransactionOutput[] outputsData = NeoInterface.createOutput(assetId, HtlcPay, trader.address);

            List<TransactionAttribute> attributes = this.MakeTransactionAttributes(this.addressHtlc, timestamp);

            transaction = this.AllocateTransaction(inputsData, outputsData, attributes);
        }

        protected virtual void MakeUpHTRDTXTransaction(out Transaction transaction, ChannelTrader trader, string HtlcPay, string txId, double timestamp)
        {
            // Assembly transaction data for HTRDTX
            string opdata = NeoInterface.CreateOpdata(this.addressRsmc, trader.address, HtlcPay, this.assetId);
            Log.Debug("HTRDTX opdata: {0}", opdata);

            // Create attribute for HTRDTX
            List<TransactionAttribute> attributes = this.MakeTransactionAttributes(this.addressRsmc, txId, trader.scriptHash, timestamp);

            // Nep-5: Allocation transaction for HTRDTX
            transaction = this.AllocateTransaction(opdata, attributes);
        }

        protected virtual void MakeUpHTRDTXTransaction(out ContractTransaction transaction, ChannelTrader trader, 
            string HtlcPay, string txId, double timestamp)
        {
            // Assembly transaction data of both wallets in input and output directions
            CoinReference[] inputsData = NeoInterface.createInputsData(txId, 0);
            TransactionOutput[] outputsData = NeoInterface.createOutput(this.assetId, HtlcPay, trader.address);

            // Create attribute for HTRDTX
            List<TransactionAttribute> attributes = this.MakeTransactionAttributes(this.addressRsmc, txId, trader.scriptHash, timestamp);

            // Neo or NeoGas: Allocation transaction for HTRDTX
            transaction = this.AllocateTransaction(inputsData, outputsData, attributes);
        }

        protected virtual void MakeUpHTTXTransaction(out Transaction transaction, string HtlcPay, double timestamp)
        {
            string opdata = NeoInterface.CreateOpdata(this.addressHtlc, this.addressRsmc, HtlcPay, this.assetId);
            Log.Debug("HTTX opdata: {0}", opdata);

            List<TransactionAttribute> attributes = this.MakeTransactionAttributes(this.addressHtlc, timestamp);

            transaction = this.AllocateTransaction(opdata, attributes);
        }

        protected virtual void MakeUpHTTXTransaction(out ContractTransaction transaction, string HtlcPay, string txId, double timestamp)
        {
            // Assembly transaction with input for both wallets
            CoinReference[] inputsData = NeoInterface.createInputsData(txId, 2);

            // Assembly transaction with output for both wallets
            TransactionOutput[] outputsData = NeoInterface.createOutput(assetId, HtlcPay, this.addressRsmc);

            List<TransactionAttribute> attributes = this.MakeTransactionAttributes(this.addressHtlc, timestamp);

            transaction = this.AllocateTransaction(inputsData, outputsData, attributes);
        }

        /* ==============================================================================
            * Sets of Private methods
            * Default is for neo blockchain (neo, neogas or nep-5 coin).
            * ==============================================================================
            */
        private void MakeUpRevocableOrBreachTransaction(out Transaction transaction, string contractAddress,
            ChannelTrader trader, string txId, string balance, double timestamp)
        {
            // Assembly data for Commitment or BreachRemedy transaction
            string opdata = NeoInterface.CreateOpdata(contractAddress, trader.address, balance, this.assetId);
            Log.Debug("MakeUpRevocableOrBreachTransaction opdata: {0}", opdata);

            // Create attributes for Commitment or BreachRemedy transaction
            List<TransactionAttribute> attributes = this.MakeTransactionAttributes(contractAddress, txId, trader.scriptHash, timestamp);
            
            // Allocate a new InvocationTransaction for Commitment or BreachRemedy transaction
            transaction = this.AllocateTransaction(opdata, attributes);
        }

        private void MakeUpCommitmentTransaction(out Transaction transaction, string contractAddress,
            ChannelTrader localTrader, ChannelTrader remoteTrader, double timestamp)
        {
            string debugInfo = localTrader.address.Equals(contractAddress) ? "Settle" : "CTX";

            string opdata = NeoInterface.CreateOpdata(this.addressFunding, contractAddress, localTrader.balance, this.assetId);
            Log.Debug("{0} opdata: {1}", debugInfo, opdata);

            string peerOpdata = NeoInterface.CreateOpdata(this.addressFunding, remoteTrader.address, remoteTrader.balance, this.assetId);
            Log.Debug("{0} peerOpdata: {1}", debugInfo, peerOpdata);

            List<TransactionAttribute> attributes = this.MakeTransactionAttributes(this.addressFunding, timestamp);

            // Allocate a new InvocationTransaction for Commitment transaction
            transaction = this.AllocateTransaction(opdata + peerOpdata, attributes);
        }

        private List<TransactionAttribute> MakeTransactionAttributes(string contractAddress, double timestamp)
        {
            List<TransactionAttribute> attributes = new List<TransactionAttribute>();

            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Script, contractAddress.ToScriptHash(), attributes).MakeAttribute(out attributes);
#if DEBUG_LOCAL
            new NeoInterface.TransactionAttributeLong(TransactionAttributeUsage.Remark, this.timestampLong, attributes).MakeAttribute(out attributes);
#else
            new NeoInterface.TransactionAttributeDouble(TransactionAttributeUsage.Remark, timestamp, attributes).MakeAttribute(out attributes);
#endif
            
            return attributes;
        }

        private List<TransactionAttribute> MakeTransactionAttributes(double timestamp)
        {
            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Script, this.local.scriptHash, attributes).MakeAttribute(out attributes);
            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Script, this.remote.scriptHash, attributes).MakeAttribute(out attributes);
#if DEBUG_LOCAL
            new NeoInterface.TransactionAttributeLong(TransactionAttributeUsage.Remark, this.timestampLong, attributes).MakeAttribute(out attributes);
#else
            new NeoInterface.TransactionAttributeDouble(TransactionAttributeUsage.Remark, timestamp, attributes).MakeAttribute(out attributes);
#endif

            return attributes;
        }

        private List<TransactionAttribute> MakeTransactionAttributes(string contractAddress, string txId, UInt160 scriptHash, double timestamp)
        {
            string preTxId = txId.Substring(2).HexToBytes().Reverse().ToArray().ToHexString().Strip("\"");
            List<TransactionAttribute> attributes = new List<TransactionAttribute>();

            // Add contract address as one of attributes
            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Script, contractAddress.ToScriptHash(), attributes).MakeAttribute(out attributes);

#if DEBUG_LOCAL
            new NeoInterface.TransactionAttributeLong(TransactionAttributeUsage.Remark, this.timestampLong, attributes).MakeAttribute(out attributes);
#else
            new NeoInterface.TransactionAttributeDouble(TransactionAttributeUsage.Remark, timestamp, attributes).MakeAttribute(out attributes);
#endif
            new NeoInterface.TransactionAttributeString(TransactionAttributeUsage.Remark1, preTxId, attributes).MakeAttribute(out attributes);
            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Remark2, scriptHash, attributes).MakeAttribute(out attributes);

            return attributes;
        }
    }
}
