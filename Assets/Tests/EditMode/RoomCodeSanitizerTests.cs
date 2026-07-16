using NUnit.Framework;
using TwoUp.Logic;

namespace TwoUp.Tests.EditMode
{
    public class RoomCodeSanitizerTests
    {
        [Test]
        public void Sanitize_UppercasesAndStripsAmbiguousChars()
        {
            Assert.AreEqual("ABCD", RoomCodeSanitizer.Sanitize("ab0o1i-cd"));
        }

        [Test]
        public void Sanitize_TruncatesToSix()
        {
            Assert.AreEqual("ABCDEF", RoomCodeSanitizer.Sanitize("abcdefgh"));
        }
    }
}
