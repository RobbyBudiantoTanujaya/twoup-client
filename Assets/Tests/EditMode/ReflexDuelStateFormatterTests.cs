using NUnit.Framework;
using TwoUp.Logic;

namespace TwoUp.Tests.EditMode
{
    public class ReflexDuelStateFormatterTests
    {
        [Test]
        public void FormatReactionMs_HandlesZeroAndNegative()
        {
            Assert.AreEqual("250 ms", ReflexDuelStateFormatter.FormatReactionMs(250));
            Assert.AreEqual("-", ReflexDuelStateFormatter.FormatReactionMs(0));
            Assert.AreEqual("-", ReflexDuelStateFormatter.FormatReactionMs(-10));
        }
    }
}
