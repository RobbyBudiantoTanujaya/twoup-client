using NUnit.Framework;
using TwoUp.Tests.EditMode.Scenes;
using TwoUp.UI;

namespace TwoUp.Tests.EditMode
{
    public class ResultEconomySceneTests
    {
        [Test]
        public void Result_HasGetCoinsObjects()
        {
            SceneAsserts.OpenScene("Assets/Scenes/Result.unity");

            var getCoinsBtn = SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Result/Btn_GetCoins");
            Assert.IsFalse(getCoinsBtn.activeSelf, "Btn_GetCoins should be inactive by default");

            var getCoinsPanel = SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Result/Panel_GetCoins");
            Assert.IsFalse(getCoinsPanel.activeSelf, "Panel_GetCoins should be inactive by default");

            var screen = SceneAsserts.AssertObject("UICanvas/Screen_Result");
            var controller = screen.GetComponent<ResultController>();
            Assert.IsNotNull(controller, "Screen_Result is missing ResultController");
            SceneAsserts.AssertRefNotNull(controller, "getCoinsPanel");
        }
    }
}
