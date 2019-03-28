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
using Neo.IO.Json;
using Settings = Wallet.Properties.Settings1;
using Neo.Wallets;
using Newtonsoft.Json;

namespace Wallet
{
    class TrinityMessage
    {
        private TrinityTcpClient client;

        public TrinityMessage(string ip, string port)
        {
            client = new TrinityTcpClient(ip, port);
            client.createConnetion();
        }

        public static void registerKeepAlive()
        {
            JObject message = new JObject();
            message["MessageType"] = "RegisterKeepAlive";
        }

        public void syncWalletData(string publicKey)
        {
            JObject message = new JObject();
            JObject messageBody = new JObject();

            message["method"] = "SyncWalletData";
            message["MessageType"] = "SyncWallet";
            message["NetMagic"] = Settings.Default.Magic.Block + Settings.Default.Magic.Trinity;

            messageBody["Publickey"] = publicKey;
            messageBody["alias"] = Settings.Default.Config.Alias;
            messageBody["AutoCreate"] = Settings.Default.Config.AutoCreate;
            messageBody["MaxChannel"] = Settings.Default.Config.MaxChannel;
            messageBody["Channel"] = Settings.Default.Config.MaxChannel;
            messageBody["Balance"] = 100;// todo

            message["MessageBody"] = messageBody;

            client.sendData(message.ToString());
        }

        public void getChannelList()
        {
            JObject message = new JObject();
            JObject messageBody = new JObject();

            message["MessageType"] = "GetChannelList";

            messageBody["Channel"] = //
            messageBody["Wallet"] = //

            message["MessageBody"] = messageBody;

            client.sendData(message.ToString());
        }
    }

    class Message
    {
        private JObject message;
        private string message_type = null;
        private string receiver = null;
        private string sender = null;
        private string message_body = null;
        private string error = null;
        private string magic = null;

        public Message(string message)
        {
            JObject msg = (JObject)(JObject)JsonConvert.DeserializeObject(message);

            /*message = msg;
            message_type = message.get("MessageType");
            receiver = message.get("Receiver");
            sender = message.get("Sender");
            message_body = message.get("MessageBody");
            error = message.get("Error");
            magic = message.get("NetMagic");*/
        }
    }

    class RegisterMessage
    {
        public RegisterMessage(string message, WalletAccount wallet)
        {

        }
    }
}
