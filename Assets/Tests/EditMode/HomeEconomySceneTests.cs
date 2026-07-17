using NUnit.Framework;
using TMPro;
using TwoUp.Tests.EditMode.Scenes;
using TwoUp.UI;

namespace TwoUp.Tests.EditMode
{
    public class HomeEconomySceneTests
    {
        [Test]
        public void Home_HasCoinEconomyObjects()
        {
            SceneAsserts.OpenScene("Assets/Scenes/Home.unity");

            var coinText = SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Home/Text_CoinBalance");
            Assert.IsNotNull(coinText.GetComponent<TMP_Text>(), "Text_CoinBalance is missing a TMP_Text component");

            var dailyPanel = SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Home/Panel_DailyReward");
            Assert.IsFalse(dailyPanel.activeSelf, "Panel_DailyReward should be inactive by default");
            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Home/Panel_DailyReward/Chrome/Btn_ClaimDaily");

            var getCoinsPanel = SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Home/Panel_GetCoins");
            Assert.IsFalse(getCoinsPanel.activeSelf, "Panel_GetCoins should be inactive by default");

            var screen = SceneAsserts.AssertObject("UICanvas/Screen_Home");
            var controller = screen.GetComponent<HomeController>();
            Assert.IsNotNull(controller, "Screen_Home is missing HomeController");

            SceneAsserts.AssertRefNotNull(controller, "coinBalanceText");
            SceneAsserts.AssertRefNotNull(controller, "dailyRewardPanel");
            SceneAsserts.AssertRefNotNull(controller, "claimDailyButton");
            SceneAsserts.AssertRefNotNull(controller, "getCoinsPanel");
        }
    }
}
