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

namespace Trinity.Trade
{
    /// <summary>
    /// This file is used to serialized the offchain transaction message's header and body
    /// definitions.
    /// Message header contains 2 parts: Mandotary and Optional parts.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class TransactionHeader
    {
        /// <summary>
        /// Mandatory contents in the message header
        /// </summary>
        public string MessageType { get; set; }
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public string ChannelName { get; set; }
        // Probably this word shoud be used in the message header in future
        // instead of the message body's word.
        // public string AssetType { get; set; }
        public string NetMagic { get; set; }
        public UInt64 TxNonce { get; set; }
        
        // Just exists only for HTLC message
        public string Router { get; set; }
        public string Next { get; set; }

        /// <summary>
        /// Optional contents in the message header
        /// </summary>
        public string Error { get; set; }
        public string Comments { get; set; }
    }

    /// <summary>
    /// Body for RegisterChannel Message
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    class RegisterChannelBody
    {
        public string AssetType { get; set; }
        public string Deposit { get; set; }
        public string OriginalMessage { get; set; } // Just for RegisterChannelFail
    }

    /// <summary>
    /// Body for Founder Message
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    class FounderBody
    {
        public string AssetType { get; set; }
        public string Deposit { get; set; }
        public string Founder { get; set; }
        public string Commitment { get; set; }
        public string RevocableDelivery { get; set; }
        public int RoleIndex { get; set; }
    }

    [MessagePackObject(keyAsPropertyName: true)]
    class SettleBody
    {
        public string Settlement { get; set; }
        public string Balance { get; set; }
    }

    [MessagePackObject(keyAsPropertyName: true)]
    class RsmcBody
    {
        public string AssetType { get; set; }
        public string Value { get; set; }
        public string Commitment { get; set; }
        public string RevocableDelivery { get; set; }
        public string BreachRemedy { get; set; }
        public int RoleIndex { get; set; }
    }

    [MessagePackObject(keyAsPropertyName: true)]
    class HtlcBody
    {
        public string AssetType { get; set; }
        public string Count { get; set; }
        public string HCTX { get; set; }
        public string RDTX { get; set; }
        public string HEDTX { get; set; }
        public string HTTX { get; set; }
        public string HTDTX { get; set; }
        public string HTRDTX { get; set; }
        public string RoleIndex { get; set; }
    }

    [MessagePackObject(keyAsPropertyName: true)]
    class RResponseBody
    {
        public string HR { get; set; }
        public string R { get; set; }
    }
}
