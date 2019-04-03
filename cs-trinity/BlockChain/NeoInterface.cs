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
using Akka.Actor;
using Neo;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Network.RPC;
using Neo.Persistence;
using Neo.Wallets;
using plugin_trinity;

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
                //    throw new RpcException(-505, "One of the Policy filters failed.");
                default:
                    throw new RpcException(-500, "Unknown error.");
            }
        }

        public static JObject sendRawTransaction(string trans)
        {
            Transaction tx = Transaction.DeserializeFrom(trans.HexToBytes());
            RelayResultReason reason = system.Blockchain.Ask<RelayResultReason>(tx).Result;
            return GetRelayResult(reason);
        }
    }
}
