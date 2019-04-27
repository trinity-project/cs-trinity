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

using Trinity.TrinityWallet;
using Trinity.BlockChain;

namespace Trinity
{
    public class TntWallet
    {
        private readonly NeoSystem neoSystem;
        private readonly Wallet neoWallet;
        private readonly KeyPair walletKey;
        public string pubKey;

        public TntWallet(NeoSystem system, Wallet wallet, string pubKey)
        {
            this.neoSystem = system;
            this.neoWallet = wallet;
            this.pubKey = pubKey;
            this.walletKey = this.neoWallet?.GetAccount(pubKey.ConvertToScriptHash()).GetKey();
        }

        public void Start()
        {

        }

        private void Handle(string message)
        {

        }

        public string Sign(string content)
        {
            return NeoInterface.Sign(content, this.walletKey.PrivateKey);
        }

        public bool VerifySignarture(string content, string contentSign)
        {
            return NeoInterface.VerifySignature(content, contentSign,
                this.walletKey.PublicKey.EncodePoint(false).ToArray());
        }
    }
}
