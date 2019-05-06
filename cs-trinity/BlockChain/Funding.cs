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
#define DEBUG
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

using Trinity.Wallets.Templates.Definitions;

namespace Trinity.BlockChain
{
    public sealed class FundingOrigin
    {
        static long testTime = 1554866712;

        ///<summary>
        /// Constructor the body of funding trade
        ///</summary>
        ///<param name="PublicKeySelf">Founder's public key</param>
        ///<param name="DepositSelf">Founder's deposit</param>
        ///<param name="PublicKeyOther">Partner's public key</param>
        ///<param name="DepositOther">Partner's deposit</param>
        ///<param name="AssetId">Uniform Asset ID</param>
        ///<returns>
        /// Funding Trade Body
        ///</returns>
        
        public static JObject createFundingTx(string PublicKeySelf, string DepositSelf, string PublicKeyOther, string DepositOther, string AssetId)
        {
            Contract contract = NeoInterface.CreateMultiSigContract(PublicKeySelf, PublicKeyOther);
            string contractAddress = contract.Address;
            UInt160 ScriptHashSelf = PublicKeySelf.ToHash160();
            UInt160 ScriptHashOther = NeoInterface.PublicKeyToScriptHash(PublicKeyOther);
            string AddressSelf = NeoInterface.ToAddress1(ScriptHashSelf);
            string AddressOther = NeoInterface.ToAddress1(ScriptHashOther);
            //string address = script_hash.ToAddress();       //无法获取gui的参数，暂不使用             

            string op_dataSelf = NeoInterface.CreateOpdata(AddressSelf, contractAddress, DepositSelf, AssetId);
            Console.WriteLine("op_dataSelf:");
            Console.WriteLine(op_dataSelf);
            string op_dataOther = NeoInterface.CreateOpdata(AddressOther, contractAddress, DepositOther, AssetId);
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
            new NeoInterface.TransactionAttributeDouble(TransactionAttributeUsage.Remark, t, attributes).MakeAttribute(out attributes);

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
            t1 = t.ToString() + ".123456";
#endif
            Console.WriteLine(t);
            Console.WriteLine(t1);
            JObject RSMCContract = NeoInterface.CreateRSMCContract(ScriptHashSelf, PublicKeySelf, ScriptHashOther, PublicKeyOther, t1);
            Console.WriteLine(RSMCContract);
            string RSMCContractAddress = RSMCContract["address"].ToString();
            RSMCContractAddress = NeoInterface.FormatJObject(RSMCContractAddress);                                                   //暂用

            string op_data_to_RSMC = NeoInterface.CreateOpdata(AddressFunding, RSMCContractAddress, BalanceSelf, AssetId);
            //string AddressSelf = ToAddress1(ScriptHashSelf);
            //string op_data_to_RSMC = CreateOpdata(addressFunding, AddressSelf, balanceSelf, AssetId);
            Console.WriteLine("op_data_to_RSMC:");
            Console.WriteLine(op_data_to_RSMC);
            string op_data_to_other = NeoInterface.CreateOpdata(AddressFunding, AddressOther, BalanceOther, AssetId);
            Console.WriteLine("op_data_to_other:");
            Console.WriteLine(op_data_to_other);

            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            UInt160 address_hash_funding = NeoInterface.ToScriptHash1(AddressFunding);
            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Script, address_hash_funding, attributes).MakeAttribute(out attributes);
            new NeoInterface.TransactionAttributeDouble(TransactionAttributeUsage.Remark, t, attributes).MakeAttribute(out attributes);

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
            string op_data_to_self = NeoInterface.CreateOpdata(AddressRSMC, AddressSelf, BalanceSelf, AssetId);
            Console.WriteLine("op_data_to_self:");
            Console.WriteLine(op_data_to_self);

            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            UInt160 address_hash_RSMC = NeoInterface.ToScriptHash1(AddressRSMC);
            TimeSpan cha = DateTime.Now - TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            long t = (long)cha.TotalSeconds;                                    //时间戳
#if DEBUG
            t = testTime;
#endif
            string pre_txid = CTxId.NeoStrip().HexToBytes().Reverse().ToArray().ToHexString();                    //pre_txid
            UInt160 ScriptHashSelf = NeoInterface.ToScriptHash1(AddressSelf);                                                    //outputTo

            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Script, address_hash_RSMC, attributes).MakeAttribute(out attributes);
            new NeoInterface.TransactionAttributeDouble(TransactionAttributeUsage.Remark, t, attributes).MakeAttribute(out attributes);
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
            result["witness"] = "01{blockheight_script}40{signOther}40{signSelf}fd" + NeoInterface.CreateVerifyScript(RSMCScript);

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
            string op_data_to_self = NeoInterface.CreateOpdata(AddressRSMC, AddressOther, BalanceSelf, AssetId);
            Console.WriteLine("op_data_to_self:");
            Console.WriteLine(op_data_to_self);

            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            UInt160 address_hash_RSMC = NeoInterface.ToScriptHash1(AddressRSMC);
            TimeSpan cha = DateTime.Now - TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            long t = (long)cha.TotalSeconds;                                    //时间戳
#if DEBUG
            t = testTime;
#endif
            string pre_txid = CTxId.NeoStrip().HexToBytes().Reverse().ToArray().ToHexString();            //pre_txid
            UInt160 ScriptHashSelf = NeoInterface.ToScriptHash1(AddressOther);                              //outputTo

            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Script, address_hash_RSMC, attributes).MakeAttribute(out attributes);
            new NeoInterface.TransactionAttributeDouble(TransactionAttributeUsage.Remark, t, attributes).MakeAttribute(out attributes);
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
            result["witness"] = "01{blockheight_script}40{signOther}40{signSelf}fd" + NeoInterface.CreateVerifyScript(RSMCScript);

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
            string op_data_to_self = NeoInterface.CreateOpdata(AddressFunding, AddressSelf, BalanceSelf, AssetId);
            Console.WriteLine("op_data_to_self:");
            Console.WriteLine(op_data_to_self);
            string op_data_to_other = NeoInterface.CreateOpdata(AddressFunding, AddressOther, BalanceOther, AssetId);
            Console.WriteLine("op_data_to_other:");
            Console.WriteLine(op_data_to_other);

            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            UInt160 address_hash_funding = NeoInterface.ToScriptHash1(AddressFunding);
            new NeoInterface.TransactionAttributeUInt160(TransactionAttributeUsage.Script, address_hash_funding, attributes).MakeAttribute(out attributes);
            new NeoInterface.TransactionAttributeDouble(TransactionAttributeUsage.Remark, t, attributes).MakeAttribute(out attributes);
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
        
    }
}
