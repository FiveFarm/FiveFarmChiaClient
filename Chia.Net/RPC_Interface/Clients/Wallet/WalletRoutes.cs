using System;

namespace Chia.NET.Clients
{
    internal static class WalletRoutes
    {
        public static Uri GetWalletBalance(string apiUrl)
            => new Uri(apiUrl + "get_wallet_balance");

        public static Uri GetWalletAddress(string apiUrl)
            => new Uri(apiUrl + "get_next_address");

        public static Uri GetTransactions(string apiUrl)
    => new Uri(apiUrl + "get_transactions");

        public static Uri GetWallets(string apiUrl)
=> new Uri(apiUrl + "get_wallets");

        public static Uri GetHeightInfo(string apiUrl)
            => new Uri(apiUrl + "get_farmed_amount");

        //get_wallets
        //get_transactions
    }
}
