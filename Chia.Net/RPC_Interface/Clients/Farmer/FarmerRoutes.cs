using System;

namespace Chia.NET.Clients.Farmer
{
    public static class FarmerRoutes
    {
        public static Uri SetRewardTargets(string apiUrl)
            => new Uri(apiUrl + "set_reward_targets");

        public static Uri GetPoolState(string apiUrl)
           => new Uri(apiUrl + "get_pool_state");
        
        public static Uri GetPoolInfo(string apiUrl)
           => new Uri(apiUrl + "pool_info");
    }
}
