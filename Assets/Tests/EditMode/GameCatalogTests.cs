using NUnit.Framework;
using TwoUp;
using UnityEditor;

namespace TwoUp.Tests.EditMode
{
    public class GameCatalogTests
    {
        private const string CatalogPath = "Assets/Config/GameCatalog.asset";

        private static GameCatalog LoadCatalog() => AssetDatabase.LoadAssetAtPath<GameCatalog>(CatalogPath);

        [Test]
        public void Find_ReturnsEntryByExactId()
        {
            var catalog = LoadCatalog();

            Assert.AreEqual("AirHockey", catalog.Find("air_hockey").sceneName);
        }

        [Test]
        public void Find_UnknownIdReturnsNull()
        {
            var catalog = LoadCatalog();

            Assert.IsNull(catalog.Find("no_such_game"));
        }

        [Test]
        public void Entries_HasExactlySixInProductionOrder()
        {
            var catalog = LoadCatalog();

            var expectedIds = new[]
            {
                "connect_four",
                "reflex_duel",
                "air_hockey",
                "wall_defense",
                "keepup_duo",
                "battleship",
            };

            Assert.AreEqual(expectedIds.Length, catalog.entries.Length);
            for (int i = 0; i < expectedIds.Length; i++)
                Assert.AreEqual(expectedIds[i], catalog.entries[i].gameId);
        }
    }
}
