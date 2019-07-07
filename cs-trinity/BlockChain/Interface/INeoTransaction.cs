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

using Trinity.Wallets.Templates.Definitions;

namespace Trinity.BlockChain.Interface
{
    public class INeoTransaction : NeoTransactionBase, IBlockChain
    {
        public INeoTransaction(string assetId, string pubKey, string balance, string peerPubKey, string peerBalance,
            string addressFunding = null, string scriptFunding = null)
            : base(assetId, pubKey, balance, peerPubKey, peerBalance, addressFunding, scriptFunding)
        { }

        public bool CreateFundingTx(out FundingTx fundingTx)
        {
            // Create multi-signarture contract address to store deposit
            Contract contract = NeoInterface.CreateMultiSigContract(this.pubKey, this.peerPubkey);

            fundingTx = null;
            return true;
        }

        public bool CreateBRTX(out BreachRemedyTx breachRemedyTx, string txId)
        {
            breachRemedyTx = null;
            return true;
        }

        public bool CreateCTX(out CommitmentTx commitmentTx)
        {
            commitmentTx = null;
            return true;
        }

        public bool CreateRDTX(out RevocableDeliveryTx revocableDeliveryTx, string txId)
        {
            revocableDeliveryTx = null;
            return true;
        }

        public bool CreateHEDTX(out HtlcExecutionDeliveryTx HEDTX, string HtlcPay)
        {
            HEDTX = null;
            return true;
        }

        public bool CreateHERDTX(out HtlcExecutionRevocableDeliveryTx revocableDeliveryTx, string HtlcPay, string txId = null)
        {
            revocableDeliveryTx = null;
            return true;
        }

        public bool CreateHETX(out HtlcExecutionTx HETX, string HtlcPay)
        {
            HETX = null;
            return true;
        }

        public bool CreateHTDTX(out HtlcTimeoutDeliveryTx HTDTX, string HtlcPay)
        {
            HTDTX = null;
            return true;
        }

        public bool CreateHTRDTX(out HtlcTimeoutRevocableDelivertyTx revocableDeliveryTx, string HtlcPay, string txId = null)
        {
            revocableDeliveryTx = null;
            return true;
        }

        public bool CreateHTTX(out HtlcTimoutTx HTTX, string HtlcPay)
        {
            HTTX = null;
            return true;
        }

        public bool CreateReceiverHCTX(out HtlcCommitTx HCTX, string HtlcPay, string HashR)
        {
            HCTX = null;
            return true;
        }

        public bool CreateReceiverRDTX(out HtlcRevocableDeliveryTx revocableDeliveryTx, string txId = null)
        {
            revocableDeliveryTx = null;
            return true;
        }

        public bool CreateSenderHCTX(out HtlcCommitTx HCTX, string HtlcPay, string HashR)
        {
            HCTX = null;
            return true;
        }

        public bool CreateSenderRDTX(out HtlcRevocableDeliveryTx revocableDeliveryTx, string txId = null)
        {
            revocableDeliveryTx = null;
            return true;
        }

        public bool CreateSettle(out TxContents settleTx)
        {
            settleTx = null;
            return true;
        }
    }
}
