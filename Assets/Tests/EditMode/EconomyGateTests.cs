using NUnit.Framework;
using TwoUp.Tests.EditMode.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TwoUp.Tests.EditMode
{
    public class EconomyGateTests
    {
        private static readonly string[] RetrofitScenePaths =
        {
            "Assets/Scenes/Home.unity",
            "Assets/Scenes/Voting.unity",
            "Assets/Scenes/Result.unity",
            "Assets/Scenes/Shop.unity",
        };

        [Test]
        public void AllRetrofitScenes_NoMissingScripts()
        {
            foreach (var path in RetrofitScenePaths)
            {
                SceneAsserts.OpenScene(path);
                var scene = SceneManager.GetActiveScene();

                var allMonoBehaviours = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
                for (int i = 0; i < allMonoBehaviours.Length; i++)
                {
                    Assert.IsNotNull(allMonoBehaviours[i],
                        $"Scene '{path}': found a missing-script MonoBehaviour entry (null) via Resources.FindObjectsOfTypeAll<MonoBehaviour>() at index {i}.");
                }

                foreach (var root in scene.GetRootGameObjects())
                {
                    AssertNoMissingScripts(root, path, root.name);
                }
            }
        }

        private static void AssertNoMissingScripts(GameObject go, string scenePath, string hierarchyPath)
        {
            var components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Assert.IsNotNull(components[i],
                    $"Scene '{scenePath}': GameObject '{hierarchyPath}' has a missing script (null Component at index {i} of GetComponents<Component>()).");
            }

            var t = go.transform;
            for (int i = 0; i < t.childCount; i++)
            {
                var child = t.GetChild(i).gameObject;
                AssertNoMissingScripts(child, scenePath, hierarchyPath + "/" + child.name);
            }
        }
    }
}
