using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Chia.NET.Clients
{
    public sealed class WalletClient : ChiaApiClient
    {
        private new const string ApiUrl = "https://localhost:9256/";

        public WalletClient()
            : base("wallet", ApiUrl)
        {
        }

        
        /// <summary>
        /// Retrieves balances for a wallet.
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetWalletBalance(int walletId)
        {
            var result = await PostAsync(WalletRoutes.GetWalletBalance(ApiUrl), new Dictionary<string, string>()
            {
                ["wallet_id"] = $"{walletId}"
            });
            return result;
        }

        public async Task<string> GetTransactions(int walletId)
        {
            var result = await PostAsync(WalletRoutes.GetTransactions(ApiUrl),new Dictionary<string, string>()
            {
                ["wallet_id"] = $"{walletId}"
            });
            return result;
        }

        /// <summary>
        /// Retrieves the address of the wallet at the given id for the current key.
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetWalletAddressAsync(int walletId, bool generateAddress)
        {
            var result = await PostAsync(WalletRoutes.GetWalletAddress(ApiUrl), new Dictionary<string, string>()
            {
                ["wallet_id"] = $"{walletId}",
                ["new_address"] = $"{generateAddress}"
            });
            return result;
        }

        public async Task<string> GetWallets()
        {
            var result = await PostAsync(WalletRoutes.GetWallets(ApiUrl), new Dictionary<string, string>());
            return result;
        }

        public async Task<string> GetHeightInfo()
        {
            var result = await PostAsync(WalletRoutes.GetHeightInfo(ApiUrl), new Dictionary<string, string>());
            Common.CommonConstants.SaveDebugLog($"GetHeightInfo: Data Length:{result.ToString().Length}",false,true);
            return result;
        }

    }
}
