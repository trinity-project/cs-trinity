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

using Neo.IO.Json;
using Neo.Wallets;
using Neo.IO.Data.LevelDB;

using Trinity.TrinityWallet;
using Trinity.TrinityDB.Definitions;
using Trinity.BlockChain;
using Trinity.TrinityWallet.Templates.Definitions;
using Trinity.TrinityWallet.Templates.Messages;

namespace Trinity.TrinityWallet.TransferHandler.TransactionHandler
{
    /// <summary>
    /// Class Handler for handling Founder Message
    /// </summary>
    public class FounderHandler : TransferHandler<Founder, FounderSignHandler, FounderFailHandler>
    {
        private readonly double Deposit;
        private readonly UInt64 Nonce;

        public int RoleIndex;
        public JObject fundingTx;
        public JObject commTx;
        public JObject rdTx;

        public FounderHandler() : base()
        {
            this.Request = new Founder
            {
                MessageBody = new FounderBody()
                {
                    Founder = new FundingTx(),
                    Commitment = new CommitmentTx(),
                    RevocableDelivery = new RevocableDeliveryTx()
                }
            };
        }
        

        public FounderHandler(string sender, string receiver, string channel, string asset, 
            string magic, UInt64 nonce, double deposit, int role=0) : base()
        {
            this.Request = new Founder
            {
                Sender = sender,
                Receiver = receiver,
                ChannelName = channel,
                AssetType = asset,
                NetMagic = magic,
                TxNonce = nonce,

                MessageBody = new FounderBody
                {
                    AssetType = asset,
                    Deposit = deposit,
                    RoleIndex = role,
                    Founder = new FundingTx(),
                    Commitment = new CommitmentTx(),
                    RevocableDelivery = new RevocableDeliveryTx()
                }
            };

            this.ParsePubkeyPair(sender, receiver);
            this.SetChannelInterface(sender, receiver, channel, asset);
        }

        public FounderHandler(string message) : base(message)
        {
            this.ParsePubkeyPair(this.header.Receiver, this.header.Sender);
            this.SetChannelInterface(this.Request.Receiver, this.Request.Sender,
                this.Request.ChannelName, this.Request.MessageBody.AssetType);
        }

        public override bool Handle()
        {
            if (!base.Handle())
            {
                return false;
            }

            // MessageType is not Founder
            if (!(this.Request is Founder))
            {
                return false;
            }

            // judge the role
            //if (this.IsRole0())

            return true;
        }

        public override void FailStep()
        {
            this.FHandler = null;

        }

        public override void SucceedStep()
        {
            throw new NotImplementedException();
        }

        public override bool Verify()
        {
            return true;
        }

        private bool SignFundingTx()
        {
            // Sign C1A / C1B by role index
            if (this.IsRole0(this.RoleIndex))
            {
                string deposit = this.Request.MessageBody.Deposit.ToString();
                // Because this is triggered by the RegisterChannel, the founder of this channel is value of Receiver;
                this.fundingTx = Funding.createFundingTx(this.GetPubKey(), deposit,
                    this.GetPeerPubKey(), deposit, this.Request.MessageBody.AssetType.ToAssetId());
                return true;
            }
            else if (this.IsRole1(this.RoleIndex))
            {
                // TODO: Read from the database
                Slice content = this.GetChannelInterface().GetTransaction(this.Request.TxNonce);
                if (default != content)
                {
                    TransactionTabelContens transactionContent = content.ToString().Deserialize<TransactionTabelContens>();
                    this.fundingTx["txData"] = transactionContent.founder.originalData.txData;
                    this.fundingTx["txId"] = transactionContent.founder.originalData.txId;
                    this.fundingTx["witness"] = transactionContent.founder.originalData.witness;
                    this.fundingTx["addressFunding"] = transactionContent.founder.originalData.addressFunding;
                    this.fundingTx["scriptFunding"] = transactionContent.founder.originalData.scriptFunding;
                    return true;
                }
                else {
                    this.fundingTx = null;
                }

                return false;
            }
            else
            {
                // TODO: error LOG
            }

            return false;
        }

        public void SignAndSetMessageAttribute()
        {
            string deposit = this.Request.MessageBody.Deposit.ToString();

            if (!this.SignFundingTx())
            {
                return;
            }
            
            this.commTx = Funding.createCTX(this.fundingTx["addressFunding"].ToString(), deposit,
                deposit, this.GetPubKey(), this.GetPeerPubKey(),
                this.fundingTx["scriptFunding"].ToString(), this.Request.MessageBody.AssetType.ToAssetId());

            string address = this.GetPubKey().ToScriptHash().ToAddress();
            this.rdTx = Funding.createRDTX(this.commTx["addressRSMC"].ToString(), address,
                this.Request.MessageBody.Deposit.ToString(), this.commTx["txId"].ToString(),
                this.commTx["scriptRSMC"].ToString(), this.Request.MessageBody.AssetType.ToAssetId());

            this.Request.MessageBody.Founder.SetAttribute("txId", this.fundingTx["txId"].ToString());
            this.Request.MessageBody.Founder.SetAttribute("txData", this.fundingTx["txData"].ToString());
            this.Request.MessageBody.Founder.SetAttribute("addressRSMC", this.fundingTx["addressFunding"].ToString());
            this.Request.MessageBody.Founder.SetAttribute("scriptRSMC", this.fundingTx["scriptFunding"].ToString());
            this.Request.MessageBody.Founder.SetAttribute("witness", this.fundingTx["witness"].ToString());

            this.Request.MessageBody.Commitment.SetAttribute("txId", this.commTx["txId"].ToString());
            this.Request.MessageBody.Commitment.SetAttribute("txData", this.commTx["txData"].ToString());
            this.Request.MessageBody.Commitment.SetAttribute("addressRSMC", this.commTx["addressRSMC"].ToString());
            this.Request.MessageBody.Commitment.SetAttribute("scriptRSMC", this.commTx["scriptRSMC"].ToString());
            this.Request.MessageBody.Commitment.SetAttribute("witness", this.commTx["witness"].ToString());
            
            this.Request.MessageBody.RevocableDelivery.SetAttribute("txId", this.rdTx["txId"].ToString());
            this.Request.MessageBody.RevocableDelivery.SetAttribute("txData", this.rdTx["txData"].ToString());
            this.Request.MessageBody.RevocableDelivery.SetAttribute("witness", this.rdTx["witness"].ToString());

            // record the item to database
            this.AddTransaction();
        }

        public void AddTransaction()
        {

            TransactionTabelContens transactionContent = new TransactionTabelContens
            {
                nonce = this.Request.TxNonce,
                monitorTxId = this.commTx["txId"].ToString(),
                founder = new FundingSignTx
                {
                    originalData = new FundingTx
                    {
                        txData = this.fundingTx["txData"].ToString(),
                        txId = this.fundingTx["txId"].ToString(),
                        witness = this.fundingTx["witness"].ToString(),
                        addressFunding = this.fundingTx["addressFunding"].ToString(),
                        scriptFunding = this.fundingTx["scriptFunding"].ToString()
                    },
                },
                commitment = new CommitmentSignTx
                {
                    originalData = new CommitmentTx
                    {
                        txData = this.commTx["txData"].ToString(),
                        txId = this.commTx["txId"].ToString(),
                        witness = this.commTx["witness"].ToString(),
                        addressRSMC = this.commTx["addressRSMC"].ToString(),
                        scriptRSMC = this.commTx["scriptRSMC"].ToString(),
                    },
                },
                revocableDelivery = new RevocableDeliverySignTx
                {
                    originalData = new RevocableDeliveryTx
                    {
                        txData = this.rdTx["txData"].ToString(),
                        txId = this.rdTx["txId"].ToString(),
                        witness = this.rdTx["witness"].ToString(),
                    }
                }
            };

            this.GetChannelInterface().AddTransaction(this.Request.TxNonce, transactionContent);
        }
    }

    /// <summary>
    /// Class Handler for handling FounderSign Message
    /// </summary>
    public class FounderSignHandler : TransferHandler<FounderSign, VoidHandler, VoidHandler>
    {
        public FounderSignHandler(string sender, string receiver, string channel, string asset,
            string magic, UInt64 nonce, double deposit, int role = 0)
        {
            this.Request.MessageBody.SetAttribute("AssetType", asset);
            this.Request.MessageBody.SetAttribute("Deposit", deposit);
            this.Request.MessageBody.SetAttribute("RoleIndex", role);

            this.ParsePubkeyPair(sender, receiver);
            this.SetChannelInterface(sender, receiver, channel, asset);
        }

        public FounderSignHandler(string message) : base(message)
        {
            this.ParsePubkeyPair(this.header.Receiver, this.header.Sender);
            this.SetChannelInterface(this.Request.Receiver, this.Request.Sender,
                this.Request.ChannelName, this.Request.MessageBody.AssetType);
        }

        public override bool Handle(string msg)
        {
            base.Handle(msg);
            return false;
        }

        public override void FailStep()
        {
            this.FHandler = null;

        }

        public override void SucceedStep()
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Class Handler for handling FounderFail Message
    /// </summary>
    public class FounderFailHandler : TransferHandler<FounderFail, VoidHandler, VoidHandler>
    {
        public FounderFailHandler(string sender, string receiver, string channel, string asset,
            string magic, UInt64 nonce, double deposit, int role = 0)
        {
            this.Request.MessageBody.SetAttribute("AssetType", asset);
            this.Request.MessageBody.SetAttribute("Deposit", deposit);
            this.Request.MessageBody.SetAttribute("RoleIndex", role);

            this.ParsePubkeyPair(sender, receiver);
            this.SetChannelInterface(sender, receiver, channel, asset);
        }

        public FounderFailHandler(string message) : base(message)
        {
            this.ParsePubkeyPair(this.header.Receiver, this.header.Sender);
            this.SetChannelInterface(this.Request.Receiver, this.Request.Sender,
                this.Request.ChannelName, this.Request.MessageBody.AssetType);
        }

        public override bool Handle(string msg)
        {
            base.Handle(msg);
            return false;
        }

        public override void FailStep()
        {
            this.FHandler = null;

        }

        public override void SucceedStep()
        {
            throw new NotImplementedException();
        }
    }
}
