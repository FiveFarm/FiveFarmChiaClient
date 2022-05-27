using System;

namespace Chia.NET.Clients
{
    internal static class PoolRoutes
    {
        public static Uri GetPoolInfo(string apiUrl)
               => new Uri(apiUrl + "/pool_info");
    }
}
