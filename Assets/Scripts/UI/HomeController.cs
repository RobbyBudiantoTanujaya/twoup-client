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

        private void Start()
        {
            playWithFriendButton.onClick.AddListener(OnPlayWithFriend);
            quickMatchButton.onClick.AddListener(OnQuickMatch);
            vsBotButton.onClick.AddListener(OnVsBot);
            asyncMatchesButton.onClick.AddListener(OnAsyncMatches);
            profileButton.onClick.AddListener(OnProfile);
            shopButton.onClick.AddListener(OnShop);
            settingsButton.onClick.AddListener(OnSettings);

            badgeAsyncCount.SetActive(false);

            NetworkClient.Instance.AsyncMatchListReceived += OnAsyncMatchListReceived;
            NetworkClient.Instance.Send(new Envelope { ListAsyncMatches = new ListAsyncMatches() });
        }

        private void OnDestroy()
        {
            var net = NetworkClient.Instance;
            if (net == null)
                return;
            net.AsyncMatchListReceived -= OnAsyncMatchListReceived;
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
