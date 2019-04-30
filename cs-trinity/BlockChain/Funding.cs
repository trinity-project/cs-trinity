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
using System.Numerics;
using Neo;
using Neo.VM;
using Neo.Wallets;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.SmartContract;
using Neo.IO.Json;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using System.IO;
using Trinity.BlockChain;

namespace Trinity.BlockChain
{
    class Funding
    {
        static long testTime = 1554866712;

        ///<summary>
        ///构造Funding交易
        ///</summary>
        ///<param name="PublicKeySelf">发起方公钥</param>
        ///<param name="DepositSelf">发起方押金</param>
        ///<param name="PublicKeyOther">对端公钥</param>
        ///<param name="DepositOther">对端押金</param>
        ///<param name="AssetId">资产ID</param>
        ///<returns>
        ///Funding交易数据
        ///</returns>
        public static JObject createFundingTx(string PublicKeySelf, string DepositSelf, string PublicKeyOther, string DepositOther, string AssetId)
        {
            Contract contract = NeoInterface.CreateMultiSigContract(PublicKeySelf, PublicKeyOther);
            string contractAddress = NeoInterface.ToAddress1(contract.ScriptHash);
            UInt160 ScriptHashSelf = NeoInterface.PublicKeyToScriptHash(PublicKeySelf);
            UInt160 ScriptHashOther = NeoInterface.PublicKeyToScriptHash(PublicKeyOther);
            string AddressSelf = NeoInterface.ToAddress1(ScriptHashSelf);
            string AddressOther = NeoInterface.ToAddress1(ScriptHashOther);
            //string address = script_hash.ToAddress();       //无法获取gui的参数，暂不使用             

            string op_dataSelf = NeoInterface.createOpdata(AddressSelf, contractAddress, DepositSelf, AssetId);
            Console.WriteLine("op_dataSelf:");
            Console.WriteLine(op_dataSelf);
            string op_dataOther = NeoInterface.createOpdata(AddressOther, contractAddress, DepositOther, AssetId);
            Console.WriteLine("op_dataOther:");
            Console.WriteLine(op_dataOther);
 
            TimeSpan cha = DateTime.Now - TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));                         //时间戳
            long t = (long)cha.TotalSeconds;
            #if DEBUG
                t = testTime;
            #endif

            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Script, ScriptHashSelf, attributes).MakeAttribute(out attributes);
            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Script, ScriptHashOther, attributes).MakeAttribute(out attributes);
            new NeoInterface.TransactionAttributeLong(TransactionAttributeUsage.Remark, t, attributes).MakeAttribute(out attributes);

            Transaction tx = new InvocationTransaction
            {
                Version = 1,
                Script = (op_dataSelf + op_dataOther).HexToBytes(),
            };
            tx.Attributes = attributes.ToArray();
            tx.Inputs = new CoinReference[0];
            tx.Outputs = new TransactionOutput[0];

            string witness;
            if (ScriptHashSelf > ScriptHashOther)
            {
                witness = "024140{signOther}2321" + PublicKeyOther + "ac" + "4140{signSelf}2321" + PublicKeySelf + "ac";
            }
            else
            {
                witness = "024140{signSelf}2321" + PublicKeySelf + "ac" + "4140{signOther}2321" + PublicKeyOther + "ac";
            }

            JObject result = new JObject();
            result["txData"] = tx.GetHashData().ToHexString();
            result["addressFunding"] = contractAddress;
            result["txId"] = tx.Hash.ToString();
            result["scriptFunding"] = contract.Script.ToHexString();
            result["witness"] = witness;

            return result;
        }

        ///<summary>
        ///构造CTX
        ///</summary>
        ///<param name="AddressFunding">Funding产生的多签合约地址</param>
        ///<param name="BalanceSelf">发起方余额</param>
        ///<param name="BalanceOther">对端余额</param>
        ///<param name="PublicKeySelf">发起方公钥</param>
        ///<param name="PublicKeyOther">对端公钥</param>
        ///<param name="FundingScript">Funding产生的多签合约脚本</param>
        ///<param name="AssetId">资产ID</param>
        ///<returns>
        ///C交易数据
        ///</returns>
        public static JObject createCTX(string AddressFunding, string BalanceSelf, string BalanceOther, string PublicKeySelf, string PublicKeyOther, string FundingScript, string AssetId)
        {
            // 双方Scripthash
            UInt160 ScriptHashSelf = NeoInterface.PublicKeyToScriptHash(PublicKeySelf);
            UInt160 ScriptHashOther = NeoInterface.PublicKeyToScriptHash(PublicKeyOther);
            string AddressOther = NeoInterface.ToAddress1(ScriptHashOther);

            //时间戳
            TimeSpan cha = DateTime.Now - TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            long t = (long)cha.TotalSeconds;

            string t1 = cha.ToString();
            int index = t1.LastIndexOf('.');
            string Decimal = t1.Substring(index + 1, 6);                    //截取6位小数，RSMC合约精度需要
            t1 = t.ToString() + '.' + Decimal;

            #if DEBUG
                t = testTime;
                t1 = t.ToString() + ".265512";
            #endif
            Console.WriteLine(t);
            Console.WriteLine(t1);
            JObject RSMCContract = NeoInterface.createRSMCContract(ScriptHashSelf, PublicKeySelf, ScriptHashOther, PublicKeyOther, t1);
            Console.WriteLine(RSMCContract);
            string RSMCContractAddress = RSMCContract["address"].ToString();
            RSMCContractAddress = NeoInterface.FormatJObject(RSMCContractAddress);                                                   //暂用

            string op_data_to_RSMC = NeoInterface.createOpdata(AddressFunding, RSMCContractAddress, BalanceSelf, AssetId);
            //string AddressSelf = ToAddress1(ScriptHashSelf);
            //string op_data_to_RSMC = createOpdata(addressFunding, AddressSelf, balanceSelf, AssetId);
            Console.WriteLine("op_data_to_RSMC:");
            Console.WriteLine(op_data_to_RSMC);
            string op_data_to_other = NeoInterface.createOpdata(AddressFunding, AddressOther, BalanceOther, AssetId);
            Console.WriteLine("op_data_to_other:");
            Console.WriteLine(op_data_to_other);

            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            UInt160 address_hash_funding = NeoInterface.ToScriptHash1(AddressFunding);
            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Script, address_hash_funding, attributes).MakeAttribute(out attributes);
            new NeoInterface.TransactionAttributeLong(TransactionAttributeUsage.Remark, t, attributes).MakeAttribute(out attributes);

            Transaction tx = new InvocationTransaction
            {
                Version = 1,
                Script = (op_data_to_RSMC + op_data_to_other).HexToBytes(),
            };
            tx.Attributes = attributes.ToArray();
            tx.Inputs = new CoinReference[0];
            tx.Outputs = new TransactionOutput[0];

            JObject result = new JObject();
            result["txData"] = tx.GetHashData().ToHexString();
            result["addressRSMC"] = RSMCContract["address"];
            result["scriptRSMC"] = RSMCContract["script"];
            result["txId"] = tx.Hash.ToString();
            result["witness"] = "018240{signSelf}40{signOther}da" + FundingScript;

            return result;
        }


        ///<summary>
        ///构造RDTX
        ///</summary>
        ///<param name="AddressRSMC">CTX产生的RSMC合约地址</param>
        ///<param name="AddressSelf">发起方地址</param>
        ///<param name="BalanceSelf">发起方余额</param>
        ///<param name="CTxId">C交易的TXId</param>
        ///<param name="AssetId">资产ID</param>
        ///<returns>
        ///RD交易数据
        ///</returns>
        public static JObject createRDTX(string AddressRSMC, string AddressSelf, string BalanceSelf, string CTxId, string RSMCScript, string AssetId)
        {
            string op_data_to_self = NeoInterface.createOpdata(AddressRSMC, AddressSelf, BalanceSelf, AssetId);
            Console.WriteLine("op_data_to_self:");
            Console.WriteLine(op_data_to_self);

            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            UInt160 address_hash_RSMC = NeoInterface.ToScriptHash1(AddressRSMC);
            TimeSpan cha = DateTime.Now - TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            long t = (long)cha.TotalSeconds;                                    //时间戳
            #if DEBUG
                t = testTime;
            #endif
            string pre_txid = CTxId.Substring(2).HexToBytes().Reverse().ToArray().ToHexString();                    //pre_txid
            UInt160 ScriptHashSelf = NeoInterface.ToScriptHash1(AddressSelf);                                                    //outputTo

            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Script, address_hash_RSMC, attributes).MakeAttribute(out attributes);
            new NeoInterface.TransactionAttributeLong(TransactionAttributeUsage.Remark, t, attributes).MakeAttribute(out attributes);
            new NeoInterface.TransactionAttributeString(TransactionAttributeUsage.Remark1, pre_txid, attributes).MakeAttribute(out attributes);
            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Remark2, ScriptHashSelf, attributes).MakeAttribute(out attributes);

            Transaction tx = new InvocationTransaction
            {
                Version = 1,
                Script = (op_data_to_self).HexToBytes(),
            };
            tx.Attributes = attributes.ToArray();
            tx.Inputs = new CoinReference[0];
            tx.Outputs = new TransactionOutput[0];

            JObject result = new JObject();
            result["txData"] = tx.GetHashData().ToHexString();
            result["txId"] = tx.Hash.ToString();
            result["witness"] = "01{blockheight_script}40{signOther}40{signSelf}fd" + NeoInterface.createVerifyScript(RSMCScript);

            return result;
        }

        ///<summary>
        ///构造BRTX
        ///</summary>
        ///<param name="AddressRSMC">CTX产生的RSMC合约地址</param>
        ///<param name="AddressOther">发起方地址</param>
        ///<param name="BalanceSelf">发起方余额</param>
        ///<param name="CTxId">C交易的TXId</param>
        ///<param name="AssetId">资产ID</param>
        ///<returns>
        ///BR交易数据
        ///</returns>
        public static JObject createBRTX(string AddressRSMC, string AddressOther, string BalanceSelf, string CTxId, string RSMCScript, string AssetId)
        {
            string op_data_to_self = NeoInterface.createOpdata(AddressRSMC, AddressOther, BalanceSelf, AssetId);
            Console.WriteLine("op_data_to_self:");
            Console.WriteLine(op_data_to_self);

            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            UInt160 address_hash_RSMC = NeoInterface.ToScriptHash1(AddressRSMC);
            TimeSpan cha = DateTime.Now - TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            long t = (long)cha.TotalSeconds;                                    //时间戳
            #if DEBUG
                t = testTime;
            #endif
            string pre_txid = CTxId.Substring(2).HexToBytes().Reverse().ToArray().ToHexString();            //pre_txid
            UInt160 ScriptHashSelf = NeoInterface.ToScriptHash1(AddressOther);                              //outputTo

            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Script, address_hash_RSMC, attributes).MakeAttribute(out attributes);
            new NeoInterface.TransactionAttributeLong(TransactionAttributeUsage.Remark, t, attributes).MakeAttribute(out attributes);
            new NeoInterface.TransactionAttributeString(TransactionAttributeUsage.Remark1, pre_txid, attributes).MakeAttribute(out attributes);
            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Remark2, ScriptHashSelf, attributes).MakeAttribute(out attributes);

            Transaction tx = new InvocationTransaction
            {
                Version = 1,
                Script = (op_data_to_self).HexToBytes(),
            };
            tx.Attributes = attributes.ToArray();
            tx.Inputs = new CoinReference[0];
            tx.Outputs = new TransactionOutput[0];

            JObject result = new JObject();
            result["txData"] = tx.GetHashData().ToHexString();
            result["txId"] = tx.Hash.ToString();
            result["witness"] = "01{blockheight_script}40{signOther}40{signSelf}fd" + NeoInterface.createVerifyScript(RSMCScript);

            return result;
        }

        ///<summary>
        ///构造Settle
        ///</summary>
        ///<param name="AddressFunding">Funding产生的多签合约地址</param>
        ///<param name="BalanceSelf">发起方余额</param>
        ///<param name="BalanceOther">对端余额</param>
        ///<param name="PublicKeySelf">发起方公钥</param>
        ///<param name="PublicKeyOther">对端公钥</param>
        ///<param name="FundingScript">Funding产生的多签合约脚本</param>
        ///<param name="AssetId">资产ID</param>
        ///<returns>
        ///Settle数据
        ///</returns>
        public static JObject createSettle(string AddressFunding, string BalanceSelf, string BalanceOther, string PublicKeySelf, string PublicKeyOther, string FundingScript, string AssetId)
        {
            // 双方Scripthash
            UInt160 ScriptHashSelf = NeoInterface.PublicKeyToScriptHash(PublicKeySelf);
            UInt160 ScriptHashOther = NeoInterface.PublicKeyToScriptHash(PublicKeyOther);
            string AddressOther = NeoInterface.ToAddress1(ScriptHashOther);

            TimeSpan cha = DateTime.Now - TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));                       //时间戳
            long t = (long)cha.TotalSeconds;
            #if DEBUG
                t = testTime;
            #endif

            string AddressSelf = NeoInterface.ToAddress1(ScriptHashSelf);
            string op_data_to_self = NeoInterface.createOpdata(AddressFunding, AddressSelf, BalanceSelf, AssetId);
            Console.WriteLine("op_data_to_self:");
            Console.WriteLine(op_data_to_self);
            string op_data_to_other = NeoInterface.createOpdata(AddressFunding, AddressOther, BalanceOther, AssetId);
            Console.WriteLine("op_data_to_other:");
            Console.WriteLine(op_data_to_other);

            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            UInt160 address_hash_funding = NeoInterface.ToScriptHash1(AddressFunding);
            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Script, address_hash_funding, attributes).MakeAttribute(out attributes);
            new NeoInterface.TransactionAttributeLong(TransactionAttributeUsage.Remark, t, attributes).MakeAttribute(out attributes);
            Transaction tx = new InvocationTransaction
            {
                Version = 1,
                Script = (op_data_to_self + op_data_to_other).HexToBytes(),
            };
            tx.Attributes = attributes.ToArray();
            tx.Inputs = new CoinReference[0];
            tx.Outputs = new TransactionOutput[0];

            JObject result = new JObject();
            result["txData"] = tx.GetHashData().ToHexString();
            result["txId"] = tx.Hash.ToString();
            result["witness"] = "018240{signSelf}40{signOther}da" + FundingScript;

            return result;
        }


        //static void Main()
        //{
        //    //交易数据，模拟
        //    string PrikeySelf = "d3e366d637fcb62807d7dc4b196f45c84f7768434593957a744b5adcad1cdffd";               //ASkZe5DCXsSFARJnwEB3DGxkfTK17LteRa
        //    string PublicKeySelf = "0292a25f5f0772d73d3fb50d42bb3cb443505b15e106789d19efa4d09c5ddca756";          //ASkZe5DCXsSFARJnwEB3DGxkfTK17LteRa
        //    //string PublicKeySelf = "02ea3b68aa765c9af9dfa89eeb39dde03d1816493d9e11bb827940ee47ce2536cc";          //Acwk9RhThDhn6x47GpKmUV9SS8qmGHSimC
        //    string PrikeyOther = "f78b197acdbee24e3bfbd06c375913752a7307cd0c60e042752412af365a8482";              //AY11NSgBM3Hvx56nyUXD6ocLAahrcMps6C
        //    string PublicKeyOther = "022949376faacb0c6783da8ab63548926cb3a2e8d786063a449833f927fa8853f0";         //AY11NSgBM3Hvx56nyUXD6ocLAahrcMps6C
        //    //string PublicKeyOther = "02d62f3a5e56ae9e20e0803d735465e88019ef9e7545a14c611ba72bb6fdab5d52";         //AcnJGoFrRe5QNKjSadk7yZpw8wQuTWmiRE
        //    string BalanceSelf = "100000000";
        //    string BalanceOther = "100000000";
        //    string AssetId = "849d095d07950b9e56d0c895ec48ec5100cfdff1";

        //    //FundingTx
        //    Console.WriteLine("----------FundingTx------------");
        //    JObject FundingTx = createFundingTx(PublicKeySelf, BalanceSelf, PublicKeyOther, BalanceOther, AssetId);
        //    Console.WriteLine(FundingTx);

        //    Console.WriteLine("----------FundingTx签名------------");
        //    byte[] prikeyByteSelf = PrikeySelf.HexToBytes();
        //    byte[] prikeyByteOther = PrikeyOther.HexToBytes();

        //    string txData = FundingTx["txData"].ToString();
        //    txData = FormatJObject(txData);
        //    string signSelf = NeoInterface.Sign(txData, prikeyByteSelf);
        //    string signOther = NeoInterface.Sign(txData, prikeyByteOther);
        //    Console.WriteLine(signSelf);
        //    Console.WriteLine(signOther);

        //    //CTX
        //    Console.WriteLine("----------CTX------------");
        //    string addressFunding = FundingTx["addressFunding"].ToString();
        //    addressFunding = FormatJObject(addressFunding);
        //    string fundingScript = FundingTx["scriptFunding"].ToString();
        //    fundingScript = FormatJObject(fundingScript);

        //    JObject CTX = createCTX(addressFunding, BalanceSelf, BalanceOther, PublicKeySelf, PublicKeyOther, fundingScript, AssetId);
        //    Console.WriteLine(CTX);

        //    Console.WriteLine("----------CTX签名---------");
        //    string txData1 = CTX["txData"].ToString();
        //    txData1 = FormatJObject(txData1);
        //    string signSelf1 = NeoInterface.Sign(txData1, prikeyByteSelf);
        //    string signOther1 = NeoInterface.Sign(txData1, prikeyByteOther);
        //    Console.WriteLine(signSelf1);
        //    Console.WriteLine(signOther1);

        //    //RDTX
        //    Console.WriteLine("----------RDTX------------");
        //    UInt160 ScriptHashSelf1 = PublicKeyToScriptHash(PublicKeySelf);
        //    string AddressSelf = ToAddress1(ScriptHashSelf1);
        //    string addressRSMC = CTX["addressRSMC"].ToString();
        //    addressRSMC = FormatJObject(addressRSMC);
        //    string CTxId = CTX["txId"].ToString();
        //    CTxId = FormatJObject(CTxId);
        //    string RSMCScript = CTX["scriptRSMC"].ToString();
        //    RSMCScript = FormatJObject(RSMCScript);

        //    JObject RDTX = createRDTX(addressRSMC, AddressSelf, BalanceSelf, CTxId, RSMCScript, AssetId);
        //    Console.WriteLine(RDTX);

        //    Console.WriteLine("----------RDTX签名------------");
        //    string txData2 = RDTX["txData"].ToString();
        //    txData2 = FormatJObject(txData2);
        //    string signSelf2 = NeoInterface.Sign(txData2, prikeyByteSelf);
        //    string signOther2 = NeoInterface.Sign(txData2, prikeyByteOther);
        //    Console.WriteLine(signSelf2);
        //    Console.WriteLine(signOther2);

        //    //CTX
        //    Console.WriteLine("----------Settle------------");
        //    JObject Settle = createSettle(addressFunding, BalanceSelf, BalanceOther, PublicKeySelf, PublicKeyOther, fundingScript, AssetId);
        //    Console.WriteLine(Settle);

        //    Console.WriteLine("----------Settle签名---------");
        //    string txData3 = Settle["txData"].ToString();
        //    txData3 = FormatJObject(txData3);
        //    string signSelf3 = NeoInterface.Sign(txData3, prikeyByteSelf);
        //    string signOther3 = NeoInterface.Sign(txData3, prikeyByteOther);
        //    Console.WriteLine(signSelf3);
        //    Console.WriteLine(signOther3);

        //    Console.ReadLine();
        //}
    }
}
