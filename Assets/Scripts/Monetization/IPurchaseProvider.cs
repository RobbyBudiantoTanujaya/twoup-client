using System;

namespace TwoUp.Monetization
{
    public interface IPurchaseProvider
    {
        void PurchasePremium(Action<string> onToken, Action<string> onFailed);
    }
}
