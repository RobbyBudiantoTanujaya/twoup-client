using NUnit.Framework;
using TMPro;
using TwoUp.Tests.EditMode.Scenes;
using TwoUp.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TwoUp.Tests.EditMode
{
    public class ShopEconomySceneTests
    {
        [Test]
        public void Shop_CoinBalanceObjectExists_TicketGone()
        {
            SceneAsserts.OpenScene("Assets/Scenes/Shop.unity");

            var coinText = SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Shop/Text_CoinBalance");
            Assert.IsNotNull(coinText.GetComponent<TMP_Text>(), "Text_CoinBalance is missing a TMP_Text component");

            Assert.IsFalse(AnyObjectNamed("Text_TicketBalance"), "Text_TicketBalance must not exist anywhere in the scene");

            var getCoinsPanel = SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Shop/Panel_GetCoins");
            Assert.IsFalse(getCoinsPanel.activeSelf, "Panel_GetCoins should be inactive by default");

            var screen = SceneAsserts.AssertObject("UICanvas/Screen_Shop");
            var controller = screen.GetComponent<ShopController>();
            Assert.IsNotNull(controller, "Screen_Shop is missing ShopController");

            SceneAsserts.AssertRefNotNull(controller, "coinBalanceText");
            SceneAsserts.AssertRefNotNull(controller, "getCoinsPanel");
        }

        private static bool AnyObjectNamed(string name)
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.gameObject.name == name)
                        return true;
                }
            }
            return false;
        }
    }
}
