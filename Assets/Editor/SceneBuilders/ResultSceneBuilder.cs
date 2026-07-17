using TMPro;
using TwoUp.UI;
using UnityEditor;
using UnityEngine;

namespace TwoUp.EditorTools
{
    /// <summary>Authors Result.unity (S7): game-over headline, ledger/streak/series lines, rematch flow.</summary>
    public static class ResultSceneBuilder
    {
        private static readonly Vector2 Center = UiKit.Center;

        [MenuItem("2UP/Build Scenes/Result")]
        public static void Build()
        {
            var scene = UiKit.OpenOrCreateScene("Result");
            var screen = UiKit.CreateCanvasWithScreen("Screen_Result");

            var headline = UiKit.CreateText(screen.transform, "Text_Headline", "", 72, Color.white);
            UiKit.Place(headline.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -140), new Vector2(900, 140));

            var ledgerLine = UiKit.CreateText(screen.transform, "Text_LedgerLine", "", 44, Color.white);
            UiKit.Place(ledgerLine.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -300), new Vector2(900, 80));

            var streakLine = UiKit.CreateText(screen.transform, "Text_StreakLine", "", 36, new Color(0.95f, 0.77f, 0.06f));
            UiKit.Place(streakLine.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -380), new Vector2(900, 60));

            var seriesLine = UiKit.CreateText(screen.transform, "Text_SeriesLine", "", 36, new Color(0.8f, 0.84f, 0.92f));
            UiKit.Place(seriesLine.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -450), new Vector2(900, 60));

            var opponentDecision = UiKit.CreateText(screen.transform, "Text_OpponentDecision", "", 34, new Color(0.8f, 0.84f, 0.92f));
            UiKit.Place(opponentDecision.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -520), new Vector2(900, 60));

            var countdown = UiKit.CreateText(screen.transform, "Text_Countdown", "20", 48, Color.white);
            UiKit.Place(countdown.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40, -40), new Vector2(160, 80));

            var rematch = UiKit.CreateButton(screen.transform, "Btn_Rematch", "Rematch", new Vector2(560, 120), UiKit.ButtonBg);
            UiKit.Place(rematch.gameObject, Center, Center, new Vector2(0, 60), new Vector2(560, 120));
            var rematchLabel = rematch.transform.Find("Label").GetComponent<TMP_Text>();

            var getCoinsBtn = UiKit.CreateButton(screen.transform, "Btn_GetCoins", "+c", new Vector2(140, 80), UiKit.ButtonBg);
            UiKit.Place(getCoinsBtn.gameObject, Center, Center, new Vector2(340, 60), new Vector2(140, 80));
            getCoinsBtn.gameObject.SetActive(false);

            var nextGame = UiKit.CreateButton(screen.transform, "Btn_NextGame", "Next Game", new Vector2(560, 110), UiKit.ButtonBg);
            UiKit.Place(nextGame.gameObject, Center, Center, new Vector2(0, -75), new Vector2(560, 110));

            var leave = UiKit.CreateButton(screen.transform, "Btn_Leave", "Leave", new Vector2(560, 100), UiKit.ButtonMuted);
            UiKit.Place(leave.gameObject, Center, Center, new Vector2(0, -200), new Vector2(560, 100));

            var getCoinsPanel = GetCoinsPanelBuilder.Add(screen.transform);

            var controller = IdempotentBuildUtil.GetOrAddComponent<ResultController>(screen);
            UiKit.SetRef(controller, "headlineText", headline);
            UiKit.SetRef(controller, "ledgerLineText", ledgerLine);
            UiKit.SetRef(controller, "streakLineText", streakLine);
            UiKit.SetRef(controller, "seriesLineText", seriesLine);
            UiKit.SetRef(controller, "rematchButton", rematch);
            UiKit.SetRef(controller, "rematchLabelText", rematchLabel);
            UiKit.SetRef(controller, "nextGameButton", nextGame);
            UiKit.SetRef(controller, "leaveButton", leave);
            UiKit.SetRef(controller, "opponentDecisionText", opponentDecision);
            UiKit.SetRef(controller, "countdownText", countdown);
            UiKit.SetRef(controller, "getCoinsButton", getCoinsBtn);
            UiKit.SetRef(controller, "getCoinsPanel", getCoinsPanel);

            UiKit.SaveScene(scene, "Result");
            UiKit.AddSceneToBuildSettings("Assets/Scenes/Result.unity");
        }
    }
}
