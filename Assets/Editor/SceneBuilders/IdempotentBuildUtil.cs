using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TwoUp.EditorTools
{
    /// <summary>
    /// Find-or-create helpers shared by UiKit and the per-scene builders. Using these instead of
    /// `new GameObject(...)` / `.AddComponent&lt;T&gt;()` directly is what keeps a re-run of
    /// Build()/BuildAll() from tearing down and recreating GameObjects that already exist on disk
    /// — local fileIDs/GUIDs stay stable across repeated builds instead of churning on every save.
    /// </summary>
    public static class IdempotentBuildUtil
    {
        /// <summary>
        /// Returns the child of `parent` named `name` if one already exists (searched including
        /// inactive children); otherwise creates a new GameObject with the given component types
        /// and parents it under `parent`. When `parent` is null, searches/creates among the active
        /// scene's root GameObjects instead of a child list.
        /// </summary>
        public static GameObject FindOrCreate(Transform parent, string name, params Type[] components)
        {
            var found = FindChild(parent, name);
            if (found != null)
                return found.gameObject;

            var go = components.Length > 0 ? new GameObject(name, components) : new GameObject(name);
            if (parent != null)
                go.transform.SetParent(parent, false);
            return go;
        }

        private static Transform FindChild(Transform parent, string name)
        {
            if (parent != null)
                return parent.Find(name);

            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name == name)
                    return root.transform;
            }
            return null;
        }

        /// <summary>Returns the existing component of type T on `go`, adding one only if absent.</summary>
        public static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }

        public static T GetOrAddComponent<T>(Component c) where T : Component => GetOrAddComponent<T>(c.gameObject);
    }
}
