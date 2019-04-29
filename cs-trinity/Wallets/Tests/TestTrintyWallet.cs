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

using Trinity.Network.TCP;
using Trinity.Wallets.Templates.Messages;

namespace Trinity.Wallets.Tests
    
{
    public class MockKeyPair
    {
        public readonly byte[] PrivateKey;
        public readonly string PublicKey;

        public MockKeyPair(string priKey, string pubKey)
        {
            this.PrivateKey = priKey.Replace("0x", "").HexToBytes();
            this.PublicKey = pubKey;
        }
    }

    public class TestTrinityWallet : TrinityWallet
    {
        // Variable declaration
        private readonly NeoSystem neoSystem;
        private readonly Wallet neoWallet;
        private readonly MockKeyPair walletKey;

        private readonly TrinityTcpClient client;

        public string pubKey;

        public TestTrinityWallet(NeoSystem system, Wallet wallet, string pubKey, string prikey, string ip = null, string port = null)
            : base(system, wallet, pubKey, ip, port)
        {
            this.neoSystem = system;
            this.neoWallet = wallet;
            this.pubKey = pubKey;
            this.walletKey = new MockKeyPair(prikey, pubKey);

            if (null != ip && null != port)
            {
                this.client = new TrinityTcpClient(ip, port);
                this.client.CreateConnetion();
            }
        }

        private void ProcessMessage(string message)
        {
            TransactionHeader header = message.Deserialize<TransactionHeader>();

            // To handle the message
            switch (header.MessageType)
            {
                case "RegisterChannel":
                    new TestRegisterChannelHandler(this, this.client, message).Handle();
                    break;
                case "RegisterChannelFail":
                    new TestRegisterChannelFailHandler(this, this.client, message).Handle();
                    break;
                case "Founder":
                    new TestFounderHandler(this, this.client, message).Handle();
                    break;
                case "FounderSign":
                    new TestFounderSignHandler(this, this.client, message).Handle();
                    break;
                case "FounderFail":
                    new TestFounderFailHandler(this, this.client, message).Handle();
                    break;
                case "Rsmc":
                    // new RsmcHandler(message).Handle();
                    break;
                case "RsmcSign":
                    // new RsmcSignHandler(message).Handle();
                    break;
                case "RsmcFail":
                    // new RsmcFailHandler(message).Handle();
                    break;
                case "Htlc":
                    // new HtlcHandler(message).Handle();
                    break;
                case "HtlcSign":
                    // new HtlcSignHandler(message).Handle();
                    break;
                case "HtlcFail":
                    // new HtlcFailHandler(message).Handle();
                    break;
                case "Settle":
                    new TestSettleHandler(this, this.client, message).Handle();
                    break;
                case "SettleSign":
                    new TestSettleSignHandler(this, this.client, message).Handle();
                    break;
                case "RResponse":
                    //new RResponseHandler(message).Handle();
                    break;
                default:
                    Console.WriteLine("Receive the message: {0}", header.MessageType);
                    break;
            }
        }
    }
}
