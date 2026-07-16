using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TwoUp.EditorTools
{
    /// <summary>
    /// Shared editor-authoring helpers for all per-scene builders (Boot/Lobby/ConnectFour and
    /// future screens). Pure toolkit — no scene-specific content lives here.
    /// </summary>
    public static class UiKit
    {
        public static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        public static readonly Color ScreenBg = new Color(0.10f, 0.11f, 0.16f);
        public static readonly Color CameraBg = new Color(0.07f, 0.08f, 0.12f);
        public static readonly Color PanelBg = new Color(0.13f, 0.15f, 0.22f);
        public static readonly Color ButtonBg = new Color(0.22f, 0.42f, 0.85f);
        public static readonly Color ButtonMuted = new Color(0.35f, 0.38f, 0.48f);
        public static readonly Color BoardBg = new Color(0.09f, 0.11f, 0.20f);
        public static readonly Color CellEmpty = new Color(0.16f, 0.19f, 0.28f);
        public static readonly Color BotBadgeBg = new Color(0.95f, 0.77f, 0.06f);
        public static readonly Color ListRowBg = new Color(0.13f, 0.15f, 0.22f);

        // ---------------------------------------------------------------- scene lifecycle

        public static UnityEngine.SceneManagement.Scene NewScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = CameraBg;
            cam.cullingMask = 0; // UI is Screen Space Overlay; nothing in world space
            camGo.AddComponent<AudioListener>();
            return scene;
        }

        public static void SaveScene(UnityEngine.SceneManagement.Scene scene, string name)
        {
            const string scenesDir = "Assets/Scenes";
            if (!AssetDatabase.IsValidFolder(scenesDir))
                AssetDatabase.CreateFolder("Assets", "Scenes");
            string path = $"{scenesDir}/{name}.unity";
            if (!EditorSceneManager.SaveScene(scene, path))
                Debug.LogError($"[UiKit] Failed to save {path}");
            else
                Debug.Log($"[UiKit] Saved {path}");
        }

        /// <summary>Append a scene to EditorBuildSettings.scenes, enabled, idempotently.</summary>
        public static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            int idx = scenes.FindIndex(s => s.path == scenePath);
            if (idx >= 0)
            {
                if (!scenes[idx].enabled)
                    scenes[idx] = new EditorBuildSettingsScene(scenePath, true);
            }
            else
            {
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ---------------------------------------------------------------- canvas & primitives

        /// <summary>Canvas (overlay, 1080x1920 reference) + EventSystem + full-screen Screen_* root panel.</summary>
        public static GameObject CreateCanvasWithScreen(string screenName)
        {
            var canvasGo = new GameObject("UICanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.layer = LayerMask.NameToLayer("UI");
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            // Match width so the fixed-width board/menus always fit on taller-than-16:9 phones.
            scaler.matchWidthOrHeight = 0f;

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var screen = CreatePanel(canvasGo.transform, screenName, ScreenBg);
            StretchFull(screen);
            var bg = screen.GetComponent<Image>();
            bg.raycastTarget = false;
            return screen;
        }

        public static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return go;
        }

        /// <summary>Flat-color panel (sprite-less Image). raycastTarget stays ON to block clicks behind popups.</summary>
        public static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            var go = CreateUIObject(name, parent);
            var img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        public static TMP_Text CreateText(Transform parent, string name, string text, float fontSize, Color color)
        {
            var go = CreateUIObject(name, parent);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            return tmp;
        }

        public static Button CreateButton(Transform parent, string name, string label, Vector2 size, Color bg)
        {
            var go = CreateUIObject(name, parent);
            var img = go.AddComponent<Image>();
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            img.type = Image.Type.Sliced;
            img.color = bg;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            ((RectTransform)go.transform).sizeDelta = size;
            var text = CreateText(go.transform, "Label", label, 44, Color.white);
            StretchFull(text.gameObject);
            return btn;
        }

        public static TMP_InputField CreateInputField(Transform parent, string name, string placeholder, int charLimit)
        {
            var resources = new TMP_DefaultControls.Resources
            {
                inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
            };
            var go = TMP_DefaultControls.CreateInputField(resources);
            go.name = name;
            go.layer = LayerMask.NameToLayer("UI");
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            var input = go.GetComponent<TMP_InputField>();
            input.characterLimit = charLimit;
            input.textComponent.fontSize = 44;
            var placeholderText = (TMP_Text)input.placeholder;
            placeholderText.text = placeholder;
            placeholderText.fontSize = 44;
            return input;
        }

        /// <summary>
        /// Row template for a runtime-cloned list (panel + TMP label left + TMP label right), inactive.
        /// MUST be created as a sibling of the list container (parent = container's parent), never as
        /// a child of the container that gets Destroy-cleared on populate — cloning your own template
        /// out from under itself is the classic populate-from-template bug.
        /// </summary>
        public static GameObject CreateListRowTemplate(Transform parent, string name)
        {
            var row = CreatePanel(parent, name, ListRowBg);
            ((RectTransform)row.transform).sizeDelta = new Vector2(1000, 120);

            var left = CreateText(row.transform, "LeftLabel", "", 36, Color.white);
            left.alignment = TextAlignmentOptions.MidlineLeft;
            Place(left.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24, 0), new Vector2(500, 80));

            var right = CreateText(row.transform, "RightLabel", "", 36, Color.white);
            right.alignment = TextAlignmentOptions.MidlineRight;
            Place(right.gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24, 0), new Vector2(400, 80));

            row.SetActive(false);
            return row;
        }

        // ---------------------------------------------------------------- layout

        public static void Place(GameObject go, Vector2 anchor, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }

        public static void Place(Component c, Vector2 anchor, Vector2 pivot, Vector2 anchoredPos, Vector2 size) =>
            Place(c.gameObject, anchor, pivot, anchoredPos, size);

        public static void StretchFull(GameObject go)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // ---------------------------------------------------------------- serialized wiring

        public static void SetRef(Component target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"[UiKit] No serialized field '{fieldName}' on {target.GetType().Name}");
                return;
            }
            prop.objectReferenceValue = value is Component comp && prop.propertyType == SerializedPropertyType.ObjectReference
                ? ResolveRefValue(prop, comp)
                : value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Object ResolveRefValue(SerializedProperty prop, Component comp)
        {
            // GameObject fields wired from a Component convenience overload
            return prop.type.Contains("GameObject") ? (Object)comp.gameObject : comp;
        }

        public static void SetArray<T>(Component target, string fieldName, T[] values) where T : Object
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"[UiKit] No serialized field '{fieldName}' on {target.GetType().Name}");
                return;
            }
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
