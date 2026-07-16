using NUnit.Framework;
using TwoUp.UI;
using UnityEngine.UI;

namespace TwoUp.Tests.EditMode.Scenes
{
    public class SettingsSceneTests
    {
        [Test]
        public void Settings_HasTogglesAndVersion()
        {
            SceneAsserts.OpenScene("Assets/Scenes/Settings.unity");

            var soundToggle = SceneAsserts.AssertObject("UICanvas/Screen_Settings/Toggle_Sound");
            Assert.IsNotNull(soundToggle.GetComponent<Toggle>(), "Toggle_Sound is missing Toggle");

            var musicToggle = SceneAsserts.AssertObject("UICanvas/Screen_Settings/Toggle_Music");
            Assert.IsNotNull(musicToggle.GetComponent<Toggle>(), "Toggle_Music is missing Toggle");

            var vibrationToggle = SceneAsserts.AssertObject("UICanvas/Screen_Settings/Toggle_Vibration");
            Assert.IsNotNull(vibrationToggle.GetComponent<Toggle>(), "Toggle_Vibration is missing Toggle");

            SceneAsserts.AssertObject("UICanvas/Screen_Settings/Text_Version");

            var screen = SceneAsserts.AssertObject("UICanvas/Screen_Settings");
            var controller = screen.GetComponent<SettingsController>();
            Assert.IsNotNull(controller, "Screen_Settings is missing SettingsController");

            SceneAsserts.AssertRefNotNull(controller, "backButton");
            SceneAsserts.AssertRefNotNull(controller, "soundToggle");
            SceneAsserts.AssertRefNotNull(controller, "musicToggle");
            SceneAsserts.AssertRefNotNull(controller, "vibrationToggle");
            SceneAsserts.AssertRefNotNull(controller, "restorePurchaseButton");
            SceneAsserts.AssertRefNotNull(controller, "linksText");
            SceneAsserts.AssertRefNotNull(controller, "versionText");
            SceneAsserts.AssertRefNotNull(controller, "toastText");
        }
    }
}
