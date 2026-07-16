using System;
using TMPro;
using Twoup.V1;
using TwoUp.Logic;
using TwoUp.Net;
using UnityEngine;
using UnityEngine.UI;

namespace TwoUp.UI
{
    /// <summary>
    /// Invite Room (S3): create-or-join a private room. On entry, resumes a pending deep-linked
    /// room code (join) or asks the server for a fresh room (create). Server-authoritative TTL
    /// countdown is rendered from RoomCreated.expires_at_unix_ms, never computed locally.
    /// </summary>
    public class InviteRoomController : MonoBehaviour
    {
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_Text roomCodeText;
        [SerializeField] private TMP_Text ttlCountdownText;
        [SerializeField] private Button copyLinkButton;
        [SerializeField] private TMP_Text waitingOpponentText;
        [SerializeField] private TMP_InputField roomCodeInput;
        [SerializeField] private Button joinButton;
        [SerializeField] private TMP_Text toastText;

        private long expiresAtUnixMs;
        private bool hasExpiry;

        private void Start()
        {
            backButton.onClick.AddListener(OnBack);
            copyLinkButton.onClick.AddListener(OnCopyLink);
            joinButton.onClick.AddListener(OnJoin);
            toastText.gameObject.SetActive(false);

            var net = NetworkClient.Instance;
            net.RoomCreatedReceived += OnRoomCreated;
            net.PairFoundReceived += OnPairFound;
            net.RoomJoinPendingReceived += OnRoomJoinPending;
            net.RoomExpiredReceived += OnRoomExpired;
            net.ErrorReceived += OnError;

            if (!string.IsNullOrEmpty(MatchContext.PendingRoomCode))
            {
                string pendingCode = MatchContext.PendingRoomCode;
                roomCodeInput.text = pendingCode;
                MatchContext.PendingRoomCode = null;
                net.Send(new Envelope { JoinRoom = new JoinRoom { RoomCode = pendingCode } });
            }
            else
            {
                net.Send(new Envelope { CreateRoom = new CreateRoom() });
            }
        }

        private void OnDestroy()
        {
            var net = NetworkClient.Instance;
            if (net == null)
                return;
            net.RoomCreatedReceived -= OnRoomCreated;
            net.PairFoundReceived -= OnPairFound;
            net.RoomJoinPendingReceived -= OnRoomJoinPending;
            net.RoomExpiredReceived -= OnRoomExpired;
            net.ErrorReceived -= OnError;
        }

        private void Update()
        {
            if (!hasExpiry)
                return;

            var remaining = DateTimeOffset.FromUnixTimeMilliseconds(expiresAtUnixMs) - DateTimeOffset.UtcNow;
            if (remaining < TimeSpan.Zero)
                remaining = TimeSpan.Zero;
            ttlCountdownText.text = $"{(int)remaining.TotalMinutes:00}:{remaining.Seconds:00}";
        }

        private void OnRoomCreated(RoomCreated msg)
        {
            roomCodeText.text = msg.RoomCode;
            expiresAtUnixMs = msg.ExpiresAtUnixMs;
            hasExpiry = true;
        }

        private void OnPairFound(PairFound msg)
        {
            MatchContext.PairId = msg.PairId;
            AppStateMachine.Instance.ToVoting();
        }

        private void OnRoomJoinPending(RoomJoinPending msg)
        {
            ShowToast($"{msg.InviteeDisplayName} joined - starting...");
        }

        private void OnRoomExpired(RoomExpired msg)
        {
            ShowToast("Room expired. Create a new one!");
            NetworkClient.Instance.Send(new Envelope { CreateRoom = new CreateRoom() });
        }

        private void OnError(Error msg)
        {
            ShowToast(msg.Message);
        }

        private void OnCopyLink()
        {
            GUIUtility.systemCopyBuffer = NetworkClient.Instance.InviteLinkBase + roomCodeText.text;
            ShowToast("Link copied");
        }

        private void OnJoin()
        {
            string code = RoomCodeSanitizer.Sanitize(roomCodeInput.text);
            if (string.IsNullOrEmpty(code))
            {
                ShowToast("Enter a room code");
                return;
            }
            NetworkClient.Instance.Send(new Envelope { JoinRoom = new JoinRoom { RoomCode = code } });
        }

        // TODO(contract): no close-room message exists yet, so Back leaves any reserved
        // room to expire server-side instead of notifying it explicitly.
        private void OnBack() => AppStateMachine.Instance.ToHome();

        private void ShowToast(string message)
        {
            toastText.text = message;
            toastText.gameObject.SetActive(true);
        }
    }
}
