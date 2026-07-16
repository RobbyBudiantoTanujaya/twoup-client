using TwoUp;
using TwoUp.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TwoUp.EditorTools
{
    /// <summary>Authors Voting.unity (S5): pick-a-game grid, showdown tiebreak, Vs Bot tier picker.</summary>
    public static class VotingSceneBuilder
    {
        private static readonly Vector2 Center = UiKit.Center;

        private const string CatalogPath = "Assets/Config/GameCatalog.asset";

        private static readonly Color[] CardColors =
        {
            new Color(0.20f, 0.32f, 0.55f),
            new Color(0.55f, 0.25f, 0.30f),
            new Color(0.22f, 0.45f, 0.38f),
            new Color(0.50f, 0.40f, 0.18f),
            new Color(0.35f, 0.25f, 0.50f),
            new Color(0.20f, 0.45f, 0.50f),
        };

        [MenuItem("2UP/Build Scenes/Voting")]
        public static void Build()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(CatalogPath);
            if (catalog == null || catalog.entries == null || catalog.entries.Length == 0)
            {
                Debug.LogError($"[VotingSceneBuilder] GameCatalog missing/empty at {CatalogPath} — run 2UP/Create Game Catalog first.");
                return;
            }

            var scene = UiKit.NewScene();
            var screen = UiKit.CreateCanvasWithScreen("Screen_Voting");

            var header = UiKit.CreateText(screen.transform, "Text_Header", "Pick your game!", 60, Color.white);
            UiKit.Place(header.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -100), new Vector2(900, 100));

            var subheader = UiKit.CreateText(screen.transform, "Text_Subheader", "Both agree = instant start", 32, new Color(0.8f, 0.84f, 0.92f));
            UiKit.Place(subheader.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -180), new Vector2(880, 70));

            var pairBadge = UiKit.CreateText(screen.transform, "Text_PairBadge", "", 30, UiKit.BotBadgeBg);
            UiKit.Place(pairBadge.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -240), new Vector2(600, 60));

            var countdown = UiKit.CreateText(screen.transform, "Text_Countdown", "15", 48, Color.white);
            UiKit.Place(countdown.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40, -40), new Vector2(160, 80));

            var grid = UiKit.CreateUIObject("Grid_GameCards", screen.transform);
            UiKit.Place(grid, Center, Center, new Vector2(0, 20), new Vector2(1000, 900));
            var glg = grid.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(480, 280);
            glg.spacing = new Vector2(20, 20);
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 2;
            glg.childAlignment = TextAnchor.UpperCenter;

            var cards = new GameCardView[catalog.entries.Length];
            for (int i = 0; i < catalog.entries.Length; i++)
            {
                var entry = catalog.entries[i];
                string modeStr = entry.mode == GameMode.Versus ? "Versus" : "Co-op";
                string pacingStr = entry.pacing == GamePacing.Live ? "Live" : "Turn-based";
                string tags = $"{modeStr} - {pacingStr}";
                cards[i] = CreateCard(grid.transform, entry.gameId, entry.displayName, tags, CardColors[i % CardColors.Length]);
            }

            var showdownPanel = UiKit.CreatePanel(screen.transform, "Panel_Showdown", UiKit.PanelBg);
            UiKit.Place(showdownPanel, Center, Center, Vector2.zero, new Vector2(1000, 700));

            var showdownCopy = UiKit.CreateText(showdownPanel.transform, "Text_ShowdownCopy", "Pick one to agree, or we will flip a coin!", 36, Color.white);
            UiKit.Place(showdownCopy.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(880, 120));

            var slotMine = UiKit.CreateUIObject("Slot_Mine", showdownPanel.transform);
            UiKit.Place(slotMine, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-260, -40), new Vector2(480, 280));

            var slotTheirs = UiKit.CreateUIObject("Slot_Theirs", showdownPanel.transform);
            UiKit.Place(slotTheirs, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(260, -40), new Vector2(480, 280));

            showdownPanel.SetActive(false);

            var botPickerPanel = UiKit.CreatePanel(screen.transform, "Panel_BotPicker", UiKit.PanelBg);
            UiKit.Place(botPickerPanel, Center, Center, new Vector2(0, -260), new Vector2(960, 320));

            var tierRow = UiKit.CreateUIObject("Row_Tiers", botPickerPanel.transform);
            UiKit.Place(tierRow, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(880, 110));
            var hlg = tierRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            var tierEasy = UiKit.CreateButton(tierRow.transform, "Btn_TierEasy", "Easy", new Vector2(270, 100), UiKit.ButtonMuted);
            var tierMedium = UiKit.CreateButton(tierRow.transform, "Btn_TierMedium", "Medium", new Vector2(270, 100), UiKit.ButtonBg);
            var tierHard = UiKit.CreateButton(tierRow.transform, "Btn_TierHard", "Hard", new Vector2(270, 100), UiKit.ButtonMuted);

            var startBot = UiKit.CreateButton(botPickerPanel.transform, "Btn_StartBot", "Start", new Vector2(500, 110), UiKit.ButtonBg);
            UiKit.Place(startBot.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(500, 110));

            botPickerPanel.SetActive(false);

            var findNew = UiKit.CreateButton(screen.transform, "Btn_FindNew", "Find New Match", new Vector2(600, 110), UiKit.ButtonBg);
            UiKit.Place(findNew.gameObject, Center, Center, new Vector2(0, -780), new Vector2(600, 110));
            findNew.gameObject.SetActive(false);

            var home = UiKit.CreateButton(screen.transform, "Btn_Home", "Home", new Vector2(400, 100), UiKit.ButtonMuted);
            UiKit.Place(home.gameObject, Center, Center, new Vector2(0, -900), new Vector2(400, 100));
            home.gameObject.SetActive(false);

            var toast = UiKit.CreateText(screen.transform, "Text_Toast", "", 34, Color.white);
            UiKit.Place(toast.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 60), new Vector2(800, 80));
            toast.gameObject.SetActive(false);

            var controller = screen.AddComponent<VotingController>();
            UiKit.SetArray(controller, "gameCards", cards);
            UiKit.SetRef(controller, "pairBadgeText", pairBadge);
            UiKit.SetRef(controller, "subheaderText", subheader);
            UiKit.SetRef(controller, "countdownText", countdown);
            UiKit.SetRef(controller, "botPickerPanel", botPickerPanel);
            UiKit.SetRef(controller, "tierEasyButton", tierEasy);
            UiKit.SetRef(controller, "tierMediumButton", tierMedium);
            UiKit.SetRef(controller, "tierHardButton", tierHard);
            UiKit.SetRef(controller, "startBotButton", startBot);
            UiKit.SetRef(controller, "showdownPanel", showdownPanel);
            UiKit.SetRef(controller, "slotMine", slotMine);
            UiKit.SetRef(controller, "slotTheirs", slotTheirs);
            UiKit.SetRef(controller, "findNewButton", findNew);
            UiKit.SetRef(controller, "homeButton", home);
            UiKit.SetRef(controller, "toastText", toast);

            UiKit.SaveScene(scene, "Voting");
            UiKit.AddSceneToBuildSettings("Assets/Scenes/Voting.unity");
        }

        private static GameCardView CreateCard(Transform parent, string gameId, string displayName, string tags, Color bg)
        {
            var go = UiKit.CreateUIObject($"Card_{gameId}", parent);
            var img = go.AddComponent<Image>();
            img.color = bg;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var nameLabel = UiKit.CreateText(go.transform, "NameLabel", displayName, 40, Color.white);
            nameLabel.fontStyle = TMPro.FontStyles.Bold;
            UiKit.Place(nameLabel.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(440, 80));

            var tagsLabel = UiKit.CreateText(go.transform, "TagsLabel", tags, 28, new Color(0.85f, 0.88f, 0.95f));
            UiKit.Place(tagsLabel.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(440, 60));

            var card = go.AddComponent<GameCardView>();
            card.GameId = gameId;
            UiKit.SetRef(card, "nameLabel", nameLabel);
            UiKit.SetRef(card, "tagsLabel", tagsLabel);
            UiKit.SetRef(card, "background", img);
            UiKit.SetRef(card, "button", btn);
            return card;
        }
    }
}
