using TwoUp.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TwoUp.EditorTools
{
    /// <summary>Authors ReflexDuel.unity: wait/go phases, always-on tap zone, best-of-5 round pips, round result.</summary>
    public static class ReflexDuelSceneBuilder
    {
        private static readonly Vector2 Center = UiKit.Center;
        private const int Rounds = 5;

        private static readonly Color WaitBg = new Color(0.05f, 0.06f, 0.09f);
        private static readonly Color GoBg = new Color(0.16f, 0.85f, 0.30f);
        private static readonly Color PipPending = new Color(0.35f, 0.38f, 0.48f);

        [MenuItem("2UP/Build Scenes/ReflexDuel")]
        public static void Build()
        {
            var scene = UiKit.NewScene();
            var screen = UiKit.CreateCanvasWithScreen("Screen_ReflexDuel");

            var panelWait = UiKit.CreatePanel(screen.transform, "Panel_Wait", WaitBg);
            UiKit.StretchFull(panelWait);
            panelWait.GetComponent<Image>().raycastTarget = false;
            var textWait = UiKit.CreateText(panelWait.transform, "Text_Wait", "Wait for it...", 64, Color.white);
            UiKit.StretchFull(textWait.gameObject);

            var panelGo = UiKit.CreatePanel(screen.transform, "Panel_Go", GoBg);
            UiKit.StretchFull(panelGo);
            panelGo.GetComponent<Image>().raycastTarget = false;
            var textGo = UiKit.CreateText(panelGo.transform, "Text_Go", "GO!", 120, Color.white);
            UiKit.StretchFull(textGo.gameObject);
            panelGo.SetActive(false);

            // Best-of-5 pip row (top-center), small progress markers.
            var pipsRoot = UiKit.CreateUIObject("Row_RoundPips", screen.transform);
            UiKit.Place(pipsRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -80), new Vector2(260, 36));
            var pipsLayout = pipsRoot.AddComponent<HorizontalLayoutGroup>();
            pipsLayout.spacing = 10f;
            pipsLayout.childAlignment = TextAnchor.MiddleCenter;
            pipsLayout.childControlWidth = false;
            pipsLayout.childControlHeight = false;
            pipsLayout.childForceExpandWidth = false;
            pipsLayout.childForceExpandHeight = false;

            var roundPips = new Image[Rounds];
            for (int i = 0; i < Rounds; i++)
            {
                var pip = UiKit.CreateUIObject($"Pip_{i}", pipsRoot.transform);
                var img = pip.AddComponent<Image>();
                img.color = PipPending;
                img.raycastTarget = false;
                ((RectTransform)pip.transform).sizeDelta = new Vector2(36, 36);
                roundPips[i] = img;
            }

            var scoreText = UiKit.CreateText(screen.transform, "Text_Score", "0 - 0", 56, Color.white);
            UiKit.Place(scoreText.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -140), new Vector2(400, 80));

            var panelRoundResult = UiKit.CreatePanel(screen.transform, "Panel_RoundResult", UiKit.PanelBg);
            UiKit.Place(panelRoundResult, Center, Center, Vector2.zero, new Vector2(760, 420));
            var myMsText = UiKit.CreateText(panelRoundResult.transform, "Text_MyMs", "", 44, Color.white);
            UiKit.Place(myMsText.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(680, 70));
            var theirMsText = UiKit.CreateText(panelRoundResult.transform, "Text_TheirMs", "", 44, Color.white);
            UiKit.Place(theirMsText.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -120), new Vector2(680, 70));
            var roundVerdictText = UiKit.CreateText(panelRoundResult.transform, "Text_RoundVerdict", "", 52, Color.white);
            UiKit.Place(roundVerdictText.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -220), new Vector2(680, 90));
            panelRoundResult.SetActive(false);

            // Fullscreen invisible tap zone, frontmost among the round elements above — always
            // active while playing; server alone judges false starts (AC-RD-04).
            var tapZoneGo = UiKit.CreateUIObject("Btn_TapZone", screen.transform);
            UiKit.StretchFull(tapZoneGo);
            var tapZoneImg = tapZoneGo.AddComponent<Image>();
            tapZoneImg.color = new Color(1f, 1f, 1f, 0f);
            tapZoneImg.raycastTarget = true;
            var tapZoneButton = tapZoneGo.AddComponent<Button>();
            tapZoneButton.transition = Selectable.Transition.None;
            tapZoneButton.targetGraphic = tapZoneImg;

            // Emote wheel + connection badge layer on top of the tap zone so they stay clickable.
            UiKit.BuildEmoteWheel(screen.transform);
            UiKit.BuildConnectionBadge(screen.transform);

            var toast = UiKit.CreateText(screen.transform, "Text_Toast", "", 40, Color.white);
            UiKit.Place(toast.gameObject, Center, Center, Vector2.zero, new Vector2(800, 100));
            toast.gameObject.SetActive(false);

            var controller = screen.AddComponent<ReflexDuelController>();
            UiKit.SetRef(controller, "panelWait", panelWait);
            UiKit.SetRef(controller, "panelGo", panelGo);
            UiKit.SetRef(controller, "tapZoneButton", tapZoneButton);
            UiKit.SetArray(controller, "roundPips", roundPips);
            UiKit.SetRef(controller, "scoreText", scoreText);
            UiKit.SetRef(controller, "panelRoundResult", panelRoundResult);
            UiKit.SetRef(controller, "myMsText", myMsText);
            UiKit.SetRef(controller, "theirMsText", theirMsText);
            UiKit.SetRef(controller, "roundVerdictText", roundVerdictText);

            UiKit.SaveScene(scene, "ReflexDuel");
            UiKit.AddSceneToBuildSettings("Assets/Scenes/ReflexDuel.unity");
        }
    }
}
