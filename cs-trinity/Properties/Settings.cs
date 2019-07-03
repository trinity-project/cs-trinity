using Microsoft.Extensions.Configuration;
using Neo.Network.P2P;

using System;
using System.Linq;
using System.Collections.Generic;

namespace Trinity.Properties
{
    public sealed partial class Settings
    {
        public string gatewayIP { get; private set; }
        public string gatewayPort { get; private set; }
        public string gatewayRpcPort { get; private set; }
        public string localIp { get; private set; }
        public string localPort { get; private set; }
        public string alias { get; private set; }
        public string autoCreate { get; private set; }
        public uint maxChannel { get; private set; }
        public uint trinityMagicMainNet { get; private set; }
        public uint trinityMagicTestNet { get; private set; }
        public IReadOnlyDictionary<string, Dictionary<string, double>> channelFees { get; private set; }

        public static Settings Default { get; private set; }

        public Settings()
        {
            IConfigurationSection section = new ConfigurationBuilder().AddJsonFile("TrinityProtocol.json").Build().GetSection("ProtocolConfiguration");
            Default = new Settings(section);
        }

        private Settings(IConfigurationSection section)
        {
            this.gatewayIP = section.GetSection("Gateway").GetSection("ip").Value;
            this.gatewayPort = section.GetSection("Gateway").GetSection("port").Value;
            this.gatewayRpcPort = section.GetSection("Gateway").GetSection("rpcPort").Value;
            this.localIp = section.GetSection("Local").GetSection("ip").Value;
            this.localPort = section.GetSection("Local").GetSection("port").Value;

            this.alias = section.GetSection("Alias").Value;
            this.autoCreate = section.GetSection("AutoCreate").Value;
            this.maxChannel = uint.Parse(section.GetSection("ChannelSettings").GetSection("MaxChannel").Value);
            this.trinityMagicMainNet = MagicMainNet;
            this.trinityMagicTestNet = MagicTestNet;

            this.channelFees = section.GetSection("ChannelFees").GetChildren().ToDictionary(p => p.Key,
                p => section.GetSection("ChannelFees").GetSection(p.Key).GetChildren().ToDictionary(v => v.Key, v => double.Parse(v.Value)));
        }
    }
}
