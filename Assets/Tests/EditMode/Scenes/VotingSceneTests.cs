using System.Linq;
using NUnit.Framework;
using TwoUp;
using TwoUp.UI;
using UnityEditor;

namespace TwoUp.Tests.EditMode.Scenes
{
    public class VotingSceneTests
    {
        private const string CatalogPath = "Assets/Config/GameCatalog.asset";

        [Test]
        public void Voting_HasSixCardsMatchingCatalog()
        {
            SceneAsserts.OpenScene("Assets/Scenes/Voting.unity");

            var grid = SceneAsserts.AssertObject("UICanvas/Screen_Voting/Grid_GameCards");
            var cards = grid.GetComponentsInChildren<GameCardView>(true);

            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(CatalogPath);
            Assert.IsNotNull(catalog, $"GameCatalog asset missing at {CatalogPath}");

            Assert.AreEqual(catalog.entries.Length, cards.Length, "Expected one GameCardView per catalog entry");

            var catalogIds = catalog.entries.Select(e => e.gameId).OrderBy(id => id).ToArray();
            var cardIds = cards.Select(c => c.GameId).OrderBy(id => id).ToArray();
            CollectionAssert.AreEqual(catalogIds, cardIds);
        }

        [Test]
        public void Voting_ShowdownAndBotPickerInactiveByDefault()
        {
            SceneAsserts.OpenScene("Assets/Scenes/Voting.unity");

            var showdown = SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Voting/Panel_Showdown");
            var botPicker = SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Voting/Panel_BotPicker");
            Assert.IsFalse(showdown.activeSelf, "Panel_Showdown must start inactive");
            Assert.IsFalse(botPicker.activeSelf, "Panel_BotPicker must start inactive");

            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Voting/Panel_Showdown/Text_ShowdownCopy");
            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Voting/Panel_Showdown/Slot_Mine");
            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Voting/Panel_Showdown/Slot_Theirs");
            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Voting/Panel_BotPicker/Row_Tiers/Btn_TierEasy");
            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Voting/Panel_BotPicker/Row_Tiers/Btn_TierMedium");
            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Voting/Panel_BotPicker/Row_Tiers/Btn_TierHard");
            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Voting/Panel_BotPicker/Btn_StartBot");

            var findNew = SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Voting/Btn_FindNew");
            var home = SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Voting/Btn_Home");
            var toast = SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Voting/Text_Toast");
            Assert.IsFalse(findNew.activeSelf, "Btn_FindNew must start inactive");
            Assert.IsFalse(home.activeSelf, "Btn_Home must start inactive");
            Assert.IsFalse(toast.activeSelf, "Text_Toast must start inactive");
        }

        [Test]
        public void Voting_ControllerRefsWired()
        {
            SceneAsserts.OpenScene("Assets/Scenes/Voting.unity");

            var screen = SceneAsserts.AssertObject("UICanvas/Screen_Voting");
            var controller = screen.GetComponent<VotingController>();
            Assert.IsNotNull(controller, "Screen_Voting is missing VotingController");

            SceneAsserts.AssertRefNotNull(controller, "pairBadgeText");
            SceneAsserts.AssertRefNotNull(controller, "subheaderText");
            SceneAsserts.AssertRefNotNull(controller, "countdownText");
            SceneAsserts.AssertRefNotNull(controller, "botPickerPanel");
            SceneAsserts.AssertRefNotNull(controller, "tierEasyButton");
            SceneAsserts.AssertRefNotNull(controller, "tierMediumButton");
            SceneAsserts.AssertRefNotNull(controller, "tierHardButton");
            SceneAsserts.AssertRefNotNull(controller, "startBotButton");
            SceneAsserts.AssertRefNotNull(controller, "showdownPanel");
            SceneAsserts.AssertRefNotNull(controller, "slotMine");
            SceneAsserts.AssertRefNotNull(controller, "slotTheirs");
            SceneAsserts.AssertRefNotNull(controller, "findNewButton");
            SceneAsserts.AssertRefNotNull(controller, "homeButton");
            SceneAsserts.AssertRefNotNull(controller, "toastText");

            var so = new SerializedObject(controller);
            var arr = so.FindProperty("gameCards");
            Assert.IsNotNull(arr, "VotingController missing gameCards field");
            Assert.AreEqual(6, arr.arraySize, "Expected 6 wired game cards");
        }
    }
}
