using System.Collections;
using TMPro;
using Twoup.V1;
using TwoUp.Logic;
using TwoUp.Net;
using UnityEngine;
using UnityEngine.UI;

namespace TwoUp.UI
{
    /// <summary>
    /// Result (S7): renders the last GameOver from MatchContext.LastGameOver (set by the game
    /// controller before ToResult()), tracks the local best-of series, and drives the
    /// rematch/next-game/leave flow.
    /// </summary>
    public class ResultController : MonoBehaviour
    {
        [SerializeField] private TMP_Text headlineText;
        [SerializeField] private TMP_Text ledgerLineText;
        [SerializeField] private TMP_Text streakLineText;
        [SerializeField] private TMP_Text seriesLineText;
        [SerializeField] private Button rematchButton;
        [SerializeField] private Button nextGameButton;
        [SerializeField] private Button leaveButton;
        [SerializeField] private TMP_Text opponentDecisionText;
        [SerializeField] private TMP_Text countdownText;

        private const float InitialCountdownMs = 20000f;

        private bool countingDown;
        private float countdownRemainingMs;
        private Coroutine toastRoutine;

        private void Start()
        {
            var gameOver = MatchContext.LastGameOver;
            if (gameOver == null)
            {
                Debug.LogError("ResultController.Start: MatchContext.LastGameOver is null");
                AppStateMachine.Instance.ToHome();
                return;
            }

            rematchButton.onClick.AddListener(() => SendRematchChoice(RematchChoice.RematchSameGame));
            nextGameButton.onClick.AddListener(() => SendRematchChoice(RematchChoice.NextGame));
            leaveButton.onClick.AddListener(OnLeaveClicked);

            opponentDecisionText.text = "";
            streakLineText.text = "";

            var net = NetworkClient.Instance;
            net.RematchStatusUpdateReceived += OnRematchStatusUpdate;
            net.GameStartReceived += OnGameStart;
            net.PairFoundReceived += OnPairFound;
            net.PairDetailReceived += OnPairDetail;
            net.ErrorReceived += OnError;

            Render(gameOver);

            countingDown = true;
            countdownRemainingMs = InitialCountdownMs;
            countdownText.text = VotingCountdownFormatter.Format((int)countdownRemainingMs);
        }

        private void OnDestroy()
        {
            var net = NetworkClient.Instance;
            if (net == null)
                return;
            net.RematchStatusUpdateReceived -= OnRematchStatusUpdate;
            net.GameStartReceived -= OnGameStart;
            net.PairFoundReceived -= OnPairFound;
            net.PairDetailReceived -= OnPairDetail;
            net.ErrorReceived -= OnError;
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

        private void Render(GameOver gameOver)
        {
            string myPlayerId = NetworkClient.Instance.PlayerId;
            var result = gameOver.Result;

            headlineText.text = result.Draw
                ? "Draw!"
                : result.WinnerPlayerIds.Contains(myPlayerId) ? "You win!" : "You lose";

            UpdateSeries(result, myPlayerId);

            var entry = AppStateMachine.Instance.Catalog.Find(MatchContext.GameId);
            if (entry != null && entry.mode == GameMode.CoOp)
            {
                ledgerLineText.text = LedgerDeltaFormatter.FormatDuo(result.CoOpScore, result.CoOpScore, result.CoOpNewBest);
                return;
            }

            var opponent = MatchContext.Opponent;
            if (opponent != null && !opponent.IsBot)
            {
                NetworkClient.Instance.Send(new Envelope
                {
                    GetPairDetail = new GetPairDetail { OtherPlayerId = opponent.PlayerId },
                });
            }
            else
            {
                ledgerLineText.text = $"vs {GetOpponentName()} (AI)";
            }
        }

        private void UpdateSeries(GameResult result, string myPlayerId)
        {
            if (result.Draw)
            {
                // no change
            }
            else if (result.WinnerPlayerIds.Contains(myPlayerId))
            {
                MatchContext.SeriesWinsMine++;
            }
            else if (result.WinnerPlayerIds.Count > 0)
            {
                MatchContext.SeriesWinsTheirs++;
            }

            bool hasSeries = MatchContext.SeriesWinsMine != 0 || MatchContext.SeriesWinsTheirs != 0;
            seriesLineText.gameObject.SetActive(hasSeries);
            if (hasSeries)
                seriesLineText.text = $"Series {MatchContext.SeriesWinsMine} : {MatchContext.SeriesWinsTheirs}";
        }

        private string GetOpponentName() => MatchContext.Opponent?.DisplayName ?? "Opponent";

        private void SendRematchChoice(RematchChoice choice)
        {
            NetworkClient.Instance.Send(new Envelope
            {
                RematchRequest = new RematchRequest { MatchId = MatchContext.MatchId, Accept = true, Choice = choice },
            });
            rematchButton.interactable = false;
            nextGameButton.interactable = false;
            opponentDecisionText.text = "Waiting...";
        }

        private void OnLeaveClicked()
        {
            NetworkClient.Instance.Send(new Envelope
            {
                RematchRequest = new RematchRequest { MatchId = MatchContext.MatchId, Accept = false },
            });
            MatchContext.Clear();
            AppStateMachine.Instance.ToHome();
        }

        private void OnRematchStatusUpdate(RematchStatusUpdate msg)
        {
            if (msg.PlayerId == NetworkClient.Instance.PlayerId)
                return;

            string name = GetOpponentName();
            opponentDecisionText.text = msg.Choice == RematchChoice.RematchSameGame
                ? $"{name} wants a rematch!"
                : $"{name} wants a different game";
        }

        private void OnGameStart(GameStart msg)
        {
            MatchContext.PendingGameStart = msg;
            MatchContext.MatchId = msg.MatchId;
            MatchContext.LastGameOver = null;
            AppStateMachine.Instance.ToGame(MatchContext.GameId);
        }

        private void OnPairFound(PairFound msg)
        {
            MatchContext.PairId = msg.PairId;
            MatchContext.SeriesWinsMine = 0;
            MatchContext.SeriesWinsTheirs = 0;
            AppStateMachine.Instance.ToVoting();
        }

        private void OnPairDetail(PairDetail msg)
        {
            string name = GetOpponentName();
            ledgerLineText.text = LedgerDeltaFormatter.FormatHeadline(msg.Summary.WinsMine, msg.Summary.WinsTheirs, name);
            bool holderIsMe = msg.StreakHolderPlayerId == NetworkClient.Instance.PlayerId;
            streakLineText.text = LedgerDeltaFormatter.FormatStreak(holderIsMe, name, msg.StreakCount);
        }

        private void OnError(Error msg)
        {
            if (msg.Code != "rematch_declined" && msg.Code != "rematch_timeout")
                return;

            string name = GetOpponentName();
            MatchContext.Clear();
            opponentDecisionText.text = $"{name} left. GG!";

            if (toastRoutine != null)
                StopCoroutine(toastRoutine);
            toastRoutine = StartCoroutine(ToHomeAfterDelay(1.5f));
        }

        private IEnumerator ToHomeAfterDelay(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            AppStateMachine.Instance.ToHome();
        }
    }
}
