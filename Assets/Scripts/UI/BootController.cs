using TMPro;
using Twoup.V1;
using TwoUp.Net;
using UnityEngine;
using UnityEngine.UI;

namespace TwoUp.UI
{
    /// <summary>Boot flow: connect → ClientHello → ServerHello → Home. Shows retry on failure.</summary>
    public class BootController : MonoBehaviour
    {
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button retryButton;
        [SerializeField] private PushTokenClient pushTokenClient;

        private void Start()
        {
            retryButton.onClick.AddListener(Connect);

            var net = NetworkClient.Instance;
            net.Connected += OnConnected;
            net.Disconnected += OnDisconnected;
            net.ServerHelloReceived += OnServerHello;

            Connect();
        }

        private void OnDestroy()
        {
            var net = NetworkClient.Instance;
            if (net == null)
                return;
            net.Connected -= OnConnected;
            net.Disconnected -= OnDisconnected;
            net.ServerHelloReceived -= OnServerHello;
        }

        private void Connect()
        {
            retryButton.gameObject.SetActive(false);
            statusText.text = "Connecting...";
            NetworkClient.Instance.Connect();
        }

        private void OnConnected()
        {
            statusText.text = "Handshaking...";
            NetworkClient.Instance.Send(new Envelope
            {
                ClientHello = new ClientHello
                {
                    DeviceId = DeviceIdentity.GetOrCreateDeviceId(),
                    DisplayName = DeviceIdentity.DisplayName,
                }
            });
        }

        private void OnServerHello(ServerHello hello)
        {
            statusText.text = $"Connected as {hello.PlayerId}";
            pushTokenClient.OnIdentified();
            if (!string.IsNullOrEmpty(MatchContext.PendingRoomCode))
                AppStateMachine.Instance.ToInvite();
            else
                AppStateMachine.Instance.ToHome();
        }

        private void OnDisconnected()
        {
            statusText.text = "Could not reach the server.";
            retryButton.gameObject.SetActive(true);
        }
    }
}
