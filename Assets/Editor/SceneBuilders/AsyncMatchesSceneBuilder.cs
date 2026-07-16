using TMPro;
using TwoUp.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TwoUp.EditorTools
{
    /// <summary>Authors AsyncList.unity (S11): matches waiting for a move, resume-on-tap.</summary>
    public static class AsyncMatchesSceneBuilder
    {
        private static readonly Vector2 Center = UiKit.Center;

        [MenuItem("2UP/Build Scenes/AsyncMatches")]
        public static void Build()
        {
            var scene = UiKit.NewScene();
            var screen = UiKit.CreateCanvasWithScreen("Screen_AsyncList");

            var back = UiKit.CreateButton(screen.transform, "Btn_Back", "Back", new Vector2(220, 90), UiKit.ButtonMuted);
            UiKit.Place(back.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40, -40), new Vector2(220, 90));

            var title = UiKit.CreateText(screen.transform, "Text_Title", "Async Matches", 56, Color.white);
            UiKit.Place(title.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(700, 90));

            // Standard ScrollView: ScrollRect(root) -> Viewport(RectMask2D) -> Content(VerticalLayoutGroup+ContentSizeFitter).
            var scrollViewGo = UiKit.CreateUIObject("ScrollView", screen.transform);
            UiKit.Place(scrollViewGo, Center, Center, new Vector2(0, -80), new Vector2(1000, 1500));
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

            // RowTemplate MUST be a sibling of Content (child of Viewport), never a child of Content —
            // Content is cleared+repopulated on every list refresh, which would destroy an in-place template.
            var rowTemplate = CreateRowTemplate(viewportGo.transform);

            var emptyText = UiKit.CreateText(screen.transform, "Text_Empty", "No async matches", 40, new Color(0.8f, 0.84f, 0.92f));
            UiKit.Place(emptyText.gameObject, Center, Center, new Vector2(0, -80), new Vector2(800, 100));

            var controller = screen.AddComponent<AsyncMatchesController>();
            UiKit.SetRef(controller, "backButton", back);
            UiKit.SetRef(controller, "content", contentGo.transform);
            UiKit.SetRef(controller, "rowTemplate", rowTemplate);
            UiKit.SetRef(controller, "emptyText", emptyText);

            UiKit.SaveScene(scene, "AsyncList");
            UiKit.AddSceneToBuildSettings("Assets/Scenes/AsyncList.unity");
        }

        private static AsyncMatchRowView CreateRowTemplate(Transform parent)
        {
            var row = UiKit.CreatePanel(parent, "RowTemplate", UiKit.ListRowBg);
            ((RectTransform)row.transform).sizeDelta = new Vector2(1000, 140);
            var layoutElement = row.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 140;

            var titleLabel = UiKit.CreateText(row.transform, "Text_Title", "", 36, Color.white);
            titleLabel.alignment = TextAlignmentOptions.TopLeft;
            UiKit.Place(titleLabel.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24, -16), new Vector2(700, 60));

            var statusLabel = UiKit.CreateText(row.transform, "Text_Status", "", 30, new Color(0.8f, 0.84f, 0.92f));
            statusLabel.alignment = TextAlignmentOptions.BottomLeft;
            UiKit.Place(statusLabel.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24, 16), new Vector2(700, 50));

            var img = row.GetComponent<Image>();
            var button = row.AddComponent<Button>();
            button.targetGraphic = img;

            var view = row.AddComponent<AsyncMatchRowView>();
            UiKit.SetRef(view, "titleLabel", titleLabel);
            UiKit.SetRef(view, "statusLabel", statusLabel);
            UiKit.SetRef(view, "button", button);

            row.SetActive(false);
            return view;
        }
    }
}
