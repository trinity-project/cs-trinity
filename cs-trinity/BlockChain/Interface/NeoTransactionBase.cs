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
        // Convert the double to string with with accuracy of 6 decimal points.
        protected delegate TResult DoubleFixed6<in T, out TResult>(T value);

        protected double timestamp => (DateTime.Now - TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1))).TotalSeconds;
        // 6-precision, but the last number is aways 0(caused by C#)
        protected DoubleFixed6<double, string> ToFixed6 = currentTimestamp => currentTimestamp.ToString("F6");
#endif

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

        // Peers of the trade
        // self wallet trade information of the channel
        protected readonly string balance;
        protected readonly string pubKey;
        protected UInt160 scriptHash => (null != this.pubKey) ? this.pubKey.ToHash160() : null;
        protected string address => (null != this.pubKey) ? this.pubKey.ToAddress() : null;

        // peer wallet trade information of the channel
        protected readonly string peerBalance;
        protected readonly string peerPubkey;
        protected UInt160 peerScriptHash => (null != this.peerPubkey) ? this.peerPubkey.ToHash160() : null;
        protected string peerAddress => (null != this.peerPubkey) ? this.peerPubkey.ToAddress() : null;

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
            
            this.pubKey = pubKey.NeoStrip();
            this.balance = balance.NeoStrip();

            this.peerPubkey = peerPubKey.NeoStrip();
            this.peerBalance = peerBalance.NeoStrip();

            this.addressFunding = addressFunding?.NeoStrip();
            this.scriptFunding = scriptFunding?.NeoStrip();
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
         * ==============================================================================
         */
        /// <summary>
        /// Generate the witness for channel transaction.
        /// Default is for neo blockchain (neo, neogas or nep-5 coin).
        /// </summary>
        /// <returns> Witness for transaction </returns>
        protected virtual string MakeUpFundingWitness()
        {
            string witness;
            if (this.scriptHash > this.peerScriptHash)
            {
                witness = "024140{signOther}2321" + this.peerPubkey + "ac" + "4140{signSelf}2321" + this.pubKey + "ac";
            }
            else
            {
                witness = "024140{signSelf}2321" + this.pubKey + "ac" + "4140{signOther}2321" + this.peerPubkey + "ac";
            }

            return witness;
        }

        protected virtual void MakeUpFundingTransaction<TTransaction>(out TTransaction transaction, string contractAddress,
            string address, string pubkey)
        {
            transaction = default;
        }
    }
}
