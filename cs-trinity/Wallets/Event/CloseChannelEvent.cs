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
            if (this.currentChannelSummary.type.Equals(EnumTransactionType.SETTLE.ToString()))
            {
                currentNonce -= 1;
            }

            // get current transaction
            this.GetTransactionContentByNonce(currentNonce);

            // trigger to force closing channel
            this.TriggerForceCloseChannel();
        }

        public void AddRevocableEvent()
        {

        }

        public void TriggerRevocableEvent()
        {

        }

        public void TriggerBreachRemedyEvent()
        {

        }

        private void UpdateChannelState(EnumChannelState state)
        {
            // Update the channel state
            this.currentChannel.state = state.ToString();
            this.channelDBEntry?.UpdateChannel(channelName, this.currentChannel);
        }

        private void GetTransactionContentByNonce(UInt64 nonce)
        {
            // get current transaction
            TransactionTabelContent currentTrade = this.channelDBEntry?.TryGetTransaction<TransactionTabelContent>(nonce);
            this.TransactionType = currentTrade?.type?.ToUpper();
            switch (this.TransactionType)
            {
                case "RSMC":
                    TransactionRsmcContent rsmc = this.channelDBEntry?.TryGetTransaction<TransactionRsmcContent>(nonce);
                    this.commitment = rsmc.commitment;
                    this.revocableDelivery = rsmc.revocableDelivery;
                    this.breachRemedy = rsmc.breachRemedy;
                    break;

                case "HTLC":
                    TransactionHtlcContent htlc = this.channelDBEntry?.TryGetTransaction<TransactionHtlcContent>(nonce);
                    this.HCTX = htlc.HCTX;
                    this.RDTX = htlc.RDTX;
                    break;

                case "FUNDING":
                    TransactionFundingContent funding = this.channelDBEntry?.TryGetTransaction<TransactionFundingContent>(nonce);
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
                    this.BroadcastTransaction(this.commitment.originalData.txData, this.commitment.txDataSign, this.commitment.originalData.witness);
                    break;

                case "HTLC":
                    this.BroadcastTransaction(this.HCTX.originalData.txData, this.HCTX.txDataSign, this.HCTX.originalData.witness);
                    break;

                default:
                    return;
            }
        }

        private void TriggerRevocableTransaction()
        {
            switch (this.TransactionType)
            {
                case "RSMC":
                case "FUNDING":
                    this.BroadcastTransaction(this.revocableDelivery.originalData.txData, this.revocableDelivery.txDataSign, this.revocableDelivery.originalData.witness);
                    break;

                case "HTLC":
                    this.BroadcastTransaction(this.RDTX.originalData.txData, this.RDTX.txDataSign, this.RDTX.originalData.witness);
                    break;

                default:
                    return;
            }
        }

        private void TriggerBreachRemedyTransaction()
        {
            switch (this.TransactionType)
            {
                case "RSMC":
                    this.BroadcastTransaction(this.breachRemedy.originalData.txData, this.breachRemedy.txDataSign, this.breachRemedy.originalData.witness);
                    break;

                case "HTLC":
                    break;

                default:
                    return;
            }
        }

        private JObject BroadcastTransaction(string txData, string peerTxDataSignarture, string witness)
        {
            string txDataSignarture = this.wallet?.Sign(txData);
            witness = witness.Replace("{signOther}", txDataSignarture).Replace("{signSelf}", peerTxDataSignarture);

            // Broadcast the transaction by calling rpc interface
            return NeoInterface.SendRawTransaction(txData + witness);
        }

    }
}
