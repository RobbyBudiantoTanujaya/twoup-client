using System;
using System.Collections.Generic;
using Google.Protobuf;
using NativeWebSocket;
using Twoup.V1;
using TwoUp.Logic;
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

        public string InviteLinkBase => serverConfig.inviteLinkBase;

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
        public event Action<PairFound> PairFoundReceived;
        public event Action<VoteUpdate> VoteUpdateReceived;
        public event Action<VotingLocked> VotingLockedReceived;
        public event Action<VotingShowdown> VotingShowdownReceived;
        public event Action<VotingCancelled> VotingCancelledReceived;
        public event Action<RematchStatusUpdate> RematchStatusUpdateReceived;
        public event Action<RoomJoinPending> RoomJoinPendingReceived;
        public event Action<RoomExpired> RoomExpiredReceived;
        public event Action<AsyncMatchList> AsyncMatchListReceived;
        public event Action<MatchResumed> MatchResumedReceived;
        public event Action<MatchWentAsync> MatchWentAsyncReceived;
        public event Action<EmoteBroadcast> EmoteBroadcastReceived;
        public event Action<ProfileData> ProfileDataReceived;
        public event Action<PairDetail> PairDetailReceived;
        public event Action<ShopData> ShopDataReceived;
        public event Action<WalletUpdate> WalletUpdateReceived;
        public event Action<EconomyConfig> OnEconomyConfig;
        public event Action<WalletUpdate> OnWalletUpdate;
        public event Action<DailyRewardClaimed> OnDailyRewardClaimed;

        public float PingIntervalSeconds { get; set; } = 15f;

        private WebSocket socket;
        private float nextPingAt;
        private readonly Dictionary<string, RateGate> rateGatesByKey = new Dictionary<string, RateGate>();
        private readonly Dictionary<string, Envelope> pendingRateLimited = new Dictionary<string, Envelope>();

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

        /// <summary>
        /// Leading-edge rate limit per key: sends immediately if the key's interval has
        /// elapsed, otherwise stores the latest envelope for that key and flushes it from
        /// Update() once the interval passes (trailing latest — never drops the newest state).
        /// </summary>
        public void SendRateLimited(Envelope envelope, string key, float minIntervalSeconds)
        {
            if (!rateGatesByKey.TryGetValue(key, out var gate))
            {
                gate = new RateGate(minIntervalSeconds);
                rateGatesByKey[key] = gate;
            }

            if (gate.TryPass(key, Time.unscaledTime))
            {
                pendingRateLimited.Remove(key);
                Send(envelope);
            }
            else
            {
                pendingRateLimited[key] = envelope;
            }
        }

        public void SendClaimDailyReward()
        {
            Send(new Envelope { ClaimDailyReward = new ClaimDailyReward() });
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

            FlushRateLimited();
        }

        private void FlushRateLimited()
        {
            if (pendingRateLimited.Count == 0)
                return;

            List<string> readyKeys = null;
            foreach (var kvp in pendingRateLimited)
            {
                if (rateGatesByKey[kvp.Key].TryPass(kvp.Key, Time.unscaledTime))
                {
                    readyKeys ??= new List<string>();
                    readyKeys.Add(kvp.Key);
                }
            }

            if (readyKeys == null)
                return;

            foreach (var key in readyKeys)
            {
                var envelope = pendingRateLimited[key];
                pendingRateLimited.Remove(key);
                Send(envelope);
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
                case Envelope.PayloadOneofCase.PairFound:
                    PairFoundReceived?.Invoke(env.PairFound);
                    break;
                case Envelope.PayloadOneofCase.VoteUpdate:
                    VoteUpdateReceived?.Invoke(env.VoteUpdate);
                    break;
                case Envelope.PayloadOneofCase.VotingLocked:
                    VotingLockedReceived?.Invoke(env.VotingLocked);
                    break;
                case Envelope.PayloadOneofCase.VotingShowdown:
                    VotingShowdownReceived?.Invoke(env.VotingShowdown);
                    break;
                case Envelope.PayloadOneofCase.VotingCancelled:
                    VotingCancelledReceived?.Invoke(env.VotingCancelled);
                    break;
                case Envelope.PayloadOneofCase.RematchStatusUpdate:
                    RematchStatusUpdateReceived?.Invoke(env.RematchStatusUpdate);
                    break;
                case Envelope.PayloadOneofCase.RoomJoinPending:
                    RoomJoinPendingReceived?.Invoke(env.RoomJoinPending);
                    break;
                case Envelope.PayloadOneofCase.RoomExpired:
                    RoomExpiredReceived?.Invoke(env.RoomExpired);
                    break;
                case Envelope.PayloadOneofCase.AsyncMatchList:
                    AsyncMatchListReceived?.Invoke(env.AsyncMatchList);
                    break;
                case Envelope.PayloadOneofCase.MatchResumed:
                    MatchResumedReceived?.Invoke(env.MatchResumed);
                    break;
                case Envelope.PayloadOneofCase.MatchWentAsync:
                    MatchWentAsyncReceived?.Invoke(env.MatchWentAsync);
                    break;
                case Envelope.PayloadOneofCase.EmoteBroadcast:
                    EmoteBroadcastReceived?.Invoke(env.EmoteBroadcast);
                    break;
                case Envelope.PayloadOneofCase.ProfileData:
                    ProfileDataReceived?.Invoke(env.ProfileData);
                    break;
                case Envelope.PayloadOneofCase.PairDetail:
                    PairDetailReceived?.Invoke(env.PairDetail);
                    break;
                case Envelope.PayloadOneofCase.ShopData:
                    ShopDataReceived?.Invoke(env.ShopData);
                    break;
                case Envelope.PayloadOneofCase.WalletUpdate:
                    EconomyState.ApplyWallet(env.WalletUpdate);
                    WalletUpdateReceived?.Invoke(env.WalletUpdate);
                    OnWalletUpdate?.Invoke(env.WalletUpdate);
                    break;
                case Envelope.PayloadOneofCase.EconomyConfig:
                    EconomyState.ApplyConfig(env.EconomyConfig);
                    OnEconomyConfig?.Invoke(env.EconomyConfig);
                    break;
                case Envelope.PayloadOneofCase.DailyRewardClaimed:
                    EconomyState.ApplyDailyClaimed(env.DailyRewardClaimed);
                    OnDailyRewardClaimed?.Invoke(env.DailyRewardClaimed);
                    break;
                case Envelope.PayloadOneofCase.Pong:
                    // TODO: track round-trip latency from Pong.Ts
                    break;
                case Envelope.PayloadOneofCase.Ping:
                    Send(new Envelope { Pong = new Twoup.V1.Pong { Ts = env.Ping.Ts } });
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
