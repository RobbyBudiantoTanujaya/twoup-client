using NUnit.Framework;
using TwoUp.Logic;

namespace TwoUp.Tests.EditMode
{
    public class DeepLinkParserTests
    {
        [Test]
        public void Parse_ExtractsCodeFromHttpsAndCustomScheme()
        {
            Assert.AreEqual("ABC234", DeepLinkParser.ExtractRoomCode("https://twoup.example.com/r/abc234"));
            Assert.AreEqual("ABC234", DeepLinkParser.ExtractRoomCode("twoup://r/abc234"));
        }

        [Test]
        public void Parse_ReturnsNullForOtherPaths()
        {
            Assert.IsNull(DeepLinkParser.ExtractRoomCode("https://twoup.example.com/lobby"));
            Assert.IsNull(DeepLinkParser.ExtractRoomCode("https://twoup.example.com/r/"));
            Assert.IsNull(DeepLinkParser.ExtractRoomCode(null));
        }
    }
}
