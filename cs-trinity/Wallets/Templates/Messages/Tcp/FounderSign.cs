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

using MessagePack;
using Trinity.Wallets.Templates.Definitions;

namespace Trinity.Wallets.Templates.Messages
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class FounderSignBody
    {
        public string AssetType { get; set; }
        public long Deposit { get; set; }
        public TxContentsSignGeneric<FundingTx> Founder { get; set; }
        public TxContentsSignGeneric<CommitmentTx> Commitment { get; set; }
        public TxContentsSignGeneric<RevocableDeliveryTx> RevocableDelivery { get; set; }
        public int RoleIndex { get; set; }
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public class FounderSign : TransactionPlaneGeneric<FounderSignBody>
    {
        public string Error { get; set; }
        public string Comments { get; set; }
    }
}
