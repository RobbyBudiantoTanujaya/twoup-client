using UnityEngine;

namespace TwoUp
{
    public enum GameMode
    {
        Versus,
        CoOp,
    }

    public enum GamePacing
    {
        Live,
        TurnBased,
    }

    [CreateAssetMenu(fileName = "GameCatalog", menuName = "2UP/Game Catalog")]
    public class GameCatalog : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            public string gameId;
            public string sceneName;
            public string displayName;
            public GameMode mode;
            public GamePacing pacing;
            public Sprite cardArt;
        }

        public Entry[] entries;

        public Entry Find(string gameId) => System.Array.Find(entries, e => e.gameId == gameId);
    }
}
