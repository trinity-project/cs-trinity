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
    ///
    /// HeaderBase is provided to both control and transaction plane
    [MessagePackObject(keyAsPropertyName: true)]
    public class HeaderBase
    {
        /// <summary>
        /// Mandatory contents in the message header
        /// </summary>
        public string MessageType => this.GetType().Name;
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public string NetMagic { get; set; }
        public string AssetType { get; set; }
    }

    /// <summary>
    /// ControlHeader for Control plane messages
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class ControlHeader : HeaderBase
    {
    }

    /// <summary>
    /// ContolPlaneGeneric: generic for Control plane messages.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class ContolPlaneGeneric<TBody> : ControlHeader
    {
        public TBody MessageBody { get; set; }
    }

    /// <summary>
    /// TransactionHeader for Transaction plane messages
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class TransactionHeader : HeaderBase
    {
        /// <summary>
        /// Mandatory contents in the message header
        /// </summary>
        public string ChannelName { get; set; }
        public UInt64 TxNonce { get; set; }
    }

    /// <summary>
    /// TransactionGeneric: generic for transaction plane messages.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class TransactionPlaneGeneric<TBody> : TransactionHeader
    {
        public TBody MessageBody { get; set; }
    }

    /// <summary>
    /// For parsing the messages received from Gateway
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class ReceivedHeader
    {
        /// <summary>
        /// Mandatory contents in the message header
        /// </summary>
        public string MessageType { get; set; }
        public string Receiver { get; set; }
        public string AssetType { get; set; }
        public string NetMagic { get; set; }
    }

    /// <summary>
    /// RpcHeader for RPC messages
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class RpcHeader
    {
        public virtual string MessageType { get; set; }
        public string NetMagic { get; set; }
        public string AssetType { get; set; }
    }

    /// <summary>
    /// ContolPlaneGeneric: generic for Control plane messages.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class RpcMessageGeneric<TBody> : RpcHeader
    {
        public TBody MessageBody { get; set; }
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public class RpcResponse
    {
        public string jsonrpc { get; set; }
        public string result { get; set; }
        public int id { get; set; }
    }
}
