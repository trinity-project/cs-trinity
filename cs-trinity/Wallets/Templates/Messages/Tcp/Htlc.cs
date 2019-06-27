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

using System.Collections.Generic;

using MessagePack;

using Trinity.Wallets.Templates.Definitions;

namespace Trinity.Wallets.Templates.Messages
{
    /// <summary>
    /// This file define the HTLC Message Body.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class HtlcBody
    {
        public string AssetType { get; set; }
        public long Count { get; set; }
        public int RoleIndex { get; set; }
        public string HashR { get; set; }
        public HtlcCommitTx HCTX { get; set; }
        public HtlcRevocableDeliveryTx RDTX { get; set; }
        public TxContentsSignGeneric<HtlcExecutionTx> HETX { get; set; }
        public TxContentsSignGeneric<HtlcExecutionDeliveryTx> HEDTX { get; set; }
        public TxContentsSignGeneric<HtlcExecutionRevocableDeliveryTx> HERDTX { get; set; }
        public HtlcTimoutTx HTTX { get; set; }
        public HtlcTimeoutDeliveryTx HTDTX { get; set; }
        public HtlcTimeoutRevocableDelivertyTx HTRDTX { get; set; }
        
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public class Htlc : TransactionPlaneGeneric<HtlcBody>
    {
        public List<PathInfo> Router { get; set; }
        public string Next { get; set; }
    }
}
