using System;

namespace TwoUp.Monetization
{
    public interface IRewardedAdProvider
    {
        bool IsReady { get; }
        void Show(Action<bool> onCompleted);
    }
}
