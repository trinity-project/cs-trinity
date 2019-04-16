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
using Trinity.Trade.Tempates.Definitions;
using Trinity.Network.TCP;


namespace Trinity.Trade.TransactionType
{
    /// <summary>
    /// Prototype for register channel message
    /// </summary>
    public class RegisterChannel : Header<RegisterBody>
    {
        public RegisterChannel(string sender, string receiver, string channel, string asset, string magic, UInt64 nonce, string value) :
            base(sender, receiver, channel, asset, magic, nonce)
        {
            this.MessageBody.AssetType = asset;
            this.MessageBody.Deposit = value;
        }
    }

    public class RegisterChannelFail : RegisterChannel
    {
        public RegisterChannelFail(string sender, string receiver, string channel, string asset, string magic, UInt64 nonce, string value) :
            base(sender, receiver, channel, asset, magic, nonce, value)
        {
            this.MessageBody.OriginalMessage = value;
        }
    }

    /// <summary>
    /// This handler will process the message -- RegisterChannel
    /// </summary>
    public class RegisterChannelHandler : TrinityTransaction<RegisterChannel, FounderHandler, RegisterChannelFailHandler>
    {
        // Default Constructor
        public RegisterChannelHandler(string msg): base(msg)
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
            UInt64 nonce, string deposit) : base(sender, receiver, channel, asset, magic, nonce)
        {
            this.ChannelName = this.GenerateChannelName();
            // TODO: need to be recorded in the database?????
            
            this.Request.ChannelName = this.ChannelName;
            this.Request.MessageBody.AssetType = asset;
            this.Request.MessageBody.Deposit = deposit;
        }

        public override void MakeTransaction(TrinityTcpClient client)
        {
            // send the RegisterChannel Message to the peer.
            base.MakeTransaction(client);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override bool Handle()
        {
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
            this.SHandler = new FounderHandler(this.Message);
            this.SHandler.MakeTransaction(this.TcpHandler);
        }

        private string GenerateChannelName()
        {
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            byte[] SBytes = md5.ComputeHash(Encoding.ASCII.GetBytes(this.Sender + DateTime.Now.ToString()));

            StringBuilder nb = new StringBuilder();
            for (int i = 0; i < SBytes.Length; i++)
            {
                nb.AppendFormat("{0:x2}", SBytes[i]);
            }

            byte[] RBytes = md5.ComputeHash(Encoding.ASCII.GetBytes(this.Receiver + DateTime.Now.ToString()));
            for (int i = 0; i < RBytes.Length; i++)
            {
                nb.AppendFormat("{0:x2}", RBytes[i]);
            }

            return nb.ToString();
        }
    }

    public class RegisterChannelFailHandler : TrinityTransaction<RegisterChannel, VoidHandler, VoidHandler>
    {
        // Default Constructor
        public RegisterChannelFailHandler(string msg) : base(msg)
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
