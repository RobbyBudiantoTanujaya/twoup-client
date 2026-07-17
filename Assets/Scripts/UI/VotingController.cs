using System.Linq;
using TMPro;
using Twoup.V1;
using TwoUp.Logic;
using TwoUp.Net;
using UnityEngine;
using UnityEngine.UI;

namespace TwoUp.UI
{
    /// <summary>
    /// Voting (S5): pick-a-game grid with live vote highlighting, a showdown tiebreak
    /// (re-parents the two candidate cards into fixed slots), and the Vs Bot tier picker
    /// which bypasses voting entirely once Start is pressed (GDD S2/S5).
    /// </summary>
    public class VotingController : MonoBehaviour
    {
        [SerializeField] private GameCardView[] gameCards;
        [SerializeField] private TMP_Text pairBadgeText;
        [SerializeField] private TMP_Text subheaderText;
        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private GameObject botPickerPanel;
        [SerializeField] private Button tierEasyButton;
        [SerializeField] private Button tierMediumButton;
        [SerializeField] private Button tierHardButton;
        [SerializeField] private Button startBotButton;
        [SerializeField] private GameObject showdownPanel;
        [SerializeField] private GameObject slotMine;
        [SerializeField] private GameObject slotTheirs;
        [SerializeField] private Button findNewButton;
        [SerializeField] private Button homeButton;
        [SerializeField] private TMP_Text toastText;
        [SerializeField] private GetCoinsPanelController getCoinsPanel;

        private static readonly Color TierSelectedColor = new Color(0.22f, 0.42f, 0.85f);
        private static readonly Color TierMutedColor = new Color(0.35f, 0.38f, 0.48f);
        private const float InitialCountdownMs = 15000f;

        private string selectedGameId = "";
        private string tier = "medium";
        private bool inShowdown;
        private bool countingDown;
        private float countdownRemainingMs;

        private void Start()
        {
            foreach (var card in gameCards)
            {
                var captured = card;
                captured.Button.onClick.AddListener(() => OnCardClicked(captured));
                captured.GetCoinsButton.onClick.AddListener(() => getCoinsPanel.Show());
            }

            tierEasyButton.onClick.AddListener(() => SetTier("easy"));
            tierMediumButton.onClick.AddListener(() => SetTier("medium"));
            tierHardButton.onClick.AddListener(() => SetTier("hard"));
            startBotButton.onClick.AddListener(OnStartBot);
            findNewButton.onClick.AddListener(() => AppStateMachine.Instance.ToQueue());
            homeButton.onClick.AddListener(() => AppStateMachine.Instance.ToHome());

            toastText.gameObject.SetActive(false);
            showdownPanel.SetActive(false);
            findNewButton.gameObject.SetActive(false);
            homeButton.gameObject.SetActive(false);

            SetTier("medium");
            ApplyPairBadge();

            var net = NetworkClient.Instance;
            net.VoteUpdateReceived += OnVoteUpdate;
            net.VotingLockedReceived += OnVotingLocked;
            net.VotingShowdownReceived += OnVotingShowdown;
            net.VotingCancelledReceived += OnVotingCancelled;
            net.MatchFoundReceived += OnMatchFound;
            net.GameStartReceived += OnGameStart;

            if (MatchContext.VsBotMode)
            {
                botPickerPanel.SetActive(true);
                countdownText.gameObject.SetActive(false);
                subheaderText.gameObject.SetActive(false);
            }
            else
            {
                botPickerPanel.SetActive(false);
                countingDown = true;
                countdownRemainingMs = InitialCountdownMs;
                countdownText.text = VotingCountdownFormatter.Format((int)countdownRemainingMs);
            }
        }

        private void OnDestroy()
        {
            var net = NetworkClient.Instance;
            if (net == null)
                return;
            net.VoteUpdateReceived -= OnVoteUpdate;
            net.VotingLockedReceived -= OnVotingLocked;
            net.VotingShowdownReceived -= OnVotingShowdown;
            net.VotingCancelledReceived -= OnVotingCancelled;
            net.MatchFoundReceived -= OnMatchFound;
            net.GameStartReceived -= OnGameStart;
        }

        private void OnEnable()
        {
            EconomyState.Changed += RefreshCards;
            RefreshCards();
        }

        private void OnDisable()
        {
            EconomyState.Changed -= RefreshCards;
        }

        /// <summary>Single source of truth for whether a card's Button should be interactable (GDD: vs-Bot is always free).</summary>
        public static bool CardEnabled(string gameId, bool botPickerMode) => botPickerMode || EconomyState.CanAfford(gameId);

        private void RefreshCards()
        {
            bool botPickerMode = MatchContext.VsBotMode;
            foreach (var card in gameCards)
            {
                int cost = EconomyState.CostOf(card.GameId);
                card.CostText.text = $"{cost}c";
                card.CostText.gameObject.SetActive(!botPickerMode && cost > 0);

                bool enabled = CardEnabled(card.GameId, botPickerMode);
                card.Button.interactable = enabled;
                card.GetCoinsButton.gameObject.SetActive(!enabled);
            }
        }

        private void Update()
        {
            if (!countingDown)
                return;
            countdownRemainingMs -= Time.deltaTime * 1000f;
            if (countdownRemainingMs < 0f)
                countdownRemainingMs = 0f;
            countdownText.text = VotingCountdownFormatter.Format((int)countdownRemainingMs);
        }

        private void ApplyPairBadge()
        {
            if (MatchContext.PairTotalMatches <= 0)
            {
                pairBadgeText.gameObject.SetActive(false);
                return;
            }
            pairBadgeText.gameObject.SetActive(true);
            pairBadgeText.text = $"{MatchContext.PairMilestoneTier} - Duo Lv{MatchContext.PairDuoLevel}";
        }

        private void OnCardClicked(GameCardView card)
        {
            if (MatchContext.VsBotMode)
            {
                selectedGameId = card.GameId;
                HighlightBotSelection(card);
                return;
            }

            if (inShowdown)
            {
                if (card.GameId != selectedGameId)
                {
                    NetworkClient.Instance.Send(new Envelope
                    {
                        ShowdownPick = new ShowdownPick { PairId = MatchContext.PairId, GameId = card.GameId },
                    });
                }
                return;
            }

            NetworkClient.Instance.Send(new Envelope
            {
                VoteGame = new VoteGame { PairId = MatchContext.PairId, GameId = card.GameId },
            });
        }

        private void HighlightBotSelection(GameCardView selected)
        {
            foreach (var c in gameCards)
                c.transform.localScale = c == selected ? Vector3.one * 1.05f : Vector3.one;
        }

        private void SetTier(string value)
        {
            tier = value;
            SetTierButtonColor(tierEasyButton, value == "easy");
            SetTierButtonColor(tierMediumButton, value == "medium");
            SetTierButtonColor(tierHardButton, value == "hard");
        }

        private static void SetTierButtonColor(Button button, bool selected)
        {
            var img = button.GetComponent<Image>();
            if (img != null)
                img.color = selected ? TierSelectedColor : TierMutedColor;
        }

        private void OnStartBot()
        {
            if (string.IsNullOrEmpty(selectedGameId))
            {
                ShowToast("Pick a game first");
                return;
            }
            MatchContext.LastMatchVsBot = true;
            NetworkClient.Instance.Send(new Envelope
            {
                StartBotMatch = new StartBotMatch { GameId = selectedGameId, Tier = tier },
            });
        }

        private void OnVoteUpdate(VoteUpdate msg)
        {
            string myPlayerId = NetworkClient.Instance.PlayerId;
            string myVote = null;
            string theirVote = null;
            foreach (var kvp in msg.VotesByPlayerId)
            {
                if (kvp.Key == myPlayerId)
                    myVote = kvp.Value;
                else
                    theirVote = kvp.Value;
            }

            if (!string.IsNullOrEmpty(myVote))
                selectedGameId = myVote;

            foreach (var card in gameCards)
            {
                card.transform.localScale = card.GameId == myVote ? Vector3.one * 1.05f : Vector3.one;

                string baseTags = StripOpponentPrefix(card.TagsLabel.text);
                card.TagsLabel.text = card.GameId == theirVote ? ">> " + baseTags : baseTags;
            }
        }

        private static string StripOpponentPrefix(string text) =>
            text.StartsWith(">> ") ? text.Substring(3) : text;

        private void OnVotingLocked(VotingLocked msg)
        {
            countingDown = true;
            countdownRemainingMs = msg.CountdownMs;
            countdownText.gameObject.SetActive(true);
            countdownText.text = VotingCountdownFormatter.Format(msg.CountdownMs);
        }

        private void OnVotingShowdown(VotingShowdown msg)
        {
            inShowdown = true;
            showdownPanel.SetActive(true);

            string mineId = selectedGameId;
            string theirsId = msg.CandidateGameIds.FirstOrDefault(id => id != mineId);
            if (!msg.CandidateGameIds.Contains(mineId))
            {
                mineId = msg.CandidateGameIds.Count > 0 ? msg.CandidateGameIds[0] : mineId;
                theirsId = msg.CandidateGameIds.Count > 1 ? msg.CandidateGameIds[1] : theirsId;
            }

            MoveCardToSlot(mineId, slotMine);
            MoveCardToSlot(theirsId, slotTheirs);
        }

        private void MoveCardToSlot(string gameId, GameObject slot)
        {
            var card = gameCards.FirstOrDefault(c => c.GameId == gameId);
            if (card == null || slot == null)
                return;
            var rt = (RectTransform)card.transform;
            rt.SetParent(slot.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void OnVotingCancelled(VotingCancelled msg)
        {
            ShowToast("They left");
            findNewButton.gameObject.SetActive(true);
            homeButton.gameObject.SetActive(true);
        }

        private void OnMatchFound(MatchFound msg)
        {
            MatchContext.Set(msg, NetworkClient.Instance.PlayerId);
        }

        private void OnGameStart(GameStart msg)
        {
            MatchContext.PendingGameStart = msg;
            MatchContext.MatchId = msg.MatchId;
            AppStateMachine.Instance.ToGame(MatchContext.GameId);
        }

        private void ShowToast(string message)
        {
            toastText.text = message;
            toastText.gameObject.SetActive(true);
        }
    }
}
