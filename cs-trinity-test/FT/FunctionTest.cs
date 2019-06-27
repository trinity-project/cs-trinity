
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
using Trinity.Network.RPC;
using Trinity.Wallets.Templates.Messages;
using Neo.IO.Json;
using System.Text;

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

            TransactionFundingContent txtcontent = new TransactionFundingContent()
            {
                nonce = 1,
                monitorTxId = "0xtest_txid"
            };
            channel.AddTransaction(1, txtcontent);

            TransactionFundingContent txContent = channel.GetTransaction<TransactionFundingContent>(1);

            txContent = channel.TryGetTransaction<TransactionFundingContent>(2);

            ChannelTableContent channelContent = new ChannelTableContent()
            {
                channel = channelName,
                asset = "TNC"
            };
            channelContent.balance = 100;
            channelContent.peerBalance = 100;
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
            neoTransaction1.CreateSenderHCTX(out HtlcCommitTx hctx, HtlcValue, HashR);
            Log.Debug("HCTX: {0}", hctx.Serialize());

            Log.Debug("---------Sender_RDTX---------");
            neoTransaction1.CreateSenderRDTX(out HtlcRevocableDeliveryTx RevocableDeliveryTx, hctx.txId);
            Log.Debug("RDTX: {0}", RevocableDeliveryTx.Serialize());

            Log.Debug("---------HEDTX---------");
            neoTransaction1.CreateHEDTX(out HtlcExecutionDeliveryTx HEDTX, HtlcValue);
            Log.Debug("HEDTX: {0}", HEDTX.Serialize());

            Log.Debug("---------HTTX---------");
            neoTransaction1.CreateHTTX(out HtlcTimoutTx HTTX, HtlcValue);
            Log.Debug("HTTX: {0}", HTTX.Serialize());

            Log.Debug("---------HTRDTX---------");
            neoTransaction1.CreateHTRDTX(out HtlcTimeoutRevocableDelivertyTx RevocableDeliveryTx1, HTTX.txId, HtlcValue);
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
            neoTransaction1.CreateReceiverHCTX(out HtlcCommitTx hctx, HtlcValue, HashR);
            Log.Debug("HCTX: {0}", hctx.Serialize());

            Log.Debug("---------Receiver_RDTX---------");
            neoTransaction1.CreateReceiverRDTX(out HtlcRevocableDeliveryTx RevocableDeliveryTx, hctx.txId);
            Log.Debug("RDTX: {0}", RevocableDeliveryTx.Serialize());

            Log.Debug("---------HTDTX---------");
            neoTransaction1.CreateHTDTX(out HtlcTimeoutDeliveryTx HTDTX, HtlcValue);
            Log.Debug("HTDTX: {0}", HTDTX.Serialize());

            Log.Debug("---------HETX---------");
            neoTransaction1.CreateHETX(out HtlcExecutionTx HETX, HtlcValue);
            Log.Debug("HETX: {0}", HETX.Serialize());

            Log.Debug("---------HERDTX---------");
            neoTransaction1.CreateHERDTX(out HtlcExecutionRevocableDeliveryTx RevocableDeliveryTx1, HETX.txId, HtlcValue);
            Log.Debug("CreateHERDTX: {0}", RevocableDeliveryTx1.Serialize());

        }

        //create_receiver_HTLC_TXS
        public static void TempTest3()
        {
            string assetId = "0xc56f33fc6ecfcd0c225c4ab356fee59390af8560be0e930faebe74a6daff7c9b";
            string pubKey = "025aa64efb9a5176a550210cdc795060cab8f7711e7cd69dbe12b9bbd3ee2dd721";
            string deposit = "1";
            string peerPubKey = "0391e05b532e5e8aa9eb0ef3c3888cf7636a428c339c33ad620d0f2900437999d6";
            string peerDeposit = "1";
            string HtlcValue = "100000000";
            string balance = "0";
            string peerBalance = "200000000";

            NeoTransaction_Neo neoTransaction = new NeoTransaction_Neo(assetId, pubKey, deposit, peerPubKey, peerDeposit);
            //生成Funding
            Console.WriteLine("---------Funding---------");
            neoTransaction.CreateFundingTx(out FundingTx fundingTx);
            Log.Debug("FundingTX: {0}", fundingTx.Serialize());

            Console.WriteLine("---------CTX---------");
            neoTransaction.CreateCTX(out CommitmentTx commitmentTx);
            Log.Debug("CTX: {0}", commitmentTx.Serialize());

            Console.WriteLine("---------RDTX---------");
            neoTransaction.CreateRDTX(out RevocableDeliveryTx revocableDeliveryTx, commitmentTx.txId);
            Log.Debug("CTX: {0}", revocableDeliveryTx.Serialize());

            Console.WriteLine("---------BRTX---------");
            neoTransaction.CreateBRTX(out BreachRemedyTx breachRemedyTx, commitmentTx.txId);
            Log.Debug("CTX: {0}", breachRemedyTx.Serialize());
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

            //MFTestCreateChannel();

            TestRpc();

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

        private static void MFTestCreateChannel(bool isFounder = false)
        {
            TestCreateChannel TCCHHandler = new TestCreateChannel(isFounder);

            TCCHHandler.WCCTestRegisterKeepAlive();

            TCCHHandler.WCCTestSyncWallet();

            //TCCHHandler.WCCTestTriggerCreateChannel();
        }

        public static void TestRpc()
        {
            //string uri = "http://47.97.198.8:8077";
            string uri = "http://47.98.228.81:8077";

            GetRouterInfo content = new GetRouterInfo
            {
                Sender = "03745d64d8f1fd71c7dbb05dd043eaa94e114290642678e36d33ee5df23970e881@47.97.198.8:8089",
                Receiver = "028ec43f00663b037e2c6d32e4cbe5052d6705bb6aa4e3b0a54f607550c2b54174@47.98.228.81:8089",

                NetMagic = "195378745719990331",

                MessageBody = new RouteInfoBody
                {
                    AssetType = "TNC",
                    Value = 1,
                }
            };

            string result = TrinityRpcRequest.Post<GetRouterInfo>(uri, content.MessageType, content);
            Console.WriteLine(result);
        }

        public static void TestSignAndVefifySign()
        {
            //Make signature
            string originData = "123";
            string signedData = null;
            string privateKey = "f78b197acdbee24e3bfbd06c375913752a7307cd0c60e042752412af365a8482";
            string publicKey = "022949376faacb0c6783da8ab63548926cb3a2e8d786063a449833f927fa8853f0";

            byte[] privateByte = NeoInterface.HexString2Bytes(privateKey);
            byte[] publicByte = NeoInterface.HexString2Bytes(publicKey);

            signedData = NeoInterface.Sign(originData, privateByte);
            Console.WriteLine(signedData);

            bool verifyResult = NeoInterface.VerifySignature(originData, signedData, publicKey);
            Console.WriteLine(verifyResult.ToString());
            

            // Verify signature
            string originData1 = "d101a00400e1f50514d4c3f3dc1498733ce4b726db8546a83502a891c214296ac124021a71c449a9bad320c16429b08ad6ee53c1087472616e7366657267f1dfcf0051ec48ec95c8d0569e0b95075d099d84f10400e1f50514b1fdddf658ce5ff9f83e66ede2f333ecfcc0463e14296ac124021a71c449a9bad320c16429b08ad6ee53c1087472616e7366657267f1dfcf0051ec48ec95c8d0569e0b95075d099d84f100000000000000000220296ac124021a71c449a9bad320c16429b08ad6eef00873d73ab53340d7410000";
            string signedData1 = "107c164222d5abf1702982a5fc04fdec904c995a89b55f1549e38e45596b56cb039821262706c2d03da76c4c651ec1c731d8c089534f9e4a7f57cf953cb98ce0";

            string publicKey1 = "022949376faacb0c6783da8ab63548926cb3a2e8d786063a449833f927fa8853f0";

            byte[] publicByte1 = NeoInterface.HexString2Bytes(publicKey1);
            bool verifyResult1 = NeoInterface.VerifySignature(originData1, signedData1, publicKey1);
            Console.WriteLine(verifyResult1.ToString());
        }
    }
}
