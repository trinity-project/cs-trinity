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
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePack;

using Neo;
using Neo.Wallets;
using Neo.Cryptography.ECC;
using Neo.SmartContract;

using Trinity.BlockChain;
using Trinity.Wallets.Templates.Definitions;

using System.Web.Script.Serialization;

namespace Trinity.Wallets
{
    public static class Utils
    {
        public static void SetAttribute<TContext, TValue>(this TContext context, string name, TValue value)
        {
            try
            {
                PropertyInfo attr = typeof(TContext).GetProperty(name);
                attr.SetValue(context, value);
            }
            catch (Exception ExpInfo)
            {
                Log.Error("Failed to set attribute<{0}> to {1}: {2}",
                    name, typeof(TContext), ExpInfo);
            }
        }

        public static TValue GetAttribute<TContext, TValue>(this TContext context, string name)
        {
            try
            {
                PropertyInfo attr = typeof(TContext).GetProperty(name);
                return (TValue)attr.GetValue(context);
            }
            catch (Exception ExpInfo)
            {
                // TODO: Write to file later.
                Log.Error("Failed to get attribute<{0}> : {1}",
                    name, ExpInfo);
            }

            return default;
        }

        public static string Serialize<TMessage>(this TMessage msg)
        {
            return MessagePackSerializer.ToJson(MessagePackSerializer.Serialize(msg));
        }

        public static TMessage Deserialize<TMessage>(this string msg)
        {
            return MessagePackSerializer.Deserialize<TMessage>(MessagePackSerializer.FromJson(msg));
        }

        public static TMessage DeserializeObject<TMessage>(this string msg)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            return serializer.Deserialize<TMessage>(msg);
        }

        /////////////////////////////////////////////////////////////////////////////
        ///
        public static UInt160 ConvertToScriptHash(this string pubKey)
        {
            ECPoint ECPointPublicKey = ECPoint.DecodePoint(pubKey.RemovePrefix().HexToBytes(), ECCurve.Secp256r1);
            UInt160 ScriptHash = Contract.CreateSignatureRedeemScript(ECPointPublicKey).ToScriptHash();
            return ScriptHash;
        }

        public static string PublicKeyToAddress(this string pubKey)
        {
            return NeoInterface.ToAddress1(pubKey.RemovePrefix().ConvertToScriptHash());
        }

        public static string RemovePrefix(this string content)
        {
            return content.Replace("\"", "").Replace("0x", "").Replace("0X", "");
        }

        /////////////////////////////////////////////////////////////////////////////
        ///
        public static string ToAssetId(this string assetType, Dictionary<string, string> assetMap, bool isThrowException = true)
        {
            if (assetMap.ContainsKey(assetType))
            {
                return assetMap[assetType];
            }
            else if (assetMap.ContainsValue(assetType))
            {
                return assetType;
            }
            else
            {
                if (isThrowException)
                {
                    throw new Exception(string.Format("Not support AssetType: {0}", assetType));
                }

                return assetType;
            }
        }

        public static string ToAssetType(this string assetId, Dictionary<string, string> assetMap, bool isThrowException=true)
        {
            if (assetMap.ContainsKey(assetId))
            {
                return assetId;
            }
            else if (assetMap.ContainsValue(assetId))
            {
                return assetMap.FirstOrDefault(q => q.Value == assetId).Key;
            }
            else
            {
                if (isThrowException)
                {
                    throw new Exception(string.Format("Not support AssetID: {0}", assetId));
                }

                return assetId;
            }
        }
    }
}
