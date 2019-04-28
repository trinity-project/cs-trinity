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
using Trinity.Wallets.Templates.Messages;
using Trinity.Wallets.TransferHandler;

namespace Trinity.Wallets.TransferHandler.ControlHandler
{
    public class SyncWalletHandler : TransferHandler<SyncWalletData, VoidHandler, VoidHandler>
    {
        public SyncWalletHandler() : base()
        {
            this.Request = new SyncWalletData
            {
                MessageBody = new SyncWalletBody()
            };
        }

        public SyncWalletHandler(string sender, string magic)
        {
            this.Request = new SyncWalletData
            {
                Sender = sender,
                NetMagic = magic,

                MessageBody = new SyncWalletBody
                {
                    Channel = new Dictionary<string, Dictionary<string, double>>(),
                }
            };
        }

        public void SetPublicKey(string key)
        {
            this.Request.MessageBody.SetAttribute("Publickey", key);
        }

        public void SetAlias(string alias)
        {
            this.Request.MessageBody.SetAttribute("alias", alias);
        }

        public void SetAutoCreate(string AutoCreate)
        {
            this.Request.MessageBody.SetAttribute("AutorCreate", AutoCreate);
        }

        public void SetNetAddress(string address)
        {
            this.Request.MessageBody.SetAttribute("Ip", address);
        }

        public void SetMaxChannel(int MaxChannel)
        {
            this.Request.MessageBody.SetAttribute("MaxChannel", MaxChannel);
        }

        public void SetChannelInfo()
        {
            // TODO: here the value is read from the configuration files, this code will be update later
            // Currently ,we hardcode this value
            Dictionary<string, Dictionary<string, Double>> ChannelInfo = new Dictionary<string, Dictionary<string, Double>>();
            Dictionary<string, Double> InfoItem = new Dictionary<string, Double>();
            InfoItem.Add("TNC", 10);
            ChannelInfo.Add("Balance", InfoItem);

            InfoItem.Clear();
            InfoItem.Add("Fee", 0);
            ChannelInfo.Add("NEO", InfoItem);

            InfoItem.Clear();
            InfoItem.Add("Fee", 0.001);
            ChannelInfo.Add("GAS", InfoItem);

            InfoItem.Clear();
            InfoItem.Add("Fee", 0.01);
            InfoItem.Add("CommitMinDeposit", 1);
            InfoItem.Add("CommitMaxDeposit", 5000);
            ChannelInfo.Add("TNC", InfoItem);

            this.Request.MessageBody.SetAttribute("Channel", ChannelInfo);
        }

        public override void SetBodyAttribute<TValue>(string name, TValue value)
        {
            this.Request.MessageBody.SetAttribute(name, value);
        }
    }
}
