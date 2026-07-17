using TwoUp.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TwoUp.EditorTools
{
    /// <summary>Authors Home.unity: primary entry screen, invite-first (GDD D2).</summary>
    public static class HomeSceneBuilder
    {
        private static readonly Vector2 Center = UiKit.Center;
        private static readonly Color DimBg = new Color(0f, 0f, 0f, 0.6f);

        [MenuItem("2UP/Build Scenes/Home")]
        public static void Build()
        {
            var scene = UiKit.NewScene();
            var screen = UiKit.CreateCanvasWithScreen("Screen_Home");

            var title = UiKit.CreateText(screen.transform, "Title", "2UP", 110, Color.white);
            UiKit.Place(title.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -160), new Vector2(600, 150));
            title.fontStyle = TMPro.FontStyles.Bold;

            var coinBalance = UiKit.CreateText(screen.transform, "Text_CoinBalance", "Coins: 0", 38, Color.white);
            UiKit.Place(coinBalance.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40, -50), new Vector2(320, 70));

            var playWithFriend = UiKit.CreateButton(screen.transform, "Btn_PlayWithFriend", "Play with Friend", new Vector2(700, 130), UiKit.ButtonBg);
            UiKit.Place(playWithFriend.gameObject, Center, Center, new Vector2(0, 380), new Vector2(700, 130));

            var quickMatch = UiKit.CreateButton(screen.transform, "Btn_QuickMatch", "Quick Match", new Vector2(700, 110), UiKit.ButtonBg);
            UiKit.Place(quickMatch.gameObject, Center, Center, new Vector2(0, 230), new Vector2(700, 110));

            var vsBot = UiKit.CreateButton(screen.transform, "Btn_VsBot", "Vs Bot", new Vector2(700, 110), UiKit.ButtonBg);
            UiKit.Place(vsBot.gameObject, Center, Center, new Vector2(0, 100), new Vector2(700, 110));

            var asyncMatches = UiKit.CreateButton(screen.transform, "Btn_AsyncMatches", "Async Matches", new Vector2(500, 100), UiKit.ButtonMuted);
            UiKit.Place(asyncMatches.gameObject, Center, Center, new Vector2(0, -60), new Vector2(500, 100));

            // Pill badge near Btn_AsyncMatches, inactive by default (shown when async matches await your turn).
            var badge = UiKit.CreatePanel(screen.transform, "Badge_AsyncCount", UiKit.BotBadgeBg);
            UiKit.Place(badge, Center, Center, new Vector2(310, -60), new Vector2(220, 64));
            var badgeText = UiKit.CreateText(badge.transform, "Text", "0", 34, Color.black);
            UiKit.StretchFull(badgeText.gameObject);
            badgeText.fontStyle = TMPro.FontStyles.Bold;
            badge.SetActive(false);

            // Bottom nav row: Profile / Shop / Settings.
            var rowNav = UiKit.CreateUIObject("Row_Nav", screen.transform);
            UiKit.Place(rowNav, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 90), new Vector2(760, 100));
            var hlg = rowNav.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 30;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            var profile = UiKit.CreateButton(rowNav.transform, "Btn_Profile", "Profile", new Vector2(220, 100), UiKit.ButtonMuted);
            var shop = UiKit.CreateButton(rowNav.transform, "Btn_Shop", "Shop", new Vector2(220, 100), UiKit.ButtonMuted);
            var settings = UiKit.CreateButton(rowNav.transform, "Btn_Settings", "Settings", new Vector2(220, 100), UiKit.ButtonMuted);

            // Daily Reward popup, centered modal following the GetCoinsPanel chrome pattern. Inactive
            // by default; HomeController auto-shows it once per session when EconomyState says so.
            var dailyReward = UiKit.CreatePanel(screen.transform, "Panel_DailyReward", DimBg);
            UiKit.StretchFull(dailyReward);

            var dailyChrome = UiKit.CreatePanel(dailyReward.transform, "Chrome", UiKit.PanelBg);
            var dailyChromeImg = dailyChrome.GetComponent<Image>();
            dailyChromeImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            dailyChromeImg.type = Image.Type.Sliced;
            UiKit.Place(dailyChrome, Center, Center, Vector2.zero, new Vector2(720, 560));

            var dailyHeader = UiKit.CreateText(dailyChrome.transform, "Text_DailyHeader", "Daily Reward", 48, Color.white);
            UiKit.Place(dailyHeader.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(640, 90));
            dailyHeader.fontStyle = TMPro.FontStyles.Bold;

            var dailyStreak = UiKit.CreateText(dailyChrome.transform, "Text_StreakDay", "Day 1", 40, Color.white);
            UiKit.Place(dailyStreak.gameObject, Center, Center, new Vector2(0, 80), new Vector2(640, 70));

            var dailyAmount = UiKit.CreateText(dailyChrome.transform, "Text_RewardAmount", "Claim +0c", 40, Color.white);
            UiKit.Place(dailyAmount.gameObject, Center, Center, new Vector2(0, 0), new Vector2(640, 70));

            var claimDaily = UiKit.CreateButton(dailyChrome.transform, "Btn_ClaimDaily", "Claim", new Vector2(500, 110), UiKit.ButtonBg);
            UiKit.Place(claimDaily.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 60), new Vector2(500, 110));

            dailyReward.SetActive(false);

            var getCoinsPanel = GetCoinsPanelBuilder.Add(screen.transform);

            var controller = screen.AddComponent<HomeController>();
            UiKit.SetRef(controller, "playWithFriendButton", playWithFriend);
            UiKit.SetRef(controller, "quickMatchButton", quickMatch);
            UiKit.SetRef(controller, "vsBotButton", vsBot);
            UiKit.SetRef(controller, "asyncMatchesButton", asyncMatches);
            UiKit.SetRef(controller, "badgeAsyncCount", badge);
            UiKit.SetRef(controller, "badgeAsyncCountText", badgeText);
            UiKit.SetRef(controller, "profileButton", profile);
            UiKit.SetRef(controller, "shopButton", shop);
            UiKit.SetRef(controller, "settingsButton", settings);
            UiKit.SetRef(controller, "coinBalanceText", coinBalance);
            UiKit.SetRef(controller, "dailyRewardPanel", dailyReward);
            UiKit.SetRef(controller, "dailyStreakText", dailyStreak);
            UiKit.SetRef(controller, "dailyRewardAmountText", dailyAmount);
            UiKit.SetRef(controller, "claimDailyButton", claimDaily);
            UiKit.SetRef(controller, "getCoinsPanel", getCoinsPanel);

            UiKit.SaveScene(scene, "Home");
            UiKit.AddSceneToBuildSettings("Assets/Scenes/Home.unity");
        }
    }
}
