using TMPro;
using TwoUp;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TwoUp.EditorTools
{
    /// <summary>
    /// Builds the shared "not enough coins" overlay (Panel_GetCoins) reused by the Home, Voting,
    /// Result and Shop scene builders. Not scene-specific — callers wire their own reference to
    /// the returned controller.
    /// </summary>
    public static class GetCoinsPanelBuilder
    {
        private static readonly Vector2 Center = UiKit.Center;
        private static readonly Color DimBg = new Color(0f, 0f, 0f, 0.6f);

        public static GetCoinsPanelController Add(Transform uiCanvasRoot)
        {
            var panel = UiKit.CreatePanel(uiCanvasRoot, "Panel_GetCoins", DimBg);
            UiKit.StretchFull(panel);

            var chrome = UiKit.CreatePanel(panel.transform, "Chrome", UiKit.PanelBg);
            var chromeImg = chrome.GetComponent<Image>();
            chromeImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            chromeImg.type = Image.Type.Sliced;
            UiKit.Place(chrome, Center, Center, Vector2.zero, new Vector2(720, 560));

            var headline = UiKit.CreateText(chrome.transform, "Text_Headline", "Not enough coins", 44, Color.white);
            UiKit.Place(headline.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(640, 90));

            var balanceText = UiKit.CreateText(chrome.transform, "Text_Balance", "Coins: 0", 40, Color.white);
            UiKit.Place(balanceText.gameObject, Center, Center, new Vector2(0, 40), new Vector2(640, 70));

            var watchAdButton = UiKit.CreateButton(chrome.transform, "Btn_WatchAdShortcut", "Watch Ad (+0c)", new Vector2(600, 110), UiKit.ButtonBg);
            UiKit.Place(watchAdButton.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 150), new Vector2(600, 110));
            var watchAdLabel = watchAdButton.transform.Find("Label").GetComponent<TMP_Text>();

            var closeButton = UiKit.CreateButton(chrome.transform, "Btn_Close", "Close", new Vector2(300, 90), UiKit.ButtonMuted);
            UiKit.Place(closeButton.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(300, 90));

            var controller = IdempotentBuildUtil.GetOrAddComponent<GetCoinsPanelController>(panel);
            UiKit.SetRef(controller, "headline", headline);
            UiKit.SetRef(controller, "balanceText", balanceText);
            UiKit.SetRef(controller, "watchAdButton", watchAdButton);
            UiKit.SetRef(controller, "watchAdLabel", watchAdLabel);
            UiKit.SetRef(controller, "closeButton", closeButton);

            panel.SetActive(false);
            return controller;
        }
    }
}
