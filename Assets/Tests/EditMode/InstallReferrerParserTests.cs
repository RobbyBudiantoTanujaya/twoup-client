using NUnit.Framework;
using TwoUp.Logic;

namespace TwoUp.Tests.EditMode
{
    public class InstallReferrerParserTests
    {
        [Test]
        public void Parse_ExtractsRoomCodeFromUtmContent()
        {
            Assert.AreEqual("ABC234", InstallReferrerParser.ExtractRoomCode("utm_source=invite&utm_content=ABC234"));
        }

        [Test]
        public void Parse_ReturnsNullWithoutUtmContent()
        {
            Assert.IsNull(InstallReferrerParser.ExtractRoomCode("utm_source=google-play&utm_medium=organic"));
        }
    }
}
