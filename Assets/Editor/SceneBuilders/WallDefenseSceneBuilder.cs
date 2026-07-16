using TwoUp.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TwoUp.EditorTools
{
    /// <summary>
    /// Authors WallDefense.unity: co-op real-time defense, identical render on both clients (no
    /// mirroring per TDD.md §4.7 final decision) — the shared goal is always at the bottom, balls
    /// fall from the top, and player identity comes only from paddle color (blue seat0 / orange
    /// seat1) plus a "YOU" marker reparented onto MatchContext.MySeat's paddle at runtime.
    /// Panel_Arena children use anchor (0,0) so anchoredPosition maps 1:1 onto the server's logical
    /// coordinate space (0-1080 wide), same convention as AirHockeySceneBuilder.
    /// </summary>
    public static class WallDefenseSceneBuilder
    {
        private static readonly Vector2 BottomLeftAnchor = Vector2.zero;
        private static readonly Vector2 Center = UiKit.Center;

        private static readonly Color ArenaBg = new Color(0.08f, 0.20f, 0.16f);
        private static readonly Color GoalColor = new Color(0.55f, 0.40f, 0.20f);
        private static readonly Color Paddle0Color = new Color(0.25f, 0.55f, 0.95f);
        private static readonly Color Paddle1Color = new Color(0.95f, 0.55f, 0.15f);
        private static readonly Color BallColor = new Color(0.95f, 0.95f, 0.95f);
        private static readonly Color HeartColor = new Color(0.90f, 0.20f, 0.25f);

        private const int HeartCount = 5;

        [MenuItem("2UP/Build Scenes/WallDefense")]
        public static void Build()
        {
            var scene = UiKit.NewScene();
            var screen = UiKit.CreateCanvasWithScreen("Screen_WallDefense");

            var panelArena = UiKit.CreatePanel(screen.transform, "Panel_Arena", ArenaBg);
            UiKit.StretchFull(panelArena);
            var arenaRect = (RectTransform)panelArena.transform;
            panelArena.GetComponent<Image>().raycastTarget = true;
            var arenaTrigger = panelArena.AddComponent<EventTrigger>();

            var goal = UiKit.CreatePanel(panelArena.transform, "Img_Goal", GoalColor);
            goal.GetComponent<Image>().raycastTarget = false;
            var goalRect = (RectTransform)goal.transform;
            goalRect.anchorMin = new Vector2(0f, 0f);
            goalRect.anchorMax = new Vector2(1f, 0f);
            goalRect.pivot = new Vector2(0.5f, 0f);
            goalRect.anchoredPosition = Vector2.zero;
            goalRect.sizeDelta = new Vector2(0f, 40f);

            var paddle0 = UiKit.CreatePanel(panelArena.transform, "Img_Paddle0", Paddle0Color);
            paddle0.GetComponent<Image>().raycastTarget = false;
            UiKit.Place(paddle0, BottomLeftAnchor, Center, new Vector2(270, 160), new Vector2(240, 50));
            var paddle0Rect = (RectTransform)paddle0.transform;

            var paddle1 = UiKit.CreatePanel(panelArena.transform, "Img_Paddle1", Paddle1Color);
            paddle1.GetComponent<Image>().raycastTarget = false;
            UiKit.Place(paddle1, BottomLeftAnchor, Center, new Vector2(810, 160), new Vector2(240, 50));
            var paddle1Rect = (RectTransform)paddle1.transform;

            var youMarker = UiKit.CreateText(panelArena.transform, "Text_YouMarker", "YOU", 32, Color.white);
            UiKit.Place(youMarker.gameObject, Center, Center, Vector2.zero, new Vector2(200, 50));
            var youMarkerRect = (RectTransform)youMarker.transform;

            var poolBalls = UiKit.CreateUIObject("Pool_Balls", panelArena.transform);
            UiKit.StretchFull(poolBalls);
            var poolBallsTransform = poolBalls.transform;

            var knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            var ballTemplate = UiKit.CreatePanel(panelArena.transform, "BallTemplate", BallColor);
            ballTemplate.GetComponent<Image>().sprite = knobSprite;
            ballTemplate.GetComponent<Image>().raycastTarget = false;
            UiKit.Place(ballTemplate, BottomLeftAnchor, Center, new Vector2(540, 1800), new Vector2(60, 60));
            var ballTemplateRect = (RectTransform)ballTemplate.transform;
            ballTemplate.SetActive(false);

            var rowLives = UiKit.CreateUIObject("Row_Lives", screen.transform);
            UiKit.Place(rowLives, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30, -40), new Vector2(320, 48));
            var livesLayout = rowLives.AddComponent<HorizontalLayoutGroup>();
            livesLayout.spacing = 10f;
            livesLayout.childAlignment = TextAnchor.MiddleLeft;
            livesLayout.childControlWidth = false;
            livesLayout.childControlHeight = false;
            livesLayout.childForceExpandWidth = false;
            livesLayout.childForceExpandHeight = false;

            var livesImages = new Image[HeartCount];
            for (int i = 0; i < HeartCount; i++)
            {
                var heart = UiKit.CreatePanel(rowLives.transform, $"Heart_{i}", HeartColor);
                var heartImg = heart.GetComponent<Image>();
                heartImg.raycastTarget = false;
                ((RectTransform)heart.transform).sizeDelta = new Vector2(48, 48);
                livesImages[i] = heartImg;
            }

            var scoreText = UiKit.CreateText(screen.transform, "Text_Score", "0", 56, Color.white);
            UiKit.Place(scoreText.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(400, 80));

            var bannerWave = UiKit.CreatePanel(screen.transform, "Banner_Wave", UiKit.PanelBg);
            UiKit.Place(bannerWave, Center, Center, new Vector2(0, 400), new Vector2(760, 100));
            var bannerWaveText = UiKit.CreateText(bannerWave.transform, "Text", "Wave 1", 48, Color.white);
            UiKit.StretchFull(bannerWaveText.gameObject);
            bannerWave.SetActive(false);

            UiKit.BuildEmoteWheel(screen.transform);
            UiKit.BuildConnectionBadge(screen.transform);

            var toast = UiKit.CreateText(screen.transform, "Text_Toast", "", 40, Color.white);
            UiKit.Place(toast.gameObject, Center, Center, Vector2.zero, new Vector2(800, 100));
            toast.gameObject.SetActive(false);

            var controller = screen.AddComponent<WallDefenseController>();
            UiKit.SetRef(controller, "arenaRect", arenaRect);
            UiKit.SetRef(controller, "arenaEventTrigger", arenaTrigger);
            UiKit.SetRef(controller, "paddle0Rect", paddle0Rect);
            UiKit.SetRef(controller, "paddle1Rect", paddle1Rect);
            UiKit.SetRef(controller, "youMarkerRect", youMarkerRect);
            UiKit.SetRef(controller, "poolBalls", poolBallsTransform);
            UiKit.SetRef(controller, "ballTemplate", ballTemplateRect);
            UiKit.SetArray(controller, "livesImages", livesImages);
            UiKit.SetRef(controller, "scoreText", scoreText);
            UiKit.SetRef(controller, "bannerWave", bannerWave);
            UiKit.SetRef(controller, "bannerWaveText", bannerWaveText);
            UiKit.SetRef(controller, "toastText", toast);

            UiKit.SaveScene(scene, "WallDefense");
            UiKit.AddSceneToBuildSettings("Assets/Scenes/WallDefense.unity");
        }
    }
}
