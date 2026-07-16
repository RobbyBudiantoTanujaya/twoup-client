using Google.Protobuf;
using TMPro;
using Twoup.V1;
using TwoUp.Logic;
using TwoUp.Net;
using UnityEngine;
using UnityEngine.UI;

namespace TwoUp.UI
{
    /// <summary>
    /// Renders the server-sent ReflexDuelState and always sends a tap input on Btn_TapZone
    /// press. Server-authoritative: the client never judges false starts (AC-RD-04), it only
    /// renders whatever phase/score/round-result the server pushes. Ping interval drops to
    /// 2s while this scene is live for fresher RTT compensation (TDD.md 4.7).
    /// </summary>
    public class ReflexDuelController : MonoBehaviour
    {
        private const float LiveDuelPingIntervalSeconds = 2f;
        private const float DefaultPingIntervalSeconds = 15f;

        [SerializeField] private GameObject panelWait;
        [SerializeField] private GameObject panelGo;
        [SerializeField] private Button tapZoneButton;
        [Tooltip("5 pips in match order, best-of-5.")]
        [SerializeField] private Image[] roundPips;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private GameObject panelRoundResult;
        [SerializeField] private TMP_Text myMsText;
        [SerializeField] private TMP_Text theirMsText;
        [SerializeField] private TMP_Text roundVerdictText;

        private static readonly Color PipPending = new Color(0.35f, 0.38f, 0.48f);
        private static readonly Color PipWon = new Color(0.25f, 0.85f, 0.35f);
        private static readonly Color PipLost = new Color(0.90f, 0.30f, 0.24f);

        private void Start()
        {
            NetworkClient.Instance.PingIntervalSeconds = LiveDuelPingIntervalSeconds;

            tapZoneButton.onClick.AddListener(OnTapZoneTapped);

            var net = NetworkClient.Instance;
            net.GameStateReceived += OnGameState;
            net.GameOverReceived += OnGameOver;

            if (MatchContext.PendingGameStart != null)
            {
                RenderStateBytes(MatchContext.PendingGameStart.InitialState);
                MatchContext.PendingGameStart = null;
            }
            else if (MatchContext.PendingResumeState != null)
            {
                RenderStateBytes(MatchContext.PendingResumeState);
                MatchContext.PendingResumeState = null;
            }
            else
            {
                panelWait.SetActive(true);
                panelGo.SetActive(false);
                panelRoundResult.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            var net = NetworkClient.Instance;
            if (net == null)
                return;
            net.GameStateReceived -= OnGameState;
            net.GameOverReceived -= OnGameOver;
            net.PingIntervalSeconds = DefaultPingIntervalSeconds;
        }

        private void OnTapZoneTapped()
        {
            NetworkClient.Instance.Send(new Envelope
            {
                GameInput = new GameInput
                {
                    MatchId = MatchContext.MatchId,
                    Input = new ReflexDuelInput().ToByteString(),
                }
            });
        }

        private void OnGameState(GameState msg)
        {
            if (msg.MatchId != MatchContext.MatchId)
                return;
            RenderStateBytes(msg.State);
        }

        private void RenderStateBytes(ByteString bytes)
        {
            ReflexDuelState state;
            try
            {
                state = ReflexDuelState.Parser.ParseFrom(bytes);
            }
            catch (InvalidProtocolBufferException e)
            {
                Debug.LogError($"[RD] Unparseable ReflexDuelState: {e.Message}");
                return;
            }
            Render(state);
        }

        private void Render(ReflexDuelState state)
        {
            switch (state.Phase)
            {
                case ReflexRoundPhase.RdWaiting:
                    panelWait.SetActive(true);
                    panelGo.SetActive(false);
                    panelRoundResult.SetActive(false);
                    break;
                case ReflexRoundPhase.RdArmed:
                    panelWait.SetActive(false);
                    panelGo.SetActive(true);
                    panelRoundResult.SetActive(false);
                    break;
                case ReflexRoundPhase.RdResolved:
                    panelWait.SetActive(false);
                    panelGo.SetActive(false);
                    RenderRoundResult(state);
                    break;
            }

            RenderScore(state);
        }

        private void RenderRoundResult(ReflexDuelState state)
        {
            int mySeat = Mathf.Clamp(MatchContext.MySeat, 0, 1);
            int myMs = mySeat == 0 ? state.LastRoundMsSeat0 : state.LastRoundMsSeat1;
            int theirMs = mySeat == 0 ? state.LastRoundMsSeat1 : state.LastRoundMsSeat0;
            bool myFalseStart = mySeat == 0 ? state.LastRoundFalseStartSeat0 : state.LastRoundFalseStartSeat1;

            myMsText.text = "You: " + ReflexDuelStateFormatter.FormatReactionMs(myMs);
            theirMsText.text = "Them: " + ReflexDuelStateFormatter.FormatReactionMs(theirMs);
            roundVerdictText.text = myFalseStart
                ? "Too soon!"
                : (state.LastRoundWinnerSeat == mySeat ? "Round won!" : "Round lost");

            panelRoundResult.SetActive(true);
        }

        /// <summary>Pips fill in win order: my wins green from the left, then their wins red, rest pending.</summary>
        private void RenderScore(ReflexDuelState state)
        {
            int mySeat = Mathf.Clamp(MatchContext.MySeat, 0, 1);
            int scoreMine = mySeat == 0 ? state.ScoreSeat0 : state.ScoreSeat1;
            int scoreTheirs = mySeat == 0 ? state.ScoreSeat1 : state.ScoreSeat0;

            scoreText.text = $"{scoreMine} - {scoreTheirs}";

            for (int i = 0; i < roundPips.Length; i++)
            {
                roundPips[i].color = i < scoreMine ? PipWon
                    : i < scoreMine + scoreTheirs ? PipLost
                    : PipPending;
            }
        }

        private void OnGameOver(GameOver msg)
        {
            if (msg.MatchId != MatchContext.MatchId)
                return;
            MatchContext.LastGameOver = msg;
            AppStateMachine.Instance.ToResult();
        }
    }
}
