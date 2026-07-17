using NUnit.Framework;
using Twoup.V1;
using TwoUp.UI;

namespace TwoUp.Tests.EditMode
{
    public class ResultRematchRuleTests
    {
        [SetUp]
        public void SetUp()
        {
            EconomyState.ResetForTests();
        }

        [Test]
        public void RematchLabel_VsBotPlain_HumanShowsCost()
        {
            EconomyState.ApplyWallet(new WalletUpdate { CoinBalance = 5 });
            var cfg = new EconomyConfig();
            cfg.GameCoinCost["air_hockey"] = 2;
            EconomyState.ApplyConfig(cfg);

            Assert.AreEqual("Rematch (2c)", ResultController.RematchLabel("air_hockey", false));
            Assert.AreEqual("Rematch", ResultController.RematchLabel("air_hockey", true));
        }

        [Test]
        public void RematchEnabled_FollowsAffordabilityUnlessBot()
        {
            EconomyState.ApplyWallet(new WalletUpdate { CoinBalance = 1 });
            var cfg = new EconomyConfig();
            cfg.GameCoinCost["air_hockey"] = 2;
            EconomyState.ApplyConfig(cfg);

            Assert.IsFalse(ResultController.RematchEnabled("air_hockey", false));
            Assert.IsTrue(ResultController.RematchEnabled("air_hockey", true));
        }
    }
}
