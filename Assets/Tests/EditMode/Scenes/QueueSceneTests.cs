using NUnit.Framework;
using TwoUp.UI;

namespace TwoUp.Tests.EditMode.Scenes
{
    public class QueueSceneTests
    {
        [Test]
        public void Queue_HasStatusAndCancelWired()
        {
            SceneAsserts.OpenScene("Assets/Scenes/Queue.unity");

            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Queue/Text_Status");
            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Queue/Text_Hint");
            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Queue/Btn_Cancel");

            var screen = SceneAsserts.AssertObject("UICanvas/Screen_Queue");
            var controller = screen.GetComponent<QueueController>();
            Assert.IsNotNull(controller, "Screen_Queue is missing QueueController");

            SceneAsserts.AssertRefNotNull(controller, "statusText");
            SceneAsserts.AssertRefNotNull(controller, "hintText");
            SceneAsserts.AssertRefNotNull(controller, "cancelButton");
        }
    }
}
