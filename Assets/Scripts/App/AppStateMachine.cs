using TwoUp.Net;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TwoUp
{
    /// <summary>
    /// Thin scene-flow driver: Boot → Home → InGame → Result/Rematch → Home.
    /// Nothing else loads scenes. Result is an in-scene sub-state of the game scene
    /// (game-over panel), so it doesn't trigger a scene load.
    /// </summary>
    public class AppStateMachine : MonoBehaviour
    {
        public enum State
        {
            Boot,
            Home,
            Invite,
            Queue,
            Voting,
            InGame,
            Result,
            Profile,
            Shop,
            Settings,
            AsyncList,
        }

        public static AppStateMachine Instance { get; private set; }

        public State Current { get; private set; } = State.Boot;

        [SerializeField] private GameCatalog catalog;

        public GameCatalog Catalog => catalog;

        private const string BootScene = "Boot";
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

        public void ToHome()
        {
            Current = State.Home;
            SceneManager.LoadScene("Home");
        }

        public void ToInvite()
        {
            Current = State.Invite;
            SceneManager.LoadScene("Invite");
        }

        public void ToQueue()
        {
            Current = State.Queue;
            SceneManager.LoadScene("Queue");
        }

        public void ToVoting()
        {
            Current = State.Voting;
            SceneManager.LoadScene("Voting");
        }

        public void ToResult()
        {
            Current = State.Result;
            SceneManager.LoadScene("Result");
        }

        public void ToProfile()
        {
            Current = State.Profile;
            SceneManager.LoadScene("Profile");
        }

        public void ToShop()
        {
            Current = State.Shop;
            SceneManager.LoadScene("Shop");
        }

        public void ToSettings()
        {
            Current = State.Settings;
            SceneManager.LoadScene("Settings");
        }

        public void ToAsyncList()
        {
            Current = State.AsyncList;
            SceneManager.LoadScene("AsyncList");
        }

        public void ToGame()
        {
            Current = State.InGame;
            SceneManager.LoadScene(GameScene);
        }

        /// <summary>Routes to the scene registered for gameId in the wired GameCatalog; logs and stays put if unknown.</summary>
        public void ToGame(string gameId)
        {
            var entry = catalog != null ? catalog.Find(gameId) : null;
            if (entry == null)
            {
                Debug.LogError($"AppStateMachine.ToGame: unknown gameId '{gameId}'");
                return;
            }
            Current = State.InGame;
            SceneManager.LoadScene(entry.sceneName);
        }

        public void SetResult() => Current = State.Result;

        /// <summary>Rematch accepted → back from Result to InGame without a scene load.</summary>
        public void SetInGame() => Current = State.InGame;
    }
}
