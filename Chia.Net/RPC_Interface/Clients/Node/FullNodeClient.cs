//using Chia.NET.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Chia.NET.Clients
{
    public sealed class FullNodeClient : ChiaApiClient
    {
        private new const string ApiUrl = "https://localhost:8555/";

        public FullNodeClient()
            : base("full_node", ApiUrl)
        {
        }

        public async Task<string> GetNetworkInfo()
        {
            var result = await PostAsync(FullNodeRoutes.GetNetworkInfo(ApiUrl));
            Common.CommonConstants.SaveDebugLog($"GetNetworkInfo: Data Length:{result.ToString().Length}",false,true);
            return result;
        }

        public async Task<string> GetBlockChainState()
        {
            var result = await PostAsync(FullNodeRoutes.GetBlockchainState(ApiUrl));
            Common.CommonConstants.SaveDebugLog($"GetBlockChainState: Data Length:{result.ToString().Length}",false,true);
            return result;
        }
        public async Task<string> GetPoolInfo()
        {
            var result = await PostAsync(FullNodeRoutes.GetPoolInfo(ApiUrl));
            return result;
        }
    }
}
