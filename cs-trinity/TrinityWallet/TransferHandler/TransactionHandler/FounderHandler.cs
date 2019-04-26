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
using Trinity.Network.TCP;

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
            this.RoleMax = 1;
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
            this.RoleMax = 1;

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

            // lack of verification steps
            if (!this.Verify())
            {
                this.FailStep();
                return false;
            }

            // Add txid for monitor
            this.AddTransactionSummary(this.Request.TxNonce, this.Request.MessageBody.Founder.txId,
                this.Request.ChannelName, EnumTxType.FUNDING);

            this.AddTransactionSummary(this.Request.TxNonce, this.Request.MessageBody.Commitment.txId,
                this.Request.ChannelName, EnumTxType.COMMITMENT);

            this.AddTransactionSummary(this.Request.TxNonce, this.Request.MessageBody.RevocableDelivery.txId,
                this.Request.ChannelName, EnumTxType.REVOCABLE);

            return true;
        }

        public override void FailStep()
        {
            this.FHandler = new FounderFailHandler(this.Request.Receiver, this.Request.Sender, this.Request.ChannelName,
                    this.Request.MessageBody.AssetType, this.Request.NetMagic, this.Request.TxNonce, this.Request.MessageBody.Deposit);
            this.FHandler.MakeTransaction(this.GetClient());
        }

        public override void SucceedStep()
        {
            if (this.IsIllegalRole(this.Request.MessageBody.RoleIndex))
            {
                this.FailStep();
                Console.WriteLine("Invalid nonce for founder. Nonce: {0}", this.Request.TxNonce);
                return;
            }

            // create FoudnerSign handler for send response to peer
            this.SHandler = new FounderSignHandler(this.Request.Receiver, this.Request.Sender, this.Request.ChannelName,
                    this.Request.MessageBody.AssetType, this.Request.NetMagic, this.Request.TxNonce, this.Request.MessageBody.Deposit,
                    this.Request.MessageBody.RoleIndex);

            FounderHandler founderHandler = null;
            if (this.IsRole0(this.Request.MessageBody.RoleIndex))
            {
                // record the peer data to the database
                this.AddTransaction(true);

                // Sender Founder with Role equals to 1
                founderHandler = new FounderHandler(this.Request.Receiver, this.Request.Sender, this.Request.ChannelName,
                        this.Request.MessageBody.AssetType, this.Request.NetMagic, this.Request.TxNonce, this.Request.MessageBody.Deposit,
                        1);
            }

            // send FounderSign to peer
            this.SHandler.MakeTransaction(this.GetClient());
            founderHandler.MakeTransaction(this.GetClient());
        }

        public override void MakeTransaction(TrinityTcpClient client)
        {
            this.SignAndSetMessageAttribute();
            base.MakeTransaction(client);
        }

        public override bool Verify()
        {
            return true;
        }

        private bool SignFundingTx()
        {
            // Sign C1A / C1B by role index
            if (this.IsRole0(this.Request.MessageBody.RoleIndex))
            {
                string deposit = this.Request.MessageBody.Deposit.ToString();
                // Because this is triggered by the RegisterChannel, the founder of this channel is value of Receiver;
                this.fundingTx = Funding.createFundingTx(this.GetPubKey(), deposit,
                    this.GetPeerPubKey(), deposit, this.Request.MessageBody.AssetType.ToAssetId());
                return true;
            }
            else if (this.IsRole1(this.Request.MessageBody.RoleIndex))
            {
                // TODO: Read from the database
                TransactionTabelContens transactionContent = this.GetChannelInterface().GetTransaction(this.Request.TxNonce);
                if (default != transactionContent)
                {
                    this.fundingTx["txData"] = transactionContent.founder.originalData.txData;
                    this.fundingTx["txId"] = transactionContent.founder.originalData.txId;
                    this.fundingTx["witness"] = transactionContent.founder.originalData.witness;
                    this.fundingTx["addressFunding"] = transactionContent.founder.originalData.addressFunding;
                    this.fundingTx["scriptFunding"] = transactionContent.founder.originalData.scriptFunding;
                    return true;
                }
                else
                {
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
            if (IsRole0(this.Request.MessageBody.RoleIndex))
            {
                this.AddTransaction();
            }
            else if (IsRole0(this.Request.MessageBody.RoleIndex))
            {
                this.AddTransaction();
            }

        }

        public void AddTransaction(bool isPeer = false)
        {
            TransactionTabelContens txContent = new TransactionTabelContens
            {
                nonce = this.Request.TxNonce,
                monitorTxId = this.Request.MessageBody.Founder.txId,

                founder = new FundingSignTx(),
                commitment = new CommitmentSignTx(),
                peerCommitment = new CommitmentSignTx(),
                revocableDelivery = new RevocableDeliverySignTx(),
                peerRevocableDelivery = new RevocableDeliverySignTx()
            };

            // both sides have same founder 
            txContent.founder.originalData = this.Request.MessageBody.Founder;

            // add peer information from the message
            if (isPeer)
            {
                // add peer information
                txContent.peerCommitment.originalData = this.Request.MessageBody.Commitment;
                txContent.peerRevocableDelivery.originalData = this.Request.MessageBody.RevocableDelivery;
            }
            else
            {
                txContent.commitment.originalData = this.Request.MessageBody.Commitment;
                txContent.revocableDelivery.originalData = this.Request.MessageBody.RevocableDelivery;
            }

            this.GetChannelInterface().AddTransaction(this.Request.TxNonce, txContent);
        }

        public void UpdataTransaction()
        {
            this.GetChannelInterface();
        }

        //public void AddTransaction(bool isPeer=false)
        //{
        //    TransactionTabelContens transactionContent;
        //    // add peer information from the message
        //    if (isPeer)
        //    {
        //        transactionContent = new TransactionTabelContens();
        //        transactionContent.nonce = this.Request.TxNonce;
        //    }
        //    else
        //    {
        //        transactionContent = new TransactionTabelContens
        //        {
        //            nonce = this.Request.TxNonce,
        //            monitorTxId = this.commTx["txId"].ToString(),
        //            founder = new FundingSignTx
        //            {
        //                originalData = new FundingTx
        //                {
        //                    txData = this.fundingTx["txData"].ToString(),
        //                    txId = this.fundingTx["txId"].ToString(),
        //                    witness = this.fundingTx["witness"].ToString(),
        //                    addressFunding = this.fundingTx["addressFunding"].ToString(),
        //                    scriptFunding = this.fundingTx["scriptFunding"].ToString()
        //                },
        //            },
        //            commitment = new CommitmentSignTx
        //            {
        //                originalData = new CommitmentTx
        //                {
        //                    txData = this.commTx["txData"].ToString(),
        //                    txId = this.commTx["txId"].ToString(),
        //                    witness = this.commTx["witness"].ToString(),
        //                    addressRSMC = this.commTx["addressRSMC"].ToString(),
        //                    scriptRSMC = this.commTx["scriptRSMC"].ToString(),
        //                },
        //            },
        //            revocableDelivery = new RevocableDeliverySignTx
        //            {
        //                originalData = new RevocableDeliveryTx
        //                {
        //                    txData = this.rdTx["txData"].ToString(),
        //                    txId = this.rdTx["txId"].ToString(),
        //                    witness = this.rdTx["witness"].ToString(),
        //                }
        //            }
        //        };
        //    }

        //    this.GetChannelInterface().AddTransaction(this.Request.TxNonce, transactionContent);
        //}

        public void AddTransactionSummary(UInt64 nonce, string txId, string channel, EnumTxType type)
        {
            TransactionTabelSummary txContent = new TransactionTabelSummary
            {
                nonce = nonce,
                channel = channel,
                txType = type.ToString()
            };

            this.GetChannelInterface().AddTransaction(txId, txContent);
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
