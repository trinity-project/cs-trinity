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

using Trinity.ChannelSet;
using Trinity.ChannelSet.Definitions;
using Trinity.TrinityDB.Definitions;
using Trinity.Wallets.Templates.Messages;


namespace Trinity.Wallets.TransferHandler.TransactionHandler
{
    /// <summary>
    /// This handler will process the message -- RegisterChannel
    /// </summary>
    public class RegisterChannelHandler 
        : TransactionHandler<RegisterChannel, VoidTransactionMessage, RegisterChannelHandler, RegisterChannelFailHandler>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="receiver"></param>
        /// <param name="channel"></param>
        /// <param name="asset"></param>
        /// <param name="magic"></param>
        /// <param name="nonce"></param>
        /// <param name="deposit"></param>
        public RegisterChannelHandler(string sender, string receiver, string channel, string asset, string magic, 
            long deposit) : base(sender, receiver, null, asset, magic, 0, deposit)
        {
        }

        public RegisterChannelHandler(string message) : base(message)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override bool Handle()
        {
            Log.Debug("Handle Message {0}. Asset Type: {1}, Deposit: {2}.",
                this.Request.MessageType,
                this.Request.MessageBody.AssetType,
                this.Request.MessageBody.Deposit);
            return base.Handle();
        }

        public override bool FailStep(string errorCode)
        {
            Log.Info("Failed handling RegisterChannel. Channel: {0}, AssetType: {1}, Deposit: {2}. Error: {3}",
                this.Request.ChannelName, this.Request.MessageBody.AssetType, this.Request.MessageBody.Deposit, errorCode);
            return base.FailStep(errorCode);
        }

        public override bool SucceedStep()
        {
            // Add channel to database
            this.AddChannel(this.Request.Receiver, this.Request.Sender, EnumRole.PARTNER);
            Log.Info("Succeed handling RegisterChannel. Channel: {0}, AssetType: {1}, Deposit: {2}.",
                    this.Request.ChannelName, this.Request.MessageBody.AssetType, this.Request.MessageBody.Deposit);

            // trigger new Founder message
            FounderHandler founderHndl = new FounderHandler(
                this.Request.Receiver, this.Request.Sender, this.Request.ChannelName,
                this.Request.MessageBody.AssetType, this.Request.NetMagic, this.Request.MessageBody.Deposit);

            return founderHndl.MakeTransaction();
        }

        public override bool MakeTransaction()
        {
            if (base.MakeTransaction())
            {
                Log.Info("Succeed to send RegisterChannel. Channel: {0}, AssetType: {1}, Deposit: {2}.",
                    this.Request?.ChannelName, this.Request?.MessageBody.AssetType, this.Request?.MessageBody.Deposit);

                // Add channel to database
                this.AddChannel(this.Request.Sender, this.Request.Receiver);
                return true;
            }

            return false;
        }

        public override bool Verify()
        {
            this.VerifyDeposit(this.Request.MessageBody.Deposit);

            return true;
        }

        public void AddChannel(string uri, string peerUri, EnumRole role= EnumRole.FOUNDER)
        {
            ChannelTableContent content = new ChannelTableContent
            {
                channel = this.Request.ChannelName,
                asset = this.assetId.ToAssetType(this.GetAssetMap()),
                uri = uri,
                peer = peerUri,
                magic = this.Request.NetMagic,
                role = role.ToString(),
                state = EnumChannelState.INIT.ToString(),
                alive = 0,

                deposit = this.Request.MessageBody.Deposit,
                peerDeposit = this.Request.MessageBody.Deposit,

                balance = this.Request.MessageBody.Deposit,
                peerBalance = this.Request.MessageBody.Deposit
            };
            this.GetChannelLevelDbEntry().AddChannel(this.Request.ChannelName, content);
        }

        #region RegisterChannel_OVERRIDE_VIRUAL_SETS_OF_DIFFERENT_TRANSACTION_HANDLER
        public override void InitializeAssetType(bool useCurrentRequest = false)
        {
            this.assetId = this.Request.MessageBody.AssetType.ToAssetId(this.GetAssetMap());
        }

        public override void InitializeMessage(string sender, string receiver, string channel, string asset, string magic, ulong nonce)
        {
            // create a new channel
            if (null == channel)
            {
                channel = Channel.NewChannel(sender, receiver);
            }

            base.InitializeMessage(sender, receiver, channel, asset, magic, nonce);
        }
        public override void InitializeMessageBody(string asset, long payment, int role = 0, string hashcode = null, string rcode = null)
        {
            this.Request.MessageBody = new RegisterChannelBody
            {
                AssetType = asset,
                Deposit = payment,
            };
        }
        // Not need BlockChain API for this RegisterChannel
        public override void InitializeBlockChainApi() { }

        public override RegisterChannelFailHandler CreateResponseHndl(string errorCode = "Ok")
        {
            return new RegisterChannelFailHandler(this.Request);
        }

        #endregion //RegisterChannel_OVERRIDE_VIRUAL_SETS_OF_DIFFERENT_TRANSACTION_HANDLER
    }

    public class RegisterChannelFailHandler 
        : TransactionHandler<RegisterChannelFail, RegisterChannel, RegisterChannelHandler, RegisterChannelFailHandler>
    {
        // Default Constructor
        public RegisterChannelFailHandler() : base()
        {
        }

        public RegisterChannelFailHandler(RegisterChannel request) : base(request)
        {
            this.Request.MessageBody = new RegisterChannelFailBody
            {
                OriginalMessage = request.MessageBody
            };
        }

        public RegisterChannelFailHandler(string message) : base(message)
        {
        }

        public void DeleteChannel()
        {
            ChannelTableContent content = this.GetChannelLevelDbEntry().TryGetChannel(this.Request.ChannelName);
            if (null != content)
            {
                // Delete this channel from LevelDB
                this.GetChannelLevelDbEntry().DeleteChannel(this.Request.ChannelName);
            }
        }

        public override bool FailStep(string errorCode)
        {
            return true;
        }

        public override bool SucceedStep()
        {
            this.DeleteChannel();
            return true;
        }

        #region RegisterChannelFail_OVERRIDE_VIRUAL_SETS_OF_DIFFERENT_TRANSACTION_HANDLER
        // Not need BlockChain API for this RegisterChannel
        public override void InitializeBlockChainApi() { }
        #endregion //RegisterChannelFail_OVERRIDE_VIRUAL_SETS_OF_DIFFERENT_TRANSACTION_HANDLER
    }
}
