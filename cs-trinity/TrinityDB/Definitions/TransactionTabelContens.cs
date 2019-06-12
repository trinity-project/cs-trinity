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

namespace Trinity.TrinityDB.Definitions
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class TransactionTabelSummary
    {
        public UInt64 nonce;// { get; set; }
        public string channel;// { get; set; }
        public string txType; // mapping to EnumTxType
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public class TransactionTabelHLockPair
    {
        public UInt64 nonce;// { get; set; }
        public string channel;// { get; set; }
        public string txType; // mapping to EnumTxType
        public string rcode;
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public class TransactionTabelContent
    {
        public UInt64 nonce;
        public string monitorTxId;
        public string state;
        public long balance;
        public long peerBalance;
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public class TransactionFundingContent : TransactionTabelContent
    {
        public FundingSignTx founder;
        public CommitmentSignTx commitment;
        public RevocableDeliverySignTx revocableDelivery;
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public class TransactionRsmcContent : TransactionTabelContent
    {
        public CommitmentSignTx commitment;
        public RevocableDeliverySignTx revocableDelivery;
        public BreachRemedySignTx breachRemedy;
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public class TransactionHtlcContent : TransactionTabelContent
    {
        public HtlcCommitSignTx commitment;
        public HtlcRevocableDeliverySignTx revocableDelivery;
        public HtlcExecutionSignTx HETX;
        public HtlcExecutionDeliverySignTx HEDTX;
        public HtlcExecutionRevocableDeliverySignTx HERDTX;
        public HtlcTimoutSignTx HTTX;
        public HtlcTimeoutDeliverySignTx HTDTX;
        public HtlcTimeoutRevocableDelivertySignTx HTRDTX;
    }
}
