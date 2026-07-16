using TMPro;
using Twoup.V1;
using TwoUp.Monetization;
using TwoUp.Net;
using UnityEngine;
using UnityEngine.UI;

namespace TwoUp.UI
{
    /// <summary>
    /// Shop (S9): ticket balance + rewarded-ad ticket claim, item catalog purchases, and the
    /// 2UP Premium one-time unlock.
    /// </summary>
    public class ShopController : MonoBehaviour
    {
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_Text ticketBalanceText;
        [SerializeField] private Button watchAdButton;
        [SerializeField] private TMP_Text watchAdLabel;
        [SerializeField] private Transform content;
        [SerializeField] private ShopItemRowView rowTemplate;
        [SerializeField] private Button buyPremiumButton;
        [SerializeField] private TMP_Text toastText;

        // Swapping in real SDKs behind TWOUP_ADS/TWOUP_IAP only touches these two lines.
        private IRewardedAdProvider adProvider = new StubRewardedAdProvider();
        private IPurchaseProvider purchaseProvider = new StubPurchaseProvider();

        private void Start()
        {
            backButton.onClick.AddListener(OnBack);
            watchAdButton.onClick.AddListener(OnWatchAdClicked);
            buyPremiumButton.onClick.AddListener(OnBuyPremiumClicked);
            toastText.gameObject.SetActive(false);

            var net = NetworkClient.Instance;
            net.ShopDataReceived += OnShopDataReceived;
            net.WalletUpdateReceived += OnWalletUpdateReceived;
            net.ProfileDataReceived += OnProfileDataReceived;
            net.ErrorReceived += OnErrorReceived;
            net.Send(new Envelope { GetShop = new GetShop() });
        }

        private void OnDestroy()
        {
            var net = NetworkClient.Instance;
            if (net == null)
                return;
            net.ShopDataReceived -= OnShopDataReceived;
            net.WalletUpdateReceived -= OnWalletUpdateReceived;
            net.ProfileDataReceived -= OnProfileDataReceived;
            net.ErrorReceived -= OnErrorReceived;
        }

        private void OnBack() => AppStateMachine.Instance.ToHome();

        private void OnShopDataReceived(ShopData data)
        {
            ApplyBalance(data.TicketBalance, data.AdsRemainingToday);

            for (int i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);

            foreach (var item in data.Items)
                CreateRow(item);
        }

        private void CreateRow(ShopItem item)
        {
            var clone = Instantiate(rowTemplate, content);
            clone.gameObject.SetActive(true);
            clone.ItemKey = item.ItemKey;

            clone.NameLabel.text = item.ItemKey;
            clone.PriceLabel.text = item.Owned
                ? "OWNED"
                : (item.PremiumExclusive ? "PREMIUM" : $"{item.PriceTickets} tickets");
            clone.BuyButton.interactable = !item.Owned && !item.PremiumExclusive;

            string itemKey = item.ItemKey;
            clone.BuyButton.onClick.AddListener(() => OnBuyClicked(itemKey));
        }

        private void OnBuyClicked(string itemKey)
        {
            NetworkClient.Instance.Send(new Envelope { PurchaseItem = new PurchaseItem { ItemKey = itemKey } });
        }

        private void OnWatchAdClicked()
        {
            adProvider.Show(ok =>
            {
                if (ok)
                    NetworkClient.Instance.Send(new Envelope { ClaimAdTicket = new ClaimAdTicket() });
            });
        }

        private void OnBuyPremiumClicked()
        {
            purchaseProvider.PurchasePremium(
                token => NetworkClient.Instance.Send(new Envelope { PremiumPurchased = new PremiumPurchased { PlayPurchaseToken = token } }),
                err => ShowToast(err));
        }

        private void OnWalletUpdateReceived(WalletUpdate update)
        {
            ApplyBalance(update.TicketBalance, update.AdsRemainingToday);

            if (!string.IsNullOrEmpty(update.UnlockedItemKey))
            {
                ShowToast($"Unlocked {update.UnlockedItemKey}!");
                NetworkClient.Instance.Send(new Envelope { GetShop = new GetShop() });
            }
        }

        private void OnProfileDataReceived(ProfileData data)
        {
            ShowToast("Premium active!");
            NetworkClient.Instance.Send(new Envelope { GetShop = new GetShop() });
        }

        private void OnErrorReceived(Error error)
        {
            ShowToast(error.Code == "insufficient_tickets" ? "Not enough tickets - watch an ad!" : error.Message);
        }

        private void ApplyBalance(int ticketBalance, int adsRemainingToday)
        {
            ticketBalanceText.text = $"Tickets: {ticketBalance}";
            watchAdLabel.text = $"Watch ad (+1 ticket) - {adsRemainingToday}/5 today";
        }

        private void ShowToast(string message)
        {
            toastText.text = message;
            toastText.gameObject.SetActive(true);
        }
    }
}
