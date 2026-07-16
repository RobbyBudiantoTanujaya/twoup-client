using NUnit.Framework;
using TwoUp.Monetization;

namespace TwoUp.Tests.EditMode
{
    public class MonetizationStubTests
    {
        [Test]
        public void StubAd_CompletesTrue()
        {
            var provider = new StubRewardedAdProvider();
            bool? result = null;

            provider.Show(completed => result = completed);

            Assert.IsTrue(provider.IsReady);
            Assert.AreEqual(true, result);
        }

        [Test]
        public void StubPurchase_ReturnsToken()
        {
            var provider = new StubPurchaseProvider();
            string token = null;

            provider.PurchasePremium(t => token = t, f => Assert.Fail("onFailed should not be called"));

            Assert.AreEqual("stub-purchase-token", token);
        }
    }
}
