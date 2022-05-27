using System;

namespace Chia.NET.Clients
{
    public static class SharedRoutes
    {
        public static Uri GetConnections(string apiUrl)
            => new Uri(apiUrl + "get_connections");
    }
}
