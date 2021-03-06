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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Trinity.Wallets.Templates.Messages;
using Trinity.Network.RPC;

namespace Trinity.Wallets.TransferHandler.ControlHandler
{
    public class GetRouterInfoHandler : ControlHandler<GetRouterInfo, VoidControlMessage, GetRouterInfoHandler, VoidHandler>
    {
        public GetRouterInfoHandler(string sender, string receiver, string asset, string magic, long payment)
            : base(sender, receiver, asset, magic)
        {
            this.Request.MessageBody = new RouteInfoBody
            {
                AssetType = asset,
                Value = payment
            };
        }

        public static AckRouterInfo GetRouter(string sender, string receiver, string asset, string magic, long payment)
        {
            asset = asset.ToAssetType(startTrinity.GetAssetMap());

            GetRouterInfo request = new GetRouterInfo
            {
                Sender = sender,
                Receiver = receiver,
                NetMagic = magic,
                AssetType = asset,

                MessageBody = new RouteInfoBody
                {
                    AssetType = asset,
                    Value = payment
                }
            };

            string routerInfo = TrinityRpcRequest.PostIgnoreException(TrinityWallet.GetGatewayRpcServer(), "GetRouterInfo", request);
            return routerInfo?.Deserialize<RpcResponse>()?.result?.Deserialize<AckRouterInfo>();
        }
    }

    //public class AckRouterInfoHandler : ControlHandler<AckRouterInfo, VoidControlMessage, AckRouterInfoHandler, VoidHandler>
    //{
    //    public AckRouterInfoHandler(string message) : base(message)
    //    {
    //    }
    //}
}
