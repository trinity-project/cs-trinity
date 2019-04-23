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
using Trinity.BlockChain;
using Trinity.TrinityWallet.Templates.Definitions;
using Trinity.TrinityWallet.Templates.Messages;
using Neo.IO.Json;

namespace Trinity.TrinityWallet.TransferHandler.TransactionHandler
{
    /// <summary>
    /// Class Handler for handling Founder Message
    /// </summary>
    public class FounderHandler : TransferHandler<Founder, FounderSignHandler, FounderFailHandler>
    {
        public int RoleIndex;
        public JObject FundingTx;

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
                },
            };
            this.Request.MessageBody.SetAttribute("AssetType", asset);
            this.Request.MessageBody.SetAttribute("Deposit", deposit);
            this.Request.MessageBody.SetAttribute("RoleIndex", role);
        }

        public override bool Handle(string msg)
        {
            base.Handle(msg);

            if (!this.SignFundingTx())
            {
                return false;
            }


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

        public bool SignFundingTx()
        {
            // Sign C1A / C1B by role index
            if (this.IsFounderRoleZero(this.RoleIndex))
            {
                // Because this is triggered by the RegisterChannel, the founder of this channel is value of Receiver;
                this.FundingTx = Funding.createFundingTx("", "0", "", "0", "");
                return true;
            }
            else if (this.IsPartnerRoleOne(this.RoleIndex))
            {
                // TODO: Read from the database
                this.FundingTx = null;
                return true;
            }
            else
            {
                // TODO: error LOG
            }

            return false;
        }

        //public override void GetBodyAttribute<TValue>(string name, out TValue value)
        //{
        //    this.GetMessageAttribute<FounderBody, TValue>(this.Request.MessageBody, name, out value);
        //}

        //public override void SetBodyAttribute<TValue>(string name, TValue value)
        //{
        //    this.SetMessageAttribute<FounderBody, TValue>(this.Request.MessageBody, name, value);
        //}

        public void SetCommitment()
        {
            //this.SetBodyAttribute("Commitment", new CommitmentScript {
            //    txData = "1",
            //    addressRSMC = "2",
            //    scriptRSMC = "3",
            //    txId = "4",
            //    witness = "script"
            //});

            this.Request.MessageBody.Commitment.SetAttribute("txId", "Testtxid-222222222222");
            Console.WriteLine("txID is {0}", this.Request.MessageBody.Commitment.txId);

            //this.Request.MessageBody.Commitment.Set(new CommitmentScript {
            //    txId = "TestTxID-11111"
            //});
            //this.Request.MessageBody.Commitment = new CommitmentScript
            //{
            //    txId = "TestTxID-11111"
            //};

            Console.WriteLine(this.ToJson());
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
