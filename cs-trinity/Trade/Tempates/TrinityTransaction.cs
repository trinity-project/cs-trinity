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
using Trinity.TrinityWallet.Templates;

namespace Trinity.Trade.Tempates
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TMessage">Type of Transaction Message</typeparam>
    /// <typeparam name="TSHandler">Type of Transaction Handler When handling TMessage successfully</typeparam>
    /// <typeparam name="TFHandler">Type of Transaction Handler When handling Tmessage failed</typeparam>
    public abstract class TrinityTransaction<TMessage, TSHandler, TFHandler> : TrinityMessages<TMessage, TSHandler, TFHandler>
    {
        protected TSHandler SHandler;
        protected TFHandler FHandler;

        /// <summary>
        /// 
        /// </summary>
        public string MessageType;
        public string Sender;
        public string Receiver;
        public string ChannelName;
        public string AssetType;
        public string NetMagic;
        public string TxNonce;
        public string Router;
        public string Next;
        public string Error;
        public string Comments;

        /// <summary>
        /// Public const variable definitions.
        /// </summary>
        public const int Role_0 = 0;
        public const int Role_1 = 1;
        public const int Role_2 = 2;
        public const int Role_3 = 3;
        public const int Role_Fail = -1;

        /// <summary>
        /// Some virtual methods prototype implemented later.
        /// </summary>
        public abstract bool Handle();
        public abstract void SucceedStep();
        public abstract void FailStep();

        /// <summary>
        /// 
        /// </summary>
        public virtual TMessage MessageObject { get { return this.Request; } }
        public virtual string JsonMessage { get { return this.Message; } }

        /// <summary>
        /// Virtual Method sets. Should be overwritten in child classes.
        /// </summary>
        /// <returns></returns>
        /// Verification method sets
        public virtual bool Verify() { return true; }
        public virtual bool VerifyNonce() { return true; }
        public virtual bool VerifySignature() { return true; }
        public virtual bool VerifyBalance() { return true; }
        public virtual bool VerifyNetMagic() { return true; }

        // Sinarture Method Sets
        public virtual void SetCommitment(string commitment) { }
        public virtual void SetRecovableCommitment(string commitment) { }

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="msg"></param>
        public TrinityTransaction() : base()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="msg"></param>
        public TrinityTransaction(string msg) : base(msg)
        {
            // parse the header information
            this.GetHeader();
        }

        /// <summary>
        /// Constructor for handle interface between trinity dll and gui
        /// </summary>
        /// <param name="msg"></param>
        //public TrinityTransaction(string sender, string receiver, string channel, string asset, string magic, UInt64 nonce) : base()
        //{
        //    this.Sender = sender;
        //    this.Receiver = receiver;
        //    this.ChannelName = channel;
        //    this.AssetType = asset;
        //    this.NetMagic = magic;
        //    this.TxNonce = nonce.ToString();

        //    // to set the message header
        //    this.SetMessageHeader();
        //}

        /// <summary>
        /// Constructor for sending TMessage out to peer.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public TrinityTransaction(TMessage msg) : base(msg)
        {
        }

        public bool IsFailRole(int Role)
        {
            return Role_0 > Role || Role_3 < Role;
        }

        public bool IsFounderRoleZero(int role)
        {
            return Role_0 == role;
        }

        public bool IsPartnerRoleZero(int role)
        {
            return Role_1 == role;
        }

        public bool IsFounderRoleOne(int role)
        {
            return Role_2 == role;
        }

        public bool IsPartnerRoleOne(int role)
        {
            return Role_3 == role;
        }
        
        /// <summary>
        /// Parse the header information of the message
        /// </summary>
        public virtual void GetHeader()
        {
            this.GetHeaderAttribute("MessageType", out this.MessageType);
            this.GetHeaderAttribute("Sender", out this.Sender);
            this.GetHeaderAttribute("Receiver", out this.Receiver);

            this.GetHeaderAttribute("ChannelName", out this.ChannelName);
            this.GetHeaderAttribute("AssetType", out this.AssetType);

            this.GetHeaderAttribute("NetMagic", out this.NetMagic);
            this.GetHeaderAttribute("TxNonce", out this.TxNonce);

            this.GetHeaderAttribute("Router", out this.Router);
            this.GetHeaderAttribute("Next", out this.Next);
            this.GetHeaderAttribute("Error", out this.Error);
            this.GetHeaderAttribute("Comments", out this.Comments);
        }

        public virtual void SetHeader()
        {
            this.SetHeaderAttribute<string>("Sender", this.Sender);
            this.SetHeaderAttribute<string>("Receiver", this.Receiver);
            this.SetHeaderAttribute<string>("ChannelName", this.ChannelName);
            this.SetHeaderAttribute<string>("AssetType", this.AssetType);
            this.SetHeaderAttribute<string>("NetMagic", this.NetMagic);
            this.SetHeaderAttribute<UInt64>("TxNonce", Convert.ToUInt64(this.TxNonce));
        }

        
    }
}
