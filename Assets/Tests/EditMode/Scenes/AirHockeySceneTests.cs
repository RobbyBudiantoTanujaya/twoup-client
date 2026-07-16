using NUnit.Framework;
using TwoUp.UI;
using UnityEditor;

namespace TwoUp.Tests.EditMode.Scenes
{
    public class AirHockeySceneTests
    {
        [Test]
        public void AirHockey_HasTableAndActors()
        {
            SceneAsserts.OpenScene("Assets/Scenes/AirHockey.unity");

            SceneAsserts.AssertObject("UICanvas/Screen_AirHockey/Panel_Table");
            SceneAsserts.AssertObject("UICanvas/Screen_AirHockey/Panel_Table/Img_CenterLine");
            SceneAsserts.AssertObject("UICanvas/Screen_AirHockey/Panel_Table/Img_Puck");
            SceneAsserts.AssertObject("UICanvas/Screen_AirHockey/Panel_Table/Img_MalletSelf");
            SceneAsserts.AssertObject("UICanvas/Screen_AirHockey/Panel_Table/Img_MalletOpponent");
            SceneAsserts.AssertObject("UICanvas/Screen_AirHockey/Text_Score");
            SceneAsserts.AssertObject("UICanvas/Screen_AirHockey/Text_Timer");
            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_AirHockey/Banner_SuddenDeath/Text");
            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_AirHockey/Text_Toast");

            var bannerSuddenDeath = SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_AirHockey/Banner_SuddenDeath");
            Assert.IsFalse(bannerSuddenDeath.activeSelf, "Banner_SuddenDeath should start inactive");
        }

        [Test]
        public void AirHockey_ControllerRefsWired()
        {
            SceneAsserts.OpenScene("Assets/Scenes/AirHockey.unity");

            var screen = SceneAsserts.AssertObject("UICanvas/Screen_AirHockey");
            var controller = screen.GetComponent<AirHockeyController>();
            Assert.IsNotNull(controller, "Screen_AirHockey is missing AirHockeyController");

            SceneAsserts.AssertRefNotNull(controller, "tableRect");
            SceneAsserts.AssertRefNotNull(controller, "tableEventTrigger");
            SceneAsserts.AssertRefNotNull(controller, "puckRect");
            SceneAsserts.AssertRefNotNull(controller, "malletSelfRect");
            SceneAsserts.AssertRefNotNull(controller, "malletOpponentRect");
            SceneAsserts.AssertRefNotNull(controller, "scoreText");
            SceneAsserts.AssertRefNotNull(controller, "timerText");
            SceneAsserts.AssertRefNotNull(controller, "bannerSuddenDeath");
            SceneAsserts.AssertRefNotNull(controller, "toastText");

            var table = SceneAsserts.AssertObject("UICanvas/Screen_AirHockey/Panel_Table");
            var trigger = table.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            Assert.IsNotNull(trigger, "Panel_Table is missing an EventTrigger");
        }
    }
}
