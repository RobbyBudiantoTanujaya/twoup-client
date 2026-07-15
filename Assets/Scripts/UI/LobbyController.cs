using TMPro;
using Twoup.V1;
using TwoUp.Net;
using UnityEngine;
using UnityEngine.UI;

namespace TwoUp.UI
{
    /// <summary>
    /// Lobby: quick match (queue), create room (code), join room by code. Shows the
    /// matched-with panel on MatchFound and hands off to the game scene on GameStart.
    /// </summary>
    public class LobbyController : MonoBehaviour
    {
        [SerializeField] private Button quickMatchButton;
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button joinRoomButton;
        [SerializeField] private TMP_InputField roomCodeInput;
        [SerializeField] private Button cancelQueueButton;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private GameObject matchedPanel;
        [SerializeField] private TMP_Text opponentNameText;
        [SerializeField] private GameObject botBadge;

        private void Start()
        {
            quickMatchButton.onClick.AddListener(OnQuickMatch);
            createRoomButton.onClick.AddListener(OnCreateRoom);
            joinRoomButton.onClick.AddListener(OnJoinRoom);
            cancelQueueButton.onClick.AddListener(OnCancelQueue);

            var net = NetworkClient.Instance;
            net.RoomCreatedReceived += OnRoomCreated;
            net.MatchFoundReceived += OnMatchFound;
            net.GameStartReceived += OnGameStart;
            net.ErrorReceived += OnError;

            statusText.text = "";
            cancelQueueButton.gameObject.SetActive(false);
            matchedPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            var net = NetworkClient.Instance;
            if (net == null)
                return;
            net.RoomCreatedReceived -= OnRoomCreated;
            net.MatchFoundReceived -= OnMatchFound;
            net.GameStartReceived -= OnGameStart;
            net.ErrorReceived -= OnError;
        }

        private void SetMenuInteractable(bool on)
        {
            quickMatchButton.interactable = on;
            createRoomButton.interactable = on;
            joinRoomButton.interactable = on;
            roomCodeInput.interactable = on;
        }

        private void OnQuickMatch()
        {
            NetworkClient.Instance.Send(new Envelope { JoinQueue = new JoinQueue() });
            statusText.text = "In queue - waiting for a match...";
            SetMenuInteractable(false);
            cancelQueueButton.gameObject.SetActive(true);
        }

        private void OnCancelQueue()
        {
            NetworkClient.Instance.Send(new Envelope { LeaveQueue = new LeaveQueue() });
            ResetToMenu("Left the queue.");
        }

        private void OnCreateRoom()
        {
            NetworkClient.Instance.Send(new Envelope { CreateRoom = new CreateRoom() });
            statusText.text = "Creating room...";
            SetMenuInteractable(false);
            // TODO(contract): no message exists to cancel/close a created room.
        }

        private void OnJoinRoom()
        {
            string code = roomCodeInput.text.Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(code))
            {
                statusText.text = "Enter a room code first.";
                return;
            }
            NetworkClient.Instance.Send(new Envelope { JoinRoom = new JoinRoom { RoomCode = code } });
            statusText.text = $"Joining room {code}...";
            SetMenuInteractable(false);
        }

        private void OnRoomCreated(RoomCreated msg)
        {
            statusText.text = $"Room code: {msg.RoomCode}\nWaiting for an opponent...";
        }

        private void OnMatchFound(MatchFound msg)
        {
            MatchContext.Set(msg, NetworkClient.Instance.PlayerId);
            cancelQueueButton.gameObject.SetActive(false);
            statusText.text = "";
            var opponent = MatchContext.Opponent;
            opponentNameText.text = opponent != null ? opponent.DisplayName : "???";
            botBadge.SetActive(opponent != null && opponent.IsBot);
            matchedPanel.SetActive(true);
        }

        private void OnGameStart(GameStart msg)
        {
            MatchContext.PendingGameStart = msg;
            MatchContext.MatchId = msg.MatchId;
            AppStateMachine.Instance.ToGame();
        }

        private void OnError(Error err)
        {
            ResetToMenu($"Error: {err.Message} ({err.Code})");
        }

        private void ResetToMenu(string status)
        {
            statusText.text = status;
            SetMenuInteractable(true);
            cancelQueueButton.gameObject.SetActive(false);
            matchedPanel.SetActive(false);
        }
    }
}
