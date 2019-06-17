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

using Trinity;
using Trinity.Network.TCP;
using Trinity.Wallets.Templates.Messages;
using Trinity.Wallets.TransferHandler.ControlHandler;
using Trinity.Wallets.TransferHandler.TransactionHandler;

namespace TestTrinity.FT.Tests
{
    internal class TestRegisterWallet : RegisterWallet
    {

        public TestRegisterWallet(TrinityWallet wallet, TrinityTcpClient client,
            string ip, string port, string protocol) : base(ip, port, protocol)
        {
        }

        public void SendMessage()
        {
            this.MakeTransaction();
        }
    }

    internal class TestSyncWalletHandler : SyncWalletHandler
    {
        public TestSyncWalletHandler() : base()
        {
        }

        public TestSyncWalletHandler(TrinityWallet wallet, TrinityTcpClient client, 
            string sender, string magic) : base(sender, magic)
        {
        }

        public void SendMessage()
        {
            this.MakeTransaction();
        }

        public void MakeupSyncWalletData()
        {
            this.SetPublicKey(TestConfiguration.pubKey);
            this.SetAlias("NoAlias");
            this.SetAutoCreate("0");
            this.SetNetAddress(string.Format("{0}:{1}", TestConfiguration.localIp, TestConfiguration.LocalPort));
            this.SetMaxChannel(10);
            this.SetChannelInfo();

            // Start to send RegisterKeepAlive to gateway
            Console.WriteLine("Send SyncWalletData: {0}", this.ToJson());
        }
    }

    internal class TestRegisterChannelHandler : RegisterChannelHandler
    {
        public TestRegisterChannelHandler() : base()
        {
        }

        public TestRegisterChannelHandler(TrinityWallet wallet, TrinityTcpClient client, 
            string sender, string receiver, string channel, string asset, string magic,long deposit) 
            : base(sender, receiver, channel, asset, magic, deposit)
        {
        }

        public TestRegisterChannelHandler(TrinityWallet wallet, TrinityTcpClient client, 
            string message) : base(message)
        {
        }

        public void SendMessage()
        {
            this.MakeTransaction();
        }
    }

    internal class TestRegisterChannelFailHandler : RegisterChannelFailHandler
    {
        public TestRegisterChannelFailHandler() : base()
        {
        }

        public TestRegisterChannelFailHandler(TrinityWallet wallet, TrinityTcpClient client, RegisterChannel message) 
            : base(message)
        {
        }

        public TestRegisterChannelFailHandler(TrinityWallet wallet, TrinityTcpClient client, 
            string message) : base(message)
        {
        }

        public void SendMessage()
        {
            this.MakeTransaction();
        }
    }

    internal class TestFounderHandler : FounderHandler
    {
        public TestFounderHandler(TrinityWallet wallet, TrinityTcpClient client, 
            string sender, string receiver, string channel, string asset,
            string magic, UInt64 nonce, long deposit, int role = 0)
            : base(sender, receiver, channel, asset, magic, deposit)
        {
        }

        public TestFounderHandler(TrinityWallet wallet, TrinityTcpClient client, 
            string message) : base(message)
        {
        }

        public void SendMessage()
        {
            this.MakeTransaction();
        }
    }

    internal class TestFounderSignHandler : FounderSignHandler
    {
        public TestFounderSignHandler(TrinityWallet wallet, TrinityTcpClient client, Founder message) 
            : base(message)
        {
        }

        public TestFounderSignHandler(TrinityWallet wallet, TrinityTcpClient client, 
            string message) : base(message)
        {
        }

        public void SendMessage()
        {
            this.MakeTransaction();
        }
    }

    internal class TestFounderFailHandler : FounderFailHandler
    {
        public TestFounderFailHandler(TrinityWallet wallet, TrinityTcpClient client, 
            string message) : base(message)
        {
        }

        public void SendMessage()
        {
            this.MakeTransaction();
        }
    }

    public class TestCreateChannel
    {
        private readonly TrinityTcpClient client;
        private readonly TrinityWallet wallet;

        private readonly bool isPeer;

        private readonly string ip;
        private readonly string port;
        private readonly string pubKey;
        private readonly string priKey;
        private readonly string uri;
        private readonly string peerUri;
        private readonly string netMagic;
        private readonly string assetType;
        private readonly long deposit;

        private TestRegisterWallet registerWalletHndl;
        private TestSyncWalletHandler syncWalletHndl;

        private TestRegisterChannelHandler registerChannelHndl;
        private TestRegisterChannelFailHandler registerChannelFailHndl;

        private TestFounderHandler founderHntl;
        private TestFounderSignHandler founderSignHndl;
        private TestFounderFailHandler foudnerFailHndl;

        public TestCreateChannel(bool isPeer=false)
        {
            this.isPeer = isPeer;

            this.netMagic = TestConfiguration.magic;
            this.assetType = TestConfiguration.AssetType ;
            this.deposit = TestConfiguration.deposit;

            if (!isPeer)
            {
                this.ip = TestConfiguration.ip;
                this.port = TestConfiguration.port;
                this.pubKey = TestConfiguration.pubKey;
                this.priKey = TestConfiguration.priKey;
                this.uri = TestConfiguration.uri;
                this.peerUri = TestConfiguration.peerUri;
            }
            else
            {
                this.ip = TestConfiguration.peerIp;
                this.port = TestConfiguration.peerPort;
                this.pubKey = TestConfiguration.peerPubKey;
                this.priKey = TestConfiguration.peerPriKey;
                this.uri = TestConfiguration.peerUri;
                this.peerUri = TestConfiguration.uri;
            }
            this.client = new TrinityTcpClient(this.ip, this.port);
            this.client.CreateConnetion();
            this.wallet = new TestTrinityWallet(null, null, this.pubKey, this.priKey, "19990331", this.ip, this.port);

            startTrinity.trinityWallet = this.wallet;
        }

        public void WCCTestRegisterKeepAlive()
        {
            this.registerWalletHndl = new TestRegisterWallet(this.wallet, this.client, TestConfiguration.localIp, TestConfiguration.LocalPort, "TCP");
            //this.registerWalletHndl = new TestRegisterWallet(this.wallet, this.client, this.ip, this.port, "TCP");
            Console.WriteLine("Send RegisterKeepAlive: {0}", this.registerWalletHndl.ToJson());
            this.registerWalletHndl.SendMessage();
        }

        public void WCCTestSyncWallet()
        {
            //TestRegisterKeepAlive TestRKA = new TestRegisterKeepAlive(client);
            //TestRKA.RegisterToGateWay();
            TestSyncWalletData TestSWD = new TestSyncWalletData(client);
            //TestSWD.MakeupSyncWalletData();

            this.syncWalletHndl = new TestSyncWalletHandler(this.wallet, this.client, 
                string.Format("{0}@{1}:{2}", TestConfiguration.pubKey, TestConfiguration.localIp, TestConfiguration.LocalPort), this.netMagic);
            this.syncWalletHndl.MakeupSyncWalletData();
            //this.syncWalletHndl.SetPublicKey(this.pubKey);
            //this.syncWalletHndl.SetAlias("NoAlias");
            //this.syncWalletHndl.SetAutoCreate("0");
            //this.syncWalletHndl.SetNetAddress("localhost:20556");
            //this.syncWalletHndl.SetMaxChannel(10);
            //this.syncWalletHndl.SetChannelInfo();

            // Start to send RegisterKeepAlive to gateway
            Console.WriteLine("Send SyncWalletData: {0}", this.syncWalletHndl.ToJson());
            this.syncWalletHndl.SendMessage();

            // received the expected messages
            //this.client.ReceiveMessage("AckSyncWallet");
        }

        public void WCCTestTriggerCreateChannel()
        {
            if (!this.isPeer)
            {
                this.registerChannelHndl = new TestRegisterChannelHandler(
                this.wallet, this.client, this.uri, this.peerUri, null, this.assetType, this.netMagic, this.deposit
                );

                // send RegisterChannel
                Console.WriteLine("Send RegisterChannel: {0}", this.registerChannelHndl.ToJson());
                this.registerChannelHndl.SendMessage();

                //// expected the Founder message
                //this.client.ReceiveMessage("Founder");

                //// second message is FounderSign
                //this.client.ReceiveMessage("FounderSign");
            }
            else
            {
                //// expected the Founder message
                //this.client.ReceiveMessage("RegisterChannel");
                
                //// expected the Founder message
                //this.client.ReceiveMessage("FounderSign");

                //// second message is FounderSign
                //this.client.ReceiveMessage("Founder");
            }
            
        }
    }
}
