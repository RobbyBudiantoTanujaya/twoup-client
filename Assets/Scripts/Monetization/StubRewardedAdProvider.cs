using System;

namespace TwoUp.Monetization
{
    public class StubRewardedAdProvider : IRewardedAdProvider
    {
        public bool IsReady => true;

        public void Show(Action<bool> onCompleted)
        {
            onCompleted?.Invoke(true);
        }
    }
}
