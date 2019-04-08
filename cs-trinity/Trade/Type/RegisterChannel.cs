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
using Trinity.Trade;
using MessagePack;

namespace Trinity.Trade.Type
{
    /// <summary>
    /// Prototype for register channel message
    /// </summary>
    class RegisterMessage : TransactionHeader
    {
        public RegisterChannelBody MessageBody { get; set; }
    }

    class RegisterChannel
    {
        public RegisterMessage message;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        public RegisterChannel(string msg)
        {
            try
            {
                byte[] MsgBytes = MessagePackSerializer.FromJson(msg);
                this.message = MessagePackSerializer.Deserialize<RegisterMessage>(MsgBytes);
            }
            catch (Exception)
            {
                // TODO: Log system to record this error
            }
        }

        public string Handle()
        {
            if (null == this.message)
            {
                return null;
            }

            switch (this.message.MessageType)
            {
                case "RegisterChannel":
                    this.ActionRegister();
                    break;
                case "RegisterChannelFail":
                    this.ActionRegisterFail();
                    break;
                default:
                    break;
            }
            return null;
        }

        private string ActionRegister()
        {

            return null;
        }

        private string ActionRegisterFail()
        {
            return null;
        }
    }
}
