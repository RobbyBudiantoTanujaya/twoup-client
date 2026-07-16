using NUnit.Framework;
using TwoUp.Logic;

namespace TwoUp.Tests.EditMode
{
    public class LedgerDeltaFormatterTests
    {
        [Test]
        public void FormatHeadline_MatchesSpec()
        {
            Assert.AreEqual("You 3 : 1 Alex", LedgerDeltaFormatter.FormatHeadline(3, 1, "Alex"));
        }

        [Test]
        public void FormatStreak_EmptyBelowTwo()
        {
            Assert.AreEqual("", LedgerDeltaFormatter.FormatStreak(true, "Alex", 1));
            Assert.AreEqual("", LedgerDeltaFormatter.FormatStreak(false, "Alex", 0));
            Assert.AreEqual("You're on a 3 win streak!", LedgerDeltaFormatter.FormatStreak(true, "Alex", 3));
            Assert.AreEqual("Alex on a 4 win streak", LedgerDeltaFormatter.FormatStreak(false, "Alex", 4));
        }

        [Test]
        public void FormatDuo_NewBestArrowFormat()
        {
            string result = LedgerDeltaFormatter.FormatDuo(52, 52, true);
            StringAssert.Contains("New duo best!", result);
            Assert.AreEqual("New duo best! 52 → 52", result);
            Assert.AreEqual("Score 40 (best 52)", LedgerDeltaFormatter.FormatDuo(40, 52, false));
        }
    }
}
