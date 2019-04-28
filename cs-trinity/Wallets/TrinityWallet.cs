﻿/*
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
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo;
using Neo.Wallets;

using Trinity.Wallets;
using Trinity.BlockChain;
using Trinity.Network.TCP;
using Trinity.Wallets.Templates.Messages;
using Trinity.Wallets.TransferHandler.TransactionHandler;

namespace Trinity
{
    public class TrinityWallet
    {
        // const declaration
        private const int msSleep = 1000;

        // Variable declaration
        private readonly NeoSystem neoSystem;
        private readonly Wallet neoWallet;
        private readonly KeyPair walletKey;

        private readonly TrinityTcpClient client;

        public string pubKey;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="system"></param>
        /// <param name="wallet"></param>
        /// <param name="pubKey"></param>
        public TrinityWallet(NeoSystem system, Wallet wallet, string pubKey, string ip=null, string port=null)
        {
            this.neoSystem = system;
            this.neoWallet = wallet;
            this.pubKey = pubKey;
            this.walletKey = this.neoWallet?.GetAccount(pubKey.ConvertToScriptHash()).GetKey();

            if (null != ip && null != port)
            {
                this.client = new TrinityTcpClient(ip, port);
                this.client.CreateConnetion();
            }
        }

        public void StartThread()
        {
            Thread thread = new Thread(this.Handle)
            {
                IsBackground = true,
                Name = "TrinityWallet.Channel.Transaction"
            };
            thread.Start();
            thread.Join();

            Thread recvThread = new Thread(this.Receive)
            {
                IsBackground = true,
                Name = "TrinityWallet.ReceiveMessage"
            };
            recvThread.Start();
            recvThread.Join();
        }

        public TrinityTcpClient GetClient()
        {
            return this.client;
        }
        
        public string Sign(string content)
        {
            return NeoInterface.Sign(content, this.walletKey.PrivateKey);
        }

        public bool VerifySignarture(string content, string contentSign)
        {
            return NeoInterface.VerifySignature(content, contentSign,
                this.pubKey.ConvertToScriptHash().ToArray());
        }

     #region private_method_sets
        private void Handle()
        {
            string message = null;

            // forever loop to process message
            while (true)
            {
                // Get the message from the message queue
                if (!this.client.GetMessageFromQueue(out message))
                {
                    Thread.Sleep(msSleep);
                    continue;
                }

                // parse the message header
                
                this.ProcessMessage(message);

                // Default sleep 1s
                Thread.Sleep(msSleep);
            }
        }

        private void ProcessControlMessage(string messageType, string message)
        {
            switch (messageType)
            {
                case "AckSycWallet":
                    break;
                default:
                    Console.WriteLine("Invalid MessageType: {0}", messageType);
                    break;
            }
        }

        private void ProcessMessage(string message)
        {
            TransactionHeader header = message.Deserialize<TransactionHeader>();

            // To handle the message
            switch (header.MessageType)
            {
                case "RegisterChannel":
                    new RegisterChannelHandler(message).Handle();
                    break;
                case "RegisterChannelFail":
                    new RegisterChannelFailHandler(message).Handle();
                    break;
                case "Founder":
                    new FounderHandler(message).Handle();
                    break;
                case "FounderSign":
                    new FounderSignHandler(message).Handle();
                    break;
                case "FounderFail":
                    new FounderFailHandler(message).Handle();
                    break;
                case "Rsmc":
                    // new RsmcHandler(message).Handle();
                    break;
                case "RsmcSign":
                    // new RsmcSignHandler(message).Handle();
                    break;
                case "RsmcFail":
                    // new RsmcFailHandler(message).Handle();
                    break;
                case "Htlc":
                    // new HtlcHandler(message).Handle();
                    break;
                case "HtlcSign":
                    // new HtlcSignHandler(message).Handle();
                    break;
                case "HtlcFail":
                    // new HtlcFailHandler(message).Handle();
                    break;
                case "Settle":
                    new SettleHandler(message).Handle();
                    break;
                case "SettleSign":
                    new SettleSignHandler(message).Handle();
                    break;
                case "RResponse":
                    //new RResponseHandler(message).Handle();
                    break;
                default:
                    this.ProcessControlMessage(header.MessageType, message);
                    break;
            }
        }

        private void Receive()
        {
            this.client.ReceiveMessage();
        }
    #endregion
    }
}