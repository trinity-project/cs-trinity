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
    public class INeoNep5Transaction : NeoTransactionBase, IBlockChain
    {
        public INeoNep5Transaction(string assetId, string pubKey, string balance, string peerPubKey, string peerBalance,
            string addressFunding = null, string scriptFunding = null)
            : base(assetId, pubKey, balance, peerPubKey, peerBalance, addressFunding, scriptFunding)
        { }

        public bool CreateFundingTx(out FundingTx fundingTx, List<string> peerVout)
        {
            double currentTimestamp = this.timestamp;

            // Initial step for creating funding tx
            this.InitializeFundingTx();

            // Create Neo InvocationTransaction for the funding transaction
            this.MakeUpFundingTransaction(out Transaction transaction, timestamp);

            // create funding Transaction
            this.FinalizeFundingTx(out fundingTx, transaction, currentTimestamp);

            return true;
        }

        public bool CreateBRTX(out BreachRemedyTx breachRemedyTx, string txId)
        {
            double currentTimestamp = this.timestamp;

            // Create Neo InvocationTransaction for the  Breach Remedy transaction
            this.MakeUpBRTXTransaction(out Transaction transaction, this.addressRsmc, this.remote, txId, this.local.balance, currentTimestamp);

            // create Revocable Delivery Transaction
            this.FinalizeBRTX(out breachRemedyTx, transaction, currentTimestamp);

            return true;
        }

        public bool CreateCTX(out CommitmentTx commitmentTx)
        {
            double currentTimestamp = this.timestamp;

            // Initial step for creating Commitment tx
            this.InitializeCTX(currentTimestamp);

            // Create Neo InvocationTransaction for the Commitment transaction
            this.MakeUpCTXTransaction(out Transaction transaction, this.addressRsmc, this.local, this.remote, currentTimestamp);

            // create Commitment Transaction
            this.FinalizeCTX(out commitmentTx, transaction, currentTimestamp);

            return true;
        }

        public bool CreateRDTX(out RevocableDeliveryTx revocableDeliveryTx, string txId)
        {
            double currentTimestamp = this.timestamp;

            // Create Neo InvocationTransaction for the Revocable Delivery transaction
            this.MakeUpRDTXTransaction(out Transaction transaction, this.addressRsmc, this.local, txId, currentTimestamp);

            // create Revocable Delivery Transaction
            this.FinalizeRDTX(out revocableDeliveryTx, transaction, currentTimestamp);

            return true;
        }

        public bool CreateHEDTX(out HtlcExecutionDeliveryTx HEDTX, string HtlcPay, string txId = null)
        {
            double currentTimestamp = this.timestamp;

            // Create Neo InvocationTransaction for the Htlc Execution Delivery transaction
            this.MakeUpHEDTXTransaction(out Transaction transaction, this.remote, HtlcPay, currentTimestamp);

            // create Htlc Execution Delivery Transaction
            this.FinalizeHEDTX(out HEDTX, transaction, currentTimestamp);

            return true;
        }

        public bool CreateHERDTX(out HtlcExecutionRevocableDeliveryTx HERDTX, string HtlcPay, string txId=null)
        {
            double currentTimestamp = this.timestamp;

            // Create Neo InvocationTransaction for the Htlc Execution Revocable Delivery transaction
            this.MakeUpHERDTXTransaction(out Transaction transaction, this.local, HtlcPay, txId, currentTimestamp);

            // create Htlc Execution Revocable Delivery Transaction
            this.FinalizeHERDTX(out HERDTX, transaction, currentTimestamp);

            return true;
        }

        
        public bool CreateHETX(out HtlcExecutionTx HETX, string HtlcPay, string txId=null)
        {
            double currentTimestamp = this.timestamp;

            // Initial step for creating Htcl Execution transaction
            this.InitializeHETX(currentTimestamp);

            // Create Neo InvocationTransaction for the Htcl Execution transaction
            this.MakeUpHETXTransaction(out Transaction transaction, HtlcPay, currentTimestamp);

            // create Htcl Execution Transaction
            this.FinalizeHETX(out HETX, transaction, currentTimestamp);

            return true;
        }

        public bool CreateHTDTX(out HtlcTimeoutDeliveryTx HTDTX, string HtlcPay, string txId=null)
        {
            double currentTimestamp = this.timestamp;

            // Create Neo InvocationTransaction for the Htlc Timeout Delivery transaction
            this.MakeUpHTDTXTransaction(out Transaction transaction, this.remote, HtlcPay, currentTimestamp);

            // create Htlc Timeout Delivery Transaction
            this.FinalizeHTDTX(out HTDTX, transaction, currentTimestamp);

            return true;
        }

        public bool CreateHTRDTX(out HtlcTimeoutRevocableDelivertyTx HTRDTX, string HtlcPay, string txId = null)
        {
            double currentTimestamp = this.timestamp;

            // Create Neo InvocationTransaction for the Htlc Timeout Revocable Delivery transaction
            this.MakeUpHTRDTXTransaction(out Transaction transaction, this.local, HtlcPay, txId, currentTimestamp);

            // create Htlc Timeout Revocable Delivery Transaction
            this.FinalizeHTRDTX(out HTRDTX, transaction, currentTimestamp);

            return true;
        }

        public bool CreateHTTX(out HtlcTimoutTx HTTX, string HtlcPay, string txId = null)
        {
            double currentTimestamp = this.timestamp;

            // Initial step for creating Htcl Execution transaction
            this.InitializeHTTX(currentTimestamp);

            // Create Neo InvocationTransaction for the Htcl Execution transaction
            this.MakeUpHTTXTransaction(out Transaction transaction, HtlcPay, currentTimestamp);

            // create Htcl Execution Transaction
            this.FinalizeHTTX(out HTTX, transaction, currentTimestamp);

            return true;
        }

        public bool CreateReceiverHCTX(out HtlcCommitTx HCTX, string HtlcPay, string HashR)
        {
            double currentTimestamp = this.timestamp;

            // Initial step for creating HTLC Commitment Transaction for payee.
            this.InitializeHCTX(HashR, currentTimestamp, false);

            // Create Neo InvocationTransaction for the HCTX transaction for payee.
            this.MakeUpHCTXTransaction(out Transaction transaction, HtlcPay, currentTimestamp, false);

            // Final step for creating HTLC Commitment Transaction for payee.
            this.FinalizeHCTX(out HCTX, transaction, currentTimestamp);

            return true;
        }

        public bool CreateReceiverRDTX(out HtlcRevocableDeliveryTx revocableDeliveryTx, string txId = null)
        {
            double currentTimestamp = this.timestamp;

            // Create Neo InvocationTransaction for the HTLC Revocable Delivery transaction for payee.
            this.MakeUpHRDTXTransaction(out Transaction transaction, this.local, txId, currentTimestamp);

            // Final step for creating HTLC Revocable Delivery Transaction for payee.
            this.FinalizeHRDTX(out revocableDeliveryTx, transaction, currentTimestamp);

            return true;
        }

        public bool CreateSenderHCTX(out HtlcCommitTx HCTX, string HtlcPay, string HashR)
        {
            double currentTimestamp = this.timestamp;

            // Initial step for creating HTLC Commitment Transaction for payer.
            this.InitializeHCTX(HashR, currentTimestamp);

            // Create Neo InvocationTransaction for the HCTX transaction for payer.
            this.MakeUpHCTXTransaction(out Transaction transaction, HtlcPay, currentTimestamp);

            // Final step for creating HTLC Commitment Transaction for payer.
            this.FinalizeHCTX(out HCTX, transaction, currentTimestamp);

            return true;
        }

        public bool CreateSenderRDTX(out HtlcRevocableDeliveryTx HRDTX, string txId = null)
        {
            double currentTimestamp = this.timestamp;

            // Create Neo InvocationTransaction for the HTLC Revocable Delivery transaction for payer.
            this.MakeUpHRDTXTransaction(out Transaction transaction, this.local, txId, currentTimestamp);

            // Final step for creating HTLC Revocable Delivery Transaction for payer.
            this.FinalizeHRDTX(out HRDTX, transaction, currentTimestamp);

            return true;
        }

        public bool CreateSettle(out TxContents settleTx)
        {
            double currentTimestamp = this.timestamp;

            // Create Neo InvocationTransaction for the Settle transaction
            this.MakeUpSettleTransaction(out Transaction transaction, this.local, this.remote, currentTimestamp);

            // create Revocable Delivery Transaction
            this.FinalizeSettleTX(out settleTx, transaction, currentTimestamp);

            return true;
        }
    }
}
