using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Neo;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.VM;
using Neo.Wallets;
using Trinity.BlockChain;
using Trinity.Properties;
using Neo.Persistence;

namespace Trinity
{
    public static class startTrinity
    {
        public static Wallet currentWallet = null;
        public static string currentAccountPublicKey = "";
        public static NeoSystem NeoSystem;

        public static TrinityWallet trinityWallet;
        public static Dictionary<string, string> assetTypes = new Dictionary<string, string>();

        public static void trinityConfigure(NeoSystem neoSystem,
                                            StringCollection NEP5Watched,
                                            Wallet wallet, 
                                            string publicKey, 
                                            string magic, 
                                            string ip=null, 
                                            string port=null)
        {
            NeoSystem = neoSystem;
            currentWallet = wallet;
            currentAccountPublicKey = publicKey;
            assetTypes = GetCurrentWalletAssetType(wallet, NEP5Watched);

            ip = ip is null ? Settings.Default.gatewayIP : ip;
            port = port is null ? Settings.Default.gatewayPort : port;

            trinityWallet = new TrinityWallet(neoSystem, wallet, assetTypes, publicKey, magic, ip, port);

            MonitorTransction monitorTransction = new MonitorTransction(publicKey, ip, port, magic);
            Thread thread = new Thread(monitorTransction.monitorBlock);
            thread.Start();
        }

        public static Dictionary<string, string> GetCurrentWalletAssetType(Wallet CurrentWallet, StringCollection NEP5Watched)
        {
            try
            {
                assetTypes.Clear();
                /* get neo/gas asset information */
                using (Snapshot snapshot = Blockchain.Singleton.GetSnapshot())
                {
                    IEnumerable<Coin> coins = CurrentWallet?.GetCoins().Where(p => !p.State.HasFlag(CoinState.Spent)) ?? Enumerable.Empty<Coin>();
                    var assets = coins.GroupBy(p => p.Output.AssetId, (k, g) => new
                    {
                        Asset = snapshot.Assets.TryGet(k),
                        Value = g.Sum(p => p.Output.Value),
                        Claim = k.Equals(Blockchain.UtilityToken.Hash) ? Fixed8.Zero : Fixed8.Zero
                    }).ToDictionary(p => p.Asset.AssetId);

                    foreach (var asset in assets.Values)
                    {

                        string asset_name = asset.Asset.AssetType == AssetType.GoverningToken ? "NEO" :
                                            asset.Asset.AssetType == AssetType.UtilityToken ? "NeoGas" :
                                            asset.Asset.Name;

                        string asset_id = asset.Asset.AssetId.ToString();

                        assetTypes.Add(asset_name, asset_id);
                    }
                }

                /* get nep-5 asset information */
                if (CurrentWallet != null)
                {
                    foreach (string s in NEP5Watched)
                    {
                        UInt160 script_hash = UInt160.Parse(s);
                        byte[] script;
                        using (ScriptBuilder sb = new ScriptBuilder())
                        {
                            sb.Emit(OpCode.DEPTH, OpCode.PACK);
                            sb.EmitAppCall(script_hash, "symbol");
                            script = sb.ToArray();
                        }
                        ApplicationEngine engine = ApplicationEngine.Run(script);
                        if (engine.State.HasFlag(VMState.FAULT)) continue;
                        string nep5_name = engine.ResultStack.Pop().GetString();

                        assetTypes.Add(nep5_name, script_hash.ToString());
                    }
                }
                return assetTypes;
            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }

        public static List<string> GetAssetType()
        {
            List<string> assetList = new List<string>();
            foreach(var item in assetTypes)
            {
                assetList.Add(item.Key);
            }
            return assetList;
        }

        /*
         * start up server
         * 1. monitor server
         * 2. setup tcp connection with gateway
         * 3. receiving gateway message
         */
    }
}
