using TMPro;
using Twoup.V1;
using TwoUp.Monetization;
using TwoUp.Net;
using UnityEngine;
using UnityEngine.UI;

namespace TwoUp
{
    /// <summary>
    /// Shared "not enough coins" overlay reused across Home/Voting/Result/Shop. Built by
    /// GetCoinsPanelBuilder; each scene's own controller owns the reference and calls Show().
    /// </summary>
    public class GetCoinsPanelController : MonoBehaviour
    {
        [SerializeField] private TMP_Text headline;
        [SerializeField] private TMP_Text balanceText;
        [SerializeField] private Button watchAdButton;
        [SerializeField] private TMP_Text watchAdLabel;
        [SerializeField] private Button closeButton;

        // Same rewarded-ad claim path as the Shop screen's ad button (ShopController).
        private IRewardedAdProvider adProvider = new StubRewardedAdProvider();

        private void Start()
        {
            watchAdButton.onClick.AddListener(OnWatchAdClicked);
            closeButton.onClick.AddListener(Hide);
        }

        private void OnEnable()
        {
            EconomyState.Changed += Refresh;
        }

        private void OnDisable()
        {
            EconomyState.Changed -= Refresh;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void Refresh()
        {
            balanceText.text = $"Coins: {EconomyState.CoinBalance}";
            watchAdLabel.text = $"Watch Ad (+{EconomyState.AdRewardCoins}c)";

            if (EconomyState.AdsRemainingToday <= 0)
            {
                watchAdButton.interactable = false;
                headline.text = "No ads left. Come back tomorrow!";
            }
            else
            {
                watchAdButton.interactable = true;
                headline.text = "Not enough coins";
            }
        }

        private void OnWatchAdClicked()
        {
            adProvider.Show(ok =>
            {
                if (ok)
                    NetworkClient.Instance.Send(new Envelope { ClaimAdTicket = new ClaimAdTicket() });
            });
        }
    }
}
