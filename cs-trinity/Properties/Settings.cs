using Microsoft.Extensions.Configuration;
using Neo.Network.P2P;
using System.Linq;

namespace Trinity.Properties
{
    public sealed partial class Settings
    {
        public string gatewayIP { get; private set; }
        public string gatewayPort { get; private set; }
        public string gatewayRpcPort { get; private set; }
        public string localIp { get; private set; }
        public string localPort { get; private set; }
        public uint trinityMagicMainNet { get; private set; }
        public uint trinityMagicTestNet { get; private set; }
        public static Settings Default { get; private set; }

        public Settings()
        {
            IConfigurationSection section = new ConfigurationBuilder().AddJsonFile("TrinityProtocol.json").Build().GetSection("ProtocolConfiguration");
            Default = new Settings(section);
        }

        public Settings(IConfigurationSection section)
        {
            this.gatewayIP = section.GetSection("Gateway").GetSection("ip").Value;
            this.gatewayPort = section.GetSection("Gateway").GetSection("port").Value;
            this.gatewayRpcPort = section.GetSection("Gateway").GetSection("rpcPort").Value;
            this.localIp = section.GetSection("Local").GetSection("ip").Value;
            this.localPort = section.GetSection("Local").GetSection("port").Value;
            this.trinityMagicMainNet = MagicMainNet;
            this.trinityMagicTestNet = MagicTestNet;
        }
    }
}
