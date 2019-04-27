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
using MessagePack;

namespace Trinity.Wallets.Templates.Messages
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class SyncWalletBody
    {
        public string Publickey { get; set; }
        public string alias { get; set; }
        public string AutorCreate { get; set; }
        public string Ip { get; set; }
        public int MaxChannel { get; set; }
        public Dictionary<string, Dictionary<string, Double>> Channel { get; set; }
    }

    [MessagePackObject(keyAsPropertyName:true)]
    public class SyncWalletData
    {
        public string MessageType { get { return this.GetType().Name; } }
        public string Sender { get; set; }
        public string NetMagic { get; set; }
        public SyncWalletBody MessageBody { get; set; }
    }
}
