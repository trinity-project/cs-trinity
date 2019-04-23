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
using plugin_trinity;
using System.Numerics;

namespace Trinity.BlockChain
{
    class NeoInterface
    {
        private static readonly NeoSystem system;

        public static JObject getBalance(string assetId)
        {
            if (Plugin_trinity.api.CurrentWallet == null)
            {
                throw new RpcException(-400, "Wallet didn't open.");
            }
            else
            {
                JObject json = new JObject();
                switch (UIntBase.Parse(assetId))
                {
                    case UInt160 asset_id_160: //NEP-5 balance
                        json["balance"] = Plugin_trinity.api.CurrentWallet.GetAvailable(asset_id_160).ToString();
                        break;
                    case UInt256 asset_id_256: //Global Assets balance
                        IEnumerable<Coin> coins = Plugin_trinity.api.CurrentWallet.GetCoins().Where(p => !p.State.HasFlag(CoinState.Spent) && p.Output.AssetId.Equals(asset_id_256));
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
            if (Plugin_trinity.api.CurrentWallet == null)
            {
                throw new RpcException(-400, "Wallet didn't open.");
            }
            else
            {
                return (Plugin_trinity.api.CurrentWallet.WalletHeight > 0)? Plugin_trinity.api.CurrentWallet.WalletHeight-1 : 0;
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
            RelayResultReason reason = Plugin_trinity.api.NeoSystem.Blockchain.Ask<RelayResultReason>(tx).Result;
            return GetRelayResult(reason);
        }

        /*
        public static string sign(string data)
        {
            byte[] raw, signedData = null;

            raw = Encoding.UTF8.GetBytes(data);  //input as string
            //raw = data.HexToBytes(); //input as hex
            //UInt160 addressHash = comboBox1.Text.ToScriptHash();
            //var account = Plugin_trinity.api.CurrentWallet.GetAccount(addressHash);
            var account = Plugin_trinity.api.CurrentWallet.GetAccounts().FirstOrDefault();           
            var keys = account.GetKey();
            try
            {
                signedData = Crypto.Default.Sign(raw, keys.PrivateKey, keys.PublicKey.EncodePoint(false).Skip(1).ToArray());
            }
            catch (Exception err)
            {
                return null;
            }
            return signedData?.ToHexString();
        }
        */

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

        static System.Security.Cryptography.SHA256 sha256 = System.Security.Cryptography.SHA256.Create();

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
            //var PublicKey = ThinNeo.Cryptography.ECC.ECPoint.DecodePoint(pubkey, ThinNeo.Cryptography.ECC.ECCurve.Secp256r1);
            //var usepk = PublicKey.EncodePoint(false).Skip(1).ToArray();

            ////byte[] first = { 0x45, 0x43, 0x53, 0x31, 0x20, 0x00, 0x00, 0x00 };
            ////usepk = first.Concat(usepk).ToArray();

            ////using (System.Security.Cryptography.CngKey key = System.Security.Cryptography.CngKey.Import(usepk, System.Security.Cryptography.CngKeyBlobFormat.EccPublicBlob))
            ////using (System.Security.Cryptography.ECDsaCng ecdsa = new System.Security.Cryptography.ECDsaCng(key))

            //using (var ecdsa = System.Security.Cryptography.ECDsa.Create(new System.Security.Cryptography.ECParameters
            //{
            //    Curve = System.Security.Cryptography.ECCurve.NamedCurves.nistP256,
            //    Q = new System.Security.Cryptography.ECPoint
            //    {
            //        X = usepk.Take(32).ToArray(),
            //        Y = usepk.Skip(32).ToArray()
            //    }
            //}))
            //{
            //    var hash = sha256.ComputeHash(message);
            //    return ecdsa.VerifyHash(hash, signature);
            //}
        }
    }
}
