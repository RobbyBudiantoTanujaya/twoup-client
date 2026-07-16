using NUnit.Framework;
using TwoUp.UI;
using UnityEditor;
using UnityEngine;

namespace TwoUp.Tests.EditMode.Scenes
{
    public class WallDefenseSceneTests
    {
        [Test]
        public void WallDefense_BallTemplateIsInactiveSibling()
        {
            SceneAsserts.OpenScene("Assets/Scenes/WallDefense.unity");

            var poolBalls = SceneAsserts.AssertObject("UICanvas/Screen_WallDefense/Panel_Arena/Pool_Balls");
            var ballTemplate = SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_WallDefense/Panel_Arena/BallTemplate");

            Assert.IsFalse(ballTemplate.activeSelf, "BallTemplate should start inactive");
            Assert.AreEqual(poolBalls.transform.parent, ballTemplate.transform.parent,
                "BallTemplate must be a sibling of Pool_Balls, not a child of it");
        }

        [Test]
        public void WallDefense_HasFiveHearts()
        {
            SceneAsserts.OpenScene("Assets/Scenes/WallDefense.unity");

            var rowLives = SceneAsserts.AssertObject("UICanvas/Screen_WallDefense/Row_Lives");
            int heartCount = 0;
            foreach (Transform child in rowLives.transform)
            {
                Assert.IsNotNull(child.GetComponent<UnityEngine.UI.Image>(), $"{child.name} should be an Image heart icon");
                heartCount++;
            }
            Assert.AreEqual(5, heartCount, "Row_Lives should have exactly 5 heart icons");
        }

        [Test]
        public void WallDefense_ControllerRefsWired()
        {
            SceneAsserts.OpenScene("Assets/Scenes/WallDefense.unity");

            var screen = SceneAsserts.AssertObject("UICanvas/Screen_WallDefense");
            var controller = screen.GetComponent<WallDefenseController>();
            Assert.IsNotNull(controller, "Screen_WallDefense is missing WallDefenseController");

            SceneAsserts.AssertRefNotNull(controller, "arenaRect");
            SceneAsserts.AssertRefNotNull(controller, "arenaEventTrigger");
            SceneAsserts.AssertRefNotNull(controller, "paddle0Rect");
            SceneAsserts.AssertRefNotNull(controller, "paddle1Rect");
            SceneAsserts.AssertRefNotNull(controller, "youMarkerRect");
            SceneAsserts.AssertRefNotNull(controller, "poolBalls");
            SceneAsserts.AssertRefNotNull(controller, "ballTemplate");
            SceneAsserts.AssertRefNotNull(controller, "scoreText");
            SceneAsserts.AssertRefNotNull(controller, "bannerWave");
            SceneAsserts.AssertRefNotNull(controller, "bannerWaveText");
            SceneAsserts.AssertRefNotNull(controller, "toastText");

            var so = new SerializedObject(controller);
            var livesProp = so.FindProperty("livesImages");
            Assert.IsNotNull(livesProp, "No serialized field 'livesImages' on WallDefenseController");
            Assert.AreEqual(5, livesProp.arraySize, "livesImages should have exactly 5 entries wired");

            var arena = SceneAsserts.AssertObject("UICanvas/Screen_WallDefense/Panel_Arena");
            var trigger = arena.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            Assert.IsNotNull(trigger, "Panel_Arena is missing an EventTrigger");
        }
    }
}
