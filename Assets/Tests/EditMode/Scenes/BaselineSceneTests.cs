using NUnit.Framework;
using TwoUp.Net;
using TwoUp.UI;
using UnityEditor;

namespace TwoUp.Tests.EditMode.Scenes
{
    public class BaselineSceneTests
    {
        [Test]
        public void Boot_HasAppAndBootUi()
        {
            SceneAsserts.OpenScene("Assets/Scenes/Boot.unity");

            var app = SceneAsserts.AssertObject("App");
            Assert.IsNotNull(app.GetComponent<NetworkClient>(), "App is missing NetworkClient");
            Assert.IsNotNull(app.GetComponent<AppStateMachine>(), "App is missing AppStateMachine");

            var screen = SceneAsserts.AssertObject("UICanvas/Screen_Boot");
            var controller = screen.GetComponent<BootController>();
            Assert.IsNotNull(controller, "Screen_Boot is missing BootController");
            SceneAsserts.AssertRefNotNull(controller, "statusText");
            SceneAsserts.AssertRefNotNull(controller, "retryButton");
        }

        [Test]
        public void ConnectFour_Has42CellsAnd7Columns()
        {
            SceneAsserts.OpenScene("Assets/Scenes/ConnectFour.unity");

            var screen = SceneAsserts.AssertObject("UICanvas/Screen_Game");
            var controller = screen.GetComponent<ConnectFourController>();
            Assert.IsNotNull(controller, "Screen_Game is missing ConnectFourController");

            var so = new SerializedObject(controller);

            var cells = so.FindProperty("cells");
            Assert.IsNotNull(cells, "No serialized field 'cells' on ConnectFourController");
            Assert.AreEqual(42, cells.arraySize, "Expected 42 cells (7x6)");
            for (int i = 0; i < cells.arraySize; i++)
                Assert.IsNotNull(cells.GetArrayElementAtIndex(i).objectReferenceValue, $"cells[{i}] not wired");

            var columnButtons = so.FindProperty("columnButtons");
            Assert.IsNotNull(columnButtons, "No serialized field 'columnButtons' on ConnectFourController");
            Assert.AreEqual(7, columnButtons.arraySize, "Expected 7 column buttons");
            for (int i = 0; i < columnButtons.arraySize; i++)
                Assert.IsNotNull(columnButtons.GetArrayElementAtIndex(i).objectReferenceValue, $"columnButtons[{i}] not wired");
        }
    }
}
