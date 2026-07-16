using TwoUp.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TwoUp.EditorTools
{
    /// <summary>
    /// Authors KeepUpDuo.unity: paddle-drag 1D control (plan/TDD Blocker B1 default engineering
    /// decision), same arena convention as WallDefenseSceneBuilder — Panel_Arena children use
    /// anchor (0,0) so anchoredPosition maps 1:1 onto the server's logical coordinate space
    /// (0-1080 wide). Both paddles sit near the bottom (same X positions as WallDefense) and the
    /// ball bounces between them; the alternation rule (AC-KU-02) is visualized by a Glow under
    /// whichever paddle must touch next.
    /// </summary>
    public static class KeepUpDuoSceneBuilder
    {
        private static readonly Vector2 BottomLeftAnchor = Vector2.zero;
        private static readonly Vector2 Center = UiKit.Center;

        private static readonly Color ArenaBg = new Color(0.08f, 0.16f, 0.20f);
        private static readonly Color FloorColor = new Color(0.85f, 0.15f, 0.15f);
        private static readonly Color Paddle0Color = new Color(0.25f, 0.55f, 0.95f);
        private static readonly Color Paddle1Color = new Color(0.95f, 0.55f, 0.15f);
        private static readonly Color BallColor = new Color(0.95f, 0.95f, 0.95f);
        private static readonly Color GlowColor = new Color(0.95f, 0.85f, 0.15f, 0.45f);

        [MenuItem("2UP/Build Scenes/KeepUpDuo")]
        public static void Build()
        {
            var scene = UiKit.NewScene();
            var screen = UiKit.CreateCanvasWithScreen("Screen_KeepUpDuo");

            var panelArena = UiKit.CreatePanel(screen.transform, "Panel_Arena", ArenaBg);
            UiKit.StretchFull(panelArena);
            var arenaRect = (RectTransform)panelArena.transform;
            panelArena.GetComponent<Image>().raycastTarget = true;
            var arenaTrigger = panelArena.AddComponent<EventTrigger>();

            var floor = UiKit.CreatePanel(panelArena.transform, "Img_Floor", FloorColor);
            floor.GetComponent<Image>().raycastTarget = false;
            var floorRect = (RectTransform)floor.transform;
            floorRect.anchorMin = new Vector2(0f, 0f);
            floorRect.anchorMax = new Vector2(1f, 0f);
            floorRect.pivot = new Vector2(0.5f, 0f);
            floorRect.anchoredPosition = Vector2.zero;
            floorRect.sizeDelta = new Vector2(0f, 16f);

            var ball = UiKit.CreatePanel(panelArena.transform, "Img_Ball", BallColor);
            var knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            ball.GetComponent<Image>().sprite = knobSprite;
            ball.GetComponent<Image>().raycastTarget = false;
            UiKit.Place(ball, BottomLeftAnchor, Center, new Vector2(540, 900), new Vector2(90, 90));
            var ballRect = (RectTransform)ball.transform;

            var paddle0 = UiKit.CreatePanel(panelArena.transform, "Img_Paddle0", Paddle0Color);
            paddle0.GetComponent<Image>().raycastTarget = false;
            UiKit.Place(paddle0, BottomLeftAnchor, Center, new Vector2(270, 160), new Vector2(220, 44));
            var paddle0Rect = (RectTransform)paddle0.transform;

            var paddle1 = UiKit.CreatePanel(panelArena.transform, "Img_Paddle1", Paddle1Color);
            paddle1.GetComponent<Image>().raycastTarget = false;
            UiKit.Place(paddle1, BottomLeftAnchor, Center, new Vector2(810, 160), new Vector2(220, 44));
            var paddle1Rect = (RectTransform)paddle1.transform;

            var youMarker = UiKit.CreateText(panelArena.transform, "Text_YouMarker", "YOU", 32, Color.white);
            UiKit.Place(youMarker.gameObject, Center, Center, Vector2.zero, new Vector2(200, 50));
            var youMarkerRect = (RectTransform)youMarker.transform;

            var glow0 = UiKit.CreatePanel(panelArena.transform, "Glow_Turn0", GlowColor);
            glow0.GetComponent<Image>().raycastTarget = false;
            UiKit.Place(glow0, BottomLeftAnchor, Center, new Vector2(270, 90), new Vector2(260, 60));
            var glow0Rect = (RectTransform)glow0.transform;
            glow0.SetActive(false);

            var glow1 = UiKit.CreatePanel(panelArena.transform, "Glow_Turn1", GlowColor);
            glow1.GetComponent<Image>().raycastTarget = false;
            UiKit.Place(glow1, BottomLeftAnchor, Center, new Vector2(810, 90), new Vector2(260, 60));
            var glow1Rect = (RectTransform)glow1.transform;
            glow1.SetActive(false);

            var comboText = UiKit.CreateText(screen.transform, "Text_Combo", "0", 80, Color.white);
            UiKit.Place(comboText.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(600, 120));

            UiKit.BuildEmoteWheel(screen.transform);
            UiKit.BuildConnectionBadge(screen.transform);

            var toast = UiKit.CreateText(screen.transform, "Text_Toast", "", 40, Color.white);
            UiKit.Place(toast.gameObject, Center, Center, Vector2.zero, new Vector2(800, 100));
            toast.gameObject.SetActive(false);

            var controller = screen.AddComponent<KeepUpDuoController>();
            UiKit.SetRef(controller, "arenaRect", arenaRect);
            UiKit.SetRef(controller, "arenaEventTrigger", arenaTrigger);
            UiKit.SetRef(controller, "paddle0Rect", paddle0Rect);
            UiKit.SetRef(controller, "paddle1Rect", paddle1Rect);
            UiKit.SetRef(controller, "youMarkerRect", youMarkerRect);
            UiKit.SetRef(controller, "ballRect", ballRect);
            UiKit.SetRef(controller, "glow0Rect", glow0Rect);
            UiKit.SetRef(controller, "glow1Rect", glow1Rect);
            UiKit.SetRef(controller, "comboText", comboText);
            UiKit.SetRef(controller, "toastText", toast);

            UiKit.SaveScene(scene, "KeepUpDuo");
            UiKit.AddSceneToBuildSettings("Assets/Scenes/KeepUpDuo.unity");
        }
    }
}
