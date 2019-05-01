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

using Trinity.Wallets.TransferHandler.ControlHandler;
using Trinity.Network.TCP;


namespace TestTrinity.FT.Tests
{
    public class TestSyncWalletData : IDisposable
    {
        private readonly TrinityTcpClient Client;
         
        public TestSyncWalletData(TrinityTcpClient client)
        {
            this.Client = client;
        }

        public void Dispose()
        {
        }

        public void MakeupSyncWalletData()
        {
            SyncWalletHandler msgHandler = new SyncWalletHandler(
                string.Format("{0}@{1}:{2}", TestConfiguration.pubKey, TestConfiguration.localIp, TestConfiguration.LocalPort), "19990331");
            
            msgHandler.SetPublicKey(TestConfiguration.pubKey);
            msgHandler.SetAlias("NoAlias");
            msgHandler.SetAutoCreate("0");
            msgHandler.SetNetAddress(string.Format("{0}:{1}", TestConfiguration.localIp, TestConfiguration.LocalPort));
            msgHandler.SetMaxChannel(10);
            msgHandler.SetChannelInfo();

            // Start to send RegisterKeepAlive to gateway
            Console.WriteLine("Send SyncWalletData: {0}", msgHandler.ToJson());
            msgHandler.MakeTransaction(this.Client);

            // received the expected messages
            this.Client.ReceiveMessage("AckSyncWallet");
        }
    }
}
