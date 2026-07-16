using System.Collections.Generic;

namespace TwoUp.Logic
{
    public class RateGate
    {
        private readonly float minIntervalSeconds;
        private readonly Dictionary<string, float> lastPassAt = new Dictionary<string, float>();

        public RateGate(float minIntervalSeconds)
        {
            this.minIntervalSeconds = minIntervalSeconds;
        }

        public bool TryPass(string key, float nowSeconds)
        {
            if (lastPassAt.TryGetValue(key, out var last) && nowSeconds - last < minIntervalSeconds)
                return false;

            lastPassAt[key] = nowSeconds;
            return true;
        }
    }
}
