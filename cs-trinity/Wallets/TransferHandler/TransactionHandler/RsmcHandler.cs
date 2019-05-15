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

namespace Trinity.Wallets.TransferHandler.TransactionHandler
{
    /// <summary>
    /// Class Handler for handling Rsmc Message
    /// </summary>
    public class RsmcHandler : TransferHandler<Rsmc, RsmcSignHandler, RsmcFailHandler>
    {
        public RsmcHandler(string msg) : base(msg)
        {
        }

        public override bool Handle()
        {
            return false;
        }

        public override bool FailStep()
        {
            return base.FailStep();
        }

        public override bool SucceedStep()
        {
            return base.SucceedStep();
        }
    }

    /// <summary>
    /// Class Handler for handling RsmcSign Message
    /// </summary>
    public class RsmcSignHandler : TransferHandler<RsmcSign, RsmcHandler, RsmcFailHandler>
    {
        public RsmcSignHandler(string msg) : base(msg)
        {
        }

        public override bool Handle()
        {
            return false;
        }

        public override bool FailStep()
        {
            return base.FailStep();
        }

        public override bool SucceedStep()
        {
            return base.SucceedStep();
        }
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////
    /// RsmcFailHandler start
    ///////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Class Handler for handling RsmcFail Message
    /// </summary>
    public class RsmcFailHandler : TransferHandler<RsmcFail, VoidHandler, VoidHandler>
    {
        public RsmcFailHandler(string msg) : base(msg)
        {
        }

        public override bool Handle()
        {
            return false;
        }

        public override bool FailStep()
        {
            return base.FailStep();
        }

        public override bool SucceedStep()
        {
            return base.SucceedStep();
        }
    }
}
