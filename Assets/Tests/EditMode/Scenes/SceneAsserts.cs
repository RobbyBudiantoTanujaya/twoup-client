using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TwoUp.Tests.EditMode.Scenes
{
    /// <summary>Shared assertion helpers for opening authored scenes and checking their hierarchy/wiring.</summary>
    public static class SceneAsserts
    {
        public static void OpenScene(string path)
        {
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        }

        public static GameObject AssertObject(string hierarchyPath)
        {
            var go = GameObject.Find(hierarchyPath);
            Assert.IsNotNull(go, $"Expected GameObject at hierarchy path '{hierarchyPath}'");
            return go;
        }

        /// <summary>
        /// Like AssertObject, but walks the hierarchy via Transform.Find so it also finds
        /// inactive GameObjects along the path (GameObject.Find skips inactive objects entirely).
        /// </summary>
        public static GameObject AssertObjectIncludingInactive(string hierarchyPath)
        {
            string[] segments = hierarchyPath.Split('/');
            var root = GameObject.Find(segments[0]);
            Assert.IsNotNull(root, $"Expected root GameObject '{segments[0]}' (path '{hierarchyPath}')");

            var current = root.transform;
            for (int i = 1; i < segments.Length; i++)
            {
                current = current.Find(segments[i]);
                Assert.IsNotNull(current, $"Expected GameObject at hierarchy path '{hierarchyPath}' (missing segment '{segments[i]}')");
            }
            return current.gameObject;
        }

        public static void AssertRefNotNull(Component c, string fieldName)
        {
            Assert.IsNotNull(c, $"Component is null when checking field '{fieldName}'");
            var so = new SerializedObject(c);
            var prop = so.FindProperty(fieldName);
            Assert.IsNotNull(prop, $"No serialized field '{fieldName}' on {c.GetType().Name}");
            Assert.IsNotNull(prop.objectReferenceValue, $"Field '{fieldName}' on {c.GetType().Name} is not wired");
        }
    }
}
