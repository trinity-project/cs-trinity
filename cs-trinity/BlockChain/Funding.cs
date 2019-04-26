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

namespace Trinity.BlockChain
{
    class Funding
    {
        static long testTime = 1554866712;

        ///<summary>
        ///scriptHash转Address,暂用，因无法获取到gui里的参数
        ///</summary>
        ///<paramname="<scriptHash>"><脚本哈希></param>
        ///<returns>
        ///地址Address
        ///</returns>
        public static string ToAddress1(UInt160 scriptHash)
        {
            byte[] data = new byte[21];
            data[0] = 23;           //此参数无法获取
            Buffer.BlockCopy(scriptHash.ToArray(), 0, data, 1, 20);
            return data.Base58CheckEncode();
        }

        ///<summary>
        ///Address转scriptHash,暂用，因无法获取到gui里的参数
        ///</summary>
        ///<paramname="<Address>"><地址></param>
        ///<returns>
        ///脚本哈希scriptHash
        ///</returns>
        public static UInt160 ToScriptHash1(string address)
        {
            byte[] data = address.Base58CheckDecode();
            if (data.Length != 21)
                throw new FormatException();
            return new UInt160(data.Skip(1).ToArray());
        }

        ///<summary>
        ///Script转ScriptHash
        ///</summary>
        ///<paramname="<Script>"><脚本></param>
        ///<returns>
        ///脚本哈希ScriptHash
        ///</returns>
        public static UInt160 ScriptToScriptHash(byte[] Script)
        {
            return new UInt160(Crypto.Default.Hash160(Script));
        }

        ///<summary>
        ///Script转Address，暂用
        ///</summary>
        ///<paramname="<Script>"><地址脚本></param>
        ///<returns>
        ///地址Address
        ///</returns>
        public static string ScriptToAddress(string Script)
        {
            //UInt160 ScriptHash = Script.ToScriptHash();
            UInt160 ScriptHash = ScriptToScriptHash(Script.HexToBytes());
            string Address = ToAddress1(ScriptHash);
            //string AddressOther = ToAddress1(ScriptHashOther);
            //string address = script_hash.ToAddress();       //无法获取gui的参数，暂不使用             
            //byte[] addresshash = Encoding.ASCII.GetBytes(address).Sha256().Sha256().Take(4).ToArray();            //地址转地址Hash
            return Address;
        }

        ///<summary>
        ///去除首尾的引号“”，暂用，因获取的NEO.JObject值中包含引号
        ///</summary>
        ///<paramname="<Value>"><需要处理的字符串></param>
        ///<returns>
        ///处理后的字符串
        ///</returns>
        public static string FormatJObject(string Value)
        {
            return Value.Substring(1, Value.Length - 2);
        }

        ///<summary>
        ///long转Bytes数组
        ///</summary>
        ///<paramname="<n>"><需要转换的long类型></param>
        ///<returns>
        ///转换后的Bytes数组
        ///</returns>
        public static byte[] longToBytes(long n)
        {
            byte[] b = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                b[i] = (byte)(n >> (24 - i * 8));
            }
            return b;
        }

        ///<summary>
        ///string转HexString
        ///</summary>
        ///<paramname="<s>"><需要转换的string类型></param>
        ///<returns>
        ///转换后的HexString
        ///</returns>
        public static string StringToHexString(string s)
        {
            byte[] b = System.Text.Encoding.UTF8.GetBytes(s);
            string result = string.Empty;
            for (int i = 0; i < b.Length; i++)
            {
                result += Convert.ToString(b[i], 16);
            }
            return result;
        }

        ///<summary>
        ///int转HexString
        ///</summary>
        ///<paramname="<i>"><需要转换的int类型></param>
        ///<returns>
        ///转换后的HexString
        ///</returns>
        public static string intToHex(int i)
        {
            return BigInteger.Parse(i.ToString()).ToByteArray().ToHexString();
        }

        //创建多签合约，封装自NEO方法
        //弃用
        //public static Contract CreateMultiSigContract1(string publicKey1, string publicKey2)
        //{
        //    ECPoint[] publicKeys;
        //    List<System.String> listS = new List<System.String>();
        //    if (publicKey1.CompareTo(publicKey2) > 0)
        //    {
        //        listS.Add(publicKey1);
        //        listS.Add(publicKey2);
        //    }
        //    else
        //    {
        //        listS.Add(publicKey2);
        //        listS.Add(publicKey1);
        //    }

        //    publicKeys = listS.Select(p => ECPoint.DecodePoint(p.HexToBytes(), ECCurve.Secp256r1)).ToArray();
        //    Contract contract = Contract.CreateMultiSigContract(2, publicKeys);
        //    return contract;
        //}

        ///<summary>
        ///创建多签合约，封装自NEO方法
        ///</summary>
        ///<paramname="<publicKey1>"><发起者公钥></param>
        ///<paramname="<publicKey2>"><对端公钥></param>
        ///<returns>
        ///多签合约Contract
        ///</returns>
        public static Contract CreateMultiSigContract(string publicKey1, string publicKey2)
        {
            return new Contract
            {
                Script = CreateMultiSigRedeemScript(publicKey1, publicKey2),
                ParameterList = Enumerable.Repeat(ContractParameterType.Signature, 2).ToArray()
            };
        }

        ///<summary>
        ///多签合约模板，取自Python-Trinity
        ///</summary>
        ///<paramname="<publicKey1>"><发起者公钥></param>
        ///<paramname="<publicKey2>"><对端公钥></param>
        ///<returns>
        ///多签合约Bytes数组
        ///</returns>
        public static byte[] CreateMultiSigRedeemScript(string publicKey1, string publicKey2)
        {
            string pubkey_large;
            string pubkey_small;
            if (publicKey1.CompareTo(publicKey2) > 0)
            {
                pubkey_large = publicKey1;
                pubkey_small = publicKey2;
            }
            else
            {
                pubkey_large = publicKey2;
                pubkey_small = publicKey1;
            }

            string contractTemplate = "53c56b6c766b00527ac46c766b51527ac4616c766b00c36121{0}ac642f006c766b51c361" +
                                      "21{1}ac635f006c766b00c36121{2}ac642f006c766b51c36121{3}" +
                                      "ac62040000620400516c766b52527ac46203006c766b52c3616c7566";
            string RSMCContract = String.Format(contractTemplate, pubkey_small, pubkey_large, pubkey_large, pubkey_small);
            return RSMCContract.HexToBytes();
        }


        ///<summary>
        ///PublicKey转ScriptHash
        ///</summary>
        ///<paramname="<PublicKey>"><公钥></param>
        ///<returns>
        ///脚本哈希ScriptHash
        ///</returns>
        public static UInt160 PublicKeyToScriptHash(string PublicKey)
        {
            Neo.Cryptography.ECC.ECPoint ECPointPublicKey = Neo.Cryptography.ECC.ECPoint.DecodePoint(PublicKey.HexToBytes(), Neo.Cryptography.ECC.ECCurve.Secp256r1);
            UInt160 ScriptHash = Contract.CreateSignatureRedeemScript(ECPointPublicKey).ToScriptHash();
            return ScriptHash;
        }

        ///<summary>
        ///构造OpCode
        ///</summary>
        ///<paramname="<AddressFrom>"><付款方地址></param>
        ///<paramname="<AddressTo>"><收款方地址></param>
        ///<paramname="<Value>"><金额></param>
        ///<paramname="<AssetID>"><资产ID></param>
        ///<returns>
        ///OpCode
        ///</returns>
        public static string createOpdata(string AddressFrom, string AddressTo, string Value, string AssetID)
        {
            string OpCode = "";
            string Value1 = BigInteger.Parse(Value).ToByteArray().ToHexString();
            Console.WriteLine("Value1:");
            Console.WriteLine(Value1);
            string ScriptHashFrom = ToScriptHash1(AddressFrom).ToArray().ToHexString();
            Console.WriteLine("ScriptHashFrom:");
            Console.WriteLine(ScriptHashFrom);
            string ScriptHashTo = ToScriptHash1(AddressTo).ToArray().ToHexString();
            Console.WriteLine("ScriptHashTo:");
            Console.WriteLine(ScriptHashTo);
            string method = StringToHexString("transfer");            //7472616e73666572
            string[] invoke_args = { Value1, ScriptHashTo, ScriptHashFrom };
            foreach (var item in invoke_args)
            {
                string[] args = { intToHex(item.Length / 2), item };
                OpCode += string.Join("", args);
            }
            Console.WriteLine("OpCode:");
            Console.WriteLine(OpCode);
            OpCode += "53";  // PUSH3
            OpCode += "c1";  // PACK
            OpCode += intToHex(method.Length / 2);
            OpCode += method;
            OpCode += "67";  // APPCALL
            Console.WriteLine("AssetID:");
            Console.WriteLine(AssetID.HexToBytes().Reverse().ToArray().ToHexString());
            OpCode += AssetID.HexToBytes().Reverse().ToArray().ToHexString();
            OpCode += "f1";  // THROWIFNOT

            return OpCode;
        }


        ///<summary>
        ///构造Funding交易
        ///</summary>
        ///<paramname="<PublicKeySelf>"><发起方公钥></param>
        ///<paramname="<DepositSelf>"><发起方押金></param>
        ///<paramname="<PublicKeyOther>"><对端公钥></param>
        ///<paramname="<DepositOther>"><对端押金></param>
        ///<paramname="<AssetId>"><资产ID></param>
        ///<returns>
        ///Funding交易数据
        ///</returns>
        public static JObject createFundingTx(string PublicKeySelf, string DepositSelf, string PublicKeyOther, string DepositOther, string AssetId)
        {
            Contract contract = CreateMultiSigContract(PublicKeySelf, PublicKeyOther);
            string contractAddress = ToAddress1(contract.ScriptHash);
            Console.WriteLine("contract：");
            Console.WriteLine(contract.GetHashCode());
            Console.WriteLine(contract.Script.ToHexString());
            Console.WriteLine("地址hash：");
            Console.WriteLine(contract.ScriptHash);
            Console.WriteLine("合约地址：");
            Console.WriteLine(ToAddress1(contract.ScriptHash));


            // 双方Scripthash
            UInt160 ScriptHashSelf = PublicKeyToScriptHash(PublicKeySelf);
            UInt160 ScriptHashOther = PublicKeyToScriptHash(PublicKeyOther);
            // 双方地址
            string AddressSelf = ToAddress1(ScriptHashSelf);
            string AddressOther = ToAddress1(ScriptHashOther);
            //string address = script_hash.ToAddress();       //无法获取gui的参数，暂不使用             

            string op_dataSelf = createOpdata(AddressSelf, contractAddress, DepositSelf, AssetId);
            Console.WriteLine("op_dataSelf:");
            Console.WriteLine(op_dataSelf);
            string op_dataOther = createOpdata(AddressOther, contractAddress, DepositOther, AssetId);
            Console.WriteLine("op_dataOther:");
            Console.WriteLine(op_dataOther);
 
            TimeSpan cha = DateTime.Now - TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));                         //时间戳
            long t = (long)cha.TotalSeconds;
            #if DEBUG
                t = testTime;
            #endif

            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            new TransactionAttributeUInt160(TransactionAttributeUsage.Script, ScriptHashSelf, attributes).MakeAttribute(out attributes);
            new TransactionAttributeUInt160(TransactionAttributeUsage.Script, ScriptHashOther, attributes).MakeAttribute(out attributes);
            new TransactionAttributeLong(TransactionAttributeUsage.Remark, t, attributes).MakeAttribute(out attributes);

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
        ///构造RSMC合约
        ///</summary>
        ///<paramname="<HashSelf>"><发起方哈希></param>
        ///<paramname="<PubkeySelf>"><发起方公钥></param>
        ///<paramname="<HashOther>"><对端哈希></param>
        ///<paramname="<PubkeyOther>"><对端公钥></param>
        ///<paramname="<Timestamp>"><时间戳></param>
        ///<returns>
        ///RSMCContract RSMC合约数据
        ///</returns>
        public static JObject createRSMCContract(UInt160 HashSelf, string PubkeySelf, UInt160 HashOther, string PubkeyOther, string Timestamp)
        {
            //string Timestamp = t.ToString();
            Console.WriteLine("Timestamp:");
            Timestamp = StringToHexString(Timestamp);
            Console.WriteLine(Timestamp);
            byte[] TimestampByte = Encoding.UTF8.GetBytes(Timestamp);
            Console.WriteLine(TimestampByte.Length);
            string length = intToHex(TimestampByte.Length / 2).PadLeft(2, '0');
            Console.WriteLine(length);
            string magicTimestamp = length + Timestamp;
            Console.WriteLine(magicTimestamp);

            string contractTemplate = "5dc56b6c766b00527ac46c766b51527ac46c766b52527ac461{0}6c766b53527ac4616829537" +
                                      "97374656d2e457865637574696f6e456e67696e652e476574536372697074436f6e7461696e65726c766b5452" +
                                      "7ac46c766b54c361681d4e656f2e5472616e73616374696f6e2e476574417474726962757465736c766b55527" +
                                      "ac4616c766b55c36c766b56527ac4006c766b57527ac462b4016c766b56c36c766b57c3c36c766b58527ac461" +
                                      "6c766b58c36168154e656f2e4174747269627574652e476574446174616114{1}9c6c766b59527ac4" +
                                      "6c766b59c364c300616c766b00c36121{2}ac642f006c766b51c36121{3}ac635f006c" +
                                      "766b00c36121{4}ac642f006c766b51c36121{5}ac62040000620400516c766b5a527a" +
                                      "c462b8006c766b58c36168154e656f2e4174747269627574652e476574446174616114{6}9c6c766b5" +
                                      "b527ac46c766b5bc3644c00616c766b52c36c766b54c3617c6599016c766b5c527ac46c766b5cc36422006c76" +
                                      "6b52c36c766b00c36c766b51c3615272654a006c766b5a527ac4623700006c766b5a527ac4622c00616c766b5" +
                                      "7c351936c766b57527ac46c766b57c36c766b56c3c09f6343fe006c766b5a527ac46203006c766b5ac3616c75" +
                                      "6656c56b6c766b00527ac46c766b51527ac46c766b52527ac4616168184e656f2e426c6f636b636861696e2e4" +
                                      "765744865696768746c766b53527ac46c766b00c3011e936c766b53c39f6c766b54527ac46c766b54c364c200" +
                                      "6c766b51c36121{7}ac642f006c766b52c36121{8}ac635f006c766b51c36121" +
                                      "{9}ac642f006c766b52c36121{10}ac62040000620400516c766b55527ac4620e00006c" +
                                      "766b55527ac46203006c766b55c3616c75665ec56b6c766b00527ac46c766b51527ac4616c766b00c36168174" +
                                      "e656f2e426c6f636b636861696e2e476574426c6f636b6c766b52527ac46c766b52c36168194e656f2e426c6f" +
                                      "636b2e4765745472616e73616374696f6e736c766b53527ac46c766b51c361681d4e656f2e5472616e7361637" +
                                      "4696f6e2e476574417474726962757465736c766b54527ac4616c766b54c36c766b55527ac4006c766b56527a" +
                                      "c462d1006c766b55c36c766b56c3c36c766b57527ac4616c766b57c36168154e656f2e4174747269627574652" +
                                      "e476574446174616c766b58527ac4616c766b53c36c766b59527ac4006c766b5a527ac46264006c766b59c36c" +
                                      "766b5ac3c36c766b5b527ac4616c766b5bc36168174e656f2e5472616e73616374696f6e2e476574486173686" +
                                      "c766b58c39c6c766b5c527ac46c766b5cc3640e00516c766b5d527ac4624a00616c766b5ac351936c766b5a52" +
                                      "7ac46c766b5ac36c766b59c3c09f6393ff616c766b56c351936c766b56527ac46c766b56c36c766b55c3c09f6" +
                                      "326ff006c766b5d527ac46203006c766b5dc3616c7566";
            string ScriptHashSelf = HashSelf.ToString().Substring(2).HexToBytes().Reverse().ToArray().ToHexString();
            string ScriptHashOther = HashOther.ToString().Substring(2).HexToBytes().Reverse().ToArray().ToHexString();
            string RSMCContract = String.Format(contractTemplate, magicTimestamp, ScriptHashOther, PubkeySelf, PubkeyOther, PubkeyOther, PubkeySelf, ScriptHashSelf, PubkeySelf, PubkeyOther, PubkeyOther, PubkeySelf);

            JObject result = new JObject();
            result["script"] = RSMCContract;
            result["address"] = ScriptToAddress(RSMCContract);
            return result;
        }

        ///<summary>
        ///构造CTX
        ///</summary>
        ///<paramname="<AddressFunding>"><Funding产生的多签合约地址></param>
        ///<paramname="<BalanceSelf>"><发起方余额></param>
        ///<paramname="<BalanceOther>"><对端余额></param>
        ///<paramname="<PublicKeySelf>"><发起方公钥></param>
        ///<paramname="<PublicKeyOther>"><对端公钥></param>
        ///<paramname="<FundingScript>"><Funding产生的多签合约脚本></param>
        ///<paramname="<AssetId>"><资产ID></param>
        ///<returns>
        ///C交易数据
        ///</returns>
        public static JObject createCTX(string AddressFunding, string BalanceSelf, string BalanceOther, string PublicKeySelf, string PublicKeyOther, string FundingScript, string AssetId)
        {
            // 双方Scripthash
            UInt160 ScriptHashSelf = PublicKeyToScriptHash(PublicKeySelf);
            UInt160 ScriptHashOther = PublicKeyToScriptHash(PublicKeyOther);
            string AddressOther = ToAddress1(ScriptHashOther);

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
            JObject RSMCContract = createRSMCContract(ScriptHashSelf, PublicKeySelf, ScriptHashOther, PublicKeyOther, t1);
            Console.WriteLine(RSMCContract);
            string RSMCContractAddress = RSMCContract["address"].ToString();
            RSMCContractAddress = FormatJObject(RSMCContractAddress);                                                   //暂用

            string op_data_to_RSMC = createOpdata(AddressFunding, RSMCContractAddress, BalanceSelf, AssetId);
            //string AddressSelf = ToAddress1(ScriptHashSelf);
            //string op_data_to_RSMC = createOpdata(addressFunding, AddressSelf, balanceSelf, AssetId);
            Console.WriteLine("op_data_to_RSMC:");
            Console.WriteLine(op_data_to_RSMC);
            string op_data_to_other = createOpdata(AddressFunding, AddressOther, BalanceOther, AssetId);
            Console.WriteLine("op_data_to_other:");
            Console.WriteLine(op_data_to_other);

            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            UInt160 address_hash_funding = ToScriptHash1(AddressFunding);
            new TransactionAttributeUInt160(TransactionAttributeUsage.Script, address_hash_funding, attributes).MakeAttribute(out attributes);
            new TransactionAttributeLong(TransactionAttributeUsage.Remark, t, attributes).MakeAttribute(out attributes);

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
        ///构造验证脚本
        ///</summary>
        ///<paramname="<Script>"><脚本></param>
        ///<returns>
        ///验证脚本
        public static string createVerifyScript(string Script)
        {
            string tmp = intToHex(Script.Length / 2);
            if (tmp.Length % 2 == 1)
            {
                tmp = "0" + tmp;
            }
            string[] array = { tmp, Script };
            string verify_script = string.Join("", array);

            return verify_script;
        }

        ///<summary>
        ///构造RDTX
        ///</summary>
        ///<paramname="<AddressRSMC>"><CTX产生的RSMC合约地址></param>
        ///<paramname="<AddressSelf>"><发起方地址></param>
        ///<paramname="<BalanceSelf>"><发起方余额></param>
        ///<paramname="<CTxId>"><C交易的TXId></param>
        ///<paramname="<AssetId>"><资产ID></param>
        ///<returns>
        ///C交易数据
        ///</returns>
        public static JObject createRDTX(string AddressRSMC, string AddressSelf, string BalanceSelf, string CTxId, string RSMCScript, string AssetId)
        {
            string op_data_to_self = createOpdata(AddressRSMC, AddressSelf, BalanceSelf, AssetId);
            Console.WriteLine("op_data_to_self:");
            Console.WriteLine(op_data_to_self);

            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            UInt160 address_hash_RSMC = ToScriptHash1(AddressRSMC);
            TimeSpan cha = DateTime.Now - TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            long t = (long)cha.TotalSeconds;                                    //时间戳
            #if DEBUG
                t = testTime;
            #endif
            string pre_txid = CTxId.Substring(2).HexToBytes().Reverse().ToArray().ToHexString();                    //pre_txid
            UInt160 ScriptHashSelf = ToScriptHash1(AddressSelf);                                                    //outputTo

            new TransactionAttributeUInt160(TransactionAttributeUsage.Script, address_hash_RSMC, attributes).MakeAttribute(out attributes);
            new TransactionAttributeLong(TransactionAttributeUsage.Remark, t, attributes).MakeAttribute(out attributes);
            new TransactionAttributeString(TransactionAttributeUsage.Remark1, pre_txid, attributes).MakeAttribute(out attributes);
            new TransactionAttributeUInt160(TransactionAttributeUsage.Remark2, ScriptHashSelf, attributes).MakeAttribute(out attributes);

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
            result["witness"] = "01{blockheight_script}40{signOther}40{signSelf}fd" + createVerifyScript(RSMCScript);

            return result;
        }

        ///<summary>
        ///构造Settle
        ///</summary>
        ///<paramname="<AddressFunding>"><Funding产生的多签合约地址></param>
        ///<paramname="<BalanceSelf>"><发起方余额></param>
        ///<paramname="<BalanceOther>"><对端余额></param>
        ///<paramname="<PublicKeySelf>"><发起方公钥></param>
        ///<paramname="<PublicKeyOther>"><对端公钥></param>
        ///<paramname="<FundingScript>"><Funding产生的多签合约脚本></param>
        ///<paramname="<AssetId>"><资产ID></param>
        ///<returns>
        ///Settle数据
        ///</returns>
        public static JObject createSettle(string AddressFunding, string BalanceSelf, string BalanceOther, string PublicKeySelf, string PublicKeyOther, string FundingScript, string AssetId)
        {
            // 双方Scripthash
            UInt160 ScriptHashSelf = PublicKeyToScriptHash(PublicKeySelf);
            UInt160 ScriptHashOther = PublicKeyToScriptHash(PublicKeyOther);
            string AddressOther = ToAddress1(ScriptHashOther);

            TimeSpan cha = DateTime.Now - TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));                       //时间戳
            long t = (long)cha.TotalSeconds;
            #if DEBUG
                t = testTime;
            #endif

            string AddressSelf = ToAddress1(ScriptHashSelf);
            string op_data_to_self = createOpdata(AddressFunding, AddressSelf, BalanceSelf, AssetId);
            Console.WriteLine("op_data_to_self:");
            Console.WriteLine(op_data_to_self);
            string op_data_to_other = createOpdata(AddressFunding, AddressOther, BalanceOther, AssetId);
            Console.WriteLine("op_data_to_other:");
            Console.WriteLine(op_data_to_other);

            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            UInt160 address_hash_funding = ToScriptHash1(AddressFunding);
            new TransactionAttributeUInt160(TransactionAttributeUsage.Script, address_hash_funding, attributes).MakeAttribute(out attributes);
            new TransactionAttributeLong(TransactionAttributeUsage.Remark, t, attributes).MakeAttribute(out attributes);
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

        ///<summary>
        ///TransactionAttribute交易属性类
        ///</summary>
        public abstract class TransactionAttribute<T>
        {
            private T Data;
            public TransactionAttributeUsage Usage;
            public List<TransactionAttribute> Attributes;

            public abstract byte[] ConvertToArray(T attr);

            public TransactionAttribute(TransactionAttributeUsage Usage, T Data, List<TransactionAttribute> attributes)     //构造函数
            {
                this.Usage = Usage;
                this.Data = Data;
                this.Attributes = attributes;
            }

            public void MakeAttribute(out List<TransactionAttribute> attributes)
            {
                List<T> list = new List<T>();
                list.Add(Data);
                T[] txAttributes;
                txAttributes = list.Select(p => p).ToArray();
                HashSet<T> sAttributes = new HashSet<T>(txAttributes);
                Attributes.AddRange(sAttributes.Select(p => new TransactionAttribute
                {
                    Usage = Usage,
                    Data = ConvertToArray(p)
                }));
                attributes = Attributes;
            }

        }

        public class TransactionAttributeUInt160 : TransactionAttribute<UInt160>
        {
            public TransactionAttributeUInt160(TransactionAttributeUsage Usage, UInt160 Data, List<TransactionAttribute> attributes) : base(Usage, Data, attributes)
            {
            }

            public override byte[] ConvertToArray(UInt160 attr)
            {
                return attr.ToArray();
            }
        }
        public class TransactionAttributeString : TransactionAttribute<string>
        {
            public TransactionAttributeString(TransactionAttributeUsage Usage, string Data, List<TransactionAttribute> attributes) : base(Usage, Data, attributes)
            {
            }

            public override byte[] ConvertToArray(string attr)
            {
                return attr.HexToBytes();
            }
        }
        public class TransactionAttributeLong : TransactionAttribute<long>
        {
            public TransactionAttributeLong(TransactionAttributeUsage Usage, long Data, List<TransactionAttribute> attributes) : base(Usage, Data, attributes)
            {
            }

            public override byte[] ConvertToArray(long attr)
            {
                return longToBytes(attr);
            }
        }

        public static void KeyPair(byte[] privateKey, out ECPoint PublicKey)
        {
            if (privateKey.Length != 32 && privateKey.Length != 96 && privateKey.Length != 104)
                throw new ArgumentException();
            byte[] PrivateKey = new byte[32];
            Buffer.BlockCopy(privateKey, privateKey.Length - 32, PrivateKey, 0, 32);
            if (privateKey.Length == 32)
            {
                PublicKey = Neo.Cryptography.ECC.ECCurve.Secp256r1.G * privateKey;
            }
            else
            {
                PublicKey = Neo.Cryptography.ECC.ECPoint.FromBytes(privateKey, Neo.Cryptography.ECC.ECCurve.Secp256r1);
            }
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
