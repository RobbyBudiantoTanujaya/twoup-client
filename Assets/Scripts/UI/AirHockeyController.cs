using System.Collections;
using Google.Protobuf;
using TMPro;
using Twoup.V1;
using TwoUp.Logic;
using TwoUp.Net;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TwoUp.UI
{
    /// <summary>
    /// Renders the server-sent AirHockeyState and drags Img_MalletSelf via an EventTrigger on
    /// Panel_Table. Server-authoritative: the local mallet renders immediately on drag for
    /// responsiveness, but snaps to the server position whenever it deviates by more than
    /// ServerCorrectionThreshold logical units (AC-AH-06 fairness lives server-side; this is
    /// display-only prediction, never an override of server state).
    ///
    /// Mirror view: every player always sees themselves at the bottom of Panel_Table. Seat 0's
    /// logical space already matches that view; seat 1's view is Y-mirrored. ToViewSpace/
    /// ToLogicalSpace are the only two places that mirroring happens — all rendering and all
    /// outgoing input must go through them.
    /// </summary>
    public class AirHockeyController : MonoBehaviour
    {
        // TODO(contract): docs/TDD.md §4.7 only documents the arena's logical WIDTH (0-1080).
        // No table height is specified anywhere in the TDD/GDD. Assumed equal to the full
        // CanvasScaler reference height (1920) so the "1:1 dengan canvas reference 1080x1920"
        // mapping described in the task spec holds without inventing an unrelated constant.
        private const float ArenaWidth = 1080f;
        private const float ArenaHeight = 1920f;
        private const float MalletHalfSize = 70f;
        private const float ServerCorrectionThreshold = 150f;
        private const float MatchWentAsyncToastSeconds = 2f;

        [SerializeField] private RectTransform tableRect;
        [SerializeField] private EventTrigger tableEventTrigger;
        [SerializeField] private RectTransform puckRect;
        [SerializeField] private RectTransform malletSelfRect;
        [SerializeField] private RectTransform malletOpponentRect;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private GameObject bannerSuddenDeath;
        [SerializeField] private TMP_Text toastText;

        private readonly SnapshotInterpolator puckInterp = new SnapshotInterpolator(0.05f, 0.1f);
        private readonly SnapshotInterpolator opponentInterp = new SnapshotInterpolator(0.05f, 0.1f);

        private Vector2 localMalletLogical;
        private bool gameOver;
        private Coroutine toastRoutine;

        private void Start()
        {
            var dragEntry = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
            dragEntry.callback.AddListener(OnTableDragged);
            tableEventTrigger.triggers.Add(dragEntry);

            var net = NetworkClient.Instance;
            net.GameStateReceived += OnGameState;
            net.GameOverReceived += OnGameOver;
            net.MatchWentAsyncReceived += OnMatchWentAsync;

            bannerSuddenDeath.SetActive(false);
            toastText.gameObject.SetActive(false);
            localMalletLogical = ToLogicalSpace(malletSelfRect.anchoredPosition);

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
        }

        private void OnDestroy()
        {
            var net = NetworkClient.Instance;
            if (net == null)
                return;
            net.GameStateReceived -= OnGameState;
            net.GameOverReceived -= OnGameOver;
            net.MatchWentAsyncReceived -= OnMatchWentAsync;
        }

        private void Update()
        {
            if (gameOver)
                return;
            float now = Time.unscaledTime;
            puckRect.anchoredPosition = ToViewSpace(puckInterp.Sample(now));
            malletOpponentRect.anchoredPosition = ToViewSpace(opponentInterp.Sample(now));
        }

        private void OnTableDragged(BaseEventData data)
        {
            if (gameOver)
                return;
            var pointerData = (PointerEventData)data;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    tableRect, pointerData.position, pointerData.pressEventCamera, out var local))
                return;

            var rect = tableRect.rect;
            var view = new Vector2(local.x - rect.xMin, local.y - rect.yMin);
            view.x = Mathf.Clamp(view.x, MalletHalfSize, ArenaWidth - MalletHalfSize);
            view.y = Mathf.Clamp(view.y, MalletHalfSize, ArenaHeight * 0.5f - MalletHalfSize);

            localMalletLogical = ToLogicalSpace(view);
            malletSelfRect.anchoredPosition = view;

            NetworkClient.Instance.SendRateLimited(new Envelope
            {
                GameInput = new GameInput
                {
                    MatchId = MatchContext.MatchId,
                    Input = new AirHockeyInput { MalletX = localMalletLogical.x, MalletY = localMalletLogical.y }.ToByteString(),
                }
            }, "ah_input", 0.05f);
        }

        private void OnGameState(GameState msg)
        {
            if (msg.MatchId != MatchContext.MatchId)
                return;
            RenderStateBytes(msg.State);
        }

        private void RenderStateBytes(ByteString bytes)
        {
            AirHockeyState state;
            try
            {
                state = AirHockeyState.Parser.ParseFrom(bytes);
            }
            catch (InvalidProtocolBufferException e)
            {
                Debug.LogError($"[AH] Unparseable AirHockeyState: {e.Message}");
                return;
            }
            Render(state);
        }

        private void Render(AirHockeyState state)
        {
            float now = Time.unscaledTime;
            puckInterp.Push(new Vector2(state.PuckX, state.PuckY), now);

            int mySeat = Mathf.Clamp(MatchContext.MySeat, 0, 1);
            var myServerLogical = mySeat == 0
                ? new Vector2(state.MalletSeat0X, state.MalletSeat0Y)
                : new Vector2(state.MalletSeat1X, state.MalletSeat1Y);
            var opponentLogical = mySeat == 0
                ? new Vector2(state.MalletSeat1X, state.MalletSeat1Y)
                : new Vector2(state.MalletSeat0X, state.MalletSeat0Y);

            opponentInterp.Push(opponentLogical, now);

            if (Vector2.Distance(localMalletLogical, myServerLogical) > ServerCorrectionThreshold)
            {
                localMalletLogical = myServerLogical;
                malletSelfRect.anchoredPosition = ToViewSpace(localMalletLogical);
            }

            int scoreMine = mySeat == 0 ? state.ScoreSeat0 : state.ScoreSeat1;
            int scoreTheirs = mySeat == 0 ? state.ScoreSeat1 : state.ScoreSeat0;
            scoreText.text = $"{scoreMine} - {scoreTheirs}";

            int totalSeconds = Mathf.Max(0, state.TimeRemainingMs) / 1000;
            timerText.text = $"{totalSeconds / 60}:{totalSeconds % 60:D2}";

            bannerSuddenDeath.SetActive(state.SuddenDeath);
        }

        /// <summary>Player always sees itself at the bottom: identity for seat 0, Y-mirrored for seat 1.</summary>
        private Vector2 ToViewSpace(Vector2 logical) =>
            MatchContext.MySeat == 1 ? new Vector2(logical.x, ArenaHeight - logical.y) : logical;

        /// <summary>Inverse of ToViewSpace (the mirror is its own inverse).</summary>
        private Vector2 ToLogicalSpace(Vector2 view) =>
            MatchContext.MySeat == 1 ? new Vector2(view.x, ArenaHeight - view.y) : view;

        private void OnGameOver(GameOver msg)
        {
            if (msg.MatchId != MatchContext.MatchId)
                return;
            gameOver = true;
            MatchContext.LastGameOver = msg;
            AppStateMachine.Instance.ToResult();
        }

        private void OnMatchWentAsync(MatchWentAsync msg)
        {
            if (msg.MatchId != MatchContext.MatchId)
                return;
            if (toastRoutine != null)
                StopCoroutine(toastRoutine);
            toastRoutine = StartCoroutine(ShowToastThenGoHome());
        }

        private IEnumerator ShowToastThenGoHome()
        {
            toastText.text = "Match continues in Async Matches";
            toastText.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(MatchWentAsyncToastSeconds);
            MatchContext.Clear();
            AppStateMachine.Instance.ToHome();
        }
    }
}
