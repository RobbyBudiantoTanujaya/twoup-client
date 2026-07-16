using System;
using System.Collections;
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
    /// Toggleable emote wheel: opens a 6-button panel, sends EmoteSend rate-gated to
    /// one per 3s (GDD), and shows incoming EmoteBroadcast for 2.5s.
    /// </summary>
    public class EmoteWheelController : MonoBehaviour
    {
        public static readonly string[] EmoteIds =
        {
            "emote_thumbsup", "emote_lol", "emote_wow", "emote_cry", "emote_fire", "emote_gg",
        };

        private static readonly string[] EmoteLabels = { "+1", "LOL", "WOW", "CRY", "FIRE", "GG" };

        private const float IncomingDisplaySeconds = 2.5f;

        [SerializeField] private Button toggleButton;
        [SerializeField] private GameObject wheelPanel;
        [SerializeField] private Button[] emoteButtons;
        [SerializeField] private TMP_Text incomingEmoteText;

        private readonly RateGate rateGate = new RateGate(3f);
        private Coroutine hideIncomingRoutine;

        private void Start()
        {
            toggleButton.onClick.AddListener(OnToggleClicked);
            for (int i = 0; i < emoteButtons.Length; i++)
            {
                int index = i;
                emoteButtons[index].onClick.AddListener(() => OnEmoteClicked(index));
            }

            wheelPanel.SetActive(false);
            incomingEmoteText.text = "";

            NetworkClient.Instance.EmoteBroadcastReceived += OnEmoteBroadcastReceived;
        }

        private void OnDestroy()
        {
            var net = NetworkClient.Instance;
            if (net != null)
                net.EmoteBroadcastReceived -= OnEmoteBroadcastReceived;
        }

        private void OnToggleClicked()
        {
            wheelPanel.SetActive(!wheelPanel.activeSelf);
        }

        private void OnEmoteClicked(int index)
        {
            if (!rateGate.TryPass("emote", Time.unscaledTime))
                return;

            NetworkClient.Instance.Send(new Envelope
            {
                EmoteSend = new EmoteSend { MatchId = MatchContext.MatchId, EmoteId = EmoteIds[index] },
            });
            wheelPanel.SetActive(false);
        }

        private void OnEmoteBroadcastReceived(EmoteBroadcast broadcast)
        {
            var sender = MatchContext.Match?.Players.FirstOrDefault(p => p.PlayerId == broadcast.PlayerId);
            string displayName = sender != null ? sender.DisplayName : "???";
            int emoteIndex = Array.IndexOf(EmoteIds, broadcast.EmoteId);
            string label = emoteIndex >= 0 ? EmoteLabels[emoteIndex] : broadcast.EmoteId;

            incomingEmoteText.text = $"{displayName}: {label}";

            if (hideIncomingRoutine != null)
                StopCoroutine(hideIncomingRoutine);
            hideIncomingRoutine = StartCoroutine(HideIncomingAfterDelay());
        }

        private IEnumerator HideIncomingAfterDelay()
        {
            yield return new WaitForSecondsRealtime(IncomingDisplaySeconds);
            incomingEmoteText.text = "";
            hideIncomingRoutine = null;
        }
    }
}
