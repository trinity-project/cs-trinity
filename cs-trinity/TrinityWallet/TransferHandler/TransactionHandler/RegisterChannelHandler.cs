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
using System.Text;
using System.Security.Cryptography;

using Trinity.Network.TCP;
using Trinity.BlockChain;
using Trinity.ChannelSet;
using Trinity.TrinityWallet.Templates.Messages;
using Trinity.TrinityWallet.TransferHandler;


namespace Trinity.TrinityWallet.TransferHandler.TransactionHandler
{
    /// <summary>
    /// This handler will process the message -- RegisterChannel
    /// </summary>
    public class RegisterChannelHandler : TransferHandler<RegisterChannel, FounderHandler, RegisterChannelFailHandler>
    {
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
            UInt64 nonce, double deposit)
        {
            // TODO: need to be recorded in the database?????
            this.Request.SetAttribute("ChannelName", Channel.NewChannel(sender, receiver));
            this.Request.MessageBody.SetAttribute("AssetType", asset);
            this.Request.MessageBody.SetAttribute("Deposit", deposit);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override bool Handle(string msg)
        {
            base.Handle(msg);
            if (!(this.Request is RegisterChannel))
            {
                return false;
            }

            /// TODO: need add some verification in future
            if (!this.Verify())
            {
                this.FailStep();
                return false;
            }

            this.SucceedStep();
            
            return true;
        }

        public override void FailStep()
        {
            this.FHandler = null;
            
        }

        public override void SucceedStep()
        {
            ;
        }

        private string GenerateChannelName()
        {
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            byte[] SBytes = md5.ComputeHash(Encoding.ASCII.GetBytes("" + DateTime.Now.ToString()));

            StringBuilder nb = new StringBuilder();
            for (int i = 0; i < SBytes.Length; i++)
            {
                nb.AppendFormat("{0:x2}", SBytes[i]);
            }

            byte[] RBytes = md5.ComputeHash(Encoding.ASCII.GetBytes("" + DateTime.Now.ToString()));
            for (int i = 0; i < RBytes.Length; i++)
            {
                nb.AppendFormat("{0:x2}", RBytes[i]);
            }

            return nb.ToString();
        }
    }

    public class RegisterChannelFailHandler : TransferHandler<RegisterChannel, VoidHandler, VoidHandler>
    {
        // Default Constructor
        public RegisterChannelFailHandler() : base()
        {
        }

        public override bool Handle(string msg)
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
