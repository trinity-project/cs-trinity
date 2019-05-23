
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

using Trinity.Network.TCP;
using Trinity.ChannelSet;
using Trinity.TrinityDB.Definitions;
using Trinity.Wallets;
using TestTrinity.FT.Tests;
using Trinity;
using Trinity.BlockChain;
using Trinity.Wallets.Templates.Definitions;

namespace TestTrinity
{
    public sealed class FunctionTest
    {
        private static void TempTest()
        {
            string channelName = Channel.NewChannel("02144bbcf3139f372fd680ae29847d76b778547becacaf8e700ff0afaf1e1c4f45@10.10.10.5:8089",
                "02144bbcf3139f372fd680ae29847d76b778547becacaf8e700ff0afaf1e1c4f45@10.10.10.6:8089");
            Channel channel = new Channel(channelName, "TNC",
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

            TransactionTabelContent txtcontent = new TransactionTabelContent()
            {
                nonce = 1,
                monitorTxId = "0xtest_txid"
            };
            channel.AddTransaction(1, txtcontent);

            TransactionTabelContent txContent = channel.GetTransaction(1);

            txContent = channel.TryGetTransaction(2);

            ChannelTableContent channelContent = new ChannelTableContent()
            {
                channel = channelName,
                asset = "TNC"
            };
            channelContent.balance = new Dictionary<string, long>();
            channelContent.balance.Add("founder", 100);
            channelContent.balance.Add("partner", 100);
            channel.AddChannel(channelName, channelContent);

            List<ChannelTableContent> channelList = channel.GetChannelListOfThisWallet();

            ChannelTableContent channelItem = channel.TryGetChannel(channelName);

            //string address = NeoInterface.ToAddress1("030b97a25f520b417e436d91cd849877ff1c02fff60d7a39a578a60f51fc6eccd8".ConvertToScriptHash());

            //string origin = "04" + "6B17D1F2E12C4247F8BCE6E563A440F277037D812DEB33A0F4A13945D898C296" + "4FE342E2FE1A7F9B8EE7EB4A7C0F9E162BCE33576B315ECECBB6406837BF51F5";


        }

        //create_sender_HTLC_TXS
        public static void TempTest1()
        {
            string assetId = "849d095d07950b9e56d0c895ec48ec5100cfdff1";
            string pubKey = "0292a25f5f0772d73d3fb50d42bb3cb443505b15e106789d19efa4d09c5ddca756";
            string deposit = "100000000";
            string peerPubKey = "022949376faacb0c6783da8ab63548926cb3a2e8d786063a449833f927fa8853f0";
            string peerDeposit = "100000000";
            string HtlcValue = "100000000";
            string balance = "0";
            string peerBalance = "200000000";

            NeoTransaction neoTransaction = new NeoTransaction(assetId, pubKey, deposit, peerPubKey, peerDeposit);
            //生成Funding
            Console.WriteLine("---------Funding---------");
            neoTransaction.CreateFundingTx(out FundingTx fundingTx);
            Log.Debug("HCTX: {0}", fundingTx.Serialize());

            //生成HashR
            Console.WriteLine("---------HashR---------");
            //string R = neoTransaction.CreateR(64);
            string R = "173968fa86d12fceeda5dc0a431f433fd68323e7e0c38e14a5611acb6a11ea66";
            string HashR = NeoUtils.Sha1(R);
            Console.WriteLine(HashR);                                      //f6d5a548cbb3c8f9e02c7aa1a17afc829fa65d33

            NeoTransaction neoTransaction1 = new NeoTransaction(assetId, pubKey, deposit, peerPubKey, peerDeposit, fundingTx.addressFunding, fundingTx.scriptFunding);
            Console.WriteLine("---------Sender_HCTX---------");
            neoTransaction1.CreateSenderHCTX(out HtlcCommitTx hctx, HtlcValue, balance, peerBalance, HashR);
            Log.Debug("HCTX: {0}", hctx.Serialize());

            Log.Debug("---------Sender_RDTX---------");
            neoTransaction1.CreateSenderRDTX(out RevocableDeliveryTx RevocableDeliveryTx, balance, hctx.txId);
            Log.Debug("RDTX: {0}", RevocableDeliveryTx.Serialize());

            Log.Debug("---------HEDTX---------");
            neoTransaction1.CreateHEDTX(out HtlcExecutionDeliveryTx HEDTX, HtlcValue);
            Log.Debug("HEDTX: {0}", HEDTX.Serialize());

            Log.Debug("---------HTTX---------");
            neoTransaction1.CreateHTTX(out HtlcTimoutTx HTTX, HtlcValue);
            Log.Debug("HTTX: {0}", HTTX.Serialize());

            Log.Debug("---------HTRDTX---------");
            neoTransaction1.CreateHTRDTX(out RevocableDeliveryTx RevocableDeliveryTx1, HTTX.txId, HtlcValue);
            Log.Debug("HTRDTX: {0}", RevocableDeliveryTx1.Serialize());

        }

        //create_receiver_HTLC_TXS
        public static void TempTest2()
        {
            string assetId = "849d095d07950b9e56d0c895ec48ec5100cfdff1";
            string pubKey = "0292a25f5f0772d73d3fb50d42bb3cb443505b15e106789d19efa4d09c5ddca756";
            string deposit = "100000000";
            string peerPubKey = "022949376faacb0c6783da8ab63548926cb3a2e8d786063a449833f927fa8853f0";
            string peerDeposit = "100000000";
            string HtlcValue = "100000000";
            string balance = "0";
            string peerBalance = "200000000";

            NeoTransaction neoTransaction = new NeoTransaction(assetId, pubKey, deposit, peerPubKey, peerDeposit);
            //生成Funding
            Console.WriteLine("---------Funding---------");
            neoTransaction.CreateFundingTx(out FundingTx fundingTx);
            Log.Debug("HCTX: {0}", fundingTx.Serialize());

            //生成HashR
            Console.WriteLine("---------HashR---------");
            //string R = neoTransaction.CreateR(64);
            string R = "173968fa86d12fceeda5dc0a431f433fd68323e7e0c38e14a5611acb6a11ea66";
            string HashR = NeoUtils.Sha1(R);
            Console.WriteLine(HashR);                                      //f6d5a548cbb3c8f9e02c7aa1a17afc829fa65d33

            NeoTransaction neoTransaction1 = new NeoTransaction(assetId, pubKey, deposit, peerPubKey, peerDeposit, fundingTx.addressFunding, fundingTx.scriptFunding);
            Console.WriteLine("---------Receiver_HCTX---------");
            neoTransaction1.CreateReceiverHCTX(out HtlcCommitTx hctx, HtlcValue, balance, peerBalance, HashR);
            Log.Debug("HCTX: {0}", hctx.Serialize());

            Log.Debug("---------Receiver_RDTX---------");
            neoTransaction1.CreateReceiverRDTX(out RevocableDeliveryTx RevocableDeliveryTx, peerBalance, hctx.txId);
            Log.Debug("RDTX: {0}", RevocableDeliveryTx.Serialize());

            Log.Debug("---------HTDTX---------");
            neoTransaction1.CreateHTDTX(out HtlcTimeoutDeliveryTx HTDTX, HtlcValue);
            Log.Debug("HTDTX: {0}", HTDTX.Serialize());

            Log.Debug("---------HETX---------");
            neoTransaction1.CreateHETX(out HtlcExecutionTx HETX, HtlcValue);
            Log.Debug("HETX: {0}", HETX.Serialize());

            Log.Debug("---------HERDTX---------");
            neoTransaction1.CreateHERDTX(out RevocableDeliveryTx RevocableDeliveryTx1, HETX.txId, HtlcValue);
            Log.Debug("CreateHERDTX: {0}", RevocableDeliveryTx1.Serialize());

        }

        public static void TestMain()
        {
            //TempTest();
            // Output the message body to verify it's correct ??
            // TestVerifyMessageBody();

            // create the transport for following message tests
            // TrinityTcpClient client = new TrinityTcpClient("47.98.228.81", "8089");
            //TrinityTcpClient client = new TrinityTcpClient("10.0.0.5", "8089");
            //client.CreateConnetion();

            //// Test sets
            //MFTestRegisterKeepAlive(client); // RegisterKeepAlive
            //MFTestSyncWalletData(client); // SyncWalletData

            MFTestCreateChannel();

            Console.ReadKey();
        }

        private static void TestVerifyMessageBody()
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
            //FounderHandler FounderMsgHndl = new FounderHandler("1", "2", "3", "4", "5", 6, 7);
            //FounderMsgHndl.SetCommitment();
            // Console.WriteLine(FounderMsgHndl.ToJson());
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
            TestSWD.MakeupSyncWalletData();
        }

        private static void MFTestCreateChannel(bool isPeer = false)
        {
            TestCreateChannel TCCHHandler = new TestCreateChannel(isPeer);

            TCCHHandler.WCCTestRegisterKeepAlive();

            TCCHHandler.WCCTestSyncWallet();

            //TCCHHandler.WCCTestTriggerCreateChannel();
        }
    }
}
