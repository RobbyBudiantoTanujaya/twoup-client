using NUnit.Framework;
using Twoup.V1;
using TwoUp;

namespace TwoUp.Tests.EditMode
{
    public class EconomyStateTests
    {
        [SetUp]
        public void SetUp()
        {
            EconomyState.ResetForTests();
        }

        [Test]
        public void CostOf_ReturnsConfigValue_UnknownGameZero()
        {
            Assert.AreEqual(0, EconomyState.CostOf("battleship"));

            var cfg = new EconomyConfig();
            cfg.GameCoinCost["battleship"] = 3;
            EconomyState.ApplyConfig(cfg);

            Assert.AreEqual(3, EconomyState.CostOf("battleship"));
            Assert.AreEqual(0, EconomyState.CostOf("nope"));
        }

        [Test]
        public void CanAfford_ComparesBalanceToCost()
        {
            EconomyState.ApplyWallet(new WalletUpdate { CoinBalance = 2 });

            var cfg = new EconomyConfig();
            cfg.GameCoinCost["air_hockey"] = 2;
            cfg.GameCoinCost["battleship"] = 3;
            EconomyState.ApplyConfig(cfg);

            Assert.IsTrue(EconomyState.CanAfford("air_hockey"));
            Assert.IsFalse(EconomyState.CanAfford("battleship"));
        }

        [Test]
        public void ApplyWallet_RaisesChangedEvent()
        {
            int changedCount = 0;
            System.Action handler = () => changedCount++;
            EconomyState.Changed += handler;
            try
            {
                EconomyState.ApplyWallet(new WalletUpdate { CoinBalance = 5 });
            }
            finally
            {
                EconomyState.Changed -= handler;
            }

            Assert.AreEqual(1, changedCount);
        }

        [Test]
        public void ApplyDailyClaimed_UpdatesBalanceAndClearsAvailable()
        {
            EconomyState.ApplyDailyClaimed(new DailyRewardClaimed { CoinBalance = 25, StreakCount = 2 });

            Assert.AreEqual(25, EconomyState.CoinBalance);
            Assert.IsFalse(EconomyState.DailyRewardAvailable);
        }
    }
}
