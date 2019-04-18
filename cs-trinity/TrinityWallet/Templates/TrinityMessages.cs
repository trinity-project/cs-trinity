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
using System.Reflection;
using System.Collections.Generic;
using MessagePack;
using Trinity.Network.TCP;

namespace Trinity.TrinityWallet.Templates
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    /// <typeparam name="TSHandler"></typeparam>
    /// <typeparam name="TFHandler"></typeparam>
    public abstract class TrinityMessages<TMessage, TSHandler, TFHandler> : IDisposable
    {
        protected TMessage Request;
        protected string Message;
        protected TrinityTcpClient TcpHandler;
        private string MessageName { get { return typeof(TMessage).Name; } }

        /// <summary>
        /// Set of virtual method
        /// </summary>
        public virtual void Dispose()
        {
        }

        /// <summary>
        /// Default Constructor
        /// </summary>
        public TrinityMessages()
        {
        }

        /// <summary>
        /// Class Constructor
        /// </summary>
        /// <param name="msg">JSon String format</param>
        public TrinityMessages(string msg)
        {
            try
            {
                this.Request =
                    MessagePackSerializer.Deserialize<TMessage>(MessagePackSerializer.FromJson(msg));
            }
            catch (Exception)
            {
                // TODO: Log system to record this error
            }
        }

        public TrinityMessages(TMessage msg)
        {
            Console.WriteLine(null == this.Request);
            this.Message =
                MessagePackSerializer.ToJson(MessagePackSerializer.Serialize(msg));
        }

        public virtual string ToJson()
        {
            return MessagePackSerializer.ToJson(MessagePackSerializer.Serialize(this.Request));
        }

        public virtual TMessage GetMessage()
        {
            return this.Request;
        }

        public virtual string GetJsonMessage()
        {
            Console.WriteLine(this.Message);
            return this.Message;
        }

        public virtual void SetTcpHandler(TrinityTcpClient client)
        {
            this.TcpHandler = client;
        }

        public virtual void MakeTransaction(TrinityTcpClient client, TMessage msg)
        {
            this.TcpHandler.SendData(this.ToJson());
        }

        public virtual void MakeTransaction(TrinityTcpClient client)
        {
            this.TcpHandler.SendData(this.Message);
        }

        protected virtual void GetMessageAttribute<TContext, TValue>(TContext context, string name, out TValue value)
        {
            value = default(TValue);

            try
            {
                PropertyInfo attr = typeof(TContext).GetProperty(name);
                value = (TValue)attr.GetValue(context);
            }
            catch (Exception ExpInfo)
            {
                // TODO: Write to file later.
                Console.WriteLine("Failed to get attribute<{0}> from {1} Message: {2}",
                    name, this.MessageName, ExpInfo);
            }
        }

        protected virtual void SetMessageAttribute<TContext, TValue>(TContext context, string name, TValue value)
        {
            try
            {
                PropertyInfo attr = typeof(TContext).GetProperty(name);
                attr.SetValue(context, value);
            }
            catch (Exception ExpInfo)
            {
                // TODO: Write to file later.
                Console.WriteLine("Failed to set attribute<{0}> to {1} Message: {2}",
                    name, this.MessageName, ExpInfo);
            }
        }

        public void GetHeaderAttribute<TValue>(string name, out TValue value)
        {
            this.GetMessageAttribute<TMessage, TValue>(this.Request, name, out value);
        }

        public void SetHeaderAttribute<TValue>(string name, TValue value)
        {
            this.SetMessageAttribute<TMessage, TValue>(this.Request, name, value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public abstract void GetBodyAttribute<TValue>(string name, out TValue value);
        public abstract void SetBodyAttribute<TValue>(string name, TValue value);
    }
}
