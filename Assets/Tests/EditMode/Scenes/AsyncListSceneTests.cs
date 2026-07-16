using NUnit.Framework;
using TwoUp.UI;

namespace TwoUp.Tests.EditMode.Scenes
{
    public class AsyncListSceneTests
    {
        [Test]
        public void AsyncList_TemplateIsInactiveSiblingOfContent()
        {
            SceneAsserts.OpenScene("Assets/Scenes/AsyncList.unity");

            var content = SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_AsyncList/ScrollView/Viewport/Content");
            var template = SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_AsyncList/ScrollView/Viewport/RowTemplate");

            Assert.IsFalse(template.activeSelf, "RowTemplate must start inactive");
            Assert.AreNotEqual(content.transform, template.transform.parent, "RowTemplate must not be a child of Content (populate-from-template law)");
            Assert.IsNotNull(template.GetComponent<AsyncMatchRowView>(), "RowTemplate is missing AsyncMatchRowView");
        }

        [Test]
        public void AsyncList_ControllerRefsWired()
        {
            SceneAsserts.OpenScene("Assets/Scenes/AsyncList.unity");

            var screen = SceneAsserts.AssertObject("UICanvas/Screen_AsyncList");
            var controller = screen.GetComponent<AsyncMatchesController>();
            Assert.IsNotNull(controller, "Screen_AsyncList is missing AsyncMatchesController");

            SceneAsserts.AssertRefNotNull(controller, "backButton");
            SceneAsserts.AssertRefNotNull(controller, "content");
            SceneAsserts.AssertRefNotNull(controller, "rowTemplate");
            SceneAsserts.AssertRefNotNull(controller, "emptyText");
        }
    }
}
