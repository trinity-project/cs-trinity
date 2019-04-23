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
using Trinity.TrinityWallet.Templates.Messages;

namespace Trinity.TrinityWallet.TransferHandler.TransactionHandler
{
    ///// <summary>
    ///// Prototype for Settle / SettleSign message
    ///// </summary>
    //public class Settle : Header<SettleBody>
    //{
    //    //public Settle(string sender, string receiver, string channel, string asset, string magic, UInt64 nonce) :
    //    //    base(sender, receiver, channel, asset, magic, nonce)
    //    //{ }
    //}

    //public class SettleSign : Settle
    //{
    //    //public SettleSign(string sender, string receiver, string channel, string asset, string magic, UInt64 nonce) :
    //    //    base(sender, receiver, channel, asset, magic, nonce)
    //    //{ }
    //}

    ///// <summary>
    ///// Class Handler for handling Settle Message
    ///// </summary>
    //public class SettleHandler : TrinityTransaction<Settle, SettleSignHandler, VoidHandler>
    //{
    //    public SettleHandler(string msg) : base(msg)
    //    {
    //    }

    //    public override bool Handle()
    //    {
    //        return false;
    //    }

    //    public override void FailStep()
    //    {
    //        this.FHandler = null;

    //    }

    //    public override void SucceedStep()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    //public override void GetBodyAttribute<TValue>(string name, out TValue value)
    //    //{
    //    //    this.GetMessageAttribute<SettleBody, TValue>(this.Request.MessageBody, name, out value);
    //    //}

    //    //public override void SetBodyAttribute<TValue>(string name, TValue value)
    //    //{
    //    //    this.SetMessageAttribute<SettleBody, TValue>(this.Request.MessageBody, name, value);
    //    //}
    //}

    ///// <summary>
    ///// Class Handler for handling SettleSign Message
    ///// </summary>
    //public class SettleSignHandler : TrinityTransaction<SettleSign, VoidHandler, VoidHandler>
    //{
    //    public SettleSignHandler(string msg) : base(msg)
    //    {
    //    }

    //    public override bool Handle()
    //    {
    //        return false;
    //    }

    //    public override void FailStep()
    //    {
    //        this.FHandler = null;

    //    }

    //    public override void SucceedStep()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    //public override void GetBodyAttribute<TValue>(string name, out TValue value)
    //    //{
    //    //    this.GetMessageAttribute<SettleBody, TValue>(this.Request.MessageBody, name, out value);
    //    //}

    //    //public override void SetBodyAttribute<TValue>(string name, TValue value)
    //    //{
    //    //    this.SetMessageAttribute<SettleBody, TValue>(this.Request.MessageBody, name, value);
    //    //}
    //}
}
