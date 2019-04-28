using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo;
using Neo.Wallets;

namespace Trinity
{
    public static class startTrinity
    {
        public static Wallet currentWallet = null;
        public static string currentAccountPublicKey = "";
        public static NeoSystem NeoSystem;

        public static TrinityWallet trinityWallet;

        public static void trinityConfigure(NeoSystem neoSystem, Wallet wallet, string publicKey, string ip=null, string port=null)
        {
            NeoSystem = neoSystem;
            currentWallet = wallet;
            currentAccountPublicKey = publicKey;

            trinityWallet = new TrinityWallet(neoSystem, wallet, publicKey, TrinityWalletConfig.ip, TrinityWalletConfig.port);
        }

        /*
         * start up server
         * 1. monitor server
         * 2. setup tcp connection with gateway
         * 3. receiving gateway message
         */
    }
}
