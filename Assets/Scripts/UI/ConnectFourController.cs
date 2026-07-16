using Google.Protobuf;
using TMPro;
using Twoup.V1;
using TwoUp.Net;
using UnityEngine;
using UnityEngine.UI;

namespace TwoUp.UI
{
    /// <summary>
    /// Renders the server-sent ConnectFourState and sends ConnectFourInput on column tap.
    /// Server-authoritative: no local rules beyond input legality hints (full column or
    /// not-my-turn → column button disabled).
    /// </summary>
    public class ConnectFourController : MonoBehaviour
    {
        public const int Columns = 7;
        public const int Rows = 6;

        [SerializeField] private TMP_Text turnText;
        [SerializeField] private TMP_Text youAreText;
        [Tooltip("42 cells in display order: top-left → bottom-right, row-major.")]
        [SerializeField] private Image[] cells;
        [Tooltip("7 column tap zones, left → right.")]
        [SerializeField] private Button[] columnButtons;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private TMP_Text rematchStatusText;
        [SerializeField] private Button rematchButton;
        [SerializeField] private Button backButton;

        // TODO(contract): the contract doesn't say whether cells row 0 is the bottom or the
        // top of the board. We assume row 0 = bottom (gravity row); flip if the server differs.
        private const bool RowZeroIsBottom = true;

        private static readonly Color EmptyColor = new Color(0.16f, 0.19f, 0.28f);
        private static readonly Color[] SeatColors =
        {
            new Color(0.90f, 0.30f, 0.24f), // seat 0: red
            new Color(0.95f, 0.77f, 0.06f), // seat 1: yellow
        };

        private bool gameOver;

        private void Start()
        {
            for (int c = 0; c < Columns; c++)
            {
                int column = c; // capture per-iteration
                columnButtons[c].onClick.AddListener(() => OnColumnTapped(column));
            }
            rematchButton.onClick.AddListener(OnRematch);
            backButton.onClick.AddListener(OnBack);
            gameOverPanel.SetActive(false);

            var net = NetworkClient.Instance;
            net.GameStateReceived += OnGameState;
            net.GameOverReceived += OnGameOver;
            net.GameStartReceived += OnGameStart; // accepted rematch arrives as a fresh GameStart
            net.RematchRequestReceived += OnRematchRequest;

            int mySeat = Mathf.Clamp(MatchContext.MySeat, 0, 1);
            youAreText.text = $"You are {(mySeat == 0 ? "RED" : "YELLOW")}";
            youAreText.color = SeatColors[mySeat];

            if (MatchContext.PendingGameStart != null)
            {
                RenderStateBytes(MatchContext.PendingGameStart.InitialState);
                MatchContext.PendingGameStart = null;
            }
            else
            {
                turnText.text = "Waiting for game state...";
                SetColumnsInteractable(false);
            }
        }

        private void OnDestroy()
        {
            var net = NetworkClient.Instance;
            if (net == null)
                return;
            net.GameStateReceived -= OnGameState;
            net.GameOverReceived -= OnGameOver;
            net.GameStartReceived -= OnGameStart;
            net.RematchRequestReceived -= OnRematchRequest;
        }

        private void OnColumnTapped(int column)
        {
            if (gameOver)
                return;
            NetworkClient.Instance.Send(new Envelope
            {
                GameInput = new GameInput
                {
                    MatchId = MatchContext.MatchId,
                    Input = new ConnectFourInput { Column = column }.ToByteString(),
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
            ConnectFourState state;
            try
            {
                state = ConnectFourState.Parser.ParseFrom(bytes);
            }
            catch (InvalidProtocolBufferException e)
            {
                Debug.LogError($"[C4] Unparseable ConnectFourState: {e.Message}");
                return;
            }
            if (state.Cells.Count != Rows * Columns)
            {
                Debug.LogError($"[C4] Expected {Rows * Columns} cells, got {state.Cells.Count}");
                return;
            }
            Render(state);
        }

        private void Render(ConnectFourState state)
        {
            for (int i = 0; i < state.Cells.Count; i++)
            {
                int stateRow = i / Columns;
                int col = i % Columns;
                int displayRow = RowZeroIsBottom ? Rows - 1 - stateRow : stateRow;
                int v = state.Cells[i];
                cells[displayRow * Columns + col].color =
                    v == 0 ? EmptyColor : SeatColors[Mathf.Clamp(v - 1, 0, 1)];
            }

            bool myTurn = state.NextPlayerId == NetworkClient.Instance.PlayerId;
            turnText.text = myTurn
                ? "Your turn"
                : $"{MatchContext.Opponent?.DisplayName ?? "Opponent"}'s turn";
            for (int c = 0; c < Columns; c++)
                columnButtons[c].interactable = myTurn && !gameOver && !IsColumnFull(state, c);
        }

        /// <summary>Input legality hint only — the server stays authoritative.</summary>
        private bool IsColumnFull(ConnectFourState state, int column)
        {
            for (int r = 0; r < Rows; r++)
            {
                if (state.Cells[r * Columns + column] == 0)
                    return false;
            }
            return true;
        }

        private void SetColumnsInteractable(bool on)
        {
            foreach (var button in columnButtons)
                button.interactable = on;
        }

        private void OnGameOver(GameOver msg)
        {
            if (msg.MatchId != MatchContext.MatchId)
                return;
            gameOver = true;
            SetColumnsInteractable(false);
            AppStateMachine.Instance.SetResult();

            string myId = NetworkClient.Instance.PlayerId;
            if (msg.Result.Draw)
                resultText.text = "Draw!";
            else if (msg.Result.WinnerPlayerIds.Contains(myId))
                resultText.text = "You win!";
            else
                resultText.text = "You lose";

            rematchStatusText.text = "";
            rematchButton.interactable = true;
            turnText.text = "Game over";
            gameOverPanel.SetActive(true);
        }

        private void OnRematch()
        {
            NetworkClient.Instance.Send(new Envelope
            {
                RematchRequest = new RematchRequest { MatchId = MatchContext.MatchId, Accept = true }
            });
            rematchStatusText.text = "Waiting for opponent...";
            rematchButton.interactable = false;
        }

        private void OnRematchRequest(RematchRequest msg)
        {
            // Server relayed the opponent's rematch request; the Rematch button doubles as Accept.
            if (msg.MatchId != MatchContext.MatchId || !gameOver)
                return;
            rematchStatusText.text = "Opponent wants a rematch!";
        }

        private void OnGameStart(GameStart msg)
        {
            // Fresh game (rematch accepted). Reset and render the initial state.
            MatchContext.MatchId = msg.MatchId;
            gameOver = false;
            gameOverPanel.SetActive(false);
            AppStateMachine.Instance.SetInGame();
            RenderStateBytes(msg.InitialState);
        }

        private void OnBack()
        {
            // TODO(contract): no leave-match/forfeit message exists; we just return home.
            MatchContext.Clear();
            AppStateMachine.Instance.ToHome();
        }
    }
}
