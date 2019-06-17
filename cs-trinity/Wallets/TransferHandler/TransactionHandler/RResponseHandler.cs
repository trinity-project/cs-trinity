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
using Trinity.BlockChain;
using Trinity.Wallets.Templates.Messages;
using Trinity.TrinityDB.Definitions;

namespace Trinity.Wallets.TransferHandler.TransactionHandler
{
    /// <summary>
    /// Class Handler for handling RResponse Message
    /// </summary>
    public class RResponseHandler : TransactionHandler<RResponse, RResponse, RResponseHandler, RResponseHandler>
    {
        public RResponseHandler(string sender, string receiver, string channel, string asset,
            string magic, long payment, string hashcode, string rcode)
            : base(sender, receiver, channel, asset, magic, 0, payment, 0, hashcode, rcode)
        { }

        public RResponseHandler(string message) : base(message)
        {
        }

        public override bool FailStep(string errorCode)
        {
            return base.FailStep(errorCode);
        }

        public override bool SucceedStep()
        {
            // TODO: update the Htlc Locked pair content to level db
            TransactionTabelHLockPair currentHlock = this.GetHtlcLockTrade();
            currentHlock.rcode = this.Request.MessageBody.R;
            this.GetChannelLevelDbEntry()?.UpdateTransactionHLockPair(this.Request.MessageBody.HR, currentHlock);

            // Trigger Htlc to Rsmc
            RsmcHandler rsmcHndl = new RsmcHandler(this.Request.Receiver, this.Request.Sender,
                this.Request.ChannelName, this.Request.MessageBody.AssetType, this.Request.NetMagic,
                0, this.Request.MessageBody.Count);
            rsmcHndl.MakeTransaction();

            // Trigger RResponse to previous peer
            if (null != this.GetHtlcLockTrade()?.incomeChannel)
            {
                this.MakeRequest();
            }

            return true;
        }

        #region RResponse_OVERRIDE_VIRUAL_SETS_OF_DIFFERENT_TRANSACTION_HANDLER
        public override void InitializeMessageBody(string asset, long payment, int role = 0, string hashcode = null, string rcode = null)
        {
            this.Request.MessageBody = new RResponseBody
            {
                AssetType = asset,
                Count = payment,
                HR = hashcode,
                R = rcode
            };
        }

        public override void InitializeMessage(string sender, string receiver, string channel, string asset, string magic, ulong nonce)
        {
            base.InitializeMessage(sender, receiver, channel, asset, magic, nonce);
        }

        public override void SetLocalsFromBody()
        {
            this.HashR = this.onGoingRequest.MessageBody.HR;
        }

        public override RResponseHandler CreateRequestHndl(int role=0)
        {
            ChannelTableContent incomeChannel = this.GetChannelLevelDbEntry()?.TryGetChannel(this.GetHtlcLockTrade()?.incomeChannel);
            return new RResponseHandler(this.onGoingRequest.Receiver, incomeChannel.peer, incomeChannel.channel,
                this.onGoingRequest.MessageBody.AssetType, this.onGoingRequest.NetMagic, this.GetHtlcLockTrade().income,
                this.onGoingRequest.MessageBody.HR, this.onGoingRequest.MessageBody.R);
        }
        #endregion
    }
}
