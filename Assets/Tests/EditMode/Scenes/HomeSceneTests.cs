using NUnit.Framework;
using TwoUp.UI;

namespace TwoUp.Tests.EditMode.Scenes
{
    public class HomeSceneTests
    {
        [Test]
        public void Home_HasAllButtonsWired()
        {
            SceneAsserts.OpenScene("Assets/Scenes/Home.unity");

            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Home/Title");
            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Home/Btn_PlayWithFriend");
            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Home/Btn_QuickMatch");
            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Home/Btn_VsBot");
            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Home/Badge_AsyncCount");
            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Home/Btn_AsyncMatches");
            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Home/Row_Nav/Btn_Profile");
            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Home/Row_Nav/Btn_Shop");
            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Home/Row_Nav/Btn_Settings");

            var screen = SceneAsserts.AssertObject("UICanvas/Screen_Home");
            var controller = screen.GetComponent<HomeController>();
            Assert.IsNotNull(controller, "Screen_Home is missing HomeController");

            SceneAsserts.AssertRefNotNull(controller, "playWithFriendButton");
            SceneAsserts.AssertRefNotNull(controller, "quickMatchButton");
            SceneAsserts.AssertRefNotNull(controller, "vsBotButton");
            SceneAsserts.AssertRefNotNull(controller, "asyncMatchesButton");
            SceneAsserts.AssertRefNotNull(controller, "badgeAsyncCount");
            SceneAsserts.AssertRefNotNull(controller, "badgeAsyncCountText");
            SceneAsserts.AssertRefNotNull(controller, "profileButton");
            SceneAsserts.AssertRefNotNull(controller, "shopButton");
            SceneAsserts.AssertRefNotNull(controller, "settingsButton");
        }

        [Test]
        public void Home_BadgeInactiveByDefault()
        {
            SceneAsserts.OpenScene("Assets/Scenes/Home.unity");

            var badge = SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Home/Badge_AsyncCount");
            Assert.IsFalse(badge.activeSelf, "Badge_AsyncCount should be inactive by default");
        }
    }
}
