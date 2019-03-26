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
using Microsoft.Extensions.Configuration;
using Neo;

namespace Wallet.Properties
{
    internal sealed partial class Settings1
    {
        public GatewaySettings Gateway { get; }
        public ChannelFreeSettings ChannelFree { get; }
        public ConfigSettings Config { get; }
        public MagicSettings Magic { get; }
        public AssetTypeSettings AssetType { get; }

        public Settings1()
        {
            if (NeedUpgrade)
            {
                Upgrade();
                NeedUpgrade = false;
                Save();
            }

            IConfigurationSection section = new ConfigurationBuilder().AddJsonFile("C:\\Neo\\trinityConfig.json").Build().GetSection("ApplicationConfiguration");
            this.Gateway = new GatewaySettings(section.GetSection("Gateway"));
            this.ChannelFree = new ChannelFreeSettings(section.GetSection("ChannelFree"));
            this.Config = new ConfigSettings(section.GetSection("Config"));
            this.Magic = new MagicSettings(section.GetSection("Magic"));
            this.AssetType = new AssetTypeSettings(section.GetSection("AssetType"));
        }

        internal class GatewaySettings
        {
            public string Address { get; }
            public string Port { get; }

            public GatewaySettings(IConfigurationSection section)
            {
                this.Address = string.Format(section.GetSection("Address").Value);
                this.Port = string.Format(section.GetSection("Port").Value);
            }
        }

        internal class ChannelFreeSettings
        {
            public float NEO { get; }
            public float GAS { get; }

            public ChannelFreeSettings(IConfigurationSection section)
            {
                this.NEO = float.Parse(section.GetSection("NEO").Value);
                this.GAS = float.Parse(section.GetSection("GAS").Value);
            }
        }

        internal class ConfigSettings
        {
            public string Alias { get; }
            public bool AutoCreate { get; }
            public int MaxChannel { get; }
            

            public ConfigSettings(IConfigurationSection section)
            {
                this.Alias = string.Format(section.GetSection("Alias").Value);
                this.AutoCreate = bool.Parse(section.GetSection("AutoCreate").Value);
                this.MaxChannel = int.Parse(section.GetSection("MaxChannel").Value);
            }
        }

        internal class MagicSettings
        {
            public int Block { get; }
            public int Trinity { get; }

            public MagicSettings(IConfigurationSection section)
            {
                this.Block = int.Parse(section.GetSection("Block").Value);
                this.Trinity = int.Parse(section.GetSection("Trinity").Value);
            }
        }
        internal class AssetTypeSettings
        {
            public string NEO { get; }
            public string GAS { get; }

            public AssetTypeSettings(IConfigurationSection section)
            {
                this.NEO = string.Format(section.GetSection("NEO").Value);
                this.GAS = string.Format(section.GetSection("GAS").Value);
            }
        }

    }
}
