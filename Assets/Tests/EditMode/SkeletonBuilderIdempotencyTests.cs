using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TwoUp.EditorTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TwoUp.Tests.EditMode
{
    /// <summary>
    /// Unity assigns fresh local fileIDs to every GameObject/component on each separate
    /// Editor save, even when SkeletonBuilder.BuildAll() authors an identical
    /// GameObject/component set run-to-run — so a raw `git diff`/`git status` after two
    /// BuildAll() runs is NOT a valid idempotency signal for these scenes (expected Unity
    /// non-determinism, not a builder bug). This test is the real idempotency contract: it
    /// extracts a canonical form per scene (hierarchy path + sorted component-type names per
    /// GameObject, ignoring fileID/GUID) before and after a fresh BuildAll() run and fails if
    /// that canonical form differs. Any future idempotency gate for BuildAll should assert via
    /// this comparator, not via a clean working tree.
    /// </summary>
    public class SkeletonBuilderIdempotencyTests
    {
        // Scenes SkeletonBuilder.BuildAll() actually (re)builds/saves. Keep in sync with
        // Assets/Editor/SkeletonBuilder.cs BuildAll().
        private static readonly string[] BuildAllScenePaths =
        {
            "Assets/Scenes/Boot.unity",
            "Assets/Scenes/ConnectFour.unity",
            "Assets/Scenes/Home.unity",
            "Assets/Scenes/Invite.unity",
            "Assets/Scenes/Queue.unity",
            "Assets/Scenes/Voting.unity",
            "Assets/Scenes/Result.unity",
            "Assets/Scenes/AsyncList.unity",
            "Assets/Scenes/Shop.unity",
        };

        [Test]
        public void BuildAll_IsSemanticallyIdempotent()
        {
            var originalBytes = BuildAllScenePaths.ToDictionary(p => p, File.ReadAllBytes);
            try
            {
                var before = BuildAllScenePaths.ToDictionary(p => p, ExtractCanonical);

                SkeletonBuilder.BuildAll();

                foreach (var path in BuildAllScenePaths)
                {
                    var after = ExtractCanonical(path);
                    CollectionAssert.AreEqual(before[path], after,
                        $"Scene '{path}' changed hierarchy/component semantics after a fresh " +
                        "BuildAll() rebuild (fileID/GUID churn is expected and ignored here; a " +
                        "real mismatch means BuildAll is not semantically idempotent).");
                }
            }
            finally
            {
                foreach (var path in BuildAllScenePaths)
                {
                    File.WriteAllBytes(path, originalBytes[path]);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>Ordered list of "hierarchyPath => Comp1,Comp2,..." entries; order matches
        /// scene authoring order (deterministic across runs), so element-wise comparison
        /// pinpoints exactly which GameObject/component set diverged.</summary>
        private static List<string> ExtractCanonical(string scenePath)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var scene = SceneManager.GetActiveScene();
            var entries = new List<string>();
            foreach (var root in scene.GetRootGameObjects())
                Walk(root.transform, root.name, entries);
            return entries;
        }

        private static void Walk(Transform t, string path, List<string> entries)
        {
            var componentNames = t.GetComponents<Component>()
                .Select(c => c == null ? "<MissingScript>" : c.GetType().Name)
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToList();
            entries.Add($"{path} => {string.Join(",", componentNames)}");

            for (int i = 0; i < t.childCount; i++)
            {
                var child = t.GetChild(i);
                Walk(child, path + "/" + child.name, entries);
            }
        }
    }
}
