using System.Collections;
using System.Collections.Generic;
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
    /// Renders the server-sent WallDefenseState. Co-op game: both clients render the arena
    /// IDENTICALLY (shared goal at the bottom, balls fall from the top) — there is no per-seat
    /// mirroring (TDD.md §4.7 final decision). Player identity comes only from paddle color
    /// (Img_Paddle0 = seat 0 blue, Img_Paddle1 = seat 1 orange) and from Text_YouMarker, which is
    /// reparented onto MatchContext.MySeat's paddle at Start. Only the opponent's paddle is
    /// smoothed via SnapshotInterpolator; the local paddle renders immediately from drag input and
    /// is set from the server only once, on the very first render, to seed its starting position.
    /// Ball positions are a populate-from-template pool synced directly from the snapshot each tick
    /// (no per-ball interpolation, per task spec).
    /// </summary>
    public class WallDefenseController : MonoBehaviour
    {
        // TODO(contract): docs/TDD.md §4.7 only documents the arena's logical WIDTH (0-1080).
        // No arena height is specified anywhere in the TDD/GDD. Assumed equal to the full
        // CanvasScaler reference height (1920), same assumption as AirHockeyController.
        private const float ArenaWidth = 1080f;
        private const float PaddleHalfWidth = 120f;
        private const float WaveBannerSeconds = 1.5f;

        [SerializeField] private RectTransform arenaRect;
        [SerializeField] private EventTrigger arenaEventTrigger;
        [SerializeField] private RectTransform paddle0Rect;
        [SerializeField] private RectTransform paddle1Rect;
        [SerializeField] private RectTransform youMarkerRect;
        [SerializeField] private Transform poolBalls;
        [SerializeField] private RectTransform ballTemplate;
        [SerializeField] private Image[] livesImages;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private GameObject bannerWave;
        [SerializeField] private TMP_Text bannerWaveText;
        [SerializeField] private TMP_Text toastText;

        private readonly SnapshotInterpolator opponentInterp = new SnapshotInterpolator(0.05f, 0.1f);
        private readonly List<RectTransform> ballPool = new List<RectTransform>();

        private bool hasRenderedOnce;
        private bool gameOver;
        private int lastWave = -1;
        private Coroutine waveBannerRoutine;

        private RectTransform MyPaddleRect => MatchContext.MySeat == 0 ? paddle0Rect : paddle1Rect;
        private RectTransform OpponentPaddleRect => MatchContext.MySeat == 0 ? paddle1Rect : paddle0Rect;

        private void Start()
        {
            var dragEntry = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
            dragEntry.callback.AddListener(OnArenaDragged);
            arenaEventTrigger.triggers.Add(dragEntry);

            bannerWave.SetActive(false);
            toastText.gameObject.SetActive(false);

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
            OpponentPaddleRect.anchoredPosition = opponentInterp.Sample(Time.unscaledTime);
        }

        private void OnArenaDragged(BaseEventData data)
        {
            if (gameOver)
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
                    Input = new WallDefenseInput { PaddleX = x }.ToByteString(),
                }
            }, "wd_input", 0.05f);
        }

        private void OnGameState(GameState msg)
        {
            if (msg.MatchId != MatchContext.MatchId)
                return;
            RenderStateBytes(msg.State);
        }

        private void RenderStateBytes(ByteString bytes)
        {
            WallDefenseState state;
            try
            {
                state = WallDefenseState.Parser.ParseFrom(bytes);
            }
            catch (InvalidProtocolBufferException e)
            {
                Debug.LogError($"[WD] Unparseable WallDefenseState: {e.Message}");
                return;
            }
            Render(state);
        }

        private void Render(WallDefenseState state)
        {
            float mySeatX = MatchContext.MySeat == 0 ? state.PaddleSeat0X : state.PaddleSeat1X;
            float opponentSeatX = MatchContext.MySeat == 0 ? state.PaddleSeat1X : state.PaddleSeat0X;

            if (!hasRenderedOnce)
            {
                var myPaddle = MyPaddleRect;
                myPaddle.anchoredPosition = new Vector2(mySeatX, myPaddle.anchoredPosition.y);
                hasRenderedOnce = true;
            }

            var opponentPaddle = OpponentPaddleRect;
            opponentInterp.Push(new Vector2(opponentSeatX, opponentPaddle.anchoredPosition.y), Time.unscaledTime);

            SyncBalls(state.Balls);
            SyncLives(state.Lives);
            scoreText.text = state.Score.ToString();

            if (state.Wave != lastWave)
            {
                lastWave = state.Wave;
                if (waveBannerRoutine != null)
                    StopCoroutine(waveBannerRoutine);
                waveBannerRoutine = StartCoroutine(ShowWaveBanner(state.Wave));
            }
        }

        private void SyncBalls(Google.Protobuf.Collections.RepeatedField<WDBall> balls)
        {
            while (ballPool.Count < balls.Count)
            {
                var clone = Instantiate(ballTemplate, poolBalls);
                clone.gameObject.SetActive(true);
                ballPool.Add(clone);
            }

            for (int i = 0; i < ballPool.Count; i++)
            {
                bool active = i < balls.Count;
                ballPool[i].gameObject.SetActive(active);
                if (active)
                    ballPool[i].anchoredPosition = new Vector2(balls[i].X, balls[i].Y);
            }
        }

        private void SyncLives(int lives)
        {
            for (int i = 0; i < livesImages.Length; i++)
                livesImages[i].gameObject.SetActive(i < lives);
        }

        private IEnumerator ShowWaveBanner(int wave)
        {
            bannerWaveText.text = $"Wave {wave}";
            bannerWave.SetActive(true);
            yield return new WaitForSecondsRealtime(WaveBannerSeconds);
            bannerWave.SetActive(false);
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
