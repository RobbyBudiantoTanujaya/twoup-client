using NUnit.Framework;

namespace TwoUp.Tests.EditMode
{
    public class SmokeTests
    {
        [Test]
        public void Sanity_AlwaysPasses()
        {
            Assert.AreEqual(2, 1 + 1);
        }

        [Test]
        public void ProtoTypes_Exist()
        {
            Assert.IsNotNull(typeof(Twoup.V1.Envelope));
        }
    }
}
