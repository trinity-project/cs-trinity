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

using Neo.Cryptography;

using Trinity.BlockChain;
using Trinity.Wallets.Templates.Messages;
using Trinity.TrinityDB.Definitions;
using Trinity.Exceptions.WalletError;

namespace Trinity.Wallets.TransferHandler.TransactionHandler
{
    /// <summary>
    /// Class Handler for handling RResponse Message
    /// </summary>
    public class RResponseHandler : TransactionHandler<RResponse, RResponse, RResponseHandler, RResponseHandler>
    {
        // current transaction by nonce
        private TransactionTabelHLockPair currentHLock = null;

        public RResponseHandler(string sender, string receiver, string channel, string asset,
            string magic, UInt64 nonce, long payment, string hashcode, string rcode)
            : base(sender, receiver, channel, asset, magic, nonce, payment, 0, hashcode, rcode)
        {
            // Get record of htlc locked payment
            this.currentHLock = this.GetHLockPair();
        }

        public RResponseHandler(string message) : base(message)
        {
            // Get record of htlc locked payment
            this.currentHLock = this.GetHLockPair();
        }
        
        public override bool FailStep(string errorCode)
        {
            Log.Error("Failed to handle RResponse. HashR: {0}, RCode: {1}",
                this.Request.MessageBody.HR, this.Request.MessageBody.R);
            return base.FailStep(errorCode);
        }

        public override bool SucceedStep()
        {
            // Check the Hash Lock Pair
            this.CheckHashLockPair();

            // update the current htlc lock payment transaction by hash
            this.currentHLock.rcode = this.Request.MessageBody.R;
            this.GetChannelLevelDbEntry()?.UpdateTransactionHLockPair(this.Request.MessageBody.HR, currentHLock);

            // Trigger Htlc to Rsmc
            if (null != this.Request.ChannelName && currentHLock.paymentChannel == this.Request.ChannelName)
            {
                RsmcHandler rsmcHndl = new RsmcHandler(this.Request.Receiver, this.Request.Sender,
                    this.Request.ChannelName, this.Request.MessageBody.AssetType, this.Request.NetMagic,
                    0, this.Request.MessageBody.Count, this.HashR);
                rsmcHndl.MakeTransaction();
            }

            Log.Info("Succeed to handle RResponse. HashR: {0}, RCode: {1}",
                this.Request.MessageBody.HR, this.Request.MessageBody.R);

            // Trigger RResponse to previous remote
            if (null != currentHLock?.incomeChannel)
            {
                ChannelTableContent nextChannel = this.GetChannelLevelDbEntry()?.GetChannel(currentHLock?.incomeChannel);
                RResponseHandler RResponseHndl = 
                    new RResponseHandler(this.Request.Receiver, nextChannel.peer, nextChannel.channel,
                this.Request.MessageBody.AssetType, this.Request.NetMagic, this.Request.TxNonce, currentHLock.income,
                this.Request.MessageBody.HR, this.Request.MessageBody.R);
                RResponseHndl.MakeTransaction();
            }

            return true;
        }

        public override bool MakeTransaction()
        {
            if (base.MakeTransaction())
            {
                Log.Info("Succeed to send RResponse to Payer. Payer: {0}", this.Request.Receiver);
                return true;
            }

            return false;
        }

        private void CheckHashLockPair()
        {
            if (null == this.Request.MessageBody.HR || null == this.Request.MessageBody.R)
            {
                throw new TransactionException(EnumTransactionErrorCode.NullReferrence_Hash_Lock_Pair,
                    string.Format("HashR or Rcode is null. HashR: {0}, Rcode: {1}",
                        this.Request.MessageBody.HR, this.Request.MessageBody.R),
                    EnumTransactionErrorBase.RRESPONSE.ToString());
            }

            // Check the HashR-R pair
            if (this.Request.MessageBody.R.Sha1() != this.Request.MessageBody.HR)
            {
                throw new TransactionException(EnumTransactionErrorCode.Imcompatible_Hash_Lock_Pair_For_Transaction,
                    string.Format("HashR or Rcode is null. HashR: {0}, Rcode: {1}",
                        this.Request.MessageBody.HR, this.Request.MessageBody.R),
                    EnumTransactionErrorBase.RRESPONSE.ToString());
            }

        }

        #region RResponse_OVERRIDE_VIRUAL_SETS_OF_DIFFERENT_TRANSACTION_HANDLER
        public override void InitializeAssetType(bool useCurrentRequest = false)
        {
            this.assetId = this.Request.MessageBody.AssetType.ToAssetId(this.GetAssetMap());
        }

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

        public override void SetLocalsFromBody()
        {
            this.HashR = this.Request.MessageBody.HR;
        }

        #endregion
    }
}
