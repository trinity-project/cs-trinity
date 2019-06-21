/*
Author: Trinity Core Team

MIT License

Copyright (c) 2018 Trinity

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
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
using Trinity.Wallets.Templates.Definitions;
using Trinity.Wallets.Templates.Messages;
using Trinity.ChannelSet;
using Trinity.ChannelSet.Definitions;
using Trinity.TrinityDB.Definitions;
using Trinity.Exceptions.WalletError;


namespace Trinity.Wallets.TransferHandler.ControlHandler
{
    /// <summary>
    /// Generic Class for handling the Control plane plane messages.
    /// Parameters details refer to the description in TransferHandler 
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    /// <typeparam name="TRMessage"></typeparam>
    /// <typeparam name="TRQHandler"></typeparam>
    /// <typeparam name="TRSPHanlder"></typeparam>
    public abstract class ControlHandler<TMessage, TRMessage, TRQHandler, TRSPHanlder>
        : TransferHandler<TMessage, TRMessage, TRQHandler, TRSPHanlder>
        where TMessage : ControlHeader, new()
        where TRMessage : ControlHeader
        where TRQHandler : TransferHandlerBase
        where TRSPHanlder : TransferHandlerBase
    {
        /// <summary>
        /// Default Constructor
        /// </summary>
        public ControlHandler() : base()
        {
        }

        public ControlHandler(string message) : base(message)
        {
        }

        public ControlHandler(string sender, string receiver, string asset, string magic)
        {
            this.Request = new TMessage
            {
                Sender = sender,
                Receiver = receiver,
                AssetType = asset,
                NetMagic = magic ?? this.GetNetMagic()
            };
        }
    }
}
