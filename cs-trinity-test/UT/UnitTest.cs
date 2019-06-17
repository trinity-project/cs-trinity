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
using Neo.IO.Json;

using Trinity.BlockChain;
using Trinity.Wallets;
using Trinity.Wallets.Templates.Definitions;

using TestTrinity.UT.Tests;

using Trinity.Network.TCP;

namespace TestTrinity
{
    public sealed class UnitTest
    {
        public static void TestMain()
        {

            new UTestChannel();
            //TempTest();
        }

        /////////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////
        /// Temperory Test
        /////////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////
        private static void TempTest()
        {
            //交易数据，模拟
            string PrikeySelf = "d3e366d637fcb62807d7dc4b196f45c84f7768434593957a744b5adcad1cdffd";               //ASkZe5DCXsSFARJnwEB3DGxkfTK17LteRa
            string PublicKeySelf = "0292a25f5f0772d73d3fb50d42bb3cb443505b15e106789d19efa4d09c5ddca756";          //ASkZe5DCXsSFARJnwEB3DGxkfTK17LteRa
            //string PublicKeySelf = "02ea3b68aa765c9af9dfa89eeb39dde03d1816493d9e11bb827940ee47ce2536cc";          //Acwk9RhThDhn6x47GpKmUV9SS8qmGHSimC
            string PrikeyOther = "f78b197acdbee24e3bfbd06c375913752a7307cd0c60e042752412af365a8482";              //AY11NSgBM3Hvx56nyUXD6ocLAahrcMps6C
            string PublicKeyOther = "022949376faacb0c6783da8ab63548926cb3a2e8d786063a449833f927fa8853f0";         //AY11NSgBM3Hvx56nyUXD6ocLAahrcMps6C
            //string PublicKeyOther = "02d62f3a5e56ae9e20e0803d735465e88019ef9e7545a14c611ba72bb6fdab5d52";         //AcnJGoFrRe5QNKjSadk7yZpw8wQuTWmiRE
            string BalanceSelf = "100000000";
            string BalanceOther = "100000000";
            string AssetId = "849d095d07950b9e56d0c895ec48ec5100cfdff1";

            NeoTransaction neoTransaction = new NeoTransaction(AssetId, PublicKeySelf, BalanceSelf, PublicKeyOther, BalanceOther);

            //FundingTx
            Console.WriteLine("----------FundingTx------------");
            neoTransaction.CreateFundingTx(out FundingTx fundingTx);
            Console.WriteLine(fundingTx?.Serialize());

            neoTransaction.CreateFundingTx(out FundingTx fundTx);

            Console.WriteLine("----------FundingTx签名------------");
            byte[] prikeyByteSelf = PrikeySelf.HexToBytes();
            byte[] prikeyByteOther = PrikeyOther.HexToBytes();

            string txData = fundingTx?.txData;
            txData = NeoInterface.FormatJObject(txData);
            string signSelf = NeoInterface.Sign(txData, prikeyByteSelf);
            string signOther = NeoInterface.Sign(txData, prikeyByteOther);
            Console.WriteLine(signSelf);
            Console.WriteLine(signOther);

            // Set the funding address information to this innstance
            neoTransaction.SetAddressFunding(fundingTx?.addressFunding);
            neoTransaction.SetScripFunding(fundingTx?.scriptFunding);

            //CTX
            Console.WriteLine("----------CTX------------");
            string addressFunding = fundingTx?.addressFunding;
            string fundingScript = fundingTx?.scriptFunding;

            // Commitment
            
            neoTransaction.CreateCTX(out CommitmentTx CTX);
            Console.WriteLine(CTX?.Serialize());

            Console.WriteLine("----------CTX签名---------");
            string txData1 = CTX?.txData;
            string signSelf1 = NeoInterface.Sign(txData1, prikeyByteSelf);
            string signOther1 = NeoInterface.Sign(txData1, prikeyByteOther);
            Console.WriteLine(signSelf1);
            Console.WriteLine(signOther1);

            // Set rsmc multi-signature address to this neoTransaction instance
            neoTransaction.SetAddressRSMC(CTX?.addressRSMC);
            neoTransaction.SetScripRSMC(CTX?.scriptRSMC);

            //RDTX
            Console.WriteLine("----------RDTX------------");
            UInt160 ScriptHashSelf1 = NeoInterface.PublicKeyToScriptHash(PublicKeySelf);
            string AddressSelf = NeoInterface.ToAddress1(ScriptHashSelf1);
            string addressRSMC = CTX?.addressRSMC;
            string CTxId = CTX?.txId;
            string RSMCScript = CTX?.scriptRSMC;

            neoTransaction.createRDTX(out RevocableDeliveryTx RDTX, CTX?.txId);
            Console.WriteLine(RDTX?.Serialize());

            Console.WriteLine("----------RDTX签名------------");
            string txData2 = RDTX?.txData;
            string signSelf2 = NeoInterface.Sign(txData2, prikeyByteSelf);
            string signOther2 = NeoInterface.Sign(txData2, prikeyByteOther);
            Console.WriteLine(signSelf2);
            Console.WriteLine(signOther2);

            //CTX
            Console.WriteLine("----------Settle------------");
            neoTransaction.CreateSettle(out TxContents Settle);
            Console.WriteLine(Settle?.Serialize());

            Console.WriteLine("----------Settle签名---------");
            string txData3 = Settle?.txData;
            string signSelf3 = NeoInterface.Sign(txData3, prikeyByteSelf);
            string signOther3 = NeoInterface.Sign(txData3, prikeyByteOther);
            Console.WriteLine(signSelf3);
            Console.WriteLine(signOther3);
        }

        //public static void Main()
        //{
        //    string assetType = "TNC";
        //    string uri = "023c72090418f5d4511e4a47f78db7e8d2483378dc644201e9ce597a2cfbf4a937@10.0.0.5:8089";
        //    string peerUri = "0285593d596c6619694430d6b5e6ac18acecff83043329aae4fe408d3573d77317@localhost:8089";
        //    string channelName = Channel.NewChannel(uri, peerUri);

        //    Channel channelEntry = new Channel(channelName, assetType, uri, peerUri);
        //    channelEntry.AddBlockHeight(uri, 2754334);
        //    uint height = channelEntry.TryGetBlockHeight(uri);
        //    Console.WriteLine(height);
        //}
    }
}
