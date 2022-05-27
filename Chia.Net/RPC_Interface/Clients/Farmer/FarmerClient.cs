using Chia.NET.Clients.Farmer;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Chia.NET.Clients
{
    public sealed class FarmerClient : ChiaApiClient
    {
        private new const string ApiUrl = "https://localhost:8559/";

        public FarmerClient()
            : base("farmer", ApiUrl)
        {
        }

        public async Task SetRewardTargets(string targetAddress)
            => await PostAsync(FarmerRoutes.SetRewardTargets(ApiUrl), new Dictionary<string, string>()
            {
                ["farmer_target"] = targetAddress,
                ["pool_target"] = targetAddress,
            });

        public async Task<string> GetPoolState()
        {
            var result = await PostAsync(FarmerRoutes.GetPoolState(ApiUrl));
            Common.CommonConstants.SaveDebugLog($"GetPoolState: Data Length:{result.ToString().Length}",false,true);
            return result;
        } 
        public async Task<string> GetPoolInfo()
        {
            var result = await PostAsync(FarmerRoutes.GetPoolInfo(ApiUrl));
            return result;
        }

    }
}
