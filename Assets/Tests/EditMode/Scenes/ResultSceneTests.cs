using NUnit.Framework;
using TwoUp.UI;

namespace TwoUp.Tests.EditMode.Scenes
{
    public class ResultSceneTests
    {
        [Test]
        public void Result_HasAllControlsWired()
        {
            SceneAsserts.OpenScene("Assets/Scenes/Result.unity");

            SceneAsserts.AssertObject("UICanvas/Screen_Result/Text_Headline");
            SceneAsserts.AssertObject("UICanvas/Screen_Result/Text_LedgerLine");
            SceneAsserts.AssertObject("UICanvas/Screen_Result/Text_StreakLine");
            SceneAsserts.AssertObject("UICanvas/Screen_Result/Text_SeriesLine");
            SceneAsserts.AssertObject("UICanvas/Screen_Result/Btn_Rematch");
            SceneAsserts.AssertObject("UICanvas/Screen_Result/Btn_NextGame");
            SceneAsserts.AssertObject("UICanvas/Screen_Result/Btn_Leave");
            SceneAsserts.AssertObject("UICanvas/Screen_Result/Text_OpponentDecision");
            SceneAsserts.AssertObject("UICanvas/Screen_Result/Text_Countdown");

            var screen = SceneAsserts.AssertObject("UICanvas/Screen_Result");
            var controller = screen.GetComponent<ResultController>();
            Assert.IsNotNull(controller, "Screen_Result is missing ResultController");

            SceneAsserts.AssertRefNotNull(controller, "headlineText");
            SceneAsserts.AssertRefNotNull(controller, "ledgerLineText");
            SceneAsserts.AssertRefNotNull(controller, "streakLineText");
            SceneAsserts.AssertRefNotNull(controller, "seriesLineText");
            SceneAsserts.AssertRefNotNull(controller, "rematchButton");
            SceneAsserts.AssertRefNotNull(controller, "nextGameButton");
            SceneAsserts.AssertRefNotNull(controller, "leaveButton");
            SceneAsserts.AssertRefNotNull(controller, "opponentDecisionText");
            SceneAsserts.AssertRefNotNull(controller, "countdownText");
        }

        [Test]
        public void Result_CountdownTextExists()
        {
            SceneAsserts.OpenScene("Assets/Scenes/Result.unity");

            var countdown = SceneAsserts.AssertObject("UICanvas/Screen_Result/Text_Countdown");
            Assert.IsNotNull(countdown.GetComponent<TMPro.TextMeshProUGUI>(), "Text_Countdown is missing a TextMeshProUGUI component");
        }
    }
}
