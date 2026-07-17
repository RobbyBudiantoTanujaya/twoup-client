using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using TwoUp.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TwoUp.EditorTools
{
    /// <summary>
    /// Shared editor-authoring helpers for all per-scene builders (Boot/Home/ConnectFour and
    /// future screens). Pure toolkit — no scene-specific content lives here. Every primitive here
    /// is find-or-create (via IdempotentBuildUtil): calling Build() again on a scene that already
    /// has these objects updates their properties in place instead of destroying and recreating
    /// them, which keeps local fileIDs/GUIDs stable across repeated builds.
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

        /// <summary>Always creates a brand-new empty scene (not idempotent across runs — used by
        /// builders that haven't been retrofitted for in-place rebuilds yet).</summary>
        public static UnityEngine.SceneManagement.Scene NewScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EnsureMainCamera();
            return scene;
        }

        /// <summary>
        /// Idempotent scene entry point: opens "Assets/Scenes/{sceneName}.unity" in place if it
        /// already exists (preserving every GameObject/component's local fileID), or creates a
        /// fresh empty scene if this is the first build. Callers still finish with SaveScene(scene,
        /// sceneName) using the same name.
        /// </summary>
        public static UnityEngine.SceneManagement.Scene OpenOrCreateScene(string sceneName)
        {
            string path = $"Assets/Scenes/{sceneName}.unity";
            var scene = File.Exists(path)
                ? EditorSceneManager.OpenScene(path, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EnsureMainCamera();
            return scene;
        }

        private static void EnsureMainCamera()
        {
            var camGo = IdempotentBuildUtil.FindOrCreate(null, "Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = IdempotentBuildUtil.GetOrAddComponent<Camera>(camGo);
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = CameraBg;
            cam.cullingMask = 0; // UI is Screen Space Overlay; nothing in world space
            IdempotentBuildUtil.GetOrAddComponent<AudioListener>(camGo);
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
            var canvasGo = IdempotentBuildUtil.FindOrCreate(null, "UICanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.layer = LayerMask.NameToLayer("UI");
            var canvas = IdempotentBuildUtil.GetOrAddComponent<Canvas>(canvasGo);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = IdempotentBuildUtil.GetOrAddComponent<CanvasScaler>(canvasGo);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            // Match width so the fixed-width board/menus always fit on taller-than-16:9 phones.
            scaler.matchWidthOrHeight = 0f;
            IdempotentBuildUtil.GetOrAddComponent<GraphicRaycaster>(canvasGo);

            var eventSystemGo = IdempotentBuildUtil.FindOrCreate(null, "EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            IdempotentBuildUtil.GetOrAddComponent<EventSystem>(eventSystemGo);
            IdempotentBuildUtil.GetOrAddComponent<StandaloneInputModule>(eventSystemGo);

            var screen = CreatePanel(canvasGo.transform, screenName, ScreenBg);
            StretchFull(screen);
            var bg = screen.GetComponent<Image>();
            bg.raycastTarget = false;
            return screen;
        }

        public static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = IdempotentBuildUtil.FindOrCreate(parent, name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            return go;
        }

        /// <summary>Flat-color panel (sprite-less Image). raycastTarget stays ON to block clicks behind popups.</summary>
        public static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            var go = CreateUIObject(name, parent);
            var img = IdempotentBuildUtil.GetOrAddComponent<Image>(go);
            img.color = color;
            return go;
        }

        public static TMP_Text CreateText(Transform parent, string name, string text, float fontSize, Color color)
        {
            var go = CreateUIObject(name, parent);
            var tmp = IdempotentBuildUtil.GetOrAddComponent<TextMeshProUGUI>(go);
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
            var img = IdempotentBuildUtil.GetOrAddComponent<Image>(go);
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            img.type = Image.Type.Sliced;
            img.color = bg;
            var btn = IdempotentBuildUtil.GetOrAddComponent<Button>(go);
            btn.targetGraphic = img;
            ((RectTransform)go.transform).sizeDelta = size;
            var text = CreateText(go.transform, "Label", label, 44, Color.white);
            StretchFull(text.gameObject);
            return btn;
        }

        /// <summary>UGUI Toggle: Background (UISprite) + Checkmark (Checkmark.psd) + trailing TMP label.</summary>
        public static Toggle CreateToggle(Transform parent, string name, string label)
        {
            var go = CreateUIObject(name, parent);
            ((RectTransform)go.transform).sizeDelta = new Vector2(700, 90);
            var toggle = IdempotentBuildUtil.GetOrAddComponent<Toggle>(go);

            var bg = CreateUIObject("Background", go.transform);
            var bgImg = IdempotentBuildUtil.GetOrAddComponent<Image>(bg);
            bgImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            bgImg.type = Image.Type.Sliced;
            bgImg.color = Color.white;
            Place(bg, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30, 0), new Vector2(60, 60));

            var checkmark = CreateUIObject("Checkmark", bg.transform);
            var checkImg = IdempotentBuildUtil.GetOrAddComponent<Image>(checkmark);
            checkImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
            checkImg.color = ButtonBg;
            StretchFull(checkmark);

            toggle.targetGraphic = bgImg;
            toggle.graphic = checkImg;
            toggle.isOn = false;

            var labelText = CreateText(go.transform, "Label", label, 40, Color.white);
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            Place(labelText.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(100, 0), new Vector2(560, 60));

            return toggle;
        }

        public static TMP_InputField CreateInputField(Transform parent, string name, string placeholder, int charLimit)
        {
            var existing = parent.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                var resources = new TMP_DefaultControls.Resources
                {
                    inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
                };
                go = TMP_DefaultControls.CreateInputField(resources);
                go.name = name;
                go.layer = LayerMask.NameToLayer("UI");
                foreach (var t in go.GetComponentsInChildren<Transform>(true))
                    t.gameObject.layer = LayerMask.NameToLayer("UI");
                go.transform.SetParent(parent, false);
            }
            var input = IdempotentBuildUtil.GetOrAddComponent<TMP_InputField>(go);
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

        // ---------------------------------------------------------------- shared HUD widgets

        /// <summary>
        /// Toggle button (bottom-right corner) + inactive 3x2 emote grid panel + bottom-center
        /// incoming-emote text, wired onto a new "EmoteWheel" child of screenRoot.
        /// </summary>
        public static EmoteWheelController BuildEmoteWheel(Transform screenRoot)
        {
            var root = CreateUIObject("EmoteWheel", screenRoot);
            StretchFull(root);

            var toggle = CreateButton(root.transform, "EmoteToggleButton", "EMOTE", new Vector2(96, 96), ButtonBg);
            Place(toggle.gameObject, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24, 24), new Vector2(96, 96));

            var wheelPanel = CreatePanel(root.transform, "EmoteWheelPanel", PanelBg);
            Place(wheelPanel, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24, 140), new Vector2(420, 280));
            var grid = IdempotentBuildUtil.GetOrAddComponent<GridLayoutGroup>(wheelPanel);
            grid.cellSize = new Vector2(130, 130);
            grid.spacing = new Vector2(10, 10);
            grid.padding = new RectOffset(10, 10, 10, 10);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;

            var emoteLabels = new[] { "+1", "LOL", "WOW", "CRY", "FIRE", "GG" };
            var emoteButtons = new Button[emoteLabels.Length];
            for (int i = 0; i < emoteLabels.Length; i++)
                emoteButtons[i] = CreateButton(wheelPanel.transform, $"EmoteButton_{i}", emoteLabels[i], new Vector2(130, 130), ButtonMuted);
            wheelPanel.SetActive(false);

            var incomingText = CreateText(root.transform, "IncomingEmoteText", "", 36, Color.white);
            Place(incomingText.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 24), new Vector2(760, 70));

            var controller = IdempotentBuildUtil.GetOrAddComponent<EmoteWheelController>(root);
            SetRef(controller, "toggleButton", toggle);
            SetRef(controller, "wheelPanel", wheelPanel);
            SetArray(controller, "emoteButtons", emoteButtons);
            SetRef(controller, "incomingEmoteText", incomingText);
            return controller;
        }

        /// <summary>Small connection status badge (top-left corner), wired onto a new "ConnectionBadge" child of screenRoot.</summary>
        public static ConnectionIndicator BuildConnectionBadge(Transform screenRoot)
        {
            var badge = CreatePanel(screenRoot, "ConnectionBadge", PanelBg);
            Place(badge, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24, -24), new Vector2(120, 48));

            var label = CreateText(badge.transform, "Label", "", 28, Color.white);
            StretchFull(label.gameObject);

            var controller = IdempotentBuildUtil.GetOrAddComponent<ConnectionIndicator>(badge);
            SetRef(controller, "label", label);
            return controller;
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
