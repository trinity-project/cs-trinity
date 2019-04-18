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
using Trinity.Trade.Tempates.Definitions;
using Trinity.Trade.Tempates;
using Trinity.BlockChain;
using Trinity.TrinityWallet.TransferHandler;
using Neo.IO.Json;

namespace Trinity.Trade.TransactionType
{
    /// <summary>
    /// Prototype for Founder / FounderSign / FounderFail message
    /// </summary>
    public class Founder : Header<FounderBody>
    {
    }

    public class FounderSign : Founder
    {
    }

    public class FounderFail : Founder
    {
    }

    /// <summary>
    /// Class Handler for handling Founder Message
    /// </summary>
    public class FounderHandler : TrinityTransaction<Founder, FounderSignHandler, FounderFailHandler>
    {
        public int RoleIndex;
        public JObject FundingTx;

        public FounderHandler(string msg) : base(msg)
        {
        }

        public FounderHandler(string sender, string receiver, string channel, string asset, 
            string magic, string deposit, int role=0) : base()
        {
            this.Request = new Founder
            {
                Sender = sender,
                Receiver = receiver,
                ChannelName = channel,
                AssetType = asset,
                NetMagic = magic,
                TxNonce = 0,
                MessageBody = new FounderBody
                {
                    AssetType = asset,
                    Deposit = deposit,
                    RoleIndex = role
                },
            };
        }

        public override bool Handle()
        {
            if (!this.SignFundingTx())
            {
                return false;
            }


            return true;
        }

        public override void FailStep()
        {
            this.FHandler = null;

        }

        public override void SucceedStep()
        {
            throw new NotImplementedException();
        }

        public override bool Verify()
        {
            return true;
        }

        public bool SignFundingTx()
        {
            // Sign C1A / C1B by role index
            if (this.IsFounderRoleZero(this.RoleIndex))
            {
                // Because this is triggered by the RegisterChannel, the founder of this channel is value of Receiver;
                this.FundingTx = Funding.createFundingTx(this.Receiver, "0", this.Sender, "0", this.AssetType);
                return true;
            }
            else if (this.IsPartnerRoleOne(this.RoleIndex))
            {
                // TODO: Read from the database
                this.FundingTx = null;
                return true;
            }
            else
            {
                // TODO: error LOG
            }

            return false;
        }

        public override void GetBodyAttribute<TValue>(string name, out TValue value)
        {
            this.GetMessageAttribute<FounderBody, TValue>(this.Request.MessageBody, name, out value);
        }

        public override void SetBodyAttribute<TValue>(string name, TValue value)
        {
            this.SetMessageAttribute<FounderBody, TValue>(this.Request.MessageBody, name, value);
        }
    }

    /// <summary>
    /// Class Handler for handling FounderSign Message
    /// </summary>
    public class FounderSignHandler : TrinityTransaction<FounderSign, VoidHandler, VoidHandler>
    {
        public FounderSignHandler(string msg) : base(msg)
        {
        }

        public override bool Handle()
        {
            return false;
        }

        public override void FailStep()
        {
            this.FHandler = null;

        }

        public override void SucceedStep()
        {
            throw new NotImplementedException();
        }

        public override void GetBodyAttribute<TValue>(string name, out TValue value)
        {
            this.GetMessageAttribute<FounderBody, TValue>(this.Request.MessageBody, name, out value);
        }

        public override void SetBodyAttribute<TValue>(string name, TValue value)
        {
            this.SetMessageAttribute<FounderBody, TValue>(this.Request.MessageBody, name, value);
        }
    }

    /// <summary>
    /// Class Handler for handling FounderFail Message
    /// </summary>
    public class FounderFailHandler : TrinityTransaction<FounderFail, VoidHandler, VoidHandler>
    {
        public FounderFailHandler(string msg) : base(msg)
        {
        }

        public override bool Handle()
        {
            return false;
        }

        public override void FailStep()
        {
            this.FHandler = null;

        }

        public override void SucceedStep()
        {
            throw new NotImplementedException();
        }

        public override void GetBodyAttribute<TValue>(string name, out TValue value)
        {
            this.GetMessageAttribute<FounderBody, TValue>(this.Request.MessageBody, name, out value);
        }

        public override void SetBodyAttribute<TValue>(string name, TValue value)
        {
            this.SetMessageAttribute<FounderBody, TValue>(this.Request.MessageBody, name, value);
        }
    }
}
