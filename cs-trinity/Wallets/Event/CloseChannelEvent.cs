﻿/*
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

using Neo.IO.Json;

using Trinity.BlockChain;
using Trinity.ChannelSet;
using Trinity.ChannelSet.Definitions;
using Trinity.TrinityDB.Definitions;
using Trinity.Wallets.Templates.Definitions;
using Trinity.Wallets.TransferHandler.ControlHandler;

namespace Trinity.Wallets.Event
{
    ///////////////////////////////////////////////////////////////////////////////////////////
    /// CloseChannelEvent Prototype
    ///////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Class Event Handler to force closing channel.
    /// </summary>
    public class CloseChannelEvent
    {
        private readonly TrinityWallet wallet;  // current opened wallet context
        private readonly string channelName = null;
        private Channel channelDBEntry = null;
        private readonly ChannelSummaryContents currentChannelSummary = null;
        private ChannelTableContent currentChannel = null;

        private string TransactionType = null;
        private string MonitorTxId = null;
        private const uint DelayBlockHeight = 1000;

        // Transaction contents
        private TxContentsSignGeneric<CommitmentTx> commitment;
        private TxContentsSignGeneric<RevocableDeliveryTx> revocableDelivery;
        private TxContentsSignGeneric<BreachRemedyTx> breachRemedy;
        private TxContentsSignGeneric<HtlcCommitTx> HCTX;
        private TxContentsSignGeneric<HtlcRevocableDeliveryTx> RDTX;

        public CloseChannelEvent(string channel, string uri)
        {
            this.channelName = channel;

            this.channelDBEntry = new Channel(channel, null, uri);

            this.currentChannel = this.channelDBEntry?.TryGetChannel(this.channelName);
            this.currentChannelSummary = this.channelDBEntry?.TryGetChannelSummary(channel);

            this.wallet = startTrinity.trinityWallet;
        }

        public void ForceClosingChannel()
        {
            // update the channel to closing state firstly
            this.UpdateChannelState(EnumChannelState.CLOSING);
            SyncNetTopologyHandler.DeleteNetworkTopology(this.currentChannel);

            if (null == this.currentChannelSummary)
            {
                // Channel is not created successfully
                return;
            }

            // if current transaction is settle trade, maybe settle is not reponsed correctly
            UInt64 currentNonce = this.currentChannelSummary.nonce;
            TransactionTabelContent latestTrade = this.channelDBEntry?.TryGetTransaction<TransactionTabelContent>(currentNonce);
            if (EnumTransactionType.SETTLE.ToString().Equals(latestTrade?.type))
            {
                currentNonce -= 1;
            }

            // get current transaction
            this.GetTransactionContentByNonce(currentNonce);

            // trigger to force closing channel
            this.TriggerForceCloseChannel();
        }

        public void AddRevocableEvent(UInt64 nonce, uint blockHeight)
        {
            BlockEventContent blockEvent = new BlockEventContent
            {
                nonce = nonce,
                channel = this.channelName,
                eventType = EnumTransactionType.REVOCABLE.ToString()
            };

            uint atBlockHeight = blockHeight + DelayBlockHeight;
            this.channelDBEntry.AddBlockEvent(atBlockHeight, blockEvent);
            Log.Debug("Add channel close revocable event at block: {0}.", atBlockHeight);
        }

        public void TriggerRevocableEvent(UInt64 nonce, uint blockHeight)
        {
            // only when channel state in Closing, trigger to broad cast the revocable trade.
            if (this.currentChannel?.state == EnumChannelState.CLOSING.ToString())
            {
                Log.Info("Start to trigger Revocable transaction at block: {0}", blockHeight);
                this.GetTransactionContentByNonce(nonce);
                this.TriggerRevocableTransaction(blockHeight);
                return;
            }
        }

        public void TriggerBreachRemedyEvent(string txId, UInt64 nonce, uint blockHeight)
        {
            if (null == txId)
            {
                return;
            }

            // get transaction by nonce+1
            this.GetTransactionContentByNonce(nonce + 1);
            if (txId.Equals(this.MonitorTxId))
            {
                this.TriggerBreachRemedyTransaction(blockHeight);
            }
            else
            {
                // add event
                this.AddRevocableEvent(nonce, blockHeight);
            }
        }

        private void UpdateChannelState(EnumChannelState state)
        {
            // Update the channel state
            this.currentChannel.state = state.ToString();
            this.channelDBEntry?.UpdateChannel(channelName, this.currentChannel);
        }

        private void GetTransactionContentByNonce(UInt64 nonce)
        {
            TransactionTabelContent currentTrade;

            // get current transaction
            try
            {
                currentTrade = this.channelDBEntry?.TryGetTransaction<TransactionTabelContent>(nonce);
            }
            catch (TrinityLevelDBException)
            {
                return;
            }

            this.TransactionType = currentTrade?.type?.ToUpper();
            switch (this.TransactionType)
            {
                case "RSMC":
                    TransactionRsmcContent rsmc = this.channelDBEntry?.TryGetTransaction<TransactionRsmcContent>(nonce);
                    this.MonitorTxId = rsmc.monitorTxId;
                    this.commitment = rsmc.commitment;
                    this.revocableDelivery = rsmc.revocableDelivery;
                    this.breachRemedy = rsmc.breachRemedy;
                    break;

                case "HTLC":
                    TransactionHtlcContent htlc = this.channelDBEntry?.TryGetTransaction<TransactionHtlcContent>(nonce);
                    this.MonitorTxId = htlc.monitorTxId;
                    this.HCTX = htlc.HCTX;
                    this.RDTX = htlc.RDTX;
                    break;

                case "FUNDING":
                    TransactionFundingContent funding = this.channelDBEntry?.TryGetTransaction<TransactionFundingContent>(nonce);
                    this.MonitorTxId = funding.monitorTxId;
                    this.commitment = funding.commitment;
                    this.revocableDelivery = funding.revocableDelivery;
                    break;

                default:
                    return;
            }
        }

        private void TriggerForceCloseChannel()
        {
            switch(this.TransactionType)
            {
                case "RSMC":
                case "FUNDING":
                    this.BroadcastTransaction(this.commitment.originalData.txId, this.commitment.originalData.txData,
                        this.commitment.txDataSign, this.commitment.originalData.witness);
                    break;

                case "HTLC":
                    this.BroadcastTransaction(this.HCTX.originalData.txId, this.HCTX.originalData.txData, this.HCTX.txDataSign, this.HCTX.originalData.witness);
                    break;

                default:
                    return;
            }
        }

        private void TriggerRevocableTransaction(uint blockHeight)
        {
            string witness;

            switch (this.TransactionType)
            {
                case "RSMC":
                case "FUNDING":
                    witness = this.revocableDelivery.originalData.witness.Replace("{blockheight_script}", this.ConvertBlockHeightString(blockHeight));
                    this.BroadcastTransaction(this.revocableDelivery.originalData.txId, this.revocableDelivery.originalData.txData,
                        this.revocableDelivery.txDataSign, witness);
                    break;

                case "HTLC":
                    witness = this.RDTX.originalData.witness.Replace("{blockheight_script}", this.ConvertBlockHeightString(blockHeight));
                    this.BroadcastTransaction(this.RDTX.originalData.txId, this.RDTX.originalData.txData, this.RDTX.txDataSign, witness);
                    break;

                default:
                    return;
            }
        }

        private void TriggerBreachRemedyTransaction(uint blockHeight)
        {
            string witness;

            switch (this.TransactionType)
            {
                case "RSMC":
                    witness = this.breachRemedy.originalData.witness.Replace("{blockheight_script}", this.ConvertBlockHeightString(blockHeight));
                    this.BroadcastTransaction(this.breachRemedy.originalData.txId, this.breachRemedy.originalData.txData, 
                        this.breachRemedy.txDataSign, this.breachRemedy.originalData.witness);
                    break;

                case "HTLC":
                    break;

                default:
                    return;
            }
        }

        private JObject BroadcastTransaction(string txId, string txData, string peerTxDataSignarture, string witness)
        {
            string txDataSignarture = this.wallet?.Sign(txData);
            witness = witness.Replace("{signOther}", txDataSignarture).Replace("{signSelf}", peerTxDataSignarture);

            // Broadcast the transaction by calling rpc interface
            try
            {
                return NeoInterface.SendRawTransaction(txData + witness);
            }
            catch (Exception ExpInfo)
            {
                Log.Error("Broadcast transaction<txId: {0}> failed. Exceptions: {1}", txId, ExpInfo);
                return false;
            }
            
        }

        private string ConvertBlockHeightString(uint blockHeight)
        {
            return NeoInterface.BlockheightToScript(blockHeight);
        }
    }
}
