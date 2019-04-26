﻿using System;
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

        public static void trinityConfigure(NeoSystem neoSystem, Wallet wallet, string publicKey)
        {
            NeoSystem = neoSystem;
            currentWallet = wallet;
            currentAccountPublicKey = publicKey;
        }

        /*
         * start up server
         * 1. monitor server
         * 2. setup tcp connection with gateway
         * 3. receiving gateway message
         */
    }
}
