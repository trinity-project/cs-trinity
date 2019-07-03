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

using Trinity.Properties;
using Trinity.Wallets.Templates.Messages;
using Trinity.Wallets.TransferHandler;

namespace Trinity.Wallets.TransferHandler.ControlHandler
{
    public class SyncWalletHandler : ControlHandler<SyncWalletData, VoidControlMessage, SyncWalletHandler, VoidHandler>
    {
        private string localAddress = null;
        public SyncWalletHandler() : base()
        {
            this.Request = new SyncWalletData
            {
                MessageBody = new SyncWalletBody()
            };
        }

        public SyncWalletHandler(string sender, string magic, string localIp="localhost", string port="21556")
            : base(sender, null, null, magic)
        {
            this.Request.MessageBody = new SyncWalletBody
            {
                Channel = new Dictionary<string, Dictionary<string, double>>(),
            };

            this.localAddress = string.Format("{0}:{1}", localIp, port);
        }

        public override bool MakeupMessage()
        {
            this.SetPublicKey(this.GetPubKey());
            this.SetAlias(Settings.Default.alias);
            this.SetAutoCreate(Settings.Default.autoCreate);
            this.SetNetAddress(string.Format("{0}:{1}", Settings.Default.localIp, Settings.Default.localPort));
            this.SetMaxChannel(Settings.Default.maxChannel);
            this.SetChannelInfo();

            return base.MakeupMessage();
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

        public void SetMaxChannel(uint MaxChannel)
        {
            this.Request.MessageBody.SetAttribute("MaxChannel", MaxChannel);
        }

        public void SetChannelInfo()
        {
            // 
            Dictionary<string, Dictionary<string, double>> ChannelInfo = new Dictionary<string, Dictionary<string, double>>();

            foreach (string assetType in this.GetAssetMap().Keys)
            {
                if (Settings.Default.channelFees.ContainsKey(assetType))
                {
                    Dictionary<string, double> InfoItem = new Dictionary<string, double>(Settings.Default.channelFees[assetType]);
                    ChannelInfo.Add(assetType, InfoItem);
                }
                else
                {
                    ChannelInfo.Add(assetType, new Dictionary<string, double>(Settings.Default.channelFees["Nep5"]));
                }
            }

            this.Request.MessageBody.SetAttribute("Channel", ChannelInfo);
        }
    }
}
