//using Chia.NET.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Chia.NET.Clients
{
    public sealed class PoolClient : ChiaApiClient
    {
        private new string ApiUrl = "";

        public PoolClient(string poolUrl) : base(poolUrl)
        {
            ApiUrl = poolUrl;
        }

        public async Task<string> GetPoolInfo()
        {
            var result = await GetAsync(PoolRoutes.GetPoolInfo(ApiUrl), null, 30);
            return result;
        }
    }
}
