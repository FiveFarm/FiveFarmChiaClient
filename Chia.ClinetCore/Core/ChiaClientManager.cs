using Chia.ClientCore.Common;
using Chia.ClientCore.Models;
using Chia.Common;
using Chia.Net;
using Chia.Net.CLI_Interface;
using Chia.Net.LogExplorer;
using Chia.NET.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chia.ClientCore.Core
{

    public enum APIType
    {
        Harvester,
        FullNode,
        Wallet,
        Farmer,
        Node,
    }
    public class ChiaClientManager
    {
        static JArray ipList = new JArray();
        static JObject con = new JObject();

        List<ConnectedNode> connectedFarmers = new List<ConnectedNode>();
        List<ConnectedNode> connectedHarvesters = new List<ConnectedNode>();

        HarvesterClient _harvesterClient;
        FullNodeClient _fullNode;
        WalletClient _wallet;
        FarmerClient _farmer;
        NodeInfo _info;
        PoolClient _poolClient;
        public ChiaClientManager(
            FarmerClient farmer,
            HarvesterClient harvesterClient,
            FullNodeClient fullNode,
            WalletClient wallet,
            NodeInfo info)
        {
            _farmer = farmer;
            _harvesterClient = harvesterClient;
            _fullNode = fullNode;
            _wallet = wallet;
            _info = info;
            //_poolClient = poolClient;
        }
        static JArray LocalIPAddress
        {
            get
            {
                if (ipList.Count == 0)
                {
                    JObject ip = new JObject();
                    try
                    {
                        var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                        //return host.AddressList.Where(x =>
                        //         x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).LastOrDefault().ToString();

                        foreach (var add in host.AddressList)
                        {
                            if (add.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                ip.Add("ip", add.ToString());
                                ipList.Add(ip);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ip.Add("Error", ex.Message);
                        ipList.Add(ip);
                    }
                }

                return ipList;
            }
        }

        static string IsConnected;
        string syncPevAlert = "";

        public async Task<string> GetChiaStatus(string userId, string clientId, bool connected)
        {
            con.RemoveAll();
            con.Add("connected", connected ? "1" : "0");
            _info.UserId = userId;
            _info.ClinetId = clientId;
            _info.ClinetName = NodeIdentification.GetHostName();
            CommonConstants.SaveDebugLog($"ClientName:{_info.ClinetName}",false,true);

            string statusString = "";
            //get_network_info
            #region Get Status from Chia


            string blockChainState = await _fullNode.GetBlockChainState();
            string fullNodeConnections = await _fullNode.GetConnections();

            JArray farmerlatestBlockChallanges = CommandLineExec.GetChallenges(out CommonConstants.ErrorCodes errCode_LAP);
            string farmerNetwork = await _farmer.GetConnections();

            (JArray lastAttemptedProofs, JArray farmed_unfinished_block, JArray poolErrorWarnings) = 
                LogFileExplorer.GetDataFromLog(farmerlatestBlockChallanges, ref errCode_LAP);
            string height = await _wallet.GetHeightInfo();

            string network_info = await _fullNode.GetNetworkInfo();
            string farmed_amount = CommandLineExec.GetFarmingStatus(out int harvesterPlotsCount);
            JObject syncAlertStatus = CommandLineExec.GetSyncStatus(ref syncPevAlert);
            string wallets = await _wallet.GetWallets();

            string pools_rpc = await _farmer.GetPoolState();
            Dictionary<string, Chia.Net.PoolInfo> launcherId_PointsStart_Pair = new Dictionary<string, Chia.Net.PoolInfo>();
            JArray Error_Codes = new JArray();
            JObject err = new JObject();

            if (!string.IsNullOrEmpty(pools_rpc) && pools_rpc.Contains("points_found_since_start"))
            {
                pools_rpc = CommonMethods.GetInnerData(pools_rpc, "pool_state");

                launcherId_PointsStart_Pair = GetPointsFoundSinceStart(pools_rpc);

                foreach (var k in launcherId_PointsStart_Pair)
                {
                    var info = $"{k.Value.EstimatedPlotSize}:{k.Value.PointsSinceStart}";
                    //CommonConstants.SaveDebugLog($"{k.Key}: {info}", false, false);
                }
            }
            else
            {
                err.Add("lap_rpc", "1");
                Error_Codes.Add(err);
            }

            if (launcherId_PointsStart_Pair == null) launcherId_PointsStart_Pair = new Dictionary<string, Chia.Net.PoolInfo>();

            // //pools data by CLI
            Task<(JArray, JObject)> Pools = CommandLineExec.GetPoolsFromPlotNFT2(harvesterPlotsCount, launcherId_PointsStart_Pair);

            //string plots = await _harvesterClient.GetPlotsAsync();

            #endregion

            #region Format Data
            JArray addresses = new JArray();
            JArray walletsTransaction = new JArray();
            JArray walletsBalance = new JArray();
            if (CommonMethods.ISValidResponse(wallets))
            {
                wallets = CommonMethods.GetInnerData(wallets, "wallets");
                JArray availableWallets = (JArray)JsonConvert.DeserializeObject(wallets);
                foreach (var v in availableWallets)
                {
                    int id = v.Value<int>("id");
                    string address = await GetWalletAddress(id);

                    string wallet_transaction = await _wallet.GetTransactions(id);
                    string walletBalance = await _wallet.GetWalletBalance(id);

                    var validWalletBalance = CommonMethods.GetInnerData(walletBalance, "wallet_balance");
                    var validWalletTransactions = CommonMethods.GetInnerData(wallet_transaction, "transactions");

                    if (!string.IsNullOrEmpty(validWalletBalance)) walletsBalance.Add((JObject)JsonConvert.DeserializeObject(validWalletBalance));
                    if (!string.IsNullOrEmpty(validWalletTransactions)) walletsTransaction.Add((JArray)JsonConvert.DeserializeObject(validWalletTransactions));
                    if (CommonMethods.ISValidResponse(address)) addresses.Add((JObject)JsonConvert.DeserializeObject(address));
                }
            }

            network_info = CommonMethods.GetInnerData(network_info, "");
            blockChainState = CommonMethods.GetInnerData(blockChainState, "blockchain_state");
            fullNodeConnections = CommonMethods.GetInnerData(fullNodeConnections, "connections");
            farmerNetwork = CommonMethods.GetInnerData(farmerNetwork, "connections");
            height = CommonMethods.GetInnerData(height, "last_height_farmed");

            JArray Alerts = new JArray();
            JArray discFarmers = new JArray();
            JArray discHarvesters = new JArray();
            //JArray stoppedPooling = new JArray();

            UpdateConnectedFarmerList(fullNodeConnections);
            UpdateConnectedHravesterList(farmerNetwork);
            discFarmers = FindDisconnected(ref connectedFarmers, "Farmer");
            discHarvesters = FindDisconnected(ref connectedHarvesters, "Harvester");
            //JObject syncAlert = SetSyncStatus(ref blockChainState, syncPevAlert);
            // stoppedPooling = FindStoppedFarmingToPool(ref launcherId_PointsStart_Pair);
            SetSyncStatus(ref blockChainState);

            err = new JObject();
            err.Add("lap", ((int)errCode_LAP).ToString());
            Error_Codes.Add(err);

            statusString = $"{{ " +
                              $"\"action\":\"onNodeMessage\"," +
                              $"\"info\":{_info.ToString()}," +
                              $"\"network_info\":{network_info}," +
                              $"\"fullNodeStatus\":{blockChainState}," +
                              $"\"full_node_network\":{fullNodeConnections}," +
                              $"\"farmer_block_challanges\":{farmerlatestBlockChallanges.ToString()}," +
                              $"\"last_attempted_proofs\":{lastAttemptedProofs.ToString()}," +
                              $"\"farmer_network\":{farmerNetwork}," +
                              $"\"farmed_amount\":{farmed_amount}," +
                              $"\"last_farmed_height\":{height}," +
                              $"\"available_wallets\":{wallets}," +
                              $"\"wallet_addresses\":{addresses}," +
                              $"\"wallet_balance\":{walletsBalance.ToString()}," +
                              $"\"wallet_transactions\":{walletsTransaction.ToString()}" +
                              "," +
                              $"\"pools\":{Pools.Result.Item1.ToString()}," +
                              $"\"dashboardtotals\":{Pools.Result.Item2.ToString()}" +
                              "," +
                              $"\"local_ips\":{LocalIPAddress.ToString()}," +
                              $"\"errorcodes\":{Error_Codes.ToString()}," +
                              $"\"connection\":{con.ToString()}" +
                              "," +
                              $"\"alerts\":" +
                              "{" +
                              $"\"farmers_disconnected\":{discFarmers.ToString()}," +
                              $"\"harvesters_disconnected\":{discHarvesters.ToString()}," +
                              $"\"sync_status\":{syncAlertStatus.ToString()}," +
                              $"\"pool_error_warning\":{poolErrorWarnings.ToString()}," +
                              $"\"farmed_unfinished_block\":{farmed_unfinished_block.ToString()}" +

                              "}" +
                              $"}}";
            #endregion

            if (!statusString.Contains("errorcodes"))
            {
            }
            return statusString;
        }

        static void SetSyncStatus(ref string blockchainstate) //, string prevAlertStatus)
        {
            //JObject alertStatus = new JObject();

            if (blockchainstate.Contains("\"sync_mode\": false"))
            {
                if (blockchainstate.Contains("\"synced\": true"))
                {
                    blockchainstate = blockchainstate.Replace("\"synced\": true", "\"synced\": \"Synced\"");
                }
                else
                {
                    blockchainstate = blockchainstate.Replace("\"synced\": false", "\"synced\": \"Not Synced\"");
                    //alertStatus.Add("\"synced\":", "\"Stopped\"");
                }
            }
            else if (blockchainstate.Contains("\"sync_mode\": true"))
            {
                if (blockchainstate.Contains("\"synced\": false"))
                {
                    blockchainstate = blockchainstate.Replace("\"synced\": false", "\"synced\": \"Syncing\"");
                }
                else
                {
                    blockchainstate = blockchainstate.Replace("\"synced\": true", "\"synced\": \"unknown\"");
                    //alertStatus.Add("\"synced\":", "\"Unknown State\"");
                }
            }
            //return alertStatus;
        }

        static Dictionary<string, Chia.Net.PoolInfo> GetPointsFoundSinceStart(string pooldata_rpc)
        {
            Dictionary<string, Chia.Net.PoolInfo> pair = new Dictionary<string, Chia.Net.PoolInfo>();
            string[] pools = pooldata_rpc.Split("authentication_token_timeout");
            char collon = ':';
            string newString = string.Empty;
            string pointsStart = string.Empty;
            string launcherid = string.Empty;

            foreach (var p in pools)
            {
                string[] lines = p.Split(new char[] { '\n' }); ;
                pointsStart = ""; launcherid = "";

                foreach (var l in lines)
                {
                    try
                    {
                        newString = l.TrimStart(' ').TrimEnd('\r');
                        //displayOnConsole("line", line);
                        string stringBeforeCollon = string.Empty;
                        int indexOfCollon = -1;
                        if (newString.Contains(collon))
                        {
                            if (newString.Contains("error_code") || newString.Contains("error_message"))
                                continue;
                            indexOfCollon = newString.IndexOf(collon);
                            stringBeforeCollon = newString.Substring(0, indexOfCollon);

                            if (stringBeforeCollon.Contains("points_found_since_start"))
                            {
                                pointsStart = (newString.Substring(indexOfCollon + 1)).Replace(",", "");
                            }
                            else if (stringBeforeCollon.Contains("launcher_id"))
                            {
                                launcherid = ((newString.Substring(indexOfCollon + 1)).Replace(",", "")).Replace("0x", "").Trim();
                            }

                            //switch (stringBeforeCollon)
                            //{
                            //    case "points_found_since_start": pointsStart = (newString.Substring(indexOfCollon + 1)).Replace(",", "");  break;
                            //    case "launcher_id": launcherid = ((newString.Substring(indexOfCollon + 1)).Replace(",", "")).Replace("\"0x", "\" "); break;
                            //    default: break;
                            //}
                        }
                    }
                    catch (Exception ex)
                    {
                        CommonConstants.SaveDebugLog($"Error: GetPointsFoundSinceStart: {ex.Message}", false, true);
                    }
                }
                //CommonConstants.SaveDebugLog($"Pair: {launcherid}: {pointsStart}", false, false);
                if (!string.IsNullOrWhiteSpace(launcherid) && !string.IsNullOrWhiteSpace(pointsStart) && !pair.ContainsKey(launcherid))
                {
                    //CommonConstants.SaveDebugLog($"Adding Pair:{launcherid}: {pointsStart}", false, false);
                    Net.PoolInfo poolInfo = new Net.PoolInfo();
                    poolInfo.PointsSinceStart = pointsStart;
                    pair.Add(launcherid, poolInfo);
                }
            }
            return pair;
        }

        public async Task<string> GetChiaGlobalStatus(string userId, string clientId)
        {
            _info.UserId = userId;
            _info.ClinetId = clientId;
            _info.ClinetName = NodeIdentification.GetHostName();
            //ChiaStatus status = new ChiaStatus();


            string statusString = "";
            //get_network_info
            #region Get Status from Chia

            string height = await _wallet.GetHeightInfo();
            string blockChainState = await _fullNode.GetBlockChainState();
            string network_info = await _fullNode.GetNetworkInfo();
            #endregion

            #region Format Data
            height = CommonMethods.GetInnerData(height, "last_height_farmed");
            blockChainState = CommonMethods.GetInnerData(blockChainState, "blockchain_state");

            statusString = $"{{ " +
                              $"\"action\":\"onChiaStream\"," +
                              $"\"info\":{_info.ToString()}," +
                              $"\"fullNodeStatus\":{blockChainState}," +
                              $"\"last_farmed_height\":{height}," +
                              $"\"network_info\":{network_info}" +
                              $"}}";
            #endregion
            return statusString;
        }

        public async Task<string> GetNewWalletAddress(int walletId)
        {
            string response = await GetWalletAddress(walletId, true);
            response = CommonMethods.GetInnerData(response, "address");
            return response;
        }

        public async Task<string> GetWalletAddress(int walletId, bool generateNew = false)
        {
            string response = await _wallet.GetWalletAddressAsync(walletId, generateNew);
            return response;
        }

        public async Task<string> GetCustomResponse(APIType type, string url, string parameters)
        {
            #region PreProcessing for Parameters
            string[] arrayOfParams = parameters.Split(new char[] { ',' });

            var dictionaryOfParams = new Dictionary<string, string>();
            foreach (var param in arrayOfParams)
            {
                if (param.Contains(":"))
                {
                    dictionaryOfParams.TryAdd(param.Substring(0, param.IndexOf(":")), param.Substring(param.IndexOf(":") + 1));
                }
            }
            #endregion

            string response = string.Empty;
            switch (type)
            {
                case APIType.Farmer: response = await _farmer.PostAsync(new Uri(_farmer.ApiUrl + url), dictionaryOfParams); break;
                case APIType.FullNode: response = await _fullNode.PostAsync(new Uri(_fullNode.ApiUrl + url), dictionaryOfParams); break;
                case APIType.Harvester: response = await _harvesterClient.PostAsync(new Uri(_harvesterClient.ApiUrl + url), dictionaryOfParams); break;
                case APIType.Wallet: response = await _wallet.PostAsync(new Uri(_wallet.ApiUrl + url), dictionaryOfParams); break;
            }

            return response;
        }

        public void UpdateConnectedFarmerList(string fullnodeNetwork)
        {
            foreach (var item in connectedFarmers)
            {
                item.Status = false;
            }

            try
            {
                string[] nodes = fullnodeNetwork.Split("{");
                char collon = ':';
                string newString = string.Empty;
                int indexOfCollon = -1;
                string stringBeforeCollon = "";

                foreach (var n in nodes)
                {
                    if (n.Contains("\"type\": 3")) //Farmer
                    {
                        string[] lines = n.Split(new char[] { '\n' }); ;
                        FullNodeConnections fnc = new FullNodeConnections();

                        foreach (var l in lines)
                        {
                            string line = l.TrimStart(' ').TrimEnd('\r');
                            if (line.Contains(collon))
                            {
                                newString = line;//.Replace("\r", "");
                                indexOfCollon = newString.IndexOf(collon);
                                stringBeforeCollon = newString.Substring(1, (indexOfCollon - 2));
                                switch (stringBeforeCollon)
                                {
                                    case "node_id":
                                        fnc.NodeId = newString.Substring(indexOfCollon + 1).Replace(",", "").Replace("\"", "").Replace(" ", "");
                                        break;
                                    case "type":
                                        fnc.Type = newString.Substring(indexOfCollon + 1).Replace(" ", "");
                                        fnc.Name = "Farmer";

                                        if (!string.IsNullOrWhiteSpace(fnc.NodeId))  //Farmer
                                        {
                                            if (connectedFarmers.Find(x => x.NodeId.Contains(fnc.NodeId)) != null) //Already exists
                                            {
                                                connectedFarmers.Find(x => x.NodeId.Contains(fnc.NodeId)).Status = true;
                                            }
                                            else //if New Connected
                                            {
                                                fnc.Status = true;
                                                connectedFarmers.Add(fnc);
                                            }
                                        }
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                clsStatus.AddLogMesssage($"Error in UpdateConnectedFarmerList: {ex.Message}");
            }
        }

        public void UpdateConnectedHravesterList(string farmerNetwork)
        {
            foreach (var item in connectedHarvesters)
            {
                item.Status = false;
            }

            try
            {
                string[] nodes = farmerNetwork.Split("{");
                char collon = ':';
                string newString = string.Empty;
                int indexOfCollon = -1;
                string stringBeforeCollon = "";

                foreach (var n in nodes)
                {
                    string[] lines = n.Split(new char[] { '\n' }); ;
                    FarmerConnections fc = new FarmerConnections();

                    foreach (var l in lines)
                    {
                        if (n.Contains("\"type\": 2")) //Harvester
                        {
                            string line = l.TrimStart(' ').TrimEnd('\r');
                            if (line.Contains(collon))
                            {
                                newString = line;//.Replace("\r", "");
                                indexOfCollon = newString.IndexOf(collon);
                                stringBeforeCollon = newString.Substring(1, (indexOfCollon - 2));
                                switch (stringBeforeCollon)
                                {
                                    case "node_id":
                                        fc.NodeId = newString.Substring(indexOfCollon + 1).Replace(",", "").Replace("\"", "").Replace(" ", "");
                                        break;
                                    case "type":
                                        fc.Type = newString.Substring(indexOfCollon + 1).Replace(" ", "");
                                        fc.Name = "Harvester";

                                        if (!string.IsNullOrWhiteSpace(fc.NodeId))  //Harvester
                                        {
                                            if (connectedHarvesters.Find(x => x.NodeId.Contains(fc.NodeId)) != null) //Already exists
                                            {
                                                connectedHarvesters.Find(x => x.NodeId.Contains(fc.NodeId)).Status = true;
                                            }
                                            else //if New Connected
                                            {
                                                fc.Status = true;
                                                connectedHarvesters.Add(fc);
                                            }
                                        }
                                        break;
                                        case "peer_host":
                                            fc.Host = newString.Substring(indexOfCollon + 1).Replace(",", "").Replace("\"", "").Replace(" ", "");
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                clsStatus.AddLogMesssage($"Error in UpdateConnectedHravesterList: {ex.Message}");

            }
        }

        public JArray FindDisconnected(ref List<ConnectedNode> connList, string connType)
        {
            int tobeRemoved = 0;
            JArray discList = new JArray();
            foreach (var item in connList)
            {
                if (item.Status == false)
                {
                    JObject obj = new JObject();
                    obj.Add("node_id", item.NodeId);
                    obj.Add("status", "Diconnected");
                    obj.Add("peer_host", item.Host);
                    discList.Add(obj);
                    Net.clsStatus.AddLogMesssage($"{connType}: Disconnected");
                    Net.clsStatus.AddLogMesssage($":{item.NodeId}, peer_host:{item.Host}");
                    tobeRemoved++;
                }
            }

            int removed = connList.RemoveAll(x => x.Status == false);

            if (removed != tobeRemoved)
            {
                Net.clsStatus.AddLogMesssage($"Removed:{removed}, 2bRemoved:{tobeRemoved}");
            }
            return discList;
        }

        public JArray FindStoppedFarmingToPool(ref Dictionary<string, Chia.Net.PoolInfo> _launcherId_PointsStart_Pair)
        {
            JArray discList = new JArray();

            try
            {
                foreach (var k in _launcherId_PointsStart_Pair)
                {
                    if (!string.IsNullOrEmpty(k.Value.FarmingStatus))
                    {
                        JObject obj = new JObject();
                        obj.Add("launcher_id", k.Key);
                        obj.Add("pool_farming_status", k.Value.FarmingStatus);
                        discList.Add(obj);
                    }
                }
            }
            catch (Exception ex)
            {
                clsStatus.AddLogMesssage($"FindStoppedFarmingToPool: {ex.Message}");
            }
            return discList;
        }


    }
}
