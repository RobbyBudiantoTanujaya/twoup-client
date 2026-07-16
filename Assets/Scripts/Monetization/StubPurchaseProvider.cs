using System;

namespace TwoUp.Monetization
{
    public class StubPurchaseProvider : IPurchaseProvider
    {
        private const string StubToken = "stub-purchase-token";

        public void PurchasePremium(Action<string> onToken, Action<string> onFailed)
        {
            onToken?.Invoke(StubToken);
        }
    }
}
