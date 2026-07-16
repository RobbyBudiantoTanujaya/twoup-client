using NUnit.Framework;
using TwoUp.UI;
using UnityEditor;

namespace TwoUp.Tests.EditMode.Scenes
{
    public class BattleshipSceneTests
    {
        [Test]
        public void Battleship_ThreeGridsEach100Cells()
        {
            SceneAsserts.OpenScene("Assets/Scenes/Battleship.unity");

            AssertGridHas100Cells("UICanvas/Screen_Battleship/Panel_Placement/Grid_MyFleet");
            AssertGridHas100Cells("UICanvas/Screen_Battleship/Panel_Firing/Grid_Target");
            AssertGridHas100Cells("UICanvas/Screen_Battleship/Panel_Firing/Grid_MyBoard");
        }

        private static void AssertGridHas100Cells(string hierarchyPath)
        {
            var go = SceneAsserts.AssertObjectIncludingInactive(hierarchyPath);
            var view = go.GetComponent<BattleshipGridView>();
            Assert.IsNotNull(view, $"{hierarchyPath} is missing BattleshipGridView");

            var so = new SerializedObject(view);

            var cellsProp = so.FindProperty("cells");
            Assert.IsNotNull(cellsProp, "No serialized field 'cells' on BattleshipGridView");
            Assert.AreEqual(100, cellsProp.arraySize, $"{hierarchyPath} should have exactly 100 cells");
            for (int i = 0; i < cellsProp.arraySize; i++)
                Assert.IsNotNull(cellsProp.GetArrayElementAtIndex(i).objectReferenceValue, $"{hierarchyPath} cell {i} is not wired");

            var buttonsProp = so.FindProperty("cellButtons");
            Assert.IsNotNull(buttonsProp, "No serialized field 'cellButtons' on BattleshipGridView");
            Assert.AreEqual(100, buttonsProp.arraySize, $"{hierarchyPath} should have exactly 100 cell buttons");
            for (int i = 0; i < buttonsProp.arraySize; i++)
                Assert.IsNotNull(buttonsProp.GetArrayElementAtIndex(i).objectReferenceValue, $"{hierarchyPath} cell button {i} is not wired");
        }

        [Test]
        public void Battleship_FiringPanelInactiveByDefault()
        {
            SceneAsserts.OpenScene("Assets/Scenes/Battleship.unity");

            var firingPanel = SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Battleship/Panel_Firing");
            Assert.IsFalse(firingPanel.activeSelf, "Panel_Firing should start inactive");

            var placementPanel = SceneAsserts.AssertObject("UICanvas/Screen_Battleship/Panel_Placement");
            Assert.IsTrue(placementPanel.activeSelf, "Panel_Placement should start active");
        }

        [Test]
        public void Battleship_ControllerRefsWired()
        {
            SceneAsserts.OpenScene("Assets/Scenes/Battleship.unity");

            var screen = SceneAsserts.AssertObject("UICanvas/Screen_Battleship");
            var controller = screen.GetComponent<BattleshipController>();
            Assert.IsNotNull(controller, "Screen_Battleship is missing BattleshipController");

            SceneAsserts.AssertRefNotNull(controller, "placementPanel");
            SceneAsserts.AssertRefNotNull(controller, "firingPanel");
            SceneAsserts.AssertRefNotNull(controller, "myFleetGrid");
            SceneAsserts.AssertRefNotNull(controller, "rotateButton");
            SceneAsserts.AssertRefNotNull(controller, "rotateButtonText");
            SceneAsserts.AssertRefNotNull(controller, "randomButton");
            SceneAsserts.AssertRefNotNull(controller, "lockPlacementButton");
            SceneAsserts.AssertRefNotNull(controller, "placementHintText");
            SceneAsserts.AssertRefNotNull(controller, "targetGrid");
            SceneAsserts.AssertRefNotNull(controller, "myBoardGrid");
            SceneAsserts.AssertRefNotNull(controller, "turnBannerText");
            SceneAsserts.AssertRefNotNull(controller, "fireButton");
            SceneAsserts.AssertRefNotNull(controller, "shotResultText");
            SceneAsserts.AssertRefNotNull(controller, "toastText");

            var so = new SerializedObject(controller);
            var trayProp = so.FindProperty("shipTrayButtons");
            Assert.IsNotNull(trayProp, "No serialized field 'shipTrayButtons' on BattleshipController");
            Assert.AreEqual(5, trayProp.arraySize, "shipTrayButtons should have exactly 5 entries wired");
            for (int i = 0; i < trayProp.arraySize; i++)
                Assert.IsNotNull(trayProp.GetArrayElementAtIndex(i).objectReferenceValue, $"shipTrayButtons[{i}] is not wired");
        }
    }
}
