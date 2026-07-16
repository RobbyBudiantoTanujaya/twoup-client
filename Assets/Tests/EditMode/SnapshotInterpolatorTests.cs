using NUnit.Framework;
using TwoUp.Logic;
using UnityEngine;

namespace TwoUp.Tests.EditMode
{
    public class SnapshotInterpolatorTests
    {
        [Test]
        public void Sample_MidTick_ReturnsMidpoint()
        {
            var interpolator = new SnapshotInterpolator(0.05f, 0.1f);

            interpolator.Push(new Vector2(0f, 0f), 0f);
            interpolator.Push(new Vector2(100f, 0f), 0.05f);

            var result = interpolator.Sample(0.075f);

            Assert.AreEqual(50f, result.x, 0.01f);
        }

        [Test]
        public void Sample_BeyondStale_SnapsToLatest()
        {
            var interpolator = new SnapshotInterpolator(0.05f, 0.1f);

            interpolator.Push(new Vector2(0f, 0f), 0f);
            interpolator.Push(new Vector2(100f, 0f), 0.05f);

            var result = interpolator.Sample(0.2f);

            Assert.AreEqual(new Vector2(100f, 0f), result);
        }

        [Test]
        public void Sample_NoData_ReturnsZero()
        {
            var interpolator = new SnapshotInterpolator(0.05f, 0.1f);

            var result = interpolator.Sample(1f);

            Assert.AreEqual(Vector2.zero, result);
        }

        [Test]
        public void Sample_SingleSnapshot_ReturnsIt()
        {
            var interpolator = new SnapshotInterpolator(0.05f, 0.1f);

            interpolator.Push(new Vector2(3f, 4f), 0f);

            var result = interpolator.Sample(0.02f);

            Assert.AreEqual(new Vector2(3f, 4f), result);
        }
    }
}
