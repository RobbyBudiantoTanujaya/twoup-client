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
