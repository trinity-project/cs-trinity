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
using Trinity;
using Trinity.ChannelSet;
using Trinity.TrinityDB.Definitions;
using Trinity.BlockChain;
using Trinity.ChannelSet.Definitions;
using Trinity.Wallets.TransferHandler.ControlHandler;

namespace Trinity.BlockChain
{
    public class MonitorTransction
    {
        private readonly string netMagic;
        private string uri;
        private Channel channel;

        public MonitorTransction(string uri, string magic)
        {
            this.netMagic = magic;
            this.uri = uri;
            channel = new Channel(null, null, uri);
        }

        public MonitorTransction(string publicKey, string ip, string port, string magic)
        {
            this.netMagic = magic;
            this.uri = String.Format("{0}@{1}:{2}", publicKey, ip, port);
            channel = new Channel(null, null, uri);
        }

        public bool SetWalletUri(string uri)
        {
            bool ret = this.uri.Equals(uri);

            if (!ret)
            {
                this.uri = uri;
            }

            return !ret;
        }

        public void StartMonitor(string publicKey, string magic, string ip, string port)
        {
            string uri = String.Format("{0}@{1}:{2}", publicKey, ip, port);
            MonitorTransction monitorTransction = new MonitorTransction(uri, magic);
            Thread thread = new Thread(monitorTransction.monitorBlock);
            thread.Start();
        }

        public void monitorBlock()
        {
            uint currentMonitordBlockHeight = 0;

            currentMonitordBlockHeight = channel.TryGetBlockHeight(this.uri);
            if (0 == currentMonitordBlockHeight)
            {
                currentMonitordBlockHeight = NeoInterface.GetWalletBlockHeight();
            }
            Thread.Sleep(1000);
            while (true)
            {
                uint blockChainHeight = 0;

                blockChainHeight = NeoInterface.GetBlockHeight();
                if (currentMonitordBlockHeight <= blockChainHeight)
                {
                    //TODO jugement whether there is matched txId, then trigger function to handle it.
                    //TimeSpan cha = DateTime.Now - TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
                    //string t = cha.ToString();
                    //Console.WriteLine("①本地块高:" + walletBlockHeitht + " 链上:" + blockChainHeight + " 当前时间:" + t);
                    MonitorTxId(currentMonitordBlockHeight);
                    channel.AddBlockHeight(this.uri, currentMonitordBlockHeight);
                    currentMonitordBlockHeight++;
                    Thread.Sleep(100);
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

        public void MonitorTxId(uint block)
        {
            uint tryGetTxidNumber = 0; ;
            List<string> txidList = NeoInterface.GetBlockTxId(block);

            //try to get txid repeatedly if list is null 
            while (txidList == null)
            {
                Thread.Sleep(5000);
                txidList = NeoInterface.GetBlockTxId(block);
                tryGetTxidNumber++;
                Log.Warn(">>>>>monitor block {0} with {1} times", block, tryGetTxidNumber);
            }
            foreach (string id in txidList)
            {               
                string id1 = NeoInterface.FormatJObject(id);
                TransactionTabelSummary Summary = channel.TryGetTransaction(id1);
                if (Summary != null)
                {
                    ConductEvent(Summary);
                }
            }
        }

        public void ConductEvent(TransactionTabelSummary Summary)
        {
            try
            {
                ChannelTableContent ChannelData = channel.TryGetChannel(Summary.channel);
                if (null == ChannelData)
                {
                    Log.Error("Channel: {0} is not found for updating operation({1}).", Summary.channel, Summary.txType);
                    return;
                }

                switch (Summary.txType.ToLower())
                {
                    case "funding":
                        ChannelData.state = EnumChannelState.OPENED.ToString();
                        Log.Debug("Change {0} to OPENED state.", Summary.channel);
                        SyncNetTopologyHandler.AddNetworkTopology(ChannelData);
                        break;
                    case "settle":
                        ChannelData.state = EnumChannelState.SETTLED.ToString();
                        Log.Debug("Change {0} to SETTLED state.", Summary.channel);

                        NeoInterface.removeContractFormAccount(channel, ChannelData);
                        break;
                    default:
                        Log.Debug("Unsuport transaction type -- {0} for trinity channel.", Summary.txType);
                        return;
                }

                // update the channel data
                channel.UpdateChannel(Summary.channel, ChannelData);
            }
            catch(Exception ExpInfo)
            {
                Log.Error("Exception occurred during update channel: {0}. Operation: {1}. Exception: {2}", 
                    Summary?.channel, Summary?.txType, ExpInfo);
            }
        }
    }
}
