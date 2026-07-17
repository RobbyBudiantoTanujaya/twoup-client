using NUnit.Framework;
using TMPro;
using TwoUp.Tests.EditMode.Scenes;
using TwoUp.UI;
using UnityEngine.UI;

namespace TwoUp.Tests.EditMode
{
    public class VotingEconomySceneTests
    {
        [Test]
        public void Voting_CardTemplateHasCostAndGetCoins()
        {
            SceneAsserts.OpenScene("Assets/Scenes/Voting.unity");

            var grid = SceneAsserts.AssertObject("UICanvas/Screen_Voting/Grid_GameCards");
            var cards = grid.GetComponentsInChildren<GameCardView>(true);
            Assert.Greater(cards.Length, 0, "Expected at least one game card in Grid_GameCards");

            var cardTransform = cards[0].transform;
            var costText = cardTransform.Find("Text_CoinCost");
            Assert.IsNotNull(costText, "Card template missing Text_CoinCost child");
            Assert.IsNotNull(costText.GetComponent<TMP_Text>(), "Text_CoinCost is missing a TMP_Text component");

            var getCoinsBtn = cardTransform.Find("Btn_GetCoins");
            Assert.IsNotNull(getCoinsBtn, "Card template missing Btn_GetCoins child");
            Assert.IsNotNull(getCoinsBtn.GetComponent<Button>(), "Btn_GetCoins is missing a Button component");

            var getCoinsPanel = SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Voting/Panel_GetCoins");
            Assert.IsFalse(getCoinsPanel.activeSelf, "Panel_GetCoins should be inactive by default");

            var screen = SceneAsserts.AssertObject("UICanvas/Screen_Voting");
            var controller = screen.GetComponent<VotingController>();
            Assert.IsNotNull(controller, "Screen_Voting is missing VotingController");
            SceneAsserts.AssertRefNotNull(controller, "getCoinsPanel");
        }
    }
}
