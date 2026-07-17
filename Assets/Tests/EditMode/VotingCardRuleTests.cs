using NUnit.Framework;
using Twoup.V1;
using TwoUp.UI;

namespace TwoUp.Tests.EditMode
{
    public class VotingCardRuleTests
    {
        [SetUp]
        public void SetUp()
        {
            EconomyState.ResetForTests();
        }

        [Test]
        public void CardEnabled_BotPickerAlwaysTrue()
        {
            Assert.IsTrue(VotingController.CardEnabled("connect_four", true));

            EconomyState.ApplyWallet(new WalletUpdate { CoinBalance = 0 });
            var cfg = new EconomyConfig();
            cfg.GameCoinCost["connect_four"] = 100;
            EconomyState.ApplyConfig(cfg);

            Assert.IsTrue(VotingController.CardEnabled("connect_four", true));
            Assert.IsTrue(VotingController.CardEnabled("unknown_game", true));
        }

        [Test]
        public void CardEnabled_FollowsAffordability()
        {
            EconomyState.ApplyWallet(new WalletUpdate { CoinBalance = 2 });
            var cfg = new EconomyConfig();
            cfg.GameCoinCost["air_hockey"] = 2;
            cfg.GameCoinCost["battleship"] = 3;
            EconomyState.ApplyConfig(cfg);

            Assert.AreEqual(EconomyState.CanAfford("air_hockey"), VotingController.CardEnabled("air_hockey", false));
            Assert.AreEqual(EconomyState.CanAfford("battleship"), VotingController.CardEnabled("battleship", false));
            Assert.IsTrue(VotingController.CardEnabled("air_hockey", false));
            Assert.IsFalse(VotingController.CardEnabled("battleship", false));
        }
    }
}
