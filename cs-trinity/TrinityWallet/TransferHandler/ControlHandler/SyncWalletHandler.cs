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
using Trinity.TrinityWallet.Templates.Messages;
using Trinity.TrinityWallet.TransferHandler;

namespace Trinity.TrinityWallet.TransferHandler.ControlHandler
{
    public class SyncWalletHandler : TransferHandler<SyncWalletData, VoidHandler, VoidHandler>
    {
        public SyncWalletHandler() : base()
        {
            this.Request = new SyncWalletData
            {
                MessageBody = new SyncWalletBody()
            };
        }

        public SyncWalletHandler(string sender, string magic)
        {
            this.Request = new SyncWalletData
            {
                Sender = sender,
                NetMagic = magic,

                MessageBody = new SyncWalletBody()
            };
        }

        public override void SetBodyAttribute<TValue>(string name, TValue value)
        {
            this.Request.MessageBody.SetAttribute(name, value);
        }

        //public override void GetBodyAttribute<TValue>(string name, out TValue value)
        //{
        //    this.GetMessageAttribute<SyncWalletBody, TValue>(this.Request.MessageBody, name, out value);
        //}

        //public override void SetBodyAttribute<TValue>(string name, TValue value)
        //{
        //    this.SetMessageAttribute<SyncWalletBody, TValue>(this.Request.MessageBody, name, value);
        //}
    }
}
