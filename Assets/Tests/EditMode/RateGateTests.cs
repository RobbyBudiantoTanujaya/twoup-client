using NUnit.Framework;
using TwoUp.Logic;

namespace TwoUp.Tests.EditMode
{
    public class RateGateTests
    {
        [Test]
        public void TryPass_FirstCallPasses()
        {
            var gate = new RateGate(0.05f);

            Assert.IsTrue(gate.TryPass("k", 0f));
        }

        [Test]
        public void TryPass_WithinIntervalBlocked()
        {
            var gate = new RateGate(0.05f);

            Assert.IsTrue(gate.TryPass("k", 0f));
            Assert.IsFalse(gate.TryPass("k", 0.03f));
        }

        [Test]
        public void TryPass_AfterIntervalPasses()
        {
            var gate = new RateGate(0.05f);

            Assert.IsTrue(gate.TryPass("k", 0f));
            Assert.IsTrue(gate.TryPass("k", 0.06f));
        }

        [Test]
        public void TryPass_KeysIndependent()
        {
            var gate = new RateGate(0.05f);

            Assert.IsTrue(gate.TryPass("a", 0f));
            Assert.IsTrue(gate.TryPass("b", 0f));
        }
    }
}
