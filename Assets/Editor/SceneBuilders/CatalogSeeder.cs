using TwoUp;
using UnityEditor;
using UnityEngine;

namespace TwoUp.EditorTools
{
    /// <summary>
    /// Seeds/overwrites Assets/Config/GameCatalog.asset with the production MVP entry list.
    /// Idempotent — rerunning replaces the entries with this exact list, so edit here rather
    /// than hand-tweaking the asset.
    /// </summary>
    public static class CatalogSeeder
    {
        private const string CatalogPath = "Assets/Config/GameCatalog.asset";

        [MenuItem("2UP/Create Game Catalog")]
        public static void CreateOrUpdate()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(CatalogPath);
            if (catalog == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/Config"))
                    AssetDatabase.CreateFolder("Assets", "Config");
                catalog = ScriptableObject.CreateInstance<GameCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.entries = new[]
            {
                new GameCatalog.Entry { gameId = "connect_four", sceneName = "ConnectFour", displayName = "Connect Four", mode = GameMode.Versus, pacing = GamePacing.TurnBased },
                new GameCatalog.Entry { gameId = "reflex_duel", sceneName = "ReflexDuel", displayName = "Reflex Duel", mode = GameMode.Versus, pacing = GamePacing.Live },
                new GameCatalog.Entry { gameId = "air_hockey", sceneName = "AirHockey", displayName = "Air Hockey", mode = GameMode.Versus, pacing = GamePacing.Live },
                new GameCatalog.Entry { gameId = "wall_defense", sceneName = "WallDefense", displayName = "Wall Defense", mode = GameMode.CoOp, pacing = GamePacing.Live },
                new GameCatalog.Entry { gameId = "keepup_duo", sceneName = "KeepUpDuo", displayName = "Keep-Up Duo", mode = GameMode.CoOp, pacing = GamePacing.Live },
                new GameCatalog.Entry { gameId = "battleship", sceneName = "Battleship", displayName = "Battleship", mode = GameMode.Versus, pacing = GamePacing.TurnBased },
            };

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[CatalogSeeder] Wrote {catalog.entries.Length} entries to {CatalogPath}");
        }
    }
}
