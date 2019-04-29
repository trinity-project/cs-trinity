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
using System.Text;
using System.Security.Cryptography;

using Trinity.ChannelSet;
using Trinity.ChannelSet.Definitions;
using Trinity.TrinityDB.Definitions;
using Trinity.Wallets.Templates.Messages;


namespace Trinity.Wallets.TransferHandler.TransactionHandler
{
    /// <summary>
    /// This handler will process the message -- RegisterChannel
    /// </summary>
    public class RegisterChannelHandler : TransferHandler<RegisterChannel, FounderHandler, RegisterChannelFailHandler>
    {
        private readonly double Deposit;
        private readonly string NetMagic;

        // Default Constructor
        public RegisterChannelHandler(): base()
        {
        }

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
            double deposit) : base()
        {
            this.Deposit = deposit;
            this.NetMagic = magic ?? this.GetNetMagic();

            if (null == channel)
            {
                channel = Channel.NewChannel(sender, receiver);
            }

            this.Request = new RegisterChannel
            {
                Sender = sender,
                Receiver = receiver,
                ChannelName = channel,
                AssetType = asset,
                NetMagic = this.NetMagic,
                MessageBody = new RegisterChannelBody
                {
                    AssetType = asset,
                    Deposit = deposit,
                }
            };

            this.ParsePubkeyPair(sender, receiver);
            this.SetChannelInterface(sender, receiver, channel, asset);
        }

        public RegisterChannelHandler(string message) : base(message)
        {
            this.ParsePubkeyPair(this.Request.Receiver, this.Request.Sender);
            this.SetChannelInterface(this.Request.Receiver, this.Request.Sender,
                this.Request.ChannelName, this.Request.MessageBody.AssetType);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override bool Handle()
        {
            return base.Handle();
        }

        public override bool FailStep()
        {
            this.FHandler = new RegisterChannelFailHandler(
                this.Request.Receiver, this.Request.Sender, this.Request.ChannelName,
                this.Request.MessageBody.AssetType, this.Request.NetMagic, this.Request.MessageBody);
            this.FHandler.MakeTransaction(this.GetClient());

            ChannelTableContent content = new ChannelTableContent
            {
                uri = this.Request.Receiver,
                peer = this.Request.Sender,
                asset = this.Request.MessageBody.AssetType,
                channel = this.Request.ChannelName,
                state = EnumChannelState.ERROR.ToString(),
            };
            this.GetChannelInterface().AddChannel(this.Request.ChannelName, content);

            return true;
        }

        public override bool SucceedStep()
        {
            this.SHandler = new FounderHandler(
                this.Request.Receiver, this.Request.Sender, this.Request.ChannelName,
                this.Request.MessageBody.AssetType, this.Request.NetMagic, 0, this.Request.MessageBody.Deposit);
            this.SHandler.MakeTransaction(this.GetClient());

            // Add channel to database
            this.AddChannel(this.Request.Receiver, this.Request.Sender, EnumRole.PARTNER);

            return true;
        }

        public override void MakeTransaction()
        {
            this.AddChannel(this.Request.Sender, this.Request.Receiver);
            base.MakeTransaction();
        }

        public void AddChannel(string uri, string peerUri, EnumRole role = EnumRole.PARTNER)
        {
            ChannelTableContent content = new ChannelTableContent
            {
                channel = this.Request.ChannelName,
                asset = this.Request.MessageBody.AssetType,
                uri = uri,
                peer = peerUri,
                magic = this.Request.NetMagic,
                role = role.ToString(),
                state = EnumChannelState.INIT.ToString(),
                alive = 0,
                deposit = new Dictionary<string, double> {
                    { uri, this.Request.MessageBody.Deposit},
                    { peerUri, this.Request.MessageBody.Deposit},
                },
                balance = new Dictionary<string, double> {
                    { uri, this.Request.MessageBody.Deposit},
                    { peerUri, this.Request.MessageBody.Deposit},
                }
            };
            this.GetChannelInterface().AddChannel(this.Request.ChannelName, content);
        }
    }

    public class RegisterChannelFailHandler : TransferHandler<RegisterChannelFail, VoidHandler, VoidHandler>
    {
        // Default Constructor
        public RegisterChannelFailHandler() : base()
        {
        }

        public RegisterChannelFailHandler(string sender, string receiver, string channel, string asset,
            string magic, RegisterChannelBody original) : base()
        {
            this.Request = new RegisterChannelFail
            {
                Sender = sender,
                Receiver = receiver,
                ChannelName = channel,
                AssetType = asset,
                NetMagic = magic,
                MessageBody = new RegisterChannelFailBody
                {
                    OriginalMessage = original
                }
            };

            this.ParsePubkeyPair(sender, receiver);
            this.SetChannelInterface(sender, receiver, channel, asset);
        }

        public RegisterChannelFailHandler(string message) : base(message)
        {
            this.ParsePubkeyPair(this.Request.Receiver, this.Request.Sender);
            this.SetChannelInterface(this.Request.Receiver, this.Request.Sender,
                this.Request.ChannelName, this.Request.MessageBody.OriginalMessage.AssetType);
        }

        public override bool Handle()
        {
            if (!base.Handle())
            {
                return false;
            }

            if (!(this.Request is RegisterChannelFail))
            {
                return false;
            }
            else
            {
                this.UpdateChannel();
                Console.WriteLine("Failed to register channel {0}", this.Request.ChannelName);
            }
            return false;
        }

        public void UpdateChannel()
        {
            ChannelTableContent content = this.GetChannelInterface().TryGetChannel(this.Request.ChannelName);
            if (null != content)
            {
                content.state = EnumChannelState.ERROR.ToString();
                this.GetChannelInterface().UpdateChannel(this.Request.ChannelName, content);
            }
        }

        public override bool FailStep()
        {
            return false;
        }

        public override bool SucceedStep()
        {
            return true;
        }
    }
}
