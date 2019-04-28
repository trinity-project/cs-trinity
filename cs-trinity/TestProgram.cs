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

using Trinity.Wallets.Templates.Definitions;
using Trinity.Wallets.TransferHandler.TransactionHandler;
using Trinity.Network.TCP;
using Trinity.Wallets.Tests;

using Neo.IO.Data.LevelDB;
using Trinity.ChannelSet;
using Trinity.TrinityDB.Definitions;


namespace Trinity
{
    class TestProgram
    {
        public static void TempTest()
        {
            string channelName = Channel.NewChannel("02144bbcf3139f372fd680ae29847d76b778547becacaf8e700ff0afaf1e1c4f45@10.10.10.5:8089",
                "02144bbcf3139f372fd680ae29847d76b778547becacaf8e700ff0afaf1e1c4f45@10.10.10.6:8089");
            Channel channel = new Channel("test", "TNC", 
                "02144bbcf3139f372fd680ae29847d76b778547becacaf8e700ff0afaf1e1c4f45@10.10.10.5:8089",
                "02144bbcf3139f372fd680ae29847d76b778547becacaf8e700ff0afaf1e1c4f45@10.10.10.6:8089");
            //TransactionTabelSummary txcontent = new TransactionTabelSummary
            //{
            //    nonce = 0,
            //    txType = "funding",
            //    channel = "testChannel-xxxx"
            //};
            //channel.AddTransaction("123456789", txcontent);
            //TransactionTabelSummary content = channel.GetTransaction("123456789");
            //Console.WriteLine("type = {0}, channel = {1}, nonce = {2}", content.txType, content.channel, content.nonce);

            //TransactionTabelContent txtcontent = new TransactionTabelContent();
            //channel.AddTransaction(1, txtcontent);

            ChannelTableContent channelContent = new ChannelTableContent();
            channelContent.balance = new Dictionary<string, double>();
            channelContent.balance.Add("founder", 100);
            channelContent.balance.Add("partner", 100);
            channel.AddChannel("test", channelContent);

            List<ChannelTableContent> channelList = channel.GetChannelListOfThisWallet();

            Console.ReadKey();
        }

        public static void TestMain()
        {
            TempTest();
            // Output the message body to verify it's correct ??
            // TestVerifyMessageBody();

            // create the transport for following message tests
            // TrinityTcpClient client = new TrinityTcpClient("47.98.228.81", "8089");
            //TrinityTcpClient client = new TrinityTcpClient("10.0.0.5", "8089");
            //client.CreateConnetion();

            //// Test sets
            //MFTestRegisterKeepAlive(client); // RegisterKeepAlive
            //MFTestSyncWalletData(client); // SyncWalletData

            //MFTestCreateChannel();

            Console.ReadKey();
        }

        public static void TestVerifyMessageBody()
        {
            //    // Message : RegisterKeepAlive
            //    RegisterKeepAlive Request = new RegisterKeepAlive();
            //    RegisterWallet MsgHandler = new RegisterWallet(Request);
            //    Console.WriteLine(MsgHandler.GetJsonMessage());
            //    using (RegisterWallet msgHandler = new RegisterWallet("test", "port"))
            //    {
            //        Console.WriteLine(msgHandler.ToJson());
            //    }

            // Message : Founder
            FounderHandler FounderMsgHndl = new FounderHandler("1", "2", "3", "4", "5", 6, 7);
            //FounderMsgHndl.SetCommitment();
            Console.WriteLine(FounderMsgHndl.ToJson());
        }

        /// <summary>
        /// Message Flow Test : RegisterKeepAlive
        /// </summary>
        /// <param name="client">Tcp client</param>
        private static void MFTestRegisterKeepAlive(TrinityTcpClient client)
        {
            TestRegisterKeepAlive TestRKA = new TestRegisterKeepAlive(client);
            TestRKA.RegisterToGateWay();
        }

        /// <summary>
        /// Message Flow Test : SyncWalletData
        /// </summary>
        /// <param name="client"></param>
        private static void MFTestSyncWalletData(TrinityTcpClient client)
        {
            TestSyncWalletData TestSWD = new TestSyncWalletData(client);
            TestSWD.SyncWalletData();
        }

        private static void MFTestCreateChannel(bool isPeer = false)
        {
            TestCreateChannel TCCHHandler = new TestCreateChannel(isPeer);

            TCCHHandler.WCCTestRegisterKeepAlive();

            TCCHHandler.WCCTestSyncWallet();

            TCCHHandler.WCCTestTriggerCreateChannel();
        }
    }
}
