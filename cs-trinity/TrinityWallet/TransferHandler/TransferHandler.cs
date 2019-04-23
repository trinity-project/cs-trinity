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
using Trinity.Network.TCP;

namespace Trinity.TrinityWallet.TransferHandler
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    /// <typeparam name="TSHandler"></typeparam>
    /// <typeparam name="TFHandler"></typeparam>
    public abstract class TransferHandler<TMessage, TSHandler, TFHandler> : IDisposable
    {
        protected TMessage Request;
        private string MessageName => typeof(TMessage).Name;
        protected TSHandler SHandler;
        protected TFHandler FHandler;

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

        public virtual void SucceedStep() { }
        public virtual void FailStep() { }

        /// <summary>
        /// Dispose method to release memory
        /// </summary>
        public virtual void Dispose()
        {
        }

        /// <summary>
        /// Default Constructor
        /// </summary>
        public TransferHandler()
        {
        }

        public virtual TValue GetHeaderValue<TValue>(string name)
        {
            return this.Request.GetAttribute<TMessage, TValue>(name);
        }

        public virtual void SetBodyAttribute<TValue>(string name, TValue value) {}
        public virtual void GetBodyAttribute<TContext>(string name) { }

        public virtual void MakeTransaction(TrinityTcpClient client)
        {
            client?.SendData(this.Request.Serialize());
        }

        public string ToJson()
        {
            return this.Request.Serialize();
        }

        public virtual bool Handle(string msg)
        {
            if (null != msg) {
                this.Request = msg.Deserialize<TMessage>();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Trinity Transaction Role define here.
        /// </summary>
        private readonly int Role_0 = 0;
        private readonly int Role_1 = 1;
        private readonly int Role_2 = 2;
        private readonly int Role_3 = 3;
        private readonly int Role_Fail = -1;

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
    }
}
