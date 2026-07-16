using NUnit.Framework;
using TwoUp.UI;

namespace TwoUp.Tests.EditMode
{
    public class EmoteWheelTests
    {
        [Test]
        public void EmoteIds_HasSixBaseEmotes()
        {
            CollectionAssert.AreEqual(
                new[] { "emote_thumbsup", "emote_lol", "emote_wow", "emote_cry", "emote_fire", "emote_gg" },
                EmoteWheelController.EmoteIds);
        }
    }
}
