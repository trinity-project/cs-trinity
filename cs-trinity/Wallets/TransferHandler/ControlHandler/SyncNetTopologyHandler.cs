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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Trinity.TrinityDB.Definitions;
using Trinity.ChannelSet.Definitions;
using Trinity.Network.RPC;
using Trinity.Wallets.Templates.Definitions;
using Trinity.Wallets.Templates.Messages;

namespace Trinity.Wallets.TransferHandler.ControlHandler
{
    public static class SyncNetTopologyHandler
    {
        internal static string SyncNetworkTopology(ChannelTableContent channelContent,
            EnumSyncTopoAction action=EnumSyncTopoAction.AddChannel)
        {
            if (null == channelContent)
            {
                Log.Error("Null input parameter is found.");
                return null;
            }

            // get the founder and receiver
            string founder;
            string receiver;
            Dictionary<string, Dictionary<string, long>> balance = new Dictionary<string, Dictionary<string, long>>();

            if (channelContent.role.Contains(EnumRole.FOUNDER.ToString()))
            {
                founder = channelContent.uri;
                receiver = channelContent.peer;

                balance.Add(founder, new Dictionary<string, long> {{ channelContent.asset, channelContent.balance}});
                balance.Add(receiver, new Dictionary<string, long> { { channelContent.asset, channelContent.peerBalance } });
            }
            else
            {
                founder = channelContent.peer;
                receiver = channelContent.uri;

                balance.Add(founder, new Dictionary<string, long> { { channelContent.asset, channelContent.peerBalance } });
                balance.Add(receiver, new Dictionary<string, long> { { channelContent.asset, channelContent.balance } });
            }

            // makeup new request to peer
            SyncNetTopology syncRequest = new SyncNetTopology
            {
                MessageType = action.ToString(),
                NetMagic = channelContent.magic,
                AssetType = channelContent.asset,

                MessageBody = new SyncNetTopologyBody
                {
                    ChannelName = channelContent.channel,
                    Founder = founder,
                    Receiver = receiver,
                    Balance = balance
                }
            };

            
            return TrinityRpcRequest.PostIgnoreException(TrinityWallet.GetGatewayRpcServer(), "SyncChannel", syncRequest);
        }

        public static void AddNetworkTopology(ChannelTableContent channelContent)
        {
            SyncNetworkTopology(channelContent, EnumSyncTopoAction.AddChannel);
        }

        public static void UpdateNetworkTopology(ChannelTableContent channelContent)
        {
            SyncNetworkTopology(channelContent, EnumSyncTopoAction.UpdateChannel);
        }

        public static void DeleteNetworkTopology(ChannelTableContent channelContent)
        {
            SyncNetworkTopology(channelContent, EnumSyncTopoAction.DeleteChannel);
        }
    }
}
