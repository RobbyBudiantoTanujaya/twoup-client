using NUnit.Framework;
using TwoUp.UI;

namespace TwoUp.Tests.EditMode.Scenes
{
    public class ProfileSceneTests
    {
        [Test]
        public void Profile_TemplateSiblingPatternCorrect()
        {
            SceneAsserts.OpenScene("Assets/Scenes/Profile.unity");

            var content = SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Profile/List_Pairs/Viewport/Content");
            var template = SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Profile/List_Pairs/Viewport/PairRowTemplate");

            Assert.IsFalse(template.activeSelf, "PairRowTemplate must start inactive");
            Assert.AreNotEqual(content.transform, template.transform.parent, "PairRowTemplate must not be a child of Content (populate-from-template law)");
            Assert.IsNotNull(template.GetComponent<PairRowView>(), "PairRowTemplate is missing PairRowView");
        }

        [Test]
        public void Profile_DetailPanelInactiveByDefault()
        {
            SceneAsserts.OpenScene("Assets/Scenes/Profile.unity");

            var detailPanel = SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Profile/Panel_PairDetail");
            Assert.IsFalse(detailPanel.activeSelf, "Panel_PairDetail must start inactive");

            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Profile/Panel_PairDetail/Text_DetailHeader");
            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Profile/Panel_PairDetail/Text_DetailAggregate");
            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Profile/Panel_PairDetail/Text_DetailStreak");
            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Profile/Panel_PairDetail/Text_VersusLines");
            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Profile/Panel_PairDetail/Text_CoopLines");
            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Profile/Panel_PairDetail/Btn_CloseDetail");

            var editNameInput = SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Profile/Panel_Me/Input_EditName");
            Assert.IsFalse(editNameInput.activeSelf, "Input_EditName must start inactive");
        }

        [Test]
        public void Profile_ControllerRefsWired()
        {
            SceneAsserts.OpenScene("Assets/Scenes/Profile.unity");

            var screen = SceneAsserts.AssertObject("UICanvas/Screen_Profile");
            var controller = screen.GetComponent<ProfileController>();
            Assert.IsNotNull(controller, "Screen_Profile is missing ProfileController");

            SceneAsserts.AssertRefNotNull(controller, "backButton");
            SceneAsserts.AssertRefNotNull(controller, "myNameText");
            SceneAsserts.AssertRefNotNull(controller, "editNameInput");
            SceneAsserts.AssertRefNotNull(controller, "editNameButton");
            SceneAsserts.AssertRefNotNull(controller, "botStatsText");
            SceneAsserts.AssertRefNotNull(controller, "content");
            SceneAsserts.AssertRefNotNull(controller, "rowTemplate");
            SceneAsserts.AssertRefNotNull(controller, "pairDetailPanel");
            SceneAsserts.AssertRefNotNull(controller, "detailHeaderText");
            SceneAsserts.AssertRefNotNull(controller, "detailAggregateText");
            SceneAsserts.AssertRefNotNull(controller, "detailStreakText");
            SceneAsserts.AssertRefNotNull(controller, "versusLinesText");
            SceneAsserts.AssertRefNotNull(controller, "coopLinesText");
            SceneAsserts.AssertRefNotNull(controller, "closeDetailButton");
        }
    }
}
