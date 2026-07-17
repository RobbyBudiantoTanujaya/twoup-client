using System.Collections.Generic;
using Twoup.V1;

namespace TwoUp
{
    /// <summary>
    /// Single source of coin balance/prices for all controllers (S2/S5/S7/S9). Updated only by
    /// NetworkClient from EconomyConfig/WalletUpdate/DailyRewardClaimed; controllers render from
    /// here and subscribe to Changed. No price/reward numbers are hardcoded — the server decides.
    /// </summary>
    public static class EconomyState
    {
        public static int CoinBalance { get; private set; }
        public static int StreakCount { get; private set; }
        public static bool DailyRewardAvailable { get; private set; }
        public static int NextDailyRewardCoins { get; private set; }
        public static int AdsRemainingToday { get; private set; }
        public static IReadOnlyDictionary<string, int> GameCoinCost { get; private set; }
        public static int AdRewardCoins { get; private set; }

        public static event System.Action Changed;

        public static void ApplyConfig(EconomyConfig cfg)
        {
            GameCoinCost = new Dictionary<string, int>(cfg.GameCoinCost);
            AdRewardCoins = cfg.AdRewardCoins;
            Changed?.Invoke();
        }

        public static void ApplyWallet(WalletUpdate w)
        {
            CoinBalance = w.CoinBalance;
            StreakCount = w.StreakCount;
            DailyRewardAvailable = w.DailyRewardAvailable;
            NextDailyRewardCoins = w.NextDailyRewardCoins;
            AdsRemainingToday = w.AdsRemainingToday;
            Changed?.Invoke();
        }

        public static void ApplyDailyClaimed(DailyRewardClaimed d)
        {
            CoinBalance = d.CoinBalance;
            StreakCount = d.StreakCount;
            DailyRewardAvailable = false;
            Changed?.Invoke();
        }

        /// <summary>0 when gameId is unknown or EconomyConfig hasn't arrived yet (fail-open render; server still enforces).</summary>
        public static int CostOf(string gameId)
        {
            if (GameCoinCost == null || gameId == null)
                return 0;
            return GameCoinCost.TryGetValue(gameId, out var cost) ? cost : 0;
        }

        public static bool CanAfford(string gameId) => CoinBalance >= CostOf(gameId);

        /// <summary>Used by tests only.</summary>
        public static void ResetForTests()
        {
            CoinBalance = 0;
            StreakCount = 0;
            DailyRewardAvailable = false;
            NextDailyRewardCoins = 0;
            AdsRemainingToday = 0;
            GameCoinCost = null;
            AdRewardCoins = 0;
        }
    }
}
