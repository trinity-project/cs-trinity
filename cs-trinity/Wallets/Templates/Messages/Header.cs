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
using MessagePack;

namespace Trinity.Wallets.Templates.Messages
{
    /// <summary>
    /// This file define the prototype of the message header.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class TransactionHeader
    {
        /// <summary>
        /// Mandatory contents in the message header
        /// </summary>
        public string MessageType => this.GetType().Name;

        public string Sender { get; set; }
        public string Receiver { get; set; }
        public string ChannelName { get; set; }
        public string AssetType { get; set; }

        public string NetMagic { get; set; }
        public UInt64 TxNonce { get; set; }
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public class ParsedHeader
    {
        /// <summary>
        /// Mandatory contents in the message header
        /// </summary>
        public string MessageType { get; set; }

        public string Sender { get; set; }
        public string Receiver { get; set; }
        public string ChannelName { get; set; }
        public string AssetType { get; set; }

        public string NetMagic { get; set; }
        public UInt64 TxNonce { get; set; }
    }

    /// <summary>
    /// This file define the prototype of the message header.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class Header<TBody> : TransactionHeader
    {
        public TBody MessageBody { get; set; }
    }

    //[MessagePackObject(keyAsPropertyName: true)]
    //public abstract class ResponseHeader<TBody> : Header<TBody>
    //{
    //    /// <summary>
    //    /// Optional contents in the message header
    //    /// </summary>
    //    public string Error { get; set; }
    //    public string Comments { get; set; }
    //}

    //[MessagePackObject(keyAsPropertyName: true)]
    //public abstract class HtlcHeader<TBody> : Header<TBody>
    //{
    //    // Just exists only for HTLC message
    //    public string Router { get; set; }
    //    public string Next { get; set; }
    //}

    //[MessagePackObject(keyAsPropertyName: true)]
    //public abstract class HtlcResponseHeader<TBody> : HtlcHeader<TBody>
    //{
    //    /// <summary>
    //    /// Optional contents in the message header
    //    /// </summary>
    //    public string Error { get; set; }
    //    public string Comments { get; set; }
    //}
}
