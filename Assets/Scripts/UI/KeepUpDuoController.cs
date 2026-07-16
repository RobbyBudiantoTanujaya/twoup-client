using Google.Protobuf;
using TMPro;
using Twoup.V1;
using TwoUp.Logic;
using TwoUp.Net;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TwoUp.UI
{
    /// <summary>
    /// Renders the server-sent KeepUpDuoState. Paddle-drag 1D control (plan/TDD Blocker B1
    /// default engineering decision). Only the opponent's paddle and the ball are smoothed via
    /// SnapshotInterpolator; the local paddle renders immediately from drag input and is set from
    /// the server only once, on the very first render, to seed its starting position. The
    /// alternation rule (AC-KU-02) is visualized via LastToucherSeat: the Glow activates under
    /// the seat OPPOSITE the last toucher, since that player is the one who must touch next.
    /// </summary>
    public class KeepUpDuoController : MonoBehaviour
    {
        private const float ArenaWidth = 1080f;
        private const float PaddleHalfWidth = 110f;

        [SerializeField] private RectTransform arenaRect;
        [SerializeField] private EventTrigger arenaEventTrigger;
        [SerializeField] private RectTransform paddle0Rect;
        [SerializeField] private RectTransform paddle1Rect;
        [SerializeField] private RectTransform youMarkerRect;
        [SerializeField] private RectTransform ballRect;
        [SerializeField] private RectTransform glow0Rect;
        [SerializeField] private RectTransform glow1Rect;
        [SerializeField] private TMP_Text comboText;
        [SerializeField] private TMP_Text toastText;

        private readonly SnapshotInterpolator ballInterp = new SnapshotInterpolator(0.05f, 0.1f);
        private readonly SnapshotInterpolator opponentInterp = new SnapshotInterpolator(0.05f, 0.1f);

        private bool hasRenderedOnce;
        private bool gameOver;
        private bool waitingForGameOver;

        private RectTransform MyPaddleRect => MatchContext.MySeat == 0 ? paddle0Rect : paddle1Rect;
        private RectTransform OpponentPaddleRect => MatchContext.MySeat == 0 ? paddle1Rect : paddle0Rect;

        private void Start()
        {
            var dragEntry = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
            dragEntry.callback.AddListener(OnArenaDragged);
            arenaEventTrigger.triggers.Add(dragEntry);

            toastText.gameObject.SetActive(false);
            glow0Rect.gameObject.SetActive(false);
            glow1Rect.gameObject.SetActive(false);

            youMarkerRect.SetParent(MyPaddleRect, false);
            youMarkerRect.anchorMin = new Vector2(0.5f, 0.5f);
            youMarkerRect.anchorMax = new Vector2(0.5f, 0.5f);
            youMarkerRect.pivot = new Vector2(0.5f, 0.5f);
            youMarkerRect.anchoredPosition = Vector2.zero;

            var net = NetworkClient.Instance;
            net.GameStateReceived += OnGameState;
            net.GameOverReceived += OnGameOver;

            if (MatchContext.PendingGameStart != null)
            {
                RenderStateBytes(MatchContext.PendingGameStart.InitialState);
                MatchContext.PendingGameStart = null;
            }
        }

        private void OnDestroy()
        {
            var net = NetworkClient.Instance;
            if (net == null)
                return;
            net.GameStateReceived -= OnGameState;
            net.GameOverReceived -= OnGameOver;
        }

        private void Update()
        {
            if (gameOver)
                return;
            ballRect.anchoredPosition = ballInterp.Sample(Time.unscaledTime);
            var opponentPaddle = OpponentPaddleRect;
            opponentPaddle.anchoredPosition = new Vector2(
                opponentInterp.Sample(Time.unscaledTime).x, opponentPaddle.anchoredPosition.y);

            glow0Rect.anchoredPosition = new Vector2(paddle0Rect.anchoredPosition.x, glow0Rect.anchoredPosition.y);
            glow1Rect.anchoredPosition = new Vector2(paddle1Rect.anchoredPosition.x, glow1Rect.anchoredPosition.y);
        }

        private void OnArenaDragged(BaseEventData data)
        {
            if (gameOver || waitingForGameOver)
                return;
            var pointerData = (PointerEventData)data;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    arenaRect, pointerData.position, pointerData.pressEventCamera, out var local))
                return;

            var rect = arenaRect.rect;
            float x = Mathf.Clamp(local.x - rect.xMin, PaddleHalfWidth, ArenaWidth - PaddleHalfWidth);

            var myPaddle = MyPaddleRect;
            myPaddle.anchoredPosition = new Vector2(x, myPaddle.anchoredPosition.y);

            NetworkClient.Instance.SendRateLimited(new Envelope
            {
                GameInput = new GameInput
                {
                    MatchId = MatchContext.MatchId,
                    Input = new KeepUpDuoInput { PaddleX = x }.ToByteString(),
                }
            }, "ku_input", 0.05f);
        }

        private void OnGameState(GameState msg)
        {
            if (msg.MatchId != MatchContext.MatchId)
                return;
            RenderStateBytes(msg.State);
        }

        private void RenderStateBytes(ByteString bytes)
        {
            KeepUpDuoState state;
            try
            {
                state = KeepUpDuoState.Parser.ParseFrom(bytes);
            }
            catch (InvalidProtocolBufferException e)
            {
                Debug.LogError($"[KU] Unparseable KeepUpDuoState: {e.Message}");
                return;
            }
            Render(state);
        }

        private void Render(KeepUpDuoState state)
        {
            float mySeatX = MatchContext.MySeat == 0 ? state.PaddleSeat0X : state.PaddleSeat1X;
            float opponentSeatX = MatchContext.MySeat == 0 ? state.PaddleSeat1X : state.PaddleSeat0X;

            if (!hasRenderedOnce)
            {
                var myPaddle = MyPaddleRect;
                myPaddle.anchoredPosition = new Vector2(mySeatX, myPaddle.anchoredPosition.y);
                ballRect.anchoredPosition = new Vector2(state.BallX, state.BallY);
                hasRenderedOnce = true;
            }

            var opponentPaddle = OpponentPaddleRect;
            opponentInterp.Push(new Vector2(opponentSeatX, opponentPaddle.anchoredPosition.y), Time.unscaledTime);
            ballInterp.Push(new Vector2(state.BallX, state.BallY), Time.unscaledTime);

            comboText.text = state.Score.ToString();

            SyncTurnGlow(state.LastToucherSeat);

            waitingForGameOver = state.GameOver;
        }

        private void SyncTurnGlow(int lastToucherSeat)
        {
            if (lastToucherSeat < 0)
            {
                glow0Rect.gameObject.SetActive(false);
                glow1Rect.gameObject.SetActive(false);
                return;
            }

            int mustTouchSeat = lastToucherSeat == 0 ? 1 : 0;
            glow0Rect.gameObject.SetActive(mustTouchSeat == 0);
            glow1Rect.gameObject.SetActive(mustTouchSeat == 1);
        }

        private void OnGameOver(GameOver msg)
        {
            if (msg.MatchId != MatchContext.MatchId)
                return;
            gameOver = true;
            MatchContext.LastGameOver = msg;
            AppStateMachine.Instance.ToResult();
        }
    }
}
