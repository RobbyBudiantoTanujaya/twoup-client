using NUnit.Framework;
using TwoUp.UI;

namespace TwoUp.Tests.EditMode.Scenes
{
    public class ShopSceneTests
    {
        [Test]
        public void Shop_TemplateSiblingPatternCorrect()
        {
            SceneAsserts.OpenScene("Assets/Scenes/Shop.unity");

            var content = SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Shop/List_Items/Viewport/Content");
            var template = SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Shop/List_Items/Viewport/ShopItemRowTemplate");

            Assert.IsFalse(template.activeSelf, "ShopItemRowTemplate must start inactive");
            Assert.AreNotEqual(content.transform, template.transform.parent, "ShopItemRowTemplate must not be a child of Content (populate-from-template law)");
            Assert.IsNotNull(template.GetComponent<ShopItemRowView>(), "ShopItemRowTemplate is missing ShopItemRowView");
        }

        [Test]
        public void Shop_ControllerRefsWired()
        {
            SceneAsserts.OpenScene("Assets/Scenes/Shop.unity");

            var screen = SceneAsserts.AssertObject("UICanvas/Screen_Shop");
            var controller = screen.GetComponent<ShopController>();
            Assert.IsNotNull(controller, "Screen_Shop is missing ShopController");

            SceneAsserts.AssertRefNotNull(controller, "backButton");
            SceneAsserts.AssertRefNotNull(controller, "coinBalanceText");
            SceneAsserts.AssertRefNotNull(controller, "watchAdButton");
            SceneAsserts.AssertRefNotNull(controller, "watchAdLabel");
            SceneAsserts.AssertRefNotNull(controller, "content");
            SceneAsserts.AssertRefNotNull(controller, "rowTemplate");
            SceneAsserts.AssertRefNotNull(controller, "buyPremiumButton");
            SceneAsserts.AssertRefNotNull(controller, "toastText");
            SceneAsserts.AssertRefNotNull(controller, "getCoinsPanel");
        }
    }
}
