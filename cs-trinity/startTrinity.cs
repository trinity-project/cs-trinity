using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Neo;
using Neo.Wallets;
using Trinity.BlockChain;
using Trinity.Properties;

namespace Trinity
{
    public static class startTrinity
    {
        public static Wallet currentWallet = null;
        public static string currentAccountPublicKey = "";
        public static NeoSystem NeoSystem;

        public static TrinityWallet trinityWallet;

        public static void trinityConfigure(NeoSystem neoSystem, Wallet wallet, string publicKey, string magic, string ip=null, string port=null)
        {
            NeoSystem = neoSystem;
            currentWallet = wallet;
            currentAccountPublicKey = publicKey;

            ip = ip is null ? Settings.Default.gatewayIP : ip;
            port = port is null ? Settings.Default.gatewayPort : port;

            trinityWallet = new TrinityWallet(neoSystem, wallet, publicKey, magic, ip, port);

            MonitorTransction monitorTransction = new MonitorTransction(publicKey, ip, port, magic);
            Thread thread = new Thread(monitorTransction.monitorBlock);
            thread.Start();
        }

        /*
         * start up server
         * 1. monitor server
         * 2. setup tcp connection with gateway
         * 3. receiving gateway message
         */
    }
}
