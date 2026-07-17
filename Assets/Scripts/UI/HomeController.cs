using TMPro;
using Twoup.V1;
using TwoUp.Net;
using UnityEngine;
using UnityEngine.UI;

namespace TwoUp.UI
{
    /// <summary>
    /// Home screen: invite-first entry point (GDD D2 - Play with Friend is the most prominent
    /// action), plus quick match, vs bot, async matches, and the bottom nav row. Fetches the
    /// async match list on entry to drive the unread-turn badge.
    /// </summary>
    public class HomeController : MonoBehaviour
    {
        [SerializeField] private Button playWithFriendButton;
        [SerializeField] private Button quickMatchButton;
        [SerializeField] private Button vsBotButton;
        [SerializeField] private Button asyncMatchesButton;
        [SerializeField] private GameObject badgeAsyncCount;
        [SerializeField] private TMP_Text badgeAsyncCountText;
        [SerializeField] private Button profileButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private TMP_Text coinBalanceText;
        [SerializeField] private GameObject dailyRewardPanel;
        [SerializeField] private TMP_Text dailyStreakText;
        [SerializeField] private TMP_Text dailyRewardAmountText;
        [SerializeField] private Button claimDailyButton;
        [SerializeField] private GetCoinsPanelController getCoinsPanel;

        // Guards the daily-reward popup to a single auto-show per app session (survives Home
        // being re-entered after a match, but resets on process restart).
        private static bool dailyShownThisSession;

        private void Start()
        {
            playWithFriendButton.onClick.AddListener(OnPlayWithFriend);
            quickMatchButton.onClick.AddListener(OnQuickMatch);
            vsBotButton.onClick.AddListener(OnVsBot);
            asyncMatchesButton.onClick.AddListener(OnAsyncMatches);
            profileButton.onClick.AddListener(OnProfile);
            shopButton.onClick.AddListener(OnShop);
            settingsButton.onClick.AddListener(OnSettings);
            claimDailyButton.onClick.AddListener(OnClaimDaily);

            badgeAsyncCount.SetActive(false);

            var net = NetworkClient.Instance;
            net.AsyncMatchListReceived += OnAsyncMatchListReceived;
            net.OnDailyRewardClaimed += OnDailyRewardClaimed;
            net.ErrorReceived += OnErrorReceived;
            net.Send(new Envelope { ListAsyncMatches = new ListAsyncMatches() });
        }

        private void OnEnable()
        {
            EconomyState.Changed += RefreshCoin;
            RefreshCoin();
        }

        private void OnDisable()
        {
            EconomyState.Changed -= RefreshCoin;
        }

        private void OnDestroy()
        {
            var net = NetworkClient.Instance;
            if (net == null)
                return;
            net.AsyncMatchListReceived -= OnAsyncMatchListReceived;
            net.OnDailyRewardClaimed -= OnDailyRewardClaimed;
            net.ErrorReceived -= OnErrorReceived;
        }

        private void RefreshCoin()
        {
            coinBalanceText.text = $"Coins: {EconomyState.CoinBalance}";

            if (!dailyShownThisSession && EconomyState.DailyRewardAvailable)
            {
                dailyShownThisSession = true;
                dailyRewardPanel.SetActive(true);
                dailyStreakText.text = $"Day {EconomyState.StreakCount + 1}";
                dailyRewardAmountText.text = $"Claim +{EconomyState.NextDailyRewardCoins}c";
            }
        }

        private void OnClaimDaily() => NetworkClient.Instance.SendClaimDailyReward();

        private void OnDailyRewardClaimed(DailyRewardClaimed reward) => dailyRewardPanel.SetActive(false);

        private void OnErrorReceived(Error error)
        {
            if (error.Code == "daily_already_claimed")
                dailyRewardPanel.SetActive(false);
        }

        private void OnPlayWithFriend()
        {
            MatchContext.VsBotMode = false;
            AppStateMachine.Instance.ToInvite();
        }

        private void OnQuickMatch()
        {
            MatchContext.VsBotMode = false;
            AppStateMachine.Instance.ToQueue();
        }

        private void OnVsBot()
        {
            MatchContext.VsBotMode = true;
            AppStateMachine.Instance.ToVoting();
        }

        private void OnAsyncMatches() => AppStateMachine.Instance.ToAsyncList();

        private void OnProfile() => AppStateMachine.Instance.ToProfile();

        private void OnShop() => AppStateMachine.Instance.ToShop();

        private void OnSettings() => AppStateMachine.Instance.ToSettings();

        private void OnAsyncMatchListReceived(AsyncMatchList list)
        {
            int yourTurnCount = 0;
            foreach (var match in list.Matches)
            {
                if (match.YourTurn)
                    yourTurnCount++;
            }

            badgeAsyncCount.SetActive(yourTurnCount > 0);
            badgeAsyncCountText.text = yourTurnCount.ToString();
        }
    }
}
