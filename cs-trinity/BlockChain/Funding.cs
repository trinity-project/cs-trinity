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
        //scriptHash转Address
        //暂用，因无法获取到gui里的参数
        public static string ToAddress1(UInt160 scriptHash)
        {
            byte[] data = new byte[21];
            data[0] = 23;           //此参数无法获取
            Buffer.BlockCopy(scriptHash.ToArray(), 0, data, 1, 20);
            return data.Base58CheckEncode();
        }

        //scriptHash转Address
        //暂用，因无法获取到gui里的参数
        public static UInt160 ToScriptHash1(string address)
        {
            byte[] data = address.Base58CheckDecode();
            if (data.Length != 21)
                throw new FormatException();
            return new UInt160(data.Skip(1).ToArray());
        }

        //去除首尾的引号
        //暂用，因获取的JObject值中包含引号
        public static string FormatJObject(string Value)
        {
            return Value.Substring(1, Value.Length - 2);
        }

        //Script转ScriptHash
        public static UInt160 ScriptToScriptHash(byte[] script)
        {
            return new UInt160(Crypto.Default.Hash160(script));
        }

        //long转Bytes数组方法
        public static byte[] longToBytes(long n)
        {
            byte[] b = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                b[i] = (byte)(n >> (24 - i * 8));
            }
            return b;
        }

        //string转HexString
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

        //int转HexString
        public static string intToHex(int value)
        {
            return BigInteger.Parse(value.ToString()).ToByteArray().ToHexString();
        }

        //创建多签合约，封装自NEO方法
        public static Contract CreateMultiSigContract(string publicKey1, string publicKey2)
        {
            ECPoint[] publicKeys;
            List<System.String> listS = new List<System.String>();
            if (publicKey1.CompareTo(publicKey2) > 0)
            {
                listS.Add(publicKey1);
                listS.Add(publicKey2);
            }
            else
            {
                listS.Add(publicKey2);
                listS.Add(publicKey1);
            }

            publicKeys = listS.Select(p => ECPoint.DecodePoint(p.HexToBytes(), ECCurve.Secp256r1)).ToArray();
            Contract contract = Contract.CreateMultiSigContract(2, publicKeys);
            return contract;
        }

        //Script转Address
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

        //PublicKey转ScriptHash
        public static UInt160 PublicKeyToScriptHash(string PublicKey)
        {
            ECPoint ECPointPublicKey = ECPoint.DecodePoint(PublicKey.HexToBytes(), ECCurve.Secp256r1);
            UInt160 ScriptHash = Contract.CreateSignatureRedeemScript(ECPointPublicKey).ToScriptHash();
            //Console.WriteLine("ScriptHash：");
            //Console.WriteLine(ScriptHash);
            return ScriptHash;
        }

        //创建opdata方法
        public static string createOpdata(string address_from, string address_to, string value, string contract_hash)
        {
            string op_data = "";
            string value1 = BigInteger.Parse(value).ToByteArray().ToHexString();
            Console.WriteLine("value1:");
            Console.WriteLine(value1);
            string scripthash_from = ToScriptHash1(address_from).ToArray().ToHexString();
            Console.WriteLine("scripthash_from:");
            Console.WriteLine(scripthash_from);
            string scripthash_to = ToScriptHash1(address_to).ToArray().ToHexString();
            Console.WriteLine("scripthash_to:");
            Console.WriteLine(scripthash_to);
            string method = StringToHexString("transfer");            //7472616e73666572
            Console.WriteLine("method:");
            Console.WriteLine(method);
            string[] invoke_args = { value1, scripthash_to, scripthash_from };
            foreach (var item in invoke_args)
            {
                string[] args = { intToHex(item.Length / 2), item };
                op_data += string.Join("", args);
            }
            Console.WriteLine("op_data:");
            Console.WriteLine(op_data);
            op_data += "53";  // PUSH3
            op_data += "c1";  // PACK
            op_data += intToHex(method.Length / 2);
            op_data += method;
            op_data += "67";  // APPCALL
            Console.WriteLine("asset_id:");
            Console.WriteLine(contract_hash.Substring(2).HexToBytes().Reverse().ToArray().ToHexString());
            op_data += contract_hash.Substring(2).HexToBytes().Reverse().ToArray().ToHexString();                   //好像多去了2位
            op_data += "f1";  // maybe THROWIFNOT

            return op_data;
        }


        //构造Funding交易
        public static JObject createFundingTx(string PublicKeySelf, string BalanceSelf, string PublicKeyOther, string BalanceOther, string AssetId)
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

            string op_dataSelf = createOpdata(AddressSelf, contractAddress, BalanceSelf, AssetId);
            Console.WriteLine("op_dataSelf:");
            Console.WriteLine(op_dataSelf);
            string op_dataOther = createOpdata(AddressOther, contractAddress, BalanceOther, AssetId);
            Console.WriteLine("op_dataOther:");
            Console.WriteLine(op_dataOther);
            //时间戳
            TimeSpan cha = DateTime.Now - TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            long t = (long)cha.TotalSeconds;
#if DEBUG
            t = 1554866704;
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
            Console.WriteLine("----------");

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
            //Console.WriteLine(result);

            return result;
        }


        //构造RSMC合约
        public static JObject createRSMCContract(UInt160 hashSelf, string pubkeySelf, UInt160 hashOther, string pubkeyOther, long t)
        {
            string Timestamp = t.ToString();
#if DEBUG
            Timestamp = 1554866704.ToString();
#endif
            byte[] TimestampByte = StringToHexString(Timestamp).HexToBytes();
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
            //Console.WriteLine("e66f32b647a7a8e0626d1b9330b107894dd4302c");
            string ScriptHashSelf = hashSelf.ToString().Substring(2).HexToBytes().Reverse().ToArray().ToHexString();
            //Console.WriteLine(ScriptHashSelf);
            //Console.WriteLine("e8388a138b365fa4e48d763016e6d4002d9b7e8e");
            string ScriptHashOther = hashOther.ToString().Substring(2).HexToBytes().Reverse().ToArray().ToHexString();
            //Console.WriteLine(ScriptHashOther);
            string RSMCContract = String.Format(contractTemplate, magicTimestamp, ScriptHashOther, pubkeySelf, pubkeyOther, pubkeyOther, pubkeySelf, ScriptHashSelf, pubkeySelf, pubkeyOther, pubkeyOther, pubkeySelf);
            //Console.WriteLine(RSMCContract);

            JObject result = new JObject();
            result["script"] = RSMCContract;
            result["address"] = ScriptToAddress(RSMCContract);
            return result;
        }

        //构造CTX
        public static JObject createCTX(string addressFunding, string balanceSelf, string balanceOther, string PublicKeySelf, string PublicKeyOther, string fundingScript, string AssetId)
        {
            // 双方Scripthash
            UInt160 ScriptHashSelf = PublicKeyToScriptHash(PublicKeySelf);
            UInt160 ScriptHashOther = PublicKeyToScriptHash(PublicKeyOther);
            string AddressOther = ToAddress1(ScriptHashOther);

            //时间戳
            TimeSpan cha = DateTime.Now - TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            long t = (long)cha.TotalSeconds;
#if DEBUG
            t = 1554866704;
#endif
            JObject RSMCContract = createRSMCContract(ScriptHashSelf, PublicKeySelf, ScriptHashOther, PublicKeyOther, t);
            Console.WriteLine(RSMCContract);
            string RSMCContractAddress = RSMCContract["address"].ToString();
            RSMCContractAddress = FormatJObject(RSMCContractAddress);                                                   //暂用

            string op_data_to_RSMC = createOpdata(addressFunding, RSMCContractAddress, balanceSelf, AssetId);
            Console.WriteLine("op_data_to_RSMC:");
            Console.WriteLine(op_data_to_RSMC);
            string op_data_to_other = createOpdata(addressFunding, AddressOther, balanceOther, AssetId);
            Console.WriteLine("op_data_to_other:");
            Console.WriteLine(op_data_to_other);

            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            UInt160 address_hash_funding = ToScriptHash1(addressFunding);
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
            Console.WriteLine("----------");

            JObject result = new JObject();
            result["txData"] = tx.GetHashData().ToHexString();
            result["addressRSMC"] = RSMCContract["address"];
            result["scriptRSMC"] = RSMCContract["script"];
            result["txId"] = tx.Hash.ToString();
            result["witness"] = "018240{signSelf}40{signOther}da" + fundingScript;
            //Console.WriteLine(result);

            return result;
        }

        //构造验证脚本
        public static string createVerifyScript(string script)
        {
            string tmp = intToHex(script.Length / 2);
            if (tmp.Length % 2 == 1)
            {
                tmp = "0" + tmp;
            }
            //string hex_len = tmp.HexToBytes().Reverse().ToArray().ToHexString();
            //Console.WriteLine("hex_len");
            //Console.WriteLine(hex_len);
            string[] array = { tmp, script };
            string verify_script = string.Join("", array);

            return verify_script;
        }

        //创建RDTX
        public static JObject createRDTX(string addressRSMC, string AddressSelf, string BalanceSelf, string CTxId, string RSMCScript, string AssetId)
        {
            string op_data_to_self = createOpdata(addressRSMC, AddressSelf, BalanceSelf, AssetId);
            Console.WriteLine("op_data_to_self:");
            Console.WriteLine(op_data_to_self);

            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            UInt160 address_hash_RSMC = ToScriptHash1(addressRSMC);
            TimeSpan cha = DateTime.Now - TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            long t = (long)cha.TotalSeconds;                                    //时间戳
            #if DEBUG
                t = 1554866704;
            #endif
            string pre_txid = CTxId.Substring(2).HexToBytes().Reverse().ToArray().ToHexString();        //pre_txid
            UInt160 ScriptHashSelf = ToScriptHash1(AddressSelf);                                        //outputTo

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
            Console.WriteLine("----------");

            JObject result = new JObject();
            result["txData"] = tx.GetHashData().ToHexString();
            result["txId"] = tx.Hash.ToString();
            result["witness"] = "01{blockheight_script}40{signOther}40{signSelf}fd" + createVerifyScript(RSMCScript);
            //Console.WriteLine(result);

            return result;
        }


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


        //static void Main()
        //{
        //    string PublicKeySelf = "02ea3b68aa765c9af9dfa89eeb39dde03d1816493d9e11bb827940ee47ce2536cc";          //Acwk9RhThDhn6x47GpKmUV9SS8qmGHSimC
        //    string PublicKeyOther = "02d62f3a5e56ae9e20e0803d735465e88019ef9e7545a14c611ba72bb6fdab5d52";         //AcnJGoFrRe5QNKjSadk7yZpw8wQuTWmiRE
        //    string BalanceSelf = "10";
        //    string BalanceOther = "10";
        //    string AssetId = "5e7fb71d90044445caf77c0c36df0901fda8340c";

        //    //FundingTx
        //    Console.WriteLine("----------FundingTx------------");
        //    JObject FundingTx = createFundingTx(PublicKeySelf, BalanceSelf, PublicKeyOther, BalanceOther, AssetId);
        //    Console.WriteLine(FundingTx);

        //    //CTX
        //    Console.WriteLine("----------CTX------------");
        //    string addressFunding = FundingTx["addressFunding"].ToString();
        //    addressFunding = FormatJObject(addressFunding);                                                                   //暂用
        //    string fundingScript = FundingTx["scriptFunding"].ToString();
        //    fundingScript = FormatJObject(fundingScript);                                                                     //暂用

        //    JObject CTX = createCTX(addressFunding, BalanceSelf, BalanceOther, PublicKeySelf, PublicKeyOther, fundingScript, AssetId);
        //    Console.WriteLine(CTX);

        //    //RDTX
        //    Console.WriteLine("----------RDTX------------");
        //    UInt160 ScriptHashSelf1 = PublicKeyToScriptHash(PublicKeySelf);
        //    string AddressSelf = ToAddress1(ScriptHashSelf1);
        //    string addressRSMC = CTX["addressRSMC"].ToString();
        //    addressRSMC = FormatJObject(addressRSMC);                                                                   //暂用
        //    string CTxId = CTX["txId"].ToString();
        //    CTxId = FormatJObject(CTxId);                                                                               //暂用
        //    string RSMCScript = CTX["scriptRSMC"].ToString();
        //    RSMCScript = FormatJObject(RSMCScript);                                                                     //暂用

        //    JObject RDTX = createRDTX(addressRSMC, AddressSelf, BalanceSelf, CTxId, RSMCScript, AssetId);
        //    Console.WriteLine(RDTX);

        //}
    }
}
