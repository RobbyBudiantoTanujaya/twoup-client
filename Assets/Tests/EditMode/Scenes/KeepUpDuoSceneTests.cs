using NUnit.Framework;
using TwoUp.UI;

namespace TwoUp.Tests.EditMode.Scenes
{
    public class KeepUpDuoSceneTests
    {
        [Test]
        public void KeepUp_HasBallPaddlesGlows()
        {
            SceneAsserts.OpenScene("Assets/Scenes/KeepUpDuo.unity");

            SceneAsserts.AssertObject("UICanvas/Screen_KeepUpDuo/Panel_Arena/Img_Ball");
            SceneAsserts.AssertObject("UICanvas/Screen_KeepUpDuo/Panel_Arena/Img_Paddle0");
            SceneAsserts.AssertObject("UICanvas/Screen_KeepUpDuo/Panel_Arena/Img_Paddle1");
            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_KeepUpDuo/Panel_Arena/Glow_Turn0");
            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_KeepUpDuo/Panel_Arena/Glow_Turn1");

            var screen = SceneAsserts.AssertObject("UICanvas/Screen_KeepUpDuo");
            var controller = screen.GetComponent<KeepUpDuoController>();
            Assert.IsNotNull(controller, "Screen_KeepUpDuo is missing KeepUpDuoController");

            SceneAsserts.AssertRefNotNull(controller, "arenaRect");
            SceneAsserts.AssertRefNotNull(controller, "arenaEventTrigger");
            SceneAsserts.AssertRefNotNull(controller, "paddle0Rect");
            SceneAsserts.AssertRefNotNull(controller, "paddle1Rect");
            SceneAsserts.AssertRefNotNull(controller, "youMarkerRect");
            SceneAsserts.AssertRefNotNull(controller, "ballRect");
            SceneAsserts.AssertRefNotNull(controller, "glow0Rect");
            SceneAsserts.AssertRefNotNull(controller, "glow1Rect");
            SceneAsserts.AssertRefNotNull(controller, "comboText");
            SceneAsserts.AssertRefNotNull(controller, "toastText");
        }

        [Test]
        public void KeepUp_GlowsInactiveByDefault()
        {
            SceneAsserts.OpenScene("Assets/Scenes/KeepUpDuo.unity");

            var glow0 = SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_KeepUpDuo/Panel_Arena/Glow_Turn0");
            var glow1 = SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_KeepUpDuo/Panel_Arena/Glow_Turn1");

            Assert.IsFalse(glow0.activeSelf, "Glow_Turn0 should start inactive");
            Assert.IsFalse(glow1.activeSelf, "Glow_Turn1 should start inactive");
        }
    }
}
