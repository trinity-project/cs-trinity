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
using Trinity.TrinityWallet.TransferHandler;


namespace Trinity.Trade.TransactionType
{
    /// <summary>
    /// Prototype for Htlc / HtlcSign / HtlcFail message
    /// </summary>
    public class Htlc : Header<HtlcBody>
    {
        //public Htlc(string sender, string receiver, string channel, string asset, string magic, UInt64 nonce, string value) :
        //    base(sender, receiver, channel, asset, magic, nonce)
        //{
        //    this.MessageBody.AssetType = asset;
        //    this.MessageBody.Count = value;
        //}
    }

    public class HtlcSign : Htlc
    {
        //public HtlcSign(string sender, string receiver, string channel, string asset, string magic, UInt64 nonce, string value) :
        //    base(sender, receiver, channel, asset, magic, nonce, value)
        //{ }
    }

    public class HtlcFail : Htlc
    {
        //public HtlcFail(string sender, string receiver, string channel, string asset, string magic, UInt64 nonce, string value) :
        //    base(sender, receiver, channel, asset, magic, nonce, value)
        //{ }
    }

    /// <summary>
    /// Class Handler for handling Htlc Message
    /// </summary>
    public class HtlcHandler : TrinityTransaction<Htlc, HtlcSignHandler, HtlcFail>
    {
        public HtlcHandler(string msg) : base(msg)
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

        public override string GetBodyAttribute(string name)
        {
            return this.GetMessageAttribute<HtlcBody>(this.Request.MessageBody, name);
        }

        public override void SetBodyAttribute(string name, string value)
        {
            this.SetMessageAttribute<HtlcBody, string>(this.Request.MessageBody, name, value);
        }

        public override void SetBodyAttribute(string name, UInt64 value)
        {
            this.SetMessageAttribute<HtlcBody, UInt64>(this.Request.MessageBody, name, value);
        }
    }

    /// <summary>
    /// Class Handler for handling HtlcSign Message
    /// </summary>
    public class HtlcSignHandler : TrinityTransaction<HtlcSign, HtlcHandler, HtlcFailHandler>
    {
        public HtlcSignHandler(string msg) : base(msg)
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
    }

    /// <summary>
    /// Class Handler for handling HtlcFail Message
    /// </summary>
    public class HtlcFailHandler : TrinityTransaction<HtlcFail, VoidHandler, VoidHandler>
    {
        public HtlcFailHandler(string msg) : base(msg)
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
    }
}
