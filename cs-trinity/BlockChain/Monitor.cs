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
using System.Threading;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using plugin_trinity;

namespace Trinity.BlockChain
{
    class Monitor
    {
        public List<string> getBlock(uint blockHeigh)
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

        public uint getBlockHeight()
        {
            return Blockchain.Singleton.Height + 1;
        }

        public void monitorBlock()
        {
            while (true)
            {
                uint blockChainHeight = 0;
                uint walletBlockHeitht = 0;
                uint deltaBlockHeitht = 0;

                try
                {
                    blockChainHeight = getBlockHeight();
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                walletBlockHeitht = Plugin_trinity.api.CurrentWallet.WalletHeight;
                deltaBlockHeitht = blockChainHeight - walletBlockHeitht;
                if (deltaBlockHeitht >= 0)
                {
                    try
                    {
                        if (deltaBlockHeitht > 0 && deltaBlockHeitht < 2000)
                        {
                            //TODO jugement whether there is matched txId, then trigger function to handle it.
                        }
                        else if (deltaBlockHeitht >= 2000)
                        {
                            //TODO if there is matched txId, Then punishment would be meaningless.
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
                else
                {

                }

                if (walletBlockHeitht < blockChainHeight)
                {
                    Thread.Sleep(1000);
                }
                else
                {
                    Thread.Sleep(15000);
                }
            }
        }

        public void registerTxId(string txId, params string[] txInfo)
        {

        }

        public void registerBlock(uint block, params string[] blockInfo)
        {

        }
    }
}
