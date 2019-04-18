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
using Trinity.TrinityWallet.TransferHandler;
using Trinity.Network.TCP;

namespace Trinity.TrinityWallet.Tests
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

        public void SyncWalletData()
        {
            SyncWalletHandler msgHandler = new SyncWalletHandler();

            msgHandler.SetTcpHandler(this.Client);
            msgHandler.SetHeaderAttribute<string>("NetMagic", "12345678");
            msgHandler.SetBodyAttribute("Publickey", "0257f6e8e5ee6a4a5413045c693b4a17c0191f1250e4ff078787c44993a1ddca81");
            msgHandler.SetBodyAttribute("alias", "NoAlias");
            msgHandler.SetBodyAttribute("AutorCreate", "0");
            msgHandler.SetBodyAttribute("Ip", "localhost:20556");
            msgHandler.SetBodyAttribute("MaxChannel", 10);
            Dictionary<string, Dictionary<string, Double>> ChannelInfo = new Dictionary<string, Dictionary<string, Double>>();
            Dictionary<string, Double> InfoItem = new Dictionary<string, Double>();
            InfoItem.Add("TNC", 10);
            ChannelInfo.Add("Balance", InfoItem);

            InfoItem.Clear();
            InfoItem.Add("Fee", 0);
            ChannelInfo.Add("NEO", InfoItem);

            InfoItem.Clear();
            InfoItem.Add("Fee", 0.001);
            ChannelInfo.Add("GAS", InfoItem);

            InfoItem.Clear();
            InfoItem.Add("Fee", 0.01);
            InfoItem.Add("CommitMinDeposit", 1);
            InfoItem.Add("CommitMaxDeposit", 5000);
            ChannelInfo.Add("TNC", InfoItem);

            msgHandler.SetBodyAttribute("Channel", ChannelInfo);

            // Start to send RegisterKeepAlive to gateway
            Console.WriteLine("Send SyncWalletData: {0}", msgHandler.ToJson());
            msgHandler.MakeTransaction(this.Client, msgHandler.GetMessage());

            // received the expected messages
            this.Client.ReceiveMessage("AckSyncWallet");
        }
    }
}
