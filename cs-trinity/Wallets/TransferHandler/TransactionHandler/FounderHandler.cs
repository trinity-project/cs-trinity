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

using Neo;
using Neo.IO.Json;
using Neo.Wallets;

using Trinity.ChannelSet.Definitions;
using Trinity.TrinityDB.Definitions;
using Trinity.BlockChain;
using Trinity.Wallets.Templates.Definitions;
using Trinity.Wallets.Templates.Messages;
using Trinity.Network.TCP;

namespace Trinity.Wallets.TransferHandler.TransactionHandler
{
    /// <summary>
    /// Class Handler for handling Founder Message
    /// </summary>
    public class FounderHandler : TransferHandler<Founder, FounderSignHandler, FounderFailHandler>
    {
        //private readonly double Deposit;
        //private readonly UInt64 Nonce;

        private int RoleIndex;
        private FundingTx fundingTx;
        private CommitmentTx commTx;
        private RevocableDeliveryTx rdTx;

        private readonly NeoTransaction neoTransaction;

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
            string magic, UInt64 nonce, long deposit, int role=0) : base()
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
                    //Founder = new FundingTx(),
                    //Commitment = new CommitmentTx(),
                    //RevocableDelivery = new RevocableDeliveryTx()
                }
            };

            this.ParsePubkeyPair(sender, receiver);
            this.SetChannelInterface(sender, receiver, channel, asset);

            this.neoTransaction = new NeoTransaction(asset.ToAssetId(), this.GetPeerPubKey(), deposit.ToString(),
                this.GetPubKey(), deposit.ToString());
        }

        public FounderHandler(string message) : base(message)
        {
            this.ParsePubkeyPair(this.Request.Receiver, this.Request.Sender);
            this.SetChannelInterface(this.Request.Receiver, this.Request.Sender,
                this.Request.ChannelName, this.Request.MessageBody.AssetType);
        }

        public override bool Handle()
        {
            if (!base.Handle())
            {
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

        public override bool FailStep()
        {
            this.FHandler = new FounderFailHandler(this.Request.Receiver, this.Request.Sender, this.Request.ChannelName,
                    this.Request.MessageBody.AssetType, this.Request.NetMagic, this.Request.TxNonce, this.Request.MessageBody.Deposit);
            this.FHandler.MakeTransaction(this.GetClient());

            return true;
        }

        public override bool SucceedStep()
        {
            if (this.IsIllegalRole(this.Request.MessageBody.RoleIndex))
            {
                this.FailStep();
                Console.WriteLine("Invalid nonce for founder. Nonce: {0}", this.Request.TxNonce);
                return false;
            }

            // create FoudnerSign handler for send response to peer
            #region New_FounderSignHandler
            this.SHandler = new FounderSignHandler(this.Request.Receiver, this.Request.Sender, this.Request.ChannelName,
                    this.Request.MessageBody.AssetType, this.Request.NetMagic, this.Request.TxNonce, this.Request.MessageBody.Deposit,
                    this.Request.MessageBody.RoleIndex);
            this.SHandler.MakeupFundingTx(this.Request.MessageBody.Founder);
            this.SHandler.MakeupCommitmentTx(this.Request.MessageBody.Commitment);
            this.SHandler.MakeupRevocableDeliveryTx(this.Request.MessageBody.RevocableDelivery);
            // send FounderSign to peer
            this.SHandler.MakeTransaction(this.GetClient());
            #endregion

            #region New_FounderHandler
            if (this.IsRole0(this.Request.MessageBody.RoleIndex))
            {
                // record the peer data to the database
                this.AddTransaction(true);

                // Sender Founder with Role equals to 1
                FounderHandler founderHandler = new FounderHandler(this.Request.Receiver, this.Request.Sender, this.Request.ChannelName,
                        this.Request.MessageBody.AssetType, this.Request.NetMagic, this.Request.TxNonce, this.Request.MessageBody.Deposit,
                        1);
                founderHandler.MakeTransaction(this.GetClient());
            }
            #endregion

            return true;
        }

        public override void MakeTransaction(TrinityTcpClient client)
        {
            base.MakeTransaction(client);
        }

        private bool SignFundingTx()
        {
            // Sign C1A / C1B by role index
            if (this.IsRole0(this.Request.MessageBody.RoleIndex))
            {
                string deposit = this.Request.MessageBody.Deposit.ToString();
                // Because this is triggered by the RegisterChannel, the founder of this channel is value of Receiver;
                this.neoTransaction.CreateFundingTx(out this.fundingTx);
                return true;
            }
            else if (this.IsRole1(this.Request.MessageBody.RoleIndex))
            {
                // TODO: Read from the database
                TransactionTabelContent transactionContent = this.GetChannelInterface().GetTransaction(this.Request.TxNonce);
                if (null != transactionContent)
                {
                    this.fundingTx = new FundingTx
                    {
                        txData = transactionContent.founder.originalData.txData,
                        txId = transactionContent.founder.originalData.txId,
                        addressFunding = transactionContent.founder.originalData.addressFunding,
                        scriptFunding = transactionContent.founder.originalData.scriptFunding,
                        witness = transactionContent.founder.originalData.witness
                    };

                    // set the neo transaction handler attribute
                    this.neoTransaction.SetAddressFunding(transactionContent.founder.originalData.addressFunding);
                    this.neoTransaction.SetScripFunding(transactionContent.founder.originalData.scriptFunding);
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

        // Todo: impmentation this method in the base class in future
        public override bool VerifyRoleIndex()
        {
            if (this.IsIllegalRole(this.Request.MessageBody.RoleIndex))
            {
                Console.WriteLine("Invalid nonce for founder. Nonce: {0}", this.Request.TxNonce);
                return false;
            }

            return true;
        }

        public override bool Verify()
        {
            return this.VerifyRoleIndex();
        }

        public override bool MakeupMessage()
        {
            return this.MakeupTransactionBody();
        }

        public bool MakeupTransactionBody()
        {
            string deposit = this.Request.MessageBody.Deposit.ToString();

            if (!this.SignFundingTx())
            {
                return false;
            }

            // Create Commitment transaction
            this.neoTransaction.CreateCTX(out this.commTx);

            // create Revocable commitment transaction
            this.neoTransaction.createRDTX(out this.rdTx, this.commTx.txId);

            this.Request.MessageBody.Founder = this.fundingTx;
            this.Request.MessageBody.Commitment = this.commTx;
            this.Request.MessageBody.RevocableDelivery = this.rdTx;

            // record the item to database
            if (IsRole0(this.Request.MessageBody.RoleIndex))
            {
                this.AddTransaction();
            }
            else if (IsRole1(this.Request.MessageBody.RoleIndex))
            {
                // update this records of founder
                TransactionTabelContent txUpdateContent = this.GetChannelInterface().TryGetTransaction(this.Request.TxNonce);
                if (null != txUpdateContent)
                {
                    txUpdateContent.commitment.originalData = this.Request.MessageBody.Commitment;
                    txUpdateContent.revocableDelivery.originalData = this.Request.MessageBody.RevocableDelivery;
                    this.GetChannelInterface().UpdateTransaction(this.Request.TxNonce, txUpdateContent);
                    Console.WriteLine("Success update the founder trade records. channel: {0}", this.Request.ChannelName);
                }
            }

            return true;
        }

        private void AddTransaction(bool isPeer = false)
        {
            TransactionTabelContent txContent = new TransactionTabelContent
            {
                nonce = this.Request.TxNonce,
                founder = new FundingSignTx(),
                commitment = new CommitmentSignTx(),
                revocableDelivery = new RevocableDeliverySignTx(),

                state = EnumTransactionState.initial.ToString()
            };

            // both sides have same founder 
            txContent.founder.originalData = this.Request.MessageBody.Founder;
            

            // add peer information from the message
            if (isPeer)
            {
                // add monitor tx id
                txContent.monitorTxId = this.Request.MessageBody.Commitment.txId;
            }
            else
            {
                txContent.commitment.originalData = this.Request.MessageBody.Commitment;
                txContent.revocableDelivery.originalData = this.Request.MessageBody.RevocableDelivery;
            }

            this.GetChannelInterface().AddTransaction(this.Request.TxNonce, txContent);
        }

        //public void AddTransaction(bool isPeer=false)
        //{
        //    TransactionTabelContent transactionContent;
        //    // add peer information from the message
        //    if (isPeer)
        //    {
        //        transactionContent = new TransactionTabelContent();
        //        transactionContent.nonce = this.Request.TxNonce;
        //    }
        //    else
        //    {
        //        transactionContent = new TransactionTabelContent
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

        private void AddTransactionSummary(UInt64 nonce, string txId, string channel, EnumTxType type)
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

    ///////////////////////////////////////////////////////////////////////////////////////////////
    /// FounderSignHandler start
    ///////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Class Handler for handling FounderSign Message
    /// </summary>
    public class FounderSignHandler : TransferHandler<FounderSign, VoidHandler, FounderFailHandler>
    {
        //private FundingSignTx fstContent;
        //private CommitmentSignTx cstContent;
        //private RevocableDeliverySignTx rdstContent;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="receiver"></param>
        /// <param name="channel"></param>
        /// <param name="asset"></param>
        /// <param name="magic"></param>
        /// <param name="nonce"></param>
        /// <param name="deposit"></param>
        /// <param name="role"></param>
        public FounderSignHandler(string sender, string receiver, string channel, string asset,
            string magic, UInt64 nonce, long deposit, int role = 0)
        {
            this.Request = new FounderSign
            {
                Sender = sender,
                Receiver = receiver,
                ChannelName = channel,
                AssetType = asset,
                NetMagic = magic,
                TxNonce = nonce,

                MessageBody = new FounderSignBody
                {
                    Deposit = deposit,
                    RoleIndex = role,
                    AssetType = asset,
                },
            };

            this.ParsePubkeyPair(sender, receiver);
            this.SetChannelInterface(sender, receiver, channel, asset);
        }

        public FounderSignHandler(string message) : base(message)
        {
            this.ParsePubkeyPair(this.Request.Receiver, this.Request.Sender);
            this.SetChannelInterface(this.Request.Receiver, this.Request.Sender,
                this.Request.ChannelName, this.Request.MessageBody.AssetType);
        }

        public override bool FailStep()
        {
            this.FHandler = new FounderFailHandler(this.Request.Receiver, this.Request.Sender, 
                this.Request.ChannelName, this.Request.MessageBody.AssetType, this.Request.NetMagic, 
                this.Request.TxNonce, this.Request.MessageBody.Deposit, this.Request.MessageBody.RoleIndex);
            this.FHandler.MakeTransaction(this.GetClient());

            return true;
        }

        public override bool SucceedStep()
        {
            // update the transaction history
            this.UpdateTransaction();

            // broadcast this transaction
            if (IsRole1(this.Request.MessageBody.RoleIndex))
            {
                this.BroadcastTransaction();
            }
            return true;
        }

        private void BroadcastTransaction()
        {
            string peerFundSign = this.Request.MessageBody.Founder.txDataSign;
            string fundSign = this.Sign(this.Request.MessageBody.Founder.originalData.txData);
            string witness = this.Request.MessageBody.Founder.originalData.witness
                .Replace("{signOther}", peerFundSign)
                .Replace("{signSelf}", fundSign);

            NeoInterface.SendRawTransaction(this.Request.MessageBody.Founder.originalData.txData + witness);
        }

        private void UpdateTransaction()
        {
            TransactionTabelContent content = this.GetChannelInterface().TryGetTransaction(this.Request.TxNonce);

            if (null == content)
            {
                return;
            }

            // start update the transaction
            content.founder.txDataSign = this.Request.MessageBody.Founder.txDataSign;
            content.commitment.txDataSign = this.Request.MessageBody.Commitment.txDataSign;
            content.revocableDelivery.txDataSign = this.Request.MessageBody.RevocableDelivery.txDataSign;

            this.GetChannelInterface().UpdateTransaction(this.Request.TxNonce, content);
        }
        
        public void MakeupFundingTx(FundingTx txContent)
        {
            string txDataSign = this.Sign(txContent.txData);

            this.Request.MessageBody.Founder = new FundingSignTx
            {
                txDataSign = txDataSign,
                originalData = txContent
            };
        }

        // Below 2 mothods could be move to base class
        public void MakeupCommitmentTx(CommitmentTx txContent)
        {
            string txDataSign = this.Sign(txContent.txData);

            this.Request.MessageBody.Commitment = new CommitmentSignTx
            {
                txDataSign = txDataSign,
                originalData = txContent
            };
        }

        public void MakeupRevocableDeliveryTx(RevocableDeliveryTx txContent)
        {
            string txDataSign = this.Sign(txContent.txData);

            this.Request.MessageBody.RevocableDelivery = new RevocableDeliverySignTx
            {
                txDataSign = txDataSign,
                originalData = txContent
            };
        }

        // Todo: impmentation this method in the base class in future
        public override bool VerifyRoleIndex()
        {
            if (this.IsIllegalRole(this.Request.MessageBody.RoleIndex))
            {
                Console.WriteLine("Invalid nonce for founder sign. Nonce: {0}", this.Request.TxNonce);
                return false;
            }

            return true;
        }

        public override bool Verify()
        {
            return this.VerifyRoleIndex();
        }
    }


    ///////////////////////////////////////////////////////////////////////////////////////////////
    /// FounderFailHandler start
    ///////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Class Handler for handling FounderFail Message
    /// </summary>
    public class FounderFailHandler : TransferHandler<FounderFail, VoidHandler, VoidHandler>
    {
        public FounderFailHandler(string sender, string receiver, string channel, string asset,
            string magic, UInt64 nonce, long deposit, int role = 0)
        {
            this.Request = this.Request = new FounderFail
            {
                Sender = sender,
                Receiver = receiver,
                ChannelName = channel,
                AssetType = asset,
                NetMagic = magic,
                TxNonce = nonce,

                MessageBody = new FounderBody
                {
                    Deposit = deposit,
                    RoleIndex = role,
                    AssetType = asset,
                },
            };
            this.Request.MessageBody.SetAttribute("AssetType", asset);
            this.Request.MessageBody.SetAttribute("Deposit", deposit);
            this.Request.MessageBody.SetAttribute("RoleIndex", role);

            this.ParsePubkeyPair(sender, receiver);
            this.SetChannelInterface(sender, receiver, channel, asset);
        }

        public FounderFailHandler(string message) : base(message)
        {
            this.ParsePubkeyPair(this.Request.Receiver, this.Request.Sender);
            this.SetChannelInterface(this.Request.Receiver, this.Request.Sender,
                this.Request.ChannelName, this.Request.MessageBody.AssetType);
        }

        public override bool Handle()
        {
            Console.WriteLine("Failed to reate channel {0}", this.Request.ChannelName);
            base.Handle();
            return false;
        }

        public override bool FailStep()
        {
            return false;
        }

        public override bool SucceedStep()
        {
            // TODO add some logs here in future
            return true;
        }
    }
}
