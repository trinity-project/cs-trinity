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
using Trinity.Wallets.Templates.Messages;
using Trinity.Wallets.TransferHandler.ControlHandler;
using Trinity.Wallets.TransferHandler.TransactionHandler;

namespace Trinity.Wallets.Tests
{
    internal class TestSettleHandler : SettleHandler
    {

        public TestSettleHandler(TrinityWallet wallet, TrinityTcpClient client, 
            string message) : base(message)
        {
            this.SetClient(client);
            this.SetWallet(wallet);
        }

        public TestSettleHandler(TrinityWallet wallet, TrinityTcpClient client, 
            string sender, string receiver, string channel, string asset, string magic)
            : base(sender, receiver, channel, asset, magic)
        {
            this.SetClient(client);
            this.SetWallet(wallet);
        }

        public void SendMessage()
        {
            this.MakeTransaction();
        }
    }

    internal class TestSettleSignHandler : SettleSignHandler
    {

        public TestSettleSignHandler(TrinityWallet wallet, TrinityTcpClient client,
            string message) : base(message)
        {
            this.SetClient(client);
            this.SetWallet(wallet);
        }

        public TestSettleSignHandler(TrinityWallet wallet, TrinityTcpClient client,
            string sender, string receiver, string channel, string asset, string magic)
            : base(sender, receiver, channel, asset, magic)
        {
            this.SetClient(client);
            this.SetWallet(wallet);
        }

        public void SendMessage()
        {
            this.MakeTransaction();
        }
    }

    public class TestCloseChannel
    {
    }
}
