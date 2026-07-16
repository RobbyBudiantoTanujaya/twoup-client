using System.Collections;
using System.Collections.Generic;
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
    /// Placement is tray-tap-rotate (plan decision #13), not free drag: pick a ship from the tray,
    /// pick its orientation via Btn_Rotate, then tap Grid_MyFleet for the origin cell. Placement is
    /// pre-checked client-side (in-bounds + non-overlap) purely as an input-legality hint — the
    /// server remains authoritative and re-validates on BattleshipPlaceInput. Firing renders
    /// whatever BattleshipState the server pushes; no local hit/miss/sunk computation.
    /// </summary>
    public class BattleshipController : MonoBehaviour
    {
        private static readonly int[] ShipLengths = { 5, 4, 3, 3, 2 };
        private const int GridSize = BattleshipPlacementValidator.GridSize;
        private const float InvalidFlashSeconds = 0.3f;
        private const float MatchWentAsyncToastSeconds = 2f;

        private static readonly Color CellEmptyColor = new Color(0.16f, 0.19f, 0.28f);
        private static readonly Color ShipColor = new Color(0.10f, 0.20f, 0.55f);
        private static readonly Color MissColor = new Color(0.92f, 0.92f, 0.92f);
        private static readonly Color HitColor = new Color(0.85f, 0.15f, 0.15f);
        private static readonly Color SunkColor = new Color(0.45f, 0.05f, 0.05f);
        private static readonly Color SelectedColor = new Color(0.95f, 0.85f, 0.15f);
        private static readonly Color InvalidFlashColor = new Color(1f, 0.2f, 0.2f);

        [SerializeField] private GameObject placementPanel;
        [SerializeField] private GameObject firingPanel;

        [SerializeField] private BattleshipGridView myFleetGrid;
        [SerializeField] private Button[] shipTrayButtons;
        [SerializeField] private Button rotateButton;
        [SerializeField] private TMP_Text rotateButtonText;
        [SerializeField] private Button randomButton;
        [SerializeField] private Button lockPlacementButton;
        [SerializeField] private TMP_Text placementHintText;

        [SerializeField] private BattleshipGridView targetGrid;
        [SerializeField] private BattleshipGridView myBoardGrid;
        [SerializeField] private TMP_Text turnBannerText;
        [SerializeField] private Button fireButton;
        [SerializeField] private TMP_Text shotResultText;

        [SerializeField] private TMP_Text toastText;

        private readonly ShipPlacement[] placedShips = new ShipPlacement[5];
        private readonly bool[] shipPlaced = new bool[5];
        private int? selectedShipIndex;
        private bool horizontal = true;

        private BattleshipState lastState;
        private bool myTurn;
        private (int row, int col)? selectedTargetCell;
        private int lastSunkCount;
        private Coroutine flashCoroutine;
        private Coroutine toastRoutine;

        private void Start()
        {
            placementPanel.SetActive(true);
            firingPanel.SetActive(false);
            toastText.gameObject.SetActive(false);
            shotResultText.text = "";
            placementHintText.text = "Place your ships";
            fireButton.interactable = false;
            lockPlacementButton.interactable = false;

            for (int i = 0; i < shipTrayButtons.Length; i++)
            {
                int index = i;
                shipTrayButtons[index].onClick.AddListener(() => SelectShip(index));
            }
            rotateButton.onClick.AddListener(OnRotateClicked);
            randomButton.onClick.AddListener(OnRandomClicked);
            lockPlacementButton.onClick.AddListener(OnLockClicked);
            fireButton.onClick.AddListener(OnFireClicked);

            myFleetGrid.CellTapped += OnMyFleetCellTapped;
            targetGrid.CellTapped += OnTargetCellTapped;

            for (int r = 0; r < GridSize; r++)
                for (int c = 0; c < GridSize; c++)
                    myFleetGrid.SetCell(r, c, CellEmptyColor);

            UpdateRotateLabel();

            var net = NetworkClient.Instance;
            net.GameStateReceived += OnGameState;
            net.GameOverReceived += OnGameOver;
            net.MatchWentAsyncReceived += OnMatchWentAsync;

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
            myFleetGrid.CellTapped -= OnMyFleetCellTapped;
            targetGrid.CellTapped -= OnTargetCellTapped;

            var net = NetworkClient.Instance;
            if (net == null)
                return;
            net.GameStateReceived -= OnGameState;
            net.GameOverReceived -= OnGameOver;
            net.MatchWentAsyncReceived -= OnMatchWentAsync;
        }

        // ------------------------------------------------------------------ placement

        private void SelectShip(int index)
        {
            if (shipPlaced[index])
                return;
            selectedShipIndex = index;
        }

        private void OnRotateClicked()
        {
            horizontal = !horizontal;
            UpdateRotateLabel();
        }

        private void UpdateRotateLabel()
        {
            rotateButtonText.text = horizontal ? "Rotate: Horizontal" : "Rotate: Vertical";
        }

        private void OnMyFleetCellTapped(int row, int col)
        {
            if (!selectedShipIndex.HasValue)
                return;
            int index = selectedShipIndex.Value;
            var candidate = new ShipPlacement
            {
                Length = ShipLengths[index],
                Row = row,
                Col = col,
                Horizontal = horizontal,
            };

            var placedList = GetPlacedShipsList();
            if (!IsPlacementValid(candidate, placedList))
            {
                FlashInvalidCell(row, col);
                return;
            }

            placedShips[index] = candidate;
            shipPlaced[index] = true;
            PaintShip(candidate);
            shipTrayButtons[index].interactable = false;
            selectedShipIndex = null;
            UpdateLockInteractable();
        }

        private bool IsPlacementValid(ShipPlacement candidate, List<ShipPlacement> placedList)
        {
            if (candidate.Row < 0 || candidate.Row >= GridSize || candidate.Col < 0 || candidate.Col >= GridSize)
                return false;
            if (candidate.Horizontal && candidate.Col + candidate.Length - 1 >= GridSize)
                return false;
            if (!candidate.Horizontal && candidate.Row + candidate.Length - 1 >= GridSize)
                return false;

            for (int i = 0; i < candidate.Length; i++)
            {
                int r = candidate.Horizontal ? candidate.Row : candidate.Row + i;
                int c = candidate.Horizontal ? candidate.Col + i : candidate.Col;
                if (BattleshipPlacementValidator.CellsOccupied(placedList, r, c))
                    return false;
            }
            return true;
        }

        private void PaintShip(ShipPlacement ship)
        {
            for (int i = 0; i < ship.Length; i++)
            {
                int r = ship.Horizontal ? ship.Row : ship.Row + i;
                int c = ship.Horizontal ? ship.Col + i : ship.Col;
                myFleetGrid.SetCell(r, c, ShipColor);
            }
        }

        private void FlashInvalidCell(int row, int col)
        {
            if (flashCoroutine != null)
                StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashCellRedThenRestore(row, col));
        }

        private IEnumerator FlashCellRedThenRestore(int row, int col)
        {
            myFleetGrid.SetCell(row, col, InvalidFlashColor);
            yield return new WaitForSeconds(InvalidFlashSeconds);
            bool occupied = BattleshipPlacementValidator.CellsOccupied(GetPlacedShipsList(), row, col);
            myFleetGrid.SetCell(row, col, occupied ? ShipColor : CellEmptyColor);
            flashCoroutine = null;
        }

        private void OnRandomClicked()
        {
            var ships = BattleshipPlacementGenerator.Generate(System.Environment.TickCount);

            for (int r = 0; r < GridSize; r++)
                for (int c = 0; c < GridSize; c++)
                    myFleetGrid.SetCell(r, c, CellEmptyColor);

            for (int i = 0; i < ships.Count; i++)
            {
                placedShips[i] = ships[i];
                shipPlaced[i] = true;
                PaintShip(ships[i]);
                shipTrayButtons[i].interactable = false;
            }
            selectedShipIndex = null;
            UpdateLockInteractable();
        }

        private void UpdateLockInteractable()
        {
            bool allPlaced = true;
            foreach (var placed in shipPlaced)
            {
                if (!placed)
                {
                    allPlaced = false;
                    break;
                }
            }
            lockPlacementButton.interactable = allPlaced && BattleshipPlacementValidator.IsValid(placedShips);
        }

        private void OnLockClicked()
        {
            var input = new BattleshipPlaceInput();
            input.Ships.AddRange(placedShips);
            NetworkClient.Instance.Send(new Envelope
            {
                GameInput = new GameInput
                {
                    MatchId = MatchContext.MatchId,
                    Input = input.ToByteString(),
                }
            });
            lockPlacementButton.interactable = false;
            placementHintText.text = "Waiting for opponent...";
        }

        private List<ShipPlacement> GetPlacedShipsList()
        {
            var list = new List<ShipPlacement>();
            for (int i = 0; i < placedShips.Length; i++)
                if (shipPlaced[i])
                    list.Add(placedShips[i]);
            return list;
        }

        // ------------------------------------------------------------------ firing

        private void OnTargetCellTapped(int row, int col)
        {
            if (!myTurn || lastState == null)
                return;
            int index = row * GridSize + col;
            if (index < lastState.MyShotsResult.Count && lastState.MyShotsResult[index] != 0)
                return;

            if (selectedTargetCell.HasValue)
            {
                var prev = selectedTargetCell.Value;
                targetGrid.SetCell(prev.row, prev.col, ColorForShotResult(ShotResultAt(prev.row, prev.col)));
            }

            selectedTargetCell = (row, col);
            targetGrid.SetCell(row, col, SelectedColor);
            fireButton.interactable = true;
        }

        private void OnFireClicked()
        {
            if (!selectedTargetCell.HasValue)
                return;
            var cell = selectedTargetCell.Value;
            NetworkClient.Instance.Send(new Envelope
            {
                GameInput = new GameInput
                {
                    MatchId = MatchContext.MatchId,
                    Input = new BattleshipFireInput { Row = cell.row, Col = cell.col }.ToByteString(),
                }
            });
            fireButton.interactable = false;
            selectedTargetCell = null;
        }

        private int ShotResultAt(int row, int col)
        {
            int index = row * GridSize + col;
            return lastState != null && index < lastState.MyShotsResult.Count ? lastState.MyShotsResult[index] : 0;
        }

        private static Color ColorForShotResult(int value)
        {
            switch (value)
            {
                case 1: return MissColor;
                case 2: return HitColor;
                case 3: return SunkColor;
                default: return CellEmptyColor;
            }
        }

        // ------------------------------------------------------------------ network

        private void OnGameState(GameState msg)
        {
            if (msg.MatchId != MatchContext.MatchId)
                return;
            RenderStateBytes(msg.State);
        }

        private void RenderStateBytes(ByteString bytes)
        {
            BattleshipState state;
            try
            {
                state = BattleshipState.Parser.ParseFrom(bytes);
            }
            catch (InvalidProtocolBufferException e)
            {
                Debug.LogError($"[BS] Unparseable BattleshipState: {e.Message}");
                return;
            }
            Render(state);
        }

        private void Render(BattleshipState state)
        {
            lastState = state;

            if (state.Phase != BattleshipPhase.BsFiring)
            {
                placementHintText.text = state.MyPlacementLocked ? "Waiting for opponent..." : "Place your ships";
                return;
            }

            if (placementPanel.activeSelf)
            {
                placementPanel.SetActive(false);
                firingPanel.SetActive(true);
            }

            RenderMyBoard(state);
            RenderTargetGrid(state);

            myTurn = state.NextPlayerId == NetworkClient.Instance.PlayerId;
            turnBannerText.text = myTurn ? "YOUR TURN" : "Their turn";
            UpdateTargetInteractable(state);

            if (state.MyShipsSunkByOpponent.Count > lastSunkCount)
            {
                shotResultText.text = $"They sunk your {state.MyShipsSunkByOpponent[state.MyShipsSunkByOpponent.Count - 1]}!";
                lastSunkCount = state.MyShipsSunkByOpponent.Count;
            }
        }

        private void RenderMyBoard(BattleshipState state)
        {
            for (int r = 0; r < GridSize; r++)
            {
                for (int c = 0; c < GridSize; c++)
                {
                    int index = r * GridSize + c;
                    bool hasShip = index < state.MyFleet.Count && state.MyFleet[index] != 0;
                    int shot = index < state.ShotsAgainstMe.Count ? state.ShotsAgainstMe[index] : 0;
                    var color = shot == 2 ? HitColor : shot == 1 ? MissColor : (hasShip ? ShipColor : CellEmptyColor);
                    myBoardGrid.SetCell(r, c, color);
                }
            }
        }

        private void RenderTargetGrid(BattleshipState state)
        {
            for (int r = 0; r < GridSize; r++)
            {
                for (int c = 0; c < GridSize; c++)
                {
                    if (selectedTargetCell.HasValue && selectedTargetCell.Value.row == r && selectedTargetCell.Value.col == c)
                        continue;

                    int index = r * GridSize + c;
                    int value = index < state.MyShotsResult.Count ? state.MyShotsResult[index] : 0;
                    targetGrid.SetCell(r, c, ColorForShotResult(value));
                }
            }
        }

        private void UpdateTargetInteractable(BattleshipState state)
        {
            for (int r = 0; r < GridSize; r++)
            {
                for (int c = 0; c < GridSize; c++)
                {
                    int index = r * GridSize + c;
                    bool shot = index < state.MyShotsResult.Count && state.MyShotsResult[index] != 0;
                    targetGrid.SetInteractable(r, c, myTurn && !shot);
                }
            }
        }

        private void OnGameOver(GameOver msg)
        {
            if (msg.MatchId != MatchContext.MatchId)
                return;
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
