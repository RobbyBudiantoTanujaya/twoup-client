using TwoUp.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TwoUp.EditorTools
{
    /// <summary>
    /// Authors AirHockey.unity: mirrored-view real-time table, drag-controlled mallet via
    /// EventTrigger on Panel_Table. Children of Panel_Table use anchor (0,0) so their
    /// anchoredPosition maps 1:1 onto the server's logical coordinate space (TDD.md §4.7).
    /// </summary>
    public static class AirHockeySceneBuilder
    {
        private static readonly Vector2 Center = UiKit.Center;
        private static readonly Vector2 BottomLeftAnchor = Vector2.zero;

        private static readonly Color TableBg = new Color(0.08f, 0.22f, 0.30f);
        private static readonly Color CenterLineColor = new Color(1f, 1f, 1f, 0.25f);
        private static readonly Color SelfMalletColor = new Color(0.25f, 0.55f, 0.95f);
        private static readonly Color OpponentMalletColor = new Color(0.90f, 0.30f, 0.24f);
        private static readonly Color PuckColor = new Color(0.95f, 0.95f, 0.95f);
        private static readonly Color SuddenDeathBg = new Color(0.85f, 0.15f, 0.15f);

        [MenuItem("2UP/Build Scenes/AirHockey")]
        public static void Build()
        {
            var scene = UiKit.NewScene();
            var screen = UiKit.CreateCanvasWithScreen("Screen_AirHockey");

            var panelTable = UiKit.CreatePanel(screen.transform, "Panel_Table", TableBg);
            UiKit.StretchFull(panelTable);
            var tableRect = (RectTransform)panelTable.transform;
            panelTable.GetComponent<Image>().raycastTarget = true;
            var trigger = panelTable.AddComponent<EventTrigger>();

            var centerLine = UiKit.CreatePanel(panelTable.transform, "Img_CenterLine", CenterLineColor);
            UiKit.Place(centerLine, Center, Center, Vector2.zero, new Vector2(1080, 6));
            centerLine.GetComponent<Image>().raycastTarget = false;

            var knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            var puck = UiKit.CreatePanel(panelTable.transform, "Img_Puck", PuckColor);
            puck.GetComponent<Image>().sprite = knobSprite;
            puck.GetComponent<Image>().raycastTarget = false;
            UiKit.Place(puck, BottomLeftAnchor, Center, new Vector2(540, 960), new Vector2(80, 80));
            var puckRect = (RectTransform)puck.transform;

            var malletSelf = UiKit.CreatePanel(panelTable.transform, "Img_MalletSelf", SelfMalletColor);
            malletSelf.GetComponent<Image>().sprite = knobSprite;
            malletSelf.GetComponent<Image>().raycastTarget = false;
            UiKit.Place(malletSelf, BottomLeftAnchor, Center, new Vector2(540, 200), new Vector2(140, 140));
            var malletSelfRect = (RectTransform)malletSelf.transform;

            var malletOpponent = UiKit.CreatePanel(panelTable.transform, "Img_MalletOpponent", OpponentMalletColor);
            malletOpponent.GetComponent<Image>().sprite = knobSprite;
            malletOpponent.GetComponent<Image>().raycastTarget = false;
            UiKit.Place(malletOpponent, BottomLeftAnchor, Center, new Vector2(540, 1720), new Vector2(140, 140));
            var malletOpponentRect = (RectTransform)malletOpponent.transform;

            var scoreText = UiKit.CreateText(screen.transform, "Text_Score", "3 - 2", 56, Color.white);
            UiKit.Place(scoreText.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(400, 80));

            var timerText = UiKit.CreateText(screen.transform, "Text_Timer", "2:30", 44, Color.white);
            UiKit.Place(timerText.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -130), new Vector2(300, 60));

            var bannerSuddenDeath = UiKit.CreatePanel(screen.transform, "Banner_SuddenDeath", SuddenDeathBg);
            UiKit.Place(bannerSuddenDeath, Center, Center, new Vector2(0, 400), new Vector2(760, 100));
            var bannerText = UiKit.CreateText(bannerSuddenDeath.transform, "Text", "SUDDEN DEATH!", 48, Color.white);
            UiKit.StretchFull(bannerText.gameObject);
            bannerSuddenDeath.SetActive(false);

            UiKit.BuildEmoteWheel(screen.transform);
            UiKit.BuildConnectionBadge(screen.transform);

            var toast = UiKit.CreateText(screen.transform, "Text_Toast", "", 40, Color.white);
            UiKit.Place(toast.gameObject, Center, Center, Vector2.zero, new Vector2(800, 100));
            toast.gameObject.SetActive(false);

            var controller = screen.AddComponent<AirHockeyController>();
            UiKit.SetRef(controller, "tableRect", tableRect);
            UiKit.SetRef(controller, "tableEventTrigger", trigger);
            UiKit.SetRef(controller, "puckRect", puckRect);
            UiKit.SetRef(controller, "malletSelfRect", malletSelfRect);
            UiKit.SetRef(controller, "malletOpponentRect", malletOpponentRect);
            UiKit.SetRef(controller, "scoreText", scoreText);
            UiKit.SetRef(controller, "timerText", timerText);
            UiKit.SetRef(controller, "bannerSuddenDeath", bannerSuddenDeath);
            UiKit.SetRef(controller, "toastText", toast);

            UiKit.SaveScene(scene, "AirHockey");
            UiKit.AddSceneToBuildSettings("Assets/Scenes/AirHockey.unity");
        }
    }
}
