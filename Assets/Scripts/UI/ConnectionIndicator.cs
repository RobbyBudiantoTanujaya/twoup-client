using TMPro;
using TwoUp.Net;
using UnityEngine;

namespace TwoUp.UI
{
    /// <summary>Shows live/reconnecting status driven by NetworkClient's connection events.</summary>
    public class ConnectionIndicator : MonoBehaviour
    {
        private static readonly Color ConnectedColor = new Color(0.25f, 0.85f, 0.35f);
        private static readonly Color DisconnectedColor = new Color(0.9f, 0.25f, 0.25f);

        [SerializeField] private TMP_Text label;

        private void Start()
        {
            var net = NetworkClient.Instance;
            net.Connected += OnConnected;
            net.Disconnected += OnDisconnected;

            if (net.IsConnected)
                OnConnected();
            else
                OnDisconnected();
        }

        private void OnDestroy()
        {
            var net = NetworkClient.Instance;
            if (net == null)
                return;
            net.Connected -= OnConnected;
            net.Disconnected -= OnDisconnected;
        }

        private void OnConnected()
        {
            label.text = "ON";
            label.color = ConnectedColor;
        }

        private void OnDisconnected()
        {
            label.text = "reconnecting";
            label.color = DisconnectedColor;
        }
    }
}
