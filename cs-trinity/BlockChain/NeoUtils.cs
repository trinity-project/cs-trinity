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
using Neo.Wallets;
using Neo.Cryptography.ECC;
using Neo.SmartContract;


namespace Trinity.BlockChain
{
    /// <summary>
    /// Wrap some extension methods for Trinity transaction 
    /// </summary>
    public static class NeoUtils
    {
        //////////////////////////////////////////////////////////////////////////////////////////
        ///  Adapt to Neo data type convertion                                                 ///
        ////////////////////////////////////////////////////////////////////////////////////////// 
        #region NeoInterfaceAdaptor
        /// <summary>
        /// Remove the useless chars in the string value for Neo
        /// </summary>
        /// <param name="value"> Any String Value Type </param>
        /// <returns> String Without Useless chars, such as "0x", "0X" or "\"" </returns>
        public static string Strip(this string value, string strip)
        {
            return value.Replace(strip, "");
        }

        /// <summary>
        /// Remove the useless chars in the string value for Neo
        /// </summary>
        /// <param name="value"> Any String Value Type </param>
        /// <returns> String Without Useless chars, such as "0x", "0X" or "\"" </returns>
        public static string NeoStrip(this string value)
        {
            return value.Strip("\"").Strip("0x").Strip("0X");
        }

        /// <summary>
        /// Convert value from string to ECPoint Type.
        /// In Neo, it can convert PublicKey from string to the ECPoint type.
        /// </summary>
        /// <param name="value"> Wallet PublicKey String or others </param>
        /// <returns> ECPoint Type Value. In Neo, it means Wallet's PublicKey </returns>
        public static ECPoint ToECPoint(this string value)
        {
            return ECPoint.DecodePoint(value.NeoStrip().HexToBytes(), ECCurve.Secp256r1);
        }

        /// <summary>
        /// Convert value from string to UInt160.
        /// In Neo, it can convert PublicKey string to ScriptHash.
        /// </summary>
        /// <param name="value"> Wallet PublicKey String or others </param>
        /// <returns> UInt160 Type Value. In Neo, it means Wallet's PublicKey ScriptHash </returns>
        public static UInt160 ToHash160(this string value)
        {
            return value.ToECPoint().ToHash160() ;
        }

        /// <summary>
        /// Convert value from ECPoint to UInt160
        /// In Neo, it can convert PublicKey from ECPoint to UInt160 (ScriptHash of Wallet's PublicKey in Neo)
        /// </summary>
        /// <param name="value"> Wallet PublicKey or others </param>
        /// <returns> UInt160 Type Value. In Neo, it means Wallet's PublicKey ScriptHash </returns>
        public static UInt160 ToHash160(this ECPoint value)
        {
            return Contract.CreateSignatureRedeemScript(value).ToScriptHash();
        }

        /// <summary>
        /// Convert value from ECPoint to String Type.
        /// In Neo, it can convert PublicKey from ECPoint to string
        /// </summary>
        /// <param name="value"> UInt160 Type Value. In Neo, it means Wallet's PublicKey String </param>
        /// <returns></returns>
        public static string ToPublicKeyString(this ECPoint value)
        {
            return value.EncodePoint(true).ToHexString();
        }

        /// <summary>
        /// In Neo, it can convert the Wallet's PublicKey string to Wallet's Address.
        /// </summary>
        /// <param name="value"> Wallet's PublicKey String </param>
        /// <returns> Wallet's Address String </returns>
        public static string ToAddress(this string value)
        {
            return value.ToHash160().ToAddress();
        }

        /// <summary>
        /// In Neo, it can convert the Wallet's PublicKey to Wallet's Address.
        /// </summary>
        /// <param name="value"> Wallet PublicKey </param>
        /// <returns> Wallet's Address </returns>
        public static string ToAddress(this ECPoint value)
        {
            return value.ToHash160().ToAddress();
        }
        #endregion // NeoInterfaceAdaptor
    }
}
