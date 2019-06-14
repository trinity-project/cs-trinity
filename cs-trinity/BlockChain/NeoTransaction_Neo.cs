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

        // peer wallet trade information of the channel
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
            long timestampLong = 1554866712;

            List<string> vouts = new List<string>();
            Vin vouts = new Vin
            {
                n = 0,
                txid = "0xcf053a9a3e375509e72934e742b91b2d6f591869f6fd74e909ca13893de5bed6",
                value = 239
            };
            string voutData = MessagePackSerializer.ToJson(MessagePackSerializer.Serialize(vout));
            vouts.Add(voutData);
#else
            List<string> vouts = NeoInterface.getGloablAssetVout(pubKey, assetId, uint.Parse(balance));
#endif
            Log.Debug("Assembly vouts. vouts: {0}.\r\n", vouts);

#if DEBUG_LOCAL
            List<string> peerVouts = new List<string>();
            Vin peerVout = new Vin
            {
                n = 0,
                txid = "0x47b590b1d0765b90e0f6c762ddff905ee4d8c3c81ef7ff35397942b87f8ba31b",
                value = 64
            };
            string peervoutData = MessagePackSerializer.ToJson(MessagePackSerializer.Serialize(peerVout));
            peerVouts.Add(peervoutData);
#else
            List<string> peerVouts = NeoInterface.getGloablAssetVout(pubKey, assetId, uint.Parse(peerBalance));
#endif
            Log.Debug("Assembly vouts. peerVouts: {0}.\r\n", peerVouts);

            // Assembly transaction with input for both wallets
            CoinReference[] self_inputs = createInput(vouts);
            CoinReference[] other_inputs = createInput(peerVouts);
            CoinReference[] inputsData = self_inputs.Concat(other_inputs).ToArray();

            uint amount = uint.Parse(balance) + uint.Parse(peerBalance);
            TransactionOutput[] output_to_fundingaddress = createOutput(assetId, amount, contract.Address);

            long totalInputs = getTotalInputs(vouts);
            long peerTotalInputs = getTotalInputs(peerVouts);

            // Assembly transaction with output for both wallets
            TransactionOutput[] output_to_self = new TransactionOutput[] { };
            TransactionOutput[] output_to_other = new TransactionOutput[] { };
            if (totalInputs > long.Parse(balance))
            {
                uint selfBalance = uint.Parse((totalInputs - long.Parse(balance)).ToString());
                output_to_self = createOutput(assetId, selfBalance, address);
            }
            if (peerTotalInputs > long.Parse(balance))
            {
                uint otherBalance = uint.Parse((peerTotalInputs - long.Parse(balance)).ToString());
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

            fundingTx = new FundingTx
            {
                txData = tx.GetHashData().ToHexString().NeoStrip(),
                addressFunding = contract.Address.NeoStrip(),
                txId = tx.Hash.ToString().Strip("\""),
                scriptFunding = contract.Script.ToHexString().NeoStrip(),
                witness = "024140{signOther}2321" + this.peerPubkey + "ac" + "4140{signSelf}2321" + this.pubKey + "ac",
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
        public bool CreateCTX()
        {
            //TODO

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="revocableDeliveryTx"></param>
        /// <param name="txId"></param>
        /// <returns></returns>
        public bool createRDTX()
        {
            //TODO

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="txId"></param>
        /// <returns></returns>
        public bool CreateBRTX()
        {
            //TODO

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settleTx"></param>
        /// <returns></returns>
        public bool CreateSettle()
        {
            //TODO

            return true;
        }

        /// <summary>
        /// It helps Trinity to create Sender HC transaction for adapting the Neo contract.
        /// </summary>
        /// <param name="HCTX"> output the HCTX body </param>
        /// <returns></returns>
        public bool CreateSenderHCTX()
        {
            //TODO

            return true;
        }

        /// <summary>
        /// It helps Trinity to create Sender RD transaction for adapting the Neo contract.
        /// </summary>
        /// <param name="RDTX"> output the RDTX body </param>
        /// <returns></returns>
        public bool CreateSenderRDTX()
        {
            //TODO

            return true;
        }

        /// <summary>
        /// It helps Trinity to create Sender HED transaction for adapting the Neo contract.
        /// </summary>
        /// <param name="HEDTX"> output the HEDTX body </param>
        /// <returns></returns>
        public bool CreateHEDTX()
        {
            //TODO

            return true;
        }

        /// <summary>
        /// It helps Trinity to create Sender HT transaction for adapting the Neo contract.
        /// </summary>
        /// <param name="HTTX"> output the HTTX body </param>
        /// <returns></returns>
        public bool CreateHTTX()
        {
            //TODO

            return true;
        }

        /// <summary>
        /// It helps Trinity to create Sender HT transaction for adapting the Neo contract.
        /// </summary>
        /// <param name="HTRDTX"> output the HTTX body </param>
        /// <returns></returns>
        public bool CreateHTRDTX()
        {
            //TODO

            return true;
        }

        /// <summary>
        /// It helps Trinity to create Sender HC transaction for adapting the Neo contract.
        /// </summary>
        /// <param name="HCTX"> output the HCTX body </param>
        /// <returns></returns>
        /// attention: balance is Receiver balance, peerBalance is Sender balance
        public bool CreateReceiverHCTX()
        {
            //TODO

            return true;
        }

        /// <summary>
        /// It helps Trinity to create Sender RD transaction for adapting the Neo contract.
        /// </summary>
        /// <param name="RDTX"> output the RDTX body </param>
        /// <returns></returns>
        /// attention: balance is Receiver balance, peerBalance is Sender balance
        public bool CreateReceiverRDTX()
        {
            //TODO

            return true;
        }

        //createHTDTX(addressHTLC, pubkeySender, HTLCValue, HTLCScript, asset_id)
        /// <summary>
        /// It helps Trinity to create Sender HTD transaction for adapting the Neo contract.
        /// </summary>
        /// <param name="HTDTX"> output the HTDTX body </param>
        /// <returns></returns>
        public bool CreateHTDTX()
        {
            //TODO

            return true;
        }

        /// <summary>
        /// It helps Trinity to create Sender HT transaction for adapting the Neo contract.
        /// </summary>
        /// <param name="HETX"> output the HTTX body </param>
        /// <returns></returns>
        public bool CreateHETX()
        {
            //TODO

            return true;
        }

        /// <summary>
        /// It helps Trinity to create Sender HT transaction for adapting the Neo contract.
        /// </summary>
        /// <param name="HERDTX"> output the HTTX body </param>
        /// <returns></returns>
        public bool CreateHERDTX()
        {
            //TODO

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
