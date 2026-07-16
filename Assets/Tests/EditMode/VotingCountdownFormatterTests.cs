using NUnit.Framework;
using TwoUp.Logic;

namespace TwoUp.Tests.EditMode
{
    public class VotingCountdownFormatterTests
    {
        [Test]
        public void Format_CeilsToWholeSeconds()
        {
            Assert.AreEqual("3", VotingCountdownFormatter.Format(2999));
            Assert.AreEqual("3", VotingCountdownFormatter.Format(3000));
            Assert.AreEqual("4", VotingCountdownFormatter.Format(3001));
            Assert.AreEqual("1", VotingCountdownFormatter.Format(1));
            Assert.AreEqual("0", VotingCountdownFormatter.Format(0));
            Assert.AreEqual("0", VotingCountdownFormatter.Format(-5));
        }
    }
}
