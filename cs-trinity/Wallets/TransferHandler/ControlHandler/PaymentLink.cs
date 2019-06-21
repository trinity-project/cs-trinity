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
using Neo.Cryptography;

using Trinity.BlockChain;
using Trinity.Wallets;
using Trinity.TrinityDB;
using Trinity.TrinityDB.Definitions;
using Trinity.ChannelSet;

namespace Trinity.Wallets.TransferHandler.ControlHandler
{
    /// <summary>
    /// This class is used to generatate some 
    /// </summary>
    public sealed class Payment
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="uri"> Current Wallet Uri </param>
        /// <param name="assetId"> Asset ID </param>
        /// <param name="payment"> How much to pay </param>
        /// <param name="comments"> User comments for this payment </param>
        /// <returns></returns>
        public static string GeneratePaymentCode(string uri, string assetId, Fixed8 payment, string comments)
        {
            if (new Fixed8(0) > payment)
            {
                Log.Error("Payment value: {0} should not be less than 0.", payment);
                return null;
            }

            string asset = assetId.NeoStrip();
            if (0 >= assetId.Length)
            {
                Log.Error("Invalid asset ID: {0}", assetId);
                return null;
            }

            string rcode = CreateRCode(uri.Split('@')[0].ToHash160());
            string hashcode = rcode.Sha1();

            // record the Hashcode and RCode pair into database.
            AddHLockTransaction(asset, hashcode, rcode, payment.GetData());

            string paymentCode = string.Format("{0}&{1}&{2}&{3}&{4}", uri, hashcode, asset, payment.GetData(), comments);

            return Base58.Encode(paymentCode.ToBytesUtf8());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scriptHash">Current Wallet public key script hash</param>
        /// <returns></returns>
        internal static string CreateRCode(UInt160 scriptHash)
        {
            byte[] randomBytes = new byte[4];

            // To avoid repeadly random data is generated, we use the ticks as seed for random
            Random random = new Random((int)DateTime.Now.Ticks);
            random.NextBytes(randomBytes);

            byte[] rcodeBytes = scriptHash.ToArray().Concat(randomBytes).ToArray().TimeAttribute();
            string rcodeString = rcodeBytes.ToHexString();
            
            return rcodeString;
        }

        internal static void AddHLockTransaction(string asset, string hashcode, string rcode, long income)
        {
            TransactionTabelHLockPair hLockPair = new TransactionTabelHLockPair
            {
                asset = asset,
                rcode = rcode,
                income = income,
            };

            Channel levelDbApi = new Channel("hLockPair", null, null);
            levelDbApi.AddTransactionHLockPair(hashcode, hLockPair);
        }

    }

    public class PaymentLink
    {
    }
}
