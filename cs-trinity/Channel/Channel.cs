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
        private string founder = null;
        private string partner = null;
        private string founder_pubkey = null;
        private string founder_address = null;
        private string partner_pubkey = null;
        private string partner_address = null;
        private string asset_type = null;
        private string channel_name = null;
        private Dictionary<string, JObject> deposit;

        public Channel(string _founder, string _partner)
        {
            founder = _founder;
            partner = _partner;
            //founder_pubkey = founder.split("@")[0];
            //founder_address = pubkey_to_address(founder_pubkey);
            //partner_pubkey = partner.split("@")[0];
            //partner_address = pubkey_to_address(partner_pubkey);
        }

        public static string[] getChannel(string address1, string address2)
        {
            string[] channel = { "test" };
            return channel;
        }

        private string init_channle_name()
        {
            string encodeStr = founder + partner + DateTime.Now.ToString();
            byte[] sourcebytes = Encoding.Default.GetBytes(encodeStr);
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] hashedbytes = md5.ComputeHash(sourcebytes);
            return BitConverter.ToString(hashedbytes).Replace("-", "");
        }

        public string create(string _assetType, UInt16 _deposit, string comment = "", string _channel_name = "")
        {
            JObject message = new JObject(); 
            JObject messageBody = new JObject();
            JObject subitem = new JObject();
            JObject deposit = new JObject();

            /*
            string[] ch = Channel.getChannel(founder_pubkey, partner_pubkey);
            if (ch.Length != 0)
            {
                Console.WriteLine("the channel have exist");
                return null;
            }

            if (0 <= _deposit)
            {
                Console.WriteLine("Could not trigger register channel because of illegal deposit:{0}", _deposit.ToString());
            }
            */

            asset_type = _assetType;            
            subitem[asset_type] = _deposit;
            deposit[founder] = subitem;
            deposit[partner] = subitem;

            channel_name = _channel_name == "" ? init_channle_name() : _channel_name;

            //result = APIChannel.add_channel(channel_name, founder, partner, EnumChannelState.INIT.name, 0, deposit, Channel.magic_number);

            message["MessageType"] = "RegisterChannel";
            message["Sender"] = founder;
            message["Receiver"] = partner;
            message["ChannelName"] = channel_name;
            message["NetMagic"] = "12345";//TODO : get Net Magic;

            messageBody["AssetType"] = _assetType;
            messageBody["Deposit"] = deposit;
            messageBody["Comments"] = comment;

            message["MessageBody"] = messageBody;

            return message.ToString();

            //TODO  send the message to gateway
        }
    }
}