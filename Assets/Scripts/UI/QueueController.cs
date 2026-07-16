using TMPro;
using Twoup.V1;
using TwoUp.Net;
using UnityEngine;
using UnityEngine.UI;

namespace TwoUp.UI
{
    /// <summary>
    /// Queue (S4): quick-match matchmaking wait. Joins the queue on entry; a bot joins
    /// after ~10s if nobody is around (GDD D4 bot-fill transparency, disclosed via Text_Hint).
    /// </summary>
    public class QueueController : MonoBehaviour
    {
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text hintText;
        [SerializeField] private Button cancelButton;

        private void Start()
        {
            cancelButton.onClick.AddListener(OnCancel);
            statusText.text = "Finding an opponent...";
            hintText.text = "~10s - a bot joins if nobody is around";

            var net = NetworkClient.Instance;
            net.PairFoundReceived += OnPairFound;
            net.ErrorReceived += OnError;

            net.Send(new Envelope { JoinQueue = new JoinQueue() });
        }

        private void OnDestroy()
        {
            var net = NetworkClient.Instance;
            if (net == null)
                return;
            net.PairFoundReceived -= OnPairFound;
            net.ErrorReceived -= OnError;
        }

        private void OnPairFound(PairFound msg)
        {
            MatchContext.PairId = msg.PairId;
            statusText.text = "Match found!";
            AppStateMachine.Instance.ToVoting();
        }

        private void OnError(Error msg)
        {
            statusText.text = msg.Message;
        }

        private void OnCancel()
        {
            NetworkClient.Instance.Send(new Envelope { LeaveQueue = new LeaveQueue() });
            AppStateMachine.Instance.ToHome();
        }
    }
}
