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
using System.Linq;
using System.Collections.Generic;

using Neo;
using Trinity.BlockChain;
using Trinity.Network.TCP;
using Trinity.Wallets.Templates.Definitions;
using Trinity.Wallets.Templates.Messages;
using Trinity.Exceptions;


namespace Trinity.Wallets.TransferHandler
{
    /// <summary>
    /// Not implementation currently.... ??? define some mandatory interface for avoiding error ???
    /// </summary>
    public interface ITransferInterface
    {

    }

    /// <summary>
    /// 
    /// </summary>
    public class TransferHandlerBase
    {
        // Private variables list
        private readonly TrinityWallet wallet;  // current opened wallet context
        private string pubKey;
        private string peerPubKey;

        /// <summary>
        /// Default Constructor
        /// </summary>
        public TransferHandlerBase()
        {
            this.wallet = startTrinity.trinityWallet;
        }

        public virtual bool Handle() { return true; }
        public virtual bool SucceedStep() { return true; }
        public virtual bool FailStep(string errorCode) { return true; }

        public virtual bool MakeTransaction() { return true; }
        public virtual bool MakeupMessage() { return true; }
        public virtual void MakeRequest(int role) { }
        public virtual void MakeResponse(string errorCode = "Ok") { }
        public virtual void ExtraSucceedAction() { }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TValue"> Trinity transaction basic type </typeparam>
        /// <param name="txContent"> Content which is used to signed </param>
        /// <returns></returns>
        public virtual TxContentsSignGeneric<TValue> MakeupSignature<TValue>(TValue txContent)
            where TValue : TxContents
        {
            return new TxContentsSignGeneric<TValue>
            {
                txDataSign = this.Sign(txContent.txData),
                originalData = txContent
            };
        }

        public TrinityTcpClient GetClient()
        {
            return this.wallet?.GetClient();
        }

        public string GetNetMagic()
        {
            return this.wallet?.GetNetMagic();
        }

        public UInt160 GetPublicKeyHash()
        {
            return this.wallet?.GetPublicKeyHash();
        }

        public string GetWalletPublicKey()
        {
            return this.wallet?.GetPublicKey();
        }

        public string GetPubKey()
        {
            return this.pubKey;
        }

        public string GetPeerPubKey()
        {
            return this.peerPubKey;
        }

        public TrinityWallet GetWallet()
        {
            return this.wallet;
        }

        public Dictionary<string, string> GetAssetMap()
        {
            return this.wallet?.GetAssetMap();
        }

        public bool IsMainNet()
        {
            return TrinityWallet.IsMainnet();
        }

        public void ParsePubkeyPair(string uri, string peerUri)
        {
            this.pubKey = uri?.Split('@').First();
            this.peerPubKey = peerUri?.Split('@').First();
        }

        /// <summary>
        /// Sign the transaction body.
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public string Sign(string content)
        {
            return this.wallet?.Sign(content);
        }

        // Verification method sets
        public virtual bool Verify() { return true; }
    }

    /// <summary>
    /// Generic class for transfer messages
    /// </summary>
    /// <typeparam name="TMessage"> Messages which is used to sent to remote </typeparam>
    /// <typeparam name="TRMessage"> Messages which is received from remote and trigger next step </typeparam>
    /// <typeparam name="TRQHandler"> Transfer Handler which is used to handle the request message </typeparam>
    /// <typeparam name="TRSPHandler"> Transfer Handler withc is used to handle the response message </typeparam>
    public abstract class TransferHandler<TMessage, TRMessage, TRQHandler, TRSPHandler> : TransferHandlerBase, IDisposable
        where TMessage : HeaderBase, new()
        where TRMessage : HeaderBase
        where TRQHandler : TransferHandlerBase
        where TRSPHandler : TransferHandlerBase
    {
        protected TMessage Request;
        protected TRMessage onGoingRequest; // Record the Received TRMessage if needed
        protected TRQHandler RQHandler;
        protected TRSPHandler RSPHandler;

        /// <summary>
        /// Default Constructor
        /// </summary>
        public TransferHandler() : base()
        {
        }

        public TransferHandler(string message) : base()
        {
            this.Request = message.Deserialize<TMessage>();
        }

        public TransferHandler(TRMessage requestMessage) : base()
        {
        }

        /// <summary>
        /// Dispose method to release memory
        /// </summary>
        public virtual void Dispose()
        {
        }

        /// <summary>
        /// Handle the messages from the remote.
        /// </summary>
        /// <returns></returns>
        public override bool Handle()
        {
            // MessageType is incompatible with the Transfer Handler
            if (!(this.Request is TMessage))
            {
                return false;
            }

            try
            {
                // Verify the messages
                if (this.Verify())
                {
                    this.SucceedStep();
                    return true;
                }
            }
            catch (TrinityException ExpInfo)
            {
                Log.Fatal("{0}: failed since trinity internal error. System error: {1}.",
                    this.Request.MessageType, ExpInfo.Message);
                this.FailStep(ExpInfo.GetErrorString());
                throw new TrinityException(ExpInfo.HResult);
            }
            catch (Exception ExpInfo)
            {
                Log.Fatal("{0}: failed since error found by system. System error: {1}.", 
                    this.Request.MessageType, ExpInfo.Message);
                this.FailStep(ExpInfo.ToString());
                throw new Exception(ExpInfo.Message);
            }
            
            return false;
        }

        public override bool FailStep(string errorCode)
        {
            this.MakeResponse(errorCode);
            return true;
        }

        /// <summary>
        /// Send the Request to remote
        /// </summary>
        /// <returns></returns>
        public override bool MakeTransaction()
        {
            if (this.MakeupMessage() && this.GetClient().SendData(this.Request.Serialize()))
            {
                return true;
            }
            else
            {
                Log.Error("Fail to send {0}.", this.Request.MessageType);
            }
            return false;
        }

        public override void MakeResponse(string errorCode = "Ok")
        {
            this.RSPHandler = this.CreateResponseHndl(errorCode);
            this.RSPHandler?.MakeTransaction();
        }

        public override void MakeRequest(int role=0)
        {
            this.RQHandler = this.CreateRequestHndl(role);
            this.RQHandler?.MakeTransaction();
        }

        /// <summary>
        /// Trigger next message by new instances
        /// </summary>
        /// <returns></returns>
        public virtual TRQHandler CreateRequestHndl(int role) { return null; }
        public virtual TRSPHandler CreateResponseHndl(string errorCode="Ok") { return null; }

        public string ToJson()
        {
            return this.Request.Serialize();
        }

        /// <summary>
        /// Virtual Method sets. Should be overwritten in child classes.
        /// </summary>
        /// <returns></returns>
        /// Verification method sets
        public virtual bool VerifyNetMagic(string magic)
        {
            return null != magic && magic.Equals(this.Request.NetMagic);
        }

        public virtual bool VerifyUri()
        {
            return this.Request.Receiver.Contains(this.GetWalletPublicKey());
        }

        public virtual bool VerifyAssetType(string assetType)
        {
            return true;
        }
    }
}
