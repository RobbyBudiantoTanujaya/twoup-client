using UnityEngine;

namespace TwoUp.Logic
{
    public class SnapshotInterpolator
    {
        private struct Snapshot
        {
            public Vector2 Position;
            public float ReceivedAtSeconds;
        }

        private readonly float tickIntervalSeconds;
        private readonly float staleSnapSeconds;

        private Snapshot prev;
        private Snapshot latest;
        private int snapshotCount;

        public SnapshotInterpolator(float tickIntervalSeconds, float staleSnapSeconds)
        {
            this.tickIntervalSeconds = tickIntervalSeconds;
            this.staleSnapSeconds = staleSnapSeconds;
        }

        public void Push(Vector2 position, float receivedAtSeconds)
        {
            prev = latest;
            latest = new Snapshot { Position = position, ReceivedAtSeconds = receivedAtSeconds };
            snapshotCount = Mathf.Min(snapshotCount + 1, 2);
        }

        public Vector2 Sample(float nowSeconds)
        {
            if (snapshotCount == 0)
                return Vector2.zero;

            if (snapshotCount == 1)
                return latest.Position;

            if (nowSeconds - latest.ReceivedAtSeconds > staleSnapSeconds)
                return latest.Position;

            float t = tickIntervalSeconds > 0f
                ? (nowSeconds - latest.ReceivedAtSeconds) / tickIntervalSeconds
                : 1f;
            t = Mathf.Clamp01(t);

            return Vector2.Lerp(prev.Position, latest.Position, t);
        }
    }
}
