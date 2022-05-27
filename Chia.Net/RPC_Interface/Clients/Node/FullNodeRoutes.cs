using System;

namespace Chia.NET.Clients
{
    internal static class FullNodeRoutes
    {
        public static Uri GetNetworkInfo(string apiUrl)
    => new Uri(apiUrl + "get_network_info");
        public static Uri GetBlockchainState(string apiUrl)
            => new Uri(apiUrl + "get_blockchain_state");

        public static Uri GetBlock(string apiUrl)
            => new Uri(apiUrl + "get_block");

        public static Uri GetBlocks(string apiUrl)
            => new Uri(apiUrl + "get_blocks");

        public static Uri GetPoolInfo(string apiUrl)
       => new Uri(apiUrl + "pool_info");
    }
}
