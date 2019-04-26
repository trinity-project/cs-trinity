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

using Trinity.Network.TCP;
using Trinity.TrinityWallet.Templates.Definitions;
using Trinity.TrinityWallet.Templates.Messages;
using Trinity.ChannelSet;

namespace Trinity.TrinityWallet.TransferHandler
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    /// <typeparam name="TSHandler"></typeparam>
    /// <typeparam name="TFHandler"></typeparam>
    public abstract class TransferHandler<TMessage, TSHandler, TFHandler> : IDisposable
    {
        protected TMessage Request;
        private string MessageName => typeof(TMessage).Name;
        protected TSHandler SHandler;
        protected TFHandler FHandler;

        private TrinityTcpClient client;
        public Channel channelDbInterface;

        private string pubKey;
        private string peerPubKey;

        // Record current Header
        public TransactionHeader header;

        /// <summary>
        /// Virtual Method sets. Should be overwritten in child classes.
        /// </summary>
        /// <returns></returns>
        /// Verification method sets
        public virtual bool Verify() { return true; }
        public virtual bool VerifyNonce() { return true; }
        public virtual bool VerifySignature() { return true; }
        public virtual bool VerifyBalance() { return true; }
        public virtual bool VerifyNetMagic() { return true; }

        public virtual void SucceedStep() { }
        public virtual void FailStep() { }

        /// <summary>
        /// Dispose method to release memory
        /// </summary>
        public virtual void Dispose()
        {
        }

        /// <summary>
        /// Default Constructor
        /// </summary>
        public TransferHandler()
        {
        }

        public TransferHandler(string message)
        {
            this.Request = message.Deserialize<TMessage>();
        }

        public virtual TValue GetHeaderValue<TValue>(string name)
        {
            return this.Request.GetAttribute<TMessage, TValue>(name);
        }

        public virtual void SetBodyAttribute<TValue>(string name, TValue value) {}
        public virtual void GetBodyAttribute<TContext>(string name) { }

        public virtual void MakeTransaction(TrinityTcpClient client)
        {
            client?.SendData(this.Request.Serialize());
        }

        public string ToJson()
        {
            return this.Request.Serialize();
        }

        public virtual bool Handle(string msg)
        {
            if (null != msg) {
                this.Request = msg.Deserialize<TMessage>();
                this.header = msg.Deserialize<TransactionHeader>();
                this.ParsePubkeyPair(this.header.Receiver, this.header.Sender);
                return true;
            }

            return false;
        }

        public virtual bool Handle()
        {
            return true;
        }

        public void SetClient(TrinityTcpClient client)
        {
            this.client = client;
        }

        public TrinityTcpClient GetClient()
        {
            return this.client;
        }

        public void ParsePubkeyPair(string uri, string peerUri)
        {
            this.pubKey = uri.Split('@').First();
            this.peerPubKey = peerUri.Split('@').First();
        }

        public string GetPubKey()
        {
            return this.pubKey;
        }

        public string GetPeerPubKey()
        {
            return this.peerPubKey;
        }

        public void SetChannelInterface(string uri, string peerUri, string channel, string asset)
        {
            if (null == this.channelDbInterface)
            {
                this.channelDbInterface = new Channel(channel, asset, uri, peerUri);
            } 
        }

        public Channel GetChannelInterface()
        {
            return this.channelDbInterface;
        }

        /// <summary>
        /// Trinity Transaction Role define here.
        /// </summary>
        private readonly UInt64 Role_0 = 0;
        private readonly UInt64 Role_1 = 1;
        private readonly UInt64 Role_2 = 2;
        private readonly UInt64 Role_3 = 3;

        public bool IsFailRole(UInt64 role)
        {
            return Role_0 > role || Role_3 < role;
        }

        public bool IsRole0(UInt64 role)
        {
            return role.Equals(Role_0);
        }

        public bool IsRole1(UInt64 role)
        {
            return role.Equals(Role_1);
        }

        public bool IsRole2(UInt64 role)
        {
            return role.Equals(Role_2);
        }

        public bool IsRole3(UInt64 role)
        {
            return role.Equals(Role_3);
        }
    }
}
