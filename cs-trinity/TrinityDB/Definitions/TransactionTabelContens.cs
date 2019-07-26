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
using MessagePack;

using Trinity.Wallets.Templates.Definitions;
using Trinity.Wallets.Templates.Messages;

namespace Trinity.TrinityDB.Definitions
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class TransactionTabelSummary
    {
        public UInt64 nonce;// { get; set; }
        public string channel;// { get; set; }
        public string txType; // mapping to EnumTransactionType
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public class TransactionTabelHLockPair
    {
        public string transactionType;
        public string asset;            // Asset type
        public string rcode;
        public string state;            // mapping to EnumTransactionState
        public string incomeChannel;    // { get; set; }
        public string paymentChannel;   // { get; set; }
        public UInt64 rsmcNonce;        // rsmc nonce
        public UInt64 htlcNonce;        // htlcNonce
        public long income;     // How much gains
        public long payment;    // How much is paid
        public List<PathInfo> router; // for adapting the old trinity
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public class TransactionTabelContent
    {
        public UInt64 nonce;
        public long balance;
        public long peerBalance;
        public long payment;
        public int role;        // record current role index
        public bool isFounder;  // indicates who leads the transaction
        public string monitorTxId;
        public string state;    // mapping to EnumTransactionState
        // TODO : might be used in the future.
        public string type;     // mapping to EnumTransactionType
        public string timestamp;
        public string channel;
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public class TransactionFundingContent : TransactionTabelContent
    {
        public TxContentsSignGeneric<FundingTx> founder;
        public TxContentsSignGeneric<CommitmentTx> commitment;
        public TxContentsSignGeneric<RevocableDeliveryTx> revocableDelivery;
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public class TransactionSettleContent : TransactionTabelContent
    {
        public TxContentsSignGeneric<TxContents> settle;
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public class TransactionRsmcContent : TransactionTabelContent
    {
        public TxContentsSignGeneric<CommitmentTx> commitment;
        public TxContentsSignGeneric<RevocableDeliveryTx> revocableDelivery;
        public TxContentsSignGeneric<BreachRemedyTx> breachRemedy;
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public class TransactionHtlcContent : TransactionTabelContent
    {
        public string hashcode;
        public TxContentsSignGeneric<HtlcCommitTx> HCTX;
        public TxContentsSignGeneric<HtlcRevocableDeliveryTx> RDTX;
        public TxContentsSignGeneric<HtlcExecutionTx> HETX;
        public TxContentsSignGeneric<HtlcExecutionDeliveryTx> HEDTX;
        public TxContentsSignGeneric<HtlcExecutionRevocableDeliveryTx> HERDTX;
        public TxContentsSignGeneric<HtlcTimoutTx> HTTX;
        public TxContentsSignGeneric<HtlcTimeoutDeliveryTx> HTDTX;
        public TxContentsSignGeneric<HtlcTimeoutRevocableDelivertyTx> HTRDTX;
    }
}
