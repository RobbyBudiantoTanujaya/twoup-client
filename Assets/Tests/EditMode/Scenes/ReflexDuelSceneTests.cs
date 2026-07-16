using NUnit.Framework;
using TwoUp.UI;
using UnityEditor;

namespace TwoUp.Tests.EditMode.Scenes
{
    public class ReflexDuelSceneTests
    {
        [Test]
        public void ReflexDuel_HasPanelsAndPips()
        {
            SceneAsserts.OpenScene("Assets/Scenes/ReflexDuel.unity");

            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_ReflexDuel/Panel_Wait/Text_Wait");
            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_ReflexDuel/Panel_Go/Text_Go");
            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_ReflexDuel/Btn_TapZone");
            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_ReflexDuel/Text_Score");
            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_ReflexDuel/Panel_RoundResult/Text_MyMs");
            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_ReflexDuel/Panel_RoundResult/Text_TheirMs");
            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_ReflexDuel/Panel_RoundResult/Text_RoundVerdict");

            var screen = SceneAsserts.AssertObject("UICanvas/Screen_ReflexDuel");
            var controller = screen.GetComponent<ReflexDuelController>();
            Assert.IsNotNull(controller, "Screen_ReflexDuel is missing ReflexDuelController");

            var so = new SerializedObject(controller);
            var pips = so.FindProperty("roundPips");
            Assert.IsNotNull(pips, "No serialized field 'roundPips' on ReflexDuelController");
            Assert.AreEqual(5, pips.arraySize, "Expected 5 round pips");
            for (int i = 0; i < pips.arraySize; i++)
                Assert.IsNotNull(pips.GetArrayElementAtIndex(i).objectReferenceValue, $"roundPips[{i}] not wired");

            SceneAsserts.AssertRefNotNull(controller, "panelWait");
            SceneAsserts.AssertRefNotNull(controller, "panelGo");
            SceneAsserts.AssertRefNotNull(controller, "tapZoneButton");
            SceneAsserts.AssertRefNotNull(controller, "scoreText");
            SceneAsserts.AssertRefNotNull(controller, "panelRoundResult");
            SceneAsserts.AssertRefNotNull(controller, "myMsText");
            SceneAsserts.AssertRefNotNull(controller, "theirMsText");
            SceneAsserts.AssertRefNotNull(controller, "roundVerdictText");
        }

        [Test]
        public void ReflexDuel_GoPanelInactiveByDefault()
        {
            SceneAsserts.OpenScene("Assets/Scenes/ReflexDuel.unity");

            var panelGo = SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_ReflexDuel/Panel_Go");
            Assert.IsFalse(panelGo.activeSelf, "Panel_Go should start inactive");

            var panelRoundResult = SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_ReflexDuel/Panel_RoundResult");
            Assert.IsFalse(panelRoundResult.activeSelf, "Panel_RoundResult should start inactive");

            var panelWait = SceneAsserts.AssertObject("UICanvas/Screen_ReflexDuel/Panel_Wait");
            Assert.IsTrue(panelWait.activeSelf, "Panel_Wait should start active");
        }
    }
}
