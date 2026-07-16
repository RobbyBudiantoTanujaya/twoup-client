using TMPro;
using TwoUp.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TwoUp.EditorTools
{
    /// <summary>Authors Shop.unity (S9): ticket balance, watch-ad ticket claim, item catalog, Premium unlock.</summary>
    public static class ShopSceneBuilder
    {
        private static readonly Vector2 Center = UiKit.Center;

        [MenuItem("2UP/Build Scenes/Shop")]
        public static void Build()
        {
            var scene = UiKit.NewScene();
            var screen = UiKit.CreateCanvasWithScreen("Screen_Shop");

            var back = UiKit.CreateButton(screen.transform, "Btn_Back", "Back", new Vector2(220, 90), UiKit.ButtonMuted);
            UiKit.Place(back.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40, -40), new Vector2(220, 90));

            var ticketBalanceText = UiKit.CreateText(screen.transform, "Text_TicketBalance", "", 48, Color.white);
            UiKit.Place(ticketBalanceText.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(700, 80));

            var (watchAdButton, watchAdLabel) = CreateWatchAdButton(screen.transform);

            var (rowTemplate, contentGo) = CreateItemList(screen.transform);

            var buyPremiumButton = UiKit.CreateButton(screen.transform, "Btn_BuyPremium", "2UP Premium $2.99", new Vector2(600, 100), UiKit.ButtonBg);
            UiKit.Place(buyPremiumButton.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 170), new Vector2(600, 100));

            var toastText = UiKit.CreateText(screen.transform, "Text_Toast", "", 34, Color.white);
            UiKit.Place(toastText.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 290), new Vector2(800, 70));
            toastText.gameObject.SetActive(false);

            var controller = screen.AddComponent<ShopController>();
            UiKit.SetRef(controller, "backButton", back);
            UiKit.SetRef(controller, "ticketBalanceText", ticketBalanceText);
            UiKit.SetRef(controller, "watchAdButton", watchAdButton);
            UiKit.SetRef(controller, "watchAdLabel", watchAdLabel);
            UiKit.SetRef(controller, "content", contentGo.transform);
            UiKit.SetRef(controller, "rowTemplate", rowTemplate);
            UiKit.SetRef(controller, "buyPremiumButton", buyPremiumButton);
            UiKit.SetRef(controller, "toastText", toastText);

            UiKit.SaveScene(scene, "Shop");
            UiKit.AddSceneToBuildSettings("Assets/Scenes/Shop.unity");
        }

        private static (Button button, TMP_Text label) CreateWatchAdButton(Transform screenTransform)
        {
            var button = UiKit.CreateButton(screenTransform, "Btn_WatchAd", "Watch ad (+1 ticket) - 0/5 today", new Vector2(900, 110), UiKit.ButtonBg);
            UiKit.Place(button.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -170), new Vector2(900, 110));
            var label = button.transform.Find("Label").GetComponent<TMP_Text>();
            label.fontSize = 32;
            return (button, label);
        }

        private static (ShopItemRowView rowTemplate, GameObject content) CreateItemList(Transform screenTransform)
        {
            // Standard ScrollView: ScrollRect(root) -> Viewport(RectMask2D) -> Content(VerticalLayoutGroup+ContentSizeFitter).
            var scrollViewGo = UiKit.CreateUIObject("List_Items", screenTransform);
            UiKit.Place(scrollViewGo, Center, Center, new Vector2(0, -40), new Vector2(1000, 1150));
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

            // ShopItemRowTemplate MUST be a sibling of Content (child of Viewport), never a child of
            // Content — Content is cleared+repopulated on every refresh, which would destroy an
            // in-place template.
            var rowTemplate = CreateRowTemplate(viewportGo.transform);

            return (rowTemplate, contentGo);
        }

        private static ShopItemRowView CreateRowTemplate(Transform parent)
        {
            var row = UiKit.CreatePanel(parent, "ShopItemRowTemplate", UiKit.ListRowBg);
            ((RectTransform)row.transform).sizeDelta = new Vector2(1000, 140);
            var layoutElement = row.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 140;

            var nameLabel = UiKit.CreateText(row.transform, "Text_Name", "", 36, Color.white);
            nameLabel.alignment = TextAlignmentOptions.TopLeft;
            UiKit.Place(nameLabel.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24, -16), new Vector2(560, 60));

            var priceLabel = UiKit.CreateText(row.transform, "Text_Price", "", 30, new Color(0.8f, 0.84f, 0.92f));
            priceLabel.alignment = TextAlignmentOptions.BottomLeft;
            UiKit.Place(priceLabel.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24, 16), new Vector2(560, 50));

            var buyButton = UiKit.CreateButton(row.transform, "Btn_Buy", "Buy", new Vector2(220, 90), UiKit.ButtonBg);
            UiKit.Place(buyButton.gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24, 0), new Vector2(220, 90));

            var view = row.AddComponent<ShopItemRowView>();
            UiKit.SetRef(view, "nameLabel", nameLabel);
            UiKit.SetRef(view, "priceLabel", priceLabel);
            UiKit.SetRef(view, "buyButton", buyButton);

            row.SetActive(false);
            return view;
        }
    }
}
