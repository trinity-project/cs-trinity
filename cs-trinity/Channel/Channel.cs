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
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace Trinity.Channel
{
    class Channel
    {
        private string Founder;
        private string Partner = null;
        private string FounderPubKey = null;
        private string FounderAddress = null;
        private string PartnerPubKey = null;
        private string PartnerAddress = null;
        private string AssetType = null;
        private string ChannelName = null;
        private Dictionary<string, Dictionary<string, double>> Deposit;
        private Dictionary<string, Dictionary<string, double>> Balance;

        public Channel(string founder, string partner)
        {
            this.Founder = founder;
            this.Partner = partner;
        }

        public Channel(string ChannelName)
        {
            this.ChannelName = ChannelName;
        }

        public static string[] GetChannel(string founder, string partner)
        {
            string[] channel = { "test" };
            return channel;
        }

        public string NewChannel()
        {
            string encodeStr = this.Founder + this.Partner + DateTime.Now.ToString();
            byte[] sourcebytes = Encoding.Default.GetBytes(encodeStr);
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] hashedbytes = md5.ComputeHash(sourcebytes);
            return BitConverter.ToString(hashedbytes).Replace("-", "");
        }

        public void Commit()
        {
            
        }
    }
}