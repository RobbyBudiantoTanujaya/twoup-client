using TMPro;
using TwoUp.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TwoUp.EditorTools
{
    /// <summary>Authors Profile.unity (S8): my name/bot ledger, per-pair ledger list, pair-detail drilldown.</summary>
    public static class ProfileSceneBuilder
    {
        private static readonly Vector2 Center = UiKit.Center;

        [MenuItem("2UP/Build Scenes/Profile")]
        public static void Build()
        {
            var scene = UiKit.NewScene();
            var screen = UiKit.CreateCanvasWithScreen("Screen_Profile");

            var back = UiKit.CreateButton(screen.transform, "Btn_Back", "Back", new Vector2(220, 90), UiKit.ButtonMuted);
            UiKit.Place(back.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40, -40), new Vector2(220, 90));

            var (mePanel, myName, editNameInput, editNameButton, botStats) = CreateMePanel(screen.transform);
            var (rowTemplate, contentGo) = CreatePairList(screen.transform);
            var (detailPanel, detailHeader, detailAggregate, detailStreak, versusLines, coopLines, closeDetail) = CreateDetailPanel(screen.transform);

            var controller = screen.AddComponent<ProfileController>();
            UiKit.SetRef(controller, "backButton", back);
            UiKit.SetRef(controller, "myNameText", myName);
            UiKit.SetRef(controller, "editNameInput", editNameInput);
            UiKit.SetRef(controller, "editNameButton", editNameButton);
            UiKit.SetRef(controller, "botStatsText", botStats);
            UiKit.SetRef(controller, "content", contentGo.transform);
            UiKit.SetRef(controller, "rowTemplate", rowTemplate);
            UiKit.SetRef(controller, "pairDetailPanel", detailPanel);
            UiKit.SetRef(controller, "detailHeaderText", detailHeader);
            UiKit.SetRef(controller, "detailAggregateText", detailAggregate);
            UiKit.SetRef(controller, "detailStreakText", detailStreak);
            UiKit.SetRef(controller, "versusLinesText", versusLines);
            UiKit.SetRef(controller, "coopLinesText", coopLines);
            UiKit.SetRef(controller, "closeDetailButton", closeDetail);

            UiKit.SaveScene(scene, "Profile");
            UiKit.AddSceneToBuildSettings("Assets/Scenes/Profile.unity");
        }

        private static (GameObject panel, TMP_Text myName, TMP_InputField editNameInput, Button editNameButton, TMP_Text botStats)
            CreateMePanel(Transform screenTransform)
        {
            var panel = UiKit.CreatePanel(screenTransform, "Panel_Me", UiKit.PanelBg);
            UiKit.Place(panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -160), new Vector2(1000, 300));

            var myName = UiKit.CreateText(panel.transform, "Text_MyName", "", 48, Color.white);
            myName.alignment = TextAlignmentOptions.TopLeft;
            UiKit.Place(myName.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30, -24), new Vector2(620, 80));

            var editNameButton = UiKit.CreateButton(panel.transform, "Btn_EditName", "Edit", new Vector2(180, 70), UiKit.ButtonBg);
            UiKit.Place(editNameButton.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30, -20), new Vector2(180, 70));

            var editNameInput = UiKit.CreateInputField(panel.transform, "Input_EditName", "Display name", 20);
            UiKit.Place(editNameInput.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30, -112), new Vector2(620, 80));
            editNameInput.gameObject.SetActive(false);

            var botStats = UiKit.CreateText(panel.transform, "Text_BotStats", "", 34, new Color(0.8f, 0.84f, 0.92f));
            botStats.alignment = TextAlignmentOptions.TopLeft;
            UiKit.Place(botStats.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30, -204), new Vector2(800, 60));

            return (panel, myName, editNameInput, editNameButton, botStats);
        }

        private static (PairRowView rowTemplate, GameObject content) CreatePairList(Transform screenTransform)
        {
            // Standard ScrollView: ScrollRect(root) -> Viewport(RectMask2D) -> Content(VerticalLayoutGroup+ContentSizeFitter).
            var scrollViewGo = UiKit.CreateUIObject("List_Pairs", screenTransform);
            UiKit.Place(scrollViewGo, Center, Center, new Vector2(0, -260), new Vector2(1000, 1180));
            var scrollRect = scrollViewGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            var viewportGo = UiKit.CreatePanel(scrollViewGo.transform, "Viewport", Color.clear);
            UiKit.StretchFull(viewportGo);
            var viewportImg = viewportGo.GetComponent<Image>();
            viewportImg.raycastTarget = false;
            viewportGo.AddComponent<RectMask2D>();

            var contentGo = UiKit.CreateUIObject("Content", viewportGo.transform);
            var contentRt = (RectTransform)contentGo.transform;
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 0f);
            var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 16f;
            var csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = (RectTransform)viewportGo.transform;
            scrollRect.content = contentRt;

            // PairRowTemplate MUST be a sibling of Content (child of Viewport), never a child of
            // Content — Content is cleared+repopulated on every refresh, which would destroy an
            // in-place template.
            var rowTemplate = CreateRowTemplate(viewportGo.transform);

            return (rowTemplate, contentGo);
        }

        private static PairRowView CreateRowTemplate(Transform parent)
        {
            var row = UiKit.CreatePanel(parent, "PairRowTemplate", UiKit.ListRowBg);
            ((RectTransform)row.transform).sizeDelta = new Vector2(1000, 140);
            var layoutElement = row.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 140;

            var nameLabel = UiKit.CreateText(row.transform, "Text_Name", "", 36, Color.white);
            nameLabel.alignment = TextAlignmentOptions.TopLeft;
            UiKit.Place(nameLabel.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24, -16), new Vector2(620, 60));

            var scoreLabel = UiKit.CreateText(row.transform, "Text_Score", "", 30, new Color(0.8f, 0.84f, 0.92f));
            scoreLabel.alignment = TextAlignmentOptions.BottomLeft;
            UiKit.Place(scoreLabel.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24, 16), new Vector2(620, 50));

            var badgeLabel = UiKit.CreateText(row.transform, "Text_Badge", "", 28, UiKit.BotBadgeBg);
            badgeLabel.alignment = TextAlignmentOptions.TopRight;
            UiKit.Place(badgeLabel.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24, -16), new Vector2(340, 60));

            var img = row.GetComponent<Image>();
            var button = row.AddComponent<Button>();
            button.targetGraphic = img;

            var view = row.AddComponent<PairRowView>();
            UiKit.SetRef(view, "nameLabel", nameLabel);
            UiKit.SetRef(view, "scoreLabel", scoreLabel);
            UiKit.SetRef(view, "badgeLabel", badgeLabel);
            UiKit.SetRef(view, "button", button);

            row.SetActive(false);
            return view;
        }

        private static (GameObject panel, TMP_Text header, TMP_Text aggregate, TMP_Text streak, TMP_Text versusLines, TMP_Text coopLines, Button closeButton)
            CreateDetailPanel(Transform screenTransform)
        {
            var panel = UiKit.CreatePanel(screenTransform, "Panel_PairDetail", UiKit.PanelBg);
            UiKit.Place(panel, Center, Center, Vector2.zero, new Vector2(980, 1400));

            var header = UiKit.CreateText(panel.transform, "Text_DetailHeader", "", 48, Color.white);
            UiKit.Place(header.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(820, 80));

            var aggregate = UiKit.CreateText(panel.transform, "Text_DetailAggregate", "", 38, Color.white);
            UiKit.Place(aggregate.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -140), new Vector2(820, 60));

            var streak = UiKit.CreateText(panel.transform, "Text_DetailStreak", "", 32, new Color(0.95f, 0.77f, 0.06f));
            UiKit.Place(streak.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -220), new Vector2(820, 60));

            var versusLines = UiKit.CreateText(panel.transform, "Text_VersusLines", "", 32, new Color(0.85f, 0.88f, 0.95f));
            versusLines.alignment = TextAlignmentOptions.TopLeft;
            UiKit.Place(versusLines.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -320), new Vector2(820, 380));

            var coopLines = UiKit.CreateText(panel.transform, "Text_CoopLines", "", 32, new Color(0.85f, 0.88f, 0.95f));
            coopLines.alignment = TextAlignmentOptions.TopLeft;
            UiKit.Place(coopLines.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -740), new Vector2(820, 380));

            var closeButton = UiKit.CreateButton(panel.transform, "Btn_CloseDetail", "Close", new Vector2(400, 100), UiKit.ButtonMuted);
            UiKit.Place(closeButton.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 60), new Vector2(400, 100));

            panel.SetActive(false);
            return (panel, header, aggregate, streak, versusLines, coopLines, closeButton);
        }
    }
}
