using TwoUp.Net;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TwoUp
{
    /// <summary>
    /// Thin scene-flow driver: Boot → Lobby → InGame → Result/Rematch → Lobby.
    /// Nothing else loads scenes. Result is an in-scene sub-state of the game scene
    /// (game-over panel), so it doesn't trigger a scene load.
    /// </summary>
    public class AppStateMachine : MonoBehaviour
    {
        public enum State
        {
            Boot,
            Lobby,
            InGame,
            Result,
        }

        public static AppStateMachine Instance { get; private set; }

        public State Current { get; private set; } = State.Boot;

        private const string BootScene = "Boot";
        private const string LobbyScene = "Lobby";
        private const string GameScene = "ConnectFour";

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

        private void Start()
        {
            if (NetworkClient.Instance != null)
                NetworkClient.Instance.Disconnected += OnDisconnected;
        }

        private void OnDisconnected()
        {
            // BootController owns retry UI while in Boot; from anywhere else, fall back to Boot.
            if (Current == State.Boot)
                return;
            MatchContext.Clear();
            ToBoot();
        }

        public void ToBoot()
        {
            Current = State.Boot;
            SceneManager.LoadScene(BootScene);
        }

        public void ToLobby()
        {
            Current = State.Lobby;
            SceneManager.LoadScene(LobbyScene);
        }

        public void ToGame()
        {
            Current = State.InGame;
            SceneManager.LoadScene(GameScene);
        }

        public void SetResult() => Current = State.Result;

        /// <summary>Rematch accepted → back from Result to InGame without a scene load.</summary>
        public void SetInGame() => Current = State.InGame;
    }
}
