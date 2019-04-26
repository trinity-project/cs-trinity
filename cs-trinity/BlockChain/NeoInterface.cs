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
using Akka.Actor;
using Neo;
using Neo.Cryptography;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Network.RPC;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.Wallets;
using System.Numerics;
using VMArray = Neo.VM.Types.Array;
using Neo.VM;
using Neo.Cryptography.ECC;

namespace Trinity.BlockChain
{
    public class NeoInterface
    {
        private static readonly NeoSystem system;
        static System.Security.Cryptography.SHA256 sha256 = System.Security.Cryptography.SHA256.Create();

        private static BigDecimal getNep5Balance(UInt160 asset_id, UInt160 scriptHash)
        {
            if (asset_id is UInt160 asset_id_160)
            {
                byte[] script;
                using (ScriptBuilder sb = new ScriptBuilder())
                {
                    sb.EmitAppCall(asset_id_160, "balanceOf", scriptHash);
                    sb.Emit(OpCode.DEPTH, OpCode.PACK);
                    sb.EmitAppCall(asset_id_160, "decimals");
                    script = sb.ToArray();
                }
                ApplicationEngine engine = ApplicationEngine.Run(script);
                if (engine.State.HasFlag(VMState.FAULT))
                    return new BigDecimal(0, 0);
                byte decimals = (byte)engine.ResultStack.Pop().GetBigInteger();
                BigInteger amount = ((VMArray)engine.ResultStack.Pop()).Aggregate(BigInteger.Zero, (x, y) => x + y.GetBigInteger());
                return new BigDecimal(amount, decimals);
            }
            else
            {
                return new BigDecimal(0, 8);
            }
        }

        public static JObject getBalance(string assetId, string publicKey)
        {
            if (startTrinity.currentWallet == null)
            {
                throw new RpcException(-400, "Wallet didn't open.");
            }
            else
            {
                JObject json = new JObject();
                UInt160 scriptHash = PublicKeyToScriptHash(startTrinity.currentAccountPublicKey);
                UInt160[] account = { scriptHash };
                IEnumerable<UInt160> accounts = account;
                switch (UIntBase.Parse(assetId))
                {
                    case UInt160 asset_id_160: //NEP-5 balance
                        json["balance"] = getNep5Balance(asset_id_160, scriptHash).ToString();
                        break;
                    case UInt256 asset_id_256: //Global Assets balance
                        IEnumerable<Coin> coins = startTrinity.currentWallet.GetCoins(accounts).Where(p => !p.State.HasFlag(CoinState.Spent) && p.Output.AssetId.Equals(asset_id_256));
                        json["balance"] = coins.Sum(p => p.Output.Value).ToString();
                        json["confirmed"] = coins.Where(p => p.State.HasFlag(CoinState.Confirmed)).Sum(p => p.Output.Value).ToString();
                        break;
                }
                return json;
            }
        }

        public static List<string> getBlockTxId(uint blockHeigh)
        {
            JObject blockInfo = new JObject();
            JArray tx;
            Block block;
            List<string> txidList = new List<string>();
            block = Blockchain.Singleton.Store.GetBlock(blockHeigh);
            tx = (JArray)block.ToJson()["tx"];
            for (int txNum = 0; txNum < tx.Count(); txNum++)
            {
                blockInfo = JObject.Parse(tx[txNum].ToString());
                blockInfo["txid"].ToString();
                txidList.Add(blockInfo["txid"].ToString());
            }
            if (txidList.Count == 0)
            {
                return null;
            }
            return txidList;
        }

        public static uint getBlockHeight()
        {
            return Blockchain.Singleton.Height + 1;
        }

        public static uint getWalletBlockHeight()
        {
            if (startTrinity.currentWallet == null)
            {
                throw new RpcException(-400, "Wallet didn't open.");
            }
            else
            {
                return (startTrinity.currentWallet.WalletHeight > 0)? startTrinity.currentWallet.WalletHeight-1 : 0;
            }            
        }

        private static JObject GetRelayResult(RelayResultReason reason)
        {
            switch (reason)
            {
                case RelayResultReason.Succeed:
                    return true;
                case RelayResultReason.AlreadyExists:
                    throw new RpcException(-501, "Block or transaction already exists and cannot be sent repeatedly.");
                case RelayResultReason.OutOfMemory:
                    throw new RpcException(-502, "The memory pool is full and no more transactions can be sent.");
                case RelayResultReason.UnableToVerify:
                    throw new RpcException(-503, "The block cannot be validated.");
                case RelayResultReason.Invalid:
                    throw new RpcException(-504, "Block or transaction validation failed.");
                //case RelayResultReason.PolicyFail:
                //  throw new RpcException(-505, "One of the Policy filters failed.");
                default:
                    throw new RpcException(-500, "Unknown error.");
            }
        }

        public static JObject sendRawTransaction(string trans)
        {
            Transaction tx = Transaction.DeserializeFrom(trans.HexToBytes());
            RelayResultReason reason = startTrinity.NeoSystem.Blockchain.Ask<RelayResultReason>(tx).Result;
            return GetRelayResult(reason);
        }

        public static byte[] GetPublicKeyFromPrivateKey(byte[] privateKey)
        {
            var PublicKey = Neo.Cryptography.ECC.ECCurve.Secp256r1.G * privateKey;
            return PublicKey.EncodePoint(true);
        }

        public static string Bytes2HexString(byte[] data)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var d in data)
            {
                sb.Append(d.ToString("x02"));
            }
            return sb.ToString();
        }
        public static byte[] HexString2Bytes(string str)
        {
            if (str.IndexOf("0x") == 0)
                str = str.Substring(2);
            byte[] outd = new byte[str.Length / 2];
            for (var i = 0; i < str.Length / 2; i++)
            {
                outd[i] = byte.Parse(str.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);
            }
            return outd;
        }

        public static string Sign(string messageData, byte[] prikey)
        {
            var Secp256r1_G = HexString2Bytes("04" + "6B17D1F2E12C4247F8BCE6E563A440F277037D812DEB33A0F4A13945D898C296" + "4FE342E2FE1A7F9B8EE7EB4A7C0F9E162BCE33576B315ECECBB6406837BF51F5");

            var message = HexString2Bytes(messageData);
            var PublicKey = Neo.Cryptography.ECC.ECCurve.Secp256r1.G * prikey;
            var pubkey = PublicKey.EncodePoint(false).Skip(1).ToArray();

            var ecdsa = new Neo.Cryptography.ECC.ECDsa(prikey, Neo.Cryptography.ECC.ECCurve.Secp256r1);
            var hash = sha256.ComputeHash(message);
            var result = ecdsa.GenerateSignature(hash);
            var data1 = result[0].ToByteArray();
            if (data1.Length > 32)
                data1 = data1.Take(32).ToArray();
            var data2 = result[1].ToByteArray();
            if (data2.Length > 32)
                data2 = data2.Take(32).ToArray();

            data1 = data1.Reverse().ToArray();
            data2 = data2.Reverse().ToArray();

            byte[] newdata = new byte[64];
            Array.Copy(data1, 0, newdata, 32 - data1.Length, data1.Length);
            Array.Copy(data2, 0, newdata, 64 - data2.Length, data2.Length);

            return Bytes2HexString(newdata);// data1.Concat(data2).ToArray();
        }

        public static bool VerifySignature(string messageData, string signatureData, byte[] pubkey)
        {
            //unity dotnet不完整，不能用dotnet自带的ecdsa

            var message = HexString2Bytes(messageData);
            var signature = HexString2Bytes(signatureData);
            var PublicKey = Neo.Cryptography.ECC.ECPoint.DecodePoint(pubkey, Neo.Cryptography.ECC.ECCurve.Secp256r1);
            var ecdsa = new Neo.Cryptography.ECC.ECDsa(PublicKey);
            var b1 = signature.Take(32).Reverse().Concat(new byte[] { 0x00 }).ToArray();
            var b2 = signature.Skip(32).Reverse().Concat(new byte[] { 0x00 }).ToArray();
            var num1 = new BigInteger(b1);
            var num2 = new BigInteger(b2);
            var hash = sha256.ComputeHash(message);
            return ecdsa.VerifySignature(hash, num1, num2);
        }

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
            string ScriptHashFrom = ToScriptHash1(AddressFrom).ToArray().ToHexString();
            string ScriptHashTo = ToScriptHash1(AddressTo).ToArray().ToHexString();
            string method = StringToHexString("transfer");            //7472616e73666572
            string[] invoke_args = { Value1, ScriptHashTo, ScriptHashFrom };
            foreach (var item in invoke_args)
            {
                string[] args = { intToHex(item.Length / 2), item };
                OpCode += string.Join("", args);
            }
            OpCode += "53";  // PUSH3
            OpCode += "c1";  // PACK
            OpCode += intToHex(method.Length / 2);
            OpCode += method;
            OpCode += "67";  // APPCALL
            OpCode += AssetID.HexToBytes().Reverse().ToArray().ToHexString();
            OpCode += "f1";  // THROWIFNOT

            return OpCode;
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
            Timestamp = NeoInterface.StringToHexString(Timestamp);
            byte[] TimestampByte = Encoding.UTF8.GetBytes(Timestamp);
            string length = NeoInterface.intToHex(TimestampByte.Length / 2).PadLeft(2, '0');
            string magicTimestamp = length + Timestamp;

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
    }
}
