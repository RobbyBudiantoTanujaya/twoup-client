using System;
using Google.Protobuf;
using NativeWebSocket;
using Twoup.V1;
using UnityEngine;

namespace TwoUp.Net
{
    /// <summary>
    /// Owns the WebSocket (binary protobuf Envelopes). Everything else sends Envelopes
    /// through Send() and subscribes to the typed events below — game/UI code never
    /// touches the socket or NativeWebSocket types directly.
    /// Lives on the persistent App object created in the Boot scene.
    /// </summary>
    public class NetworkClient : MonoBehaviour
    {
        public static NetworkClient Instance { get; private set; }

        [SerializeField] private ServerConfig serverConfig;

        /// <summary>Assigned by the server via ServerHello; null until then.</summary>
        public string PlayerId { get; private set; }

        public bool IsConnected => socket != null && socket.State == WebSocketState.Open;

        public event Action Connected;
        public event Action Disconnected;
        public event Action<ServerHello> ServerHelloReceived;
        public event Action<RoomCreated> RoomCreatedReceived;
        public event Action<MatchFound> MatchFoundReceived;
        public event Action<GameStart> GameStartReceived;
        public event Action<GameState> GameStateReceived;
        public event Action<GameOver> GameOverReceived;
        public event Action<RematchRequest> RematchRequestReceived;
        public event Action<Error> ErrorReceived;

        private WebSocket socket;
        private float nextPingAt;
        private const float PingIntervalSeconds = 15f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public async void Connect()
        {
            if (socket != null &&
                (socket.State == WebSocketState.Open || socket.State == WebSocketState.Connecting))
                return;

            string url = serverConfig.ActiveUrl;
            Debug.Log($"[Net] Connecting to {url}");
            socket = new WebSocket(url);
            socket.OnOpen += () =>
            {
                Debug.Log("[Net] Connected");
                nextPingAt = Time.unscaledTime + PingIntervalSeconds;
                Connected?.Invoke();
            };
            socket.OnMessage += HandleMessage;
            socket.OnError += err => Debug.LogError($"[Net] Socket error: {err}");
            socket.OnClose += code =>
            {
                Debug.LogWarning($"[Net] Closed: {code}");
                PlayerId = null;
                Disconnected?.Invoke();
            };
            await socket.Connect(); // completes when the connection closes
        }

        public async void Send(Envelope envelope)
        {
            if (!IsConnected)
            {
                Debug.LogWarning($"[Net] Dropped {envelope.PayloadCase}: not connected");
                return;
            }
            await socket.Send(envelope.ToByteArray());
        }

        private void Update()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            socket?.DispatchMessageQueue();
#endif
            if (IsConnected && Time.unscaledTime >= nextPingAt)
            {
                nextPingAt = Time.unscaledTime + PingIntervalSeconds;
                Send(new Envelope { Ping = new Twoup.V1.Ping { Ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() } });
            }
        }

        private void HandleMessage(byte[] bytes)
        {
            Envelope env;
            try
            {
                env = Envelope.Parser.ParseFrom(bytes);
            }
            catch (InvalidProtocolBufferException e)
            {
                Debug.LogError($"[Net] Unparseable frame ({bytes.Length} bytes): {e.Message}");
                return;
            }

            switch (env.PayloadCase)
            {
                case Envelope.PayloadOneofCase.ServerHello:
                    PlayerId = env.ServerHello.PlayerId;
                    ServerHelloReceived?.Invoke(env.ServerHello);
                    break;
                case Envelope.PayloadOneofCase.RoomCreated:
                    RoomCreatedReceived?.Invoke(env.RoomCreated);
                    break;
                case Envelope.PayloadOneofCase.MatchFound:
                    MatchFoundReceived?.Invoke(env.MatchFound);
                    break;
                case Envelope.PayloadOneofCase.GameStart:
                    GameStartReceived?.Invoke(env.GameStart);
                    break;
                case Envelope.PayloadOneofCase.GameState:
                    GameStateReceived?.Invoke(env.GameState);
                    break;
                case Envelope.PayloadOneofCase.GameOver:
                    GameOverReceived?.Invoke(env.GameOver);
                    break;
                case Envelope.PayloadOneofCase.RematchRequest:
                    RematchRequestReceived?.Invoke(env.RematchRequest);
                    break;
                case Envelope.PayloadOneofCase.Error:
                    Debug.LogWarning($"[Net] Server error {env.Error.Code}: {env.Error.Message}");
                    ErrorReceived?.Invoke(env.Error);
                    break;
                case Envelope.PayloadOneofCase.Pong:
                    // TODO: track round-trip latency from Pong.Ts
                    break;
                default:
                    Debug.LogWarning($"[Net] Unhandled payload: {env.PayloadCase}");
                    break;
            }
        }

        private async void OnApplicationQuit()
        {
            if (socket != null)
                await socket.Close();
        }
    }
}
