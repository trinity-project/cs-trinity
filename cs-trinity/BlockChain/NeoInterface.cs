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
using Neo.Wallets.NEP6;
using Neo.Cryptography.ECC;
using MessagePack;

using Trinity.Wallets;
using Trinity.TrinityDB.Definitions;
using Trinity.ChannelSet;
using Trinity.ChannelSet.Definitions;
using System.Text.RegularExpressions;

namespace Trinity.BlockChain
{
    public sealed class NeoInterface
    {

        static System.Security.Cryptography.SHA256 sha256 = System.Security.Cryptography.SHA256.Create();

        private static BigDecimal GetNep5Balance(UInt160 asset_id, UInt160 scriptHash)
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

        public static JObject GetBalance(string assetId, string publicKey)
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
                        json["balance"] = GetNep5Balance(asset_id_160, scriptHash).ToString();
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

        public static List<string> GetBlockTxId(uint blockHeigh)
        {
            JObject blockInfo = new JObject();
            JArray tx;
            Block block;
            List<string> txidList = new List<string>();
            block = Blockchain.Singleton.Store.GetBlock(blockHeigh);
            if (block == null)
            {
                return null;
            }
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

        /// <summary>
        /// Get block data for monitoring the multi-sign contract address
        /// </summary>
        /// <param name="blockHeight"></param>
        public static List<Transaction> GetBlockData(uint blockHeight)
        {
            JObject blockInfo = new JObject();
            JArray tx;
            Block block;
            List<Transaction> txList = new List<Transaction>();
            block = Blockchain.Singleton.Store.GetBlock(blockHeight);
            if (block == null)
            {
                return null;
            }
            Transaction[] transactions = block.Transactions;
            for (int txNum = 0; txNum < transactions.Count(); txNum++)
            {
                if (transactions[txNum].GetType() == typeof(ContractTransaction))
                {
                    txList.Add(transactions[txNum]);
                }
            }
            return txList;
        }

        public static uint GetBlockHeight()
        {
            return Blockchain.Singleton.HeaderHeight;
        }

        public static uint GetWalletBlockHeight()
        {
            if (startTrinity.currentWallet == null)
            {
                throw new RpcException(-400, "Wallet didn't open.");
            }
            else
            {
                return (startTrinity.currentWallet.WalletHeight > 0) ? startTrinity.currentWallet.WalletHeight - 1 : 0;
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

        public static JObject SendRawTransaction(string trans)
        {
            Transaction tx = Transaction.DeserializeFrom(trans.RemovePrefix().HexToBytes());
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
            //if (str.IndexOf("0x") == 0)
            //    str = str.Substring(2);
            str = str.RemovePrefix();
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

        public static bool VerifySignature(string messageData, string signatureData, string pubkey)
        {
            //unity dotnet不完整，不能用dotnet自带的ecdsa
            byte[] publicKey = HexString2Bytes(pubkey);

            var message = HexString2Bytes(messageData);
            var signature = HexString2Bytes(signatureData);
            var PublicKey = Neo.Cryptography.ECC.ECPoint.DecodePoint(publicKey, Neo.Cryptography.ECC.ECCurve.Secp256r1);
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
        //public static UInt160 ToScriptHash1(string address)
        //{
        //    byte[] data = address.RemovePrefix().Base58CheckDecode();
        //    if (data.Length != 21)
        //        throw new FormatException();
        //    return new UInt160(data.Skip(1).ToArray());
        //}

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
            //UInt160 ScriptHash = Script.ConvertToScriptHash();
            UInt160 ScriptHash = ScriptToScriptHash(Script.RemovePrefix().HexToBytes());
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
        public static byte[] LongToBytes(long n)
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
        public static string IntToHex(int i)
        {
            return BigInteger.Parse(i.ToString()).ToByteArray().ToHexString();
        }

        public static string BlockheightToScript(uint input)
        {
            string inputHex = Convert.ToString(input, 16);

            if (inputHex.Length % 2 == 1)
            {
                inputHex = inputHex.Insert(0, 0.ToString());
            }

            byte[] reverseValue = HexString2Bytes(inputHex).Reverse().ToArray();
            string inputReverse = Bytes2HexString(reverseValue);

            string lengthString = (inputReverse.Length / 2).ToString();
            if ((lengthString.Length % 2) != 0)
            {
                lengthString = lengthString.Insert(0, 0.ToString());
            }
            string totalLength = Convert.ToString(83 + int.Parse(lengthString));
            string output = String.Format("{0}{1}{2}", totalLength, lengthString, inputReverse);

            return output; 
        }

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
                pubkey_large = publicKey1.NeoStrip();
                pubkey_small = publicKey2.NeoStrip();
            }
            else
            {
                pubkey_large = publicKey2.NeoStrip();
                pubkey_small = publicKey1.NeoStrip();
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
            Neo.Cryptography.ECC.ECPoint ECPointPublicKey = Neo.Cryptography.ECC.ECPoint.DecodePoint(PublicKey.RemovePrefix().HexToBytes(), Neo.Cryptography.ECC.ECCurve.Secp256r1);
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
        public static string CreateOpdata(string AddressFrom, string AddressTo, string Value, string AssetID)
        {
            string OpCode = "";
            string Value1 = BigInteger.Parse(Value).ToByteArray().ToHexString();
            string ScriptHashFrom = AddressFrom.ToScriptHash().ToArray().ToHexString();
            string ScriptHashTo = AddressTo.ToScriptHash().ToArray().ToHexString();
            string method = StringToHexString("transfer");            //7472616e73666572
            string[] invoke_args = { Value1, ScriptHashTo, ScriptHashFrom };
            foreach (var item in invoke_args)
            {
                string[] args = { IntToHex(item.Length / 2), item };
                OpCode += string.Join("", args);
            }
            OpCode += "53";  // PUSH3
            OpCode += "c1";  // PACK
            OpCode += IntToHex(method.Length / 2);
            OpCode += method;
            OpCode += "67";  // APPCALL
            OpCode += AssetID.RemovePrefix().HexToBytes().Reverse().ToArray().ToHexString();
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
        public static JObject CreateRSMCContract(UInt160 HashSelf, string PubkeySelf, UInt160 HashOther, string PubkeyOther, string Timestamp)
        {
            Timestamp = NeoInterface.StringToHexString(Timestamp);
            byte[] TimestampByte = Encoding.UTF8.GetBytes(Timestamp);
            string length = NeoInterface.IntToHex(TimestampByte.Length / 2).PadLeft(2, '0');
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

        public static JObject CreateHTLCContract(string futureTimestamp, string PubkeySelf, string PubkeyOther, string HashR)
        {
            long time = long.Parse(futureTimestamp?.Split('.')[0]);
            futureTimestamp = LongToBytes(time).Reverse().ToHexString();
            Console.WriteLine("------------------futureTimestamp------------------");
            Console.WriteLine(futureTimestamp);
            //Timestamp = NeoInterface.StringToHexString(Timestamp);
            //byte[] TimestampByte = Encoding.UTF8.GetBytes(Timestamp);
            //string length = NeoInterface.IntToHex(TimestampByte.Length / 2).PadLeft(2, '0');
            //string magicTimestamp = length + Timestamp;

            string contractTemplate =
                     "58c56b6c766b00527ac46c766b51527ac46c766b52527ac4616c766b52c3a76c766b53527ac46168184e656f2e4" +
                     "26c6f636b636861696e2e4765744865696768746168184e656f2e426c6f636b636861696e2e4765744865616465" +
                     "726c766b54527ac46c766b00c36121{0}ac642f006c766b51c36121{1}ac635f006c766b00c3612" +
                     "1{2}ac642f006c766b51c36121{3}ac62040000620400516c766b55527ac46c766b54c36168174e" +
                     "656f2e4865616465722e47657454696d657374616d7004{4}9f6c766b56527ac46c766b56c364" +
                     "3600616c766b55c36422006c766b53c36114{5}9c620400006c766b57527ac46212006c766b55c36c766b57" +
                     "527ac46203006c766b57c3616c7566";
            string HTLCContract = String.Format(contractTemplate, PubkeySelf, PubkeyOther, PubkeyOther, PubkeySelf, futureTimestamp, HashR);

            JObject result = new JObject();
            result["script"] = HTLCContract;
            result["address"] = ScriptToAddress(HTLCContract);
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
                return attr.RemovePrefix().HexToBytes();
            }
        }
        public class TransactionAttributeLong : TransactionAttribute<long>
        {
            public TransactionAttributeLong(TransactionAttributeUsage Usage, long Data, List<TransactionAttribute> attributes) : base(Usage, Data, attributes)
            {
            }

            public override byte[] ConvertToArray(long attr)
            {
                return LongToBytes(attr);
            }
        }
        public class TransactionAttributeDouble : TransactionAttribute<double>
        {
            public TransactionAttributeDouble(TransactionAttributeUsage Usage, double Data, List<TransactionAttribute> attributes) : base(Usage, Data, attributes)
            {
            }

            public override byte[] ConvertToArray(double attr)
            {
                return BitConverter.GetBytes(attr);
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
        public static string CreateVerifyScript(string Script)
        {
            string tmp = IntToHex(Script.Length / 2);
            if (tmp.Length % 2 == 1)
            {
                tmp = "0" + tmp;
            }
            string[] array = { tmp, Script };
            string verify_script = string.Join("", array);

            return verify_script;
        }

        //获取账户下的符合要求的input信息
        private static Coin[] GetUnspentCoins(UInt160[] from, UInt256 asset_id, Fixed8 amount)
        {
            try
            {
                IEnumerable<UInt160> accounts = from.Length > 0 ? from : startTrinity.currentWallet.GetAccounts().Where(p => !p.Lock && !p.WatchOnly).Select(p => p.ScriptHash);
                IEnumerable<Coin> unspents = startTrinity.currentWallet.GetCoins(accounts).Where(p => p.State.HasFlag(CoinState.Confirmed) && !p.State.HasFlag(CoinState.Spent) && !p.State.HasFlag(CoinState.Frozen));

                Coin[] unspents_asset = unspents.Where(p => p.Output.AssetId == asset_id).ToArray();
                Fixed8 sum = unspents_asset.Sum(p => p.Output.Value);
                if (sum < amount) return null;
                if (sum == amount) return unspents_asset;
                Coin[] unspents_ordered = unspents_asset.OrderByDescending(p => p.Output.Value).ToArray();
                int i = 0;
                while (unspents_ordered[i].Output.Value <= amount)
                    amount -= unspents_ordered[i++].Output.Value;
                if (amount == Fixed8.Zero)
                    return unspents_ordered.Take(i).ToArray();
                else
                    return unspents_ordered.Take(i).Concat(new[] { unspents_ordered.Last(p => p.Output.Value >= amount) }).ToArray();
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        // 构造NEO/GAS founding交易需要的信息字段
        [MessagePackObject(keyAsPropertyName: true)]
        public class Vin
        {
            public ushort n;
            public string txid;
            public long value;
        }

        private const long D = 100_000_000;

        //查找未花费的资产交易信息
        public static List<string> getGlobalAssetVout(UInt160 scriptHash, string _assetId, uint _amount)
        {
            UInt256 assetId = UInt256.Parse(_assetId);
            Fixed8 amount = Fixed8.Parse(new Fixed8(_amount).ToString());
            //UInt160 account = NeoInterface.PublicKeyToScriptHash(scriptHash);
            UInt160[] accounts = { scriptHash };
            Coin[] coinList = GetUnspentCoins(accounts, assetId, amount);
            List<string> listData = new List<string>();
            if (coinList == null)
            {
                return null;
            }

            foreach (Coin coin in coinList)
            {
                Vin vin = new Vin
                {
                    n = coin.Reference.PrevIndex,
                    txid = coin.Reference.PrevHash.ToString(),
                    value = coin.Output.Value.GetData()
                };

                string vinData = MessagePackSerializer.ToJson(MessagePackSerializer.Serialize(vin));
                listData.Add(vinData);
            }
            return listData;
        }

        //根据Vout计算input
        public static CoinReference[] getInputFormVout(List<string> listData)
        {
            if (listData == null)
            {
                return null;
            }

            List<CoinReference> inputList = new List<CoinReference> { };
            foreach (string vinData in listData)
            {
                Vin vin = vinData.Deserialize<Vin>();
                CoinReference input = createInput(vin.txid, vin.n);
                inputList.Add(input);
            }
            CoinReference[] inputArray = inputList.ToArray();
            return inputArray;
        }

        public static CoinReference createInput(string txid, ushort n)
        {
            CoinReference input = new CoinReference
            {
                PrevHash = UInt256.Parse(txid),
                PrevIndex = n
            };

            return input;
        }

        public static CoinReference[] createInputsData(string txid, ushort n)
        {
            List<CoinReference> inputList = new List<CoinReference> { };
            CoinReference input = createInput(txid, n);
            inputList.Add(input);
            CoinReference[] inputArray = inputList.ToArray();
            return inputArray;
        }

        //计算output
        public static TransactionOutput[] createOutput(string _assetId, string _amount, string address, bool refunding = false)
        {
            UInt256 assetId = UInt256.Parse(_assetId);
            long uAmount = long.Parse(_amount);
            Fixed8 amount;

            if (0 == uAmount && refunding)
            {
                return null;
            }
            else if (0 == uAmount)
            {
                amount = Fixed8.Zero;
            }
            else
            {
                amount = new Fixed8(uAmount);
            }
            UInt160 address_hash = address.ToScriptHash();

            List<TransactionOutput> outputList = new List<TransactionOutput> { };
            TransactionOutput Output = new TransactionOutput
            {
                AssetId = assetId,
                Value = amount,
                ScriptHash = address_hash,
            };
            outputList.Add(Output);
            return outputList.ToArray();
        }

        public static void addContractToAccount(string sender, string receiver)
        {
            string pubKey = sender?.Split('@').First();
            string peerPubkey = receiver?.Split('@').First();
            Contract contract = NeoInterface.CreateMultiSigContract(pubKey, peerPubkey);

            WalletAccount account = startTrinity.currentWallet.CreateAccount(contract);
            if (startTrinity.currentWallet is NEP6Wallet wallet)
                wallet.Save();
            Log.Info("Succeed add contract address to account. ContractAddress: {0}.",
                contract.Address);
        }

        public static void removeContractFormAccount(Channel channel, ChannelTableContent ChannelData)
        {
            List<ChannelTableContent> ChannelList = channel.GetChannelList(ChannelData.peer, 0, EnumChannelState.OPENED.ToString());
            // When this contract address is the last one, delete it from the account
            if (ChannelList.Count == 1)
            {
                string pubKey = ChannelData.uri?.Split('@').First();
                string peerPubkey = ChannelData.peer?.Split('@').First();
                Contract contract = NeoInterface.CreateMultiSigContract(pubKey, peerPubkey);

                if (startTrinity.currentWallet.DeleteAccount(contract.ScriptHash))
                {
                    Log.Info("Succeed delete contract address form account. ContractAddress: {0}, ContractScriptHash: {0}.",
                    contract.Address, contract.ScriptHash);
                }
            }
        }

        ///<summary>
        ///verify TxData 
        ///</summary>
        ///<param name="txData"></param>
        ///<param name="assetId"></param>
        ///<param name="pubKey"></param>
        ///<param name="deposit"></param>
        ///<param name="peerPubKey"></param>
        ///<param name="peerDeposit"></param>
        public static bool verifyTxData(string txData, string _txData, string assetId)
        {
            string _txDataProcessed;
            string txDataProcessed;
            if (null == txData || null == _txData || null == assetId) return false;

            if (assetId.IsNeoOrNeoGas())
            {
                txDataProcessed = txData.Substring(110, txData.Length - 110);
                _txDataProcessed = _txData.Substring(110, _txData.Length - 110);
            } else
            {
                txDataProcessed = txData.Substring(0, txData.Length - 22);
                _txDataProcessed = _txData.Substring(0, _txData.Length - 22);
            }

            return txDataProcessed == _txDataProcessed;
        }
    }
}
