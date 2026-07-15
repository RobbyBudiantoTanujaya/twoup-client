using System.IO;
using TMPro;
using TwoUp;
using TwoUp.Net;
using TwoUp.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TwoUp.EditorTools
{
    /// <summary>
    /// Authors all walking-skeleton content: the three scenes (Boot, Lobby, ConnectFour),
    /// the ServerConfig asset, and build/player settings. Idempotent — rebuilding
    /// overwrites the scenes, so edit this builder instead of hand-tweaking scenes.
    /// Batchmode entry points: ImportTmpEssentials (pass 1), BuildAll (pass 2), BuildApk.
    /// </summary>
    public static class SkeletonBuilder
    {
        private const string ScenesDir = "Assets/Scenes";
        private const string ServerConfigPath = "Assets/Config/ServerConfig.asset";
        private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        private static readonly Color ScreenBg = new Color(0.10f, 0.11f, 0.16f);
        private static readonly Color CameraBg = new Color(0.07f, 0.08f, 0.12f);
        private static readonly Color PanelBg = new Color(0.13f, 0.15f, 0.22f);
        private static readonly Color ButtonBg = new Color(0.22f, 0.42f, 0.85f);
        private static readonly Color ButtonMuted = new Color(0.35f, 0.38f, 0.48f);
        private static readonly Color BoardBg = new Color(0.09f, 0.11f, 0.20f);
        private static readonly Color CellEmpty = new Color(0.16f, 0.19f, 0.28f);
        private static readonly Color BotBadgeBg = new Color(0.95f, 0.77f, 0.06f);

        [MenuItem("2UP/Build All")]
        public static void BuildAll()
        {
            ImportTmpEssentials();
            CreateServerConfig();
            BuildBootScene();
            BuildLobbyScene();
            BuildConnectFourScene();
            ConfigureBuildSettings();
            ConfigurePlayerSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[SkeletonBuilder] BuildAll complete");
        }

        [MenuItem("2UP/Import TMP Essentials")]
        public static void ImportTmpEssentials()
        {
            if (File.Exists(TmpSettingsPath))
            {
                Debug.Log("[SkeletonBuilder] TMP essentials already imported");
                return;
            }
            string path = FindTmpEssentialsPackage();
            if (path == null)
            {
                Debug.LogError("[SkeletonBuilder] TMP Essential Resources package not found");
                return;
            }
            AssetDatabase.ImportPackage(path, false);
            Debug.Log($"[SkeletonBuilder] Importing TMP essentials from {path} (async)");
        }

        /// <summary>
        /// Batchmode entry point. ImportPackage is asynchronous even with interactive=false,
        /// so run WITHOUT -quit; this exits the editor from the completion callback.
        /// </summary>
        public static void ImportTmpEssentialsBatch()
        {
            if (File.Exists(TmpSettingsPath))
            {
                Debug.Log("[SkeletonBuilder] TMP essentials already imported");
                EditorApplication.Exit(0);
                return;
            }
            string path = FindTmpEssentialsPackage();
            if (path == null)
            {
                Debug.LogError("[SkeletonBuilder] TMP Essential Resources package not found");
                EditorApplication.Exit(1);
                return;
            }
            AssetDatabase.importPackageCompleted += name =>
            {
                Debug.Log($"[SkeletonBuilder] TMP essentials import completed: {name}");
                AssetDatabase.SaveAssets();
                EditorApplication.Exit(0);
            };
            AssetDatabase.importPackageFailed += (name, error) =>
            {
                Debug.LogError($"[SkeletonBuilder] TMP essentials import failed: {error}");
                EditorApplication.Exit(1);
            };
            AssetDatabase.ImportPackage(path, false);
        }

        private static string FindTmpEssentialsPackage()
        {
            string[] candidates =
            {
                "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage",
                "Packages/com.unity.textmeshpro/Package Resources/TMP Essential Resources.unitypackage",
            };
            foreach (string path in candidates)
            {
                if (File.Exists(Path.GetFullPath(path)))
                    return path;
            }
            return null;
        }

        public static void CreateServerConfig()
        {
            if (AssetDatabase.LoadAssetAtPath<ServerConfig>(ServerConfigPath) != null)
                return;
            if (!AssetDatabase.IsValidFolder("Assets/Config"))
                AssetDatabase.CreateFolder("Assets", "Config");
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<ServerConfig>(), ServerConfigPath);
            Debug.Log($"[SkeletonBuilder] Created {ServerConfigPath}");
        }

        // ---------------------------------------------------------------- scenes

        public static void BuildBootScene()
        {
            var scene = NewScene();

            // Persistent App object: exists only here; DontDestroyOnLoad at runtime.
            var app = new GameObject("App");
            var net = app.AddComponent<NetworkClient>();
            app.AddComponent<AppStateMachine>();
            var config = AssetDatabase.LoadAssetAtPath<ServerConfig>(ServerConfigPath);
            SetRef(net, "serverConfig", config);

            var screen = CreateCanvasWithScreen("Screen_Boot");

            var title = CreateText(screen.transform, "Title", "2UP", 120, Color.white);
            Place(title.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -220), new Vector2(600, 160));
            title.fontStyle = FontStyles.Bold;

            var status = CreateText(screen.transform, "StatusText", "Connecting...", 44, new Color(0.8f, 0.84f, 0.92f));
            Place(status.gameObject, Center, Center, new Vector2(0, 40), new Vector2(900, 120));

            var retry = CreateButton(screen.transform, "RetryButton", "Retry", new Vector2(500, 120), ButtonBg);
            Place(retry.gameObject, Center, Center, new Vector2(0, -140), new Vector2(500, 120));
            retry.gameObject.SetActive(false);

            var controller = screen.AddComponent<BootController>();
            SetRef(controller, "statusText", status);
            SetRef(controller, "retryButton", retry);

            SaveScene(scene, "Boot");
        }

        public static void BuildLobbyScene()
        {
            var scene = NewScene();
            var screen = CreateCanvasWithScreen("Screen_Lobby");

            var title = CreateText(screen.transform, "Title", "2UP", 110, Color.white);
            Place(title.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -180), new Vector2(600, 150));
            title.fontStyle = FontStyles.Bold;

            var quick = CreateButton(screen.transform, "QuickMatchButton", "Quick Match", new Vector2(700, 130), ButtonBg);
            Place(quick.gameObject, Center, Center, new Vector2(0, 430), new Vector2(700, 130));

            var create = CreateButton(screen.transform, "CreateRoomButton", "Create Room", new Vector2(700, 130), ButtonBg);
            Place(create.gameObject, Center, Center, new Vector2(0, 270), new Vector2(700, 130));

            var input = CreateRoomCodeInput(screen.transform);
            Place(input.gameObject, Center, Center, new Vector2(-160, 110), new Vector2(380, 110));

            var join = CreateButton(screen.transform, "JoinRoomButton", "Join", new Vector2(280, 110), ButtonBg);
            Place(join.gameObject, Center, Center, new Vector2(190, 110), new Vector2(280, 110));

            var status = CreateText(screen.transform, "StatusText", "", 40, new Color(0.8f, 0.84f, 0.92f));
            Place(status.gameObject, Center, Center, new Vector2(0, -120), new Vector2(920, 220));

            var cancel = CreateButton(screen.transform, "CancelQueueButton", "Cancel", new Vector2(500, 110), ButtonMuted);
            Place(cancel.gameObject, Center, Center, new Vector2(0, -320), new Vector2(500, 110));
            cancel.gameObject.SetActive(false);

            // Matched-with panel (inactive by default)
            var panel = CreatePanel(screen.transform, "Panel_Matched", PanelBg);
            Place(panel, Center, Center, new Vector2(0, 60), new Vector2(860, 560));

            var matchedTitle = CreateText(panel.transform, "MatchedTitle", "Match found!", 60, Color.white);
            Place(matchedTitle.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -70), new Vector2(700, 90));

            var opponentName = CreateText(panel.transform, "OpponentNameText", "???", 52, Color.white);
            Place(opponentName.gameObject, Center, Center, new Vector2(0, 30), new Vector2(700, 80));

            var botBadge = CreatePanel(panel.transform, "BotBadge", BotBadgeBg);
            Place(botBadge, Center, Center, new Vector2(0, -70), new Vector2(150, 64));
            var botLabel = CreateText(botBadge.transform, "Label", "BOT", 38, Color.black);
            StretchFull(botLabel.gameObject);
            botLabel.fontStyle = FontStyles.Bold;
            botBadge.SetActive(false);

            var starting = CreateText(panel.transform, "StartingText", "Starting game...", 36, new Color(0.8f, 0.84f, 0.92f));
            Place(starting.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 60), new Vector2(700, 60));

            panel.SetActive(false);

            var controller = screen.AddComponent<LobbyController>();
            SetRef(controller, "quickMatchButton", quick);
            SetRef(controller, "createRoomButton", create);
            SetRef(controller, "joinRoomButton", join);
            SetRef(controller, "roomCodeInput", input);
            SetRef(controller, "cancelQueueButton", cancel);
            SetRef(controller, "statusText", status);
            SetRef(controller, "matchedPanel", panel);
            SetRef(controller, "opponentNameText", opponentName);
            SetRef(controller, "botBadge", botBadge);

            SaveScene(scene, "Lobby");
        }

        public static void BuildConnectFourScene()
        {
            const int columns = ConnectFourController.Columns;
            const int rows = ConnectFourController.Rows;
            const float cell = 130f, spacing = 6f, pad = 10f;
            var boardSize = new Vector2(
                columns * cell + (columns - 1) * spacing + 2 * pad,
                rows * cell + (rows - 1) * spacing + 2 * pad);

            var scene = NewScene();
            var screen = CreateCanvasWithScreen("Screen_Game");

            var turn = CreateText(screen.transform, "TurnText", "Waiting for game state...", 60, Color.white);
            Place(turn.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -130), new Vector2(950, 90));

            var youAre = CreateText(screen.transform, "YouAreText", "", 42, Color.white);
            Place(youAre.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -230), new Vector2(950, 60));

            // Board: 7x6 grid of disc Images, display order top-left → bottom-right.
            var board = CreatePanel(screen.transform, "Board", BoardBg);
            Place(board, Center, Center, new Vector2(0, 40), boardSize);
            var grid = board.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(cell, cell);
            grid.spacing = new Vector2(spacing, spacing);
            grid.padding = new RectOffset((int)pad, (int)pad, (int)pad, (int)pad);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;

            var knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            var cellImages = new Image[rows * columns];
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    var go = CreateUIObject($"Cell_r{r}c{c}", board.transform);
                    var img = go.AddComponent<Image>();
                    img.sprite = knob;
                    img.color = CellEmpty;
                    img.raycastTarget = false;
                    cellImages[r * columns + c] = img;
                }
            }

            // Invisible full-height tap zones over each column (sibling of Board, same rect).
            var columnsRoot = CreateUIObject("Columns", screen.transform);
            Place(columnsRoot, Center, Center, new Vector2(0, 40), boardSize);
            var hlg = columnsRoot.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset((int)pad, (int)pad, (int)pad, (int)pad);
            hlg.spacing = spacing;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            var columnButtons = new Button[columns];
            for (int c = 0; c < columns; c++)
            {
                var go = CreateUIObject($"ColumnButton_{c}", columnsRoot.transform);
                var img = go.AddComponent<Image>();
                img.color = new Color(1f, 1f, 1f, 0f); // invisible but raycastable tap zone
                img.raycastTarget = true;
                var btn = go.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.targetGraphic = img;
                columnButtons[c] = btn;
            }

            // Game-over / rematch panel (inactive by default; dim layer blocks board input).
            var gameOver = CreateUIObject("Panel_GameOver", screen.transform);
            StretchFull(gameOver);
            var dim = gameOver.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.72f);
            dim.raycastTarget = true;

            var popup = CreatePanel(gameOver.transform, "Popup", PanelBg);
            Place(popup, Center, Center, Vector2.zero, new Vector2(860, 640));

            var result = CreateText(popup.transform, "ResultText", "", 72, Color.white);
            Place(result.gameObject, Center, Center, new Vector2(0, 190), new Vector2(700, 100));

            var rematchStatus = CreateText(popup.transform, "RematchStatusText", "", 38, new Color(0.8f, 0.84f, 0.92f));
            Place(rematchStatus.gameObject, Center, Center, new Vector2(0, 100), new Vector2(700, 60));

            var rematch = CreateButton(popup.transform, "RematchButton", "Rematch", new Vector2(560, 120), ButtonBg);
            Place(rematch.gameObject, Center, Center, new Vector2(0, -30), new Vector2(560, 120));

            var back = CreateButton(popup.transform, "BackButton", "Back to Lobby", new Vector2(560, 120), ButtonMuted);
            Place(back.gameObject, Center, Center, new Vector2(0, -180), new Vector2(560, 120));

            gameOver.SetActive(false);

            var controller = screen.AddComponent<ConnectFourController>();
            SetRef(controller, "turnText", turn);
            SetRef(controller, "youAreText", youAre);
            SetArray(controller, "cells", cellImages);
            SetArray(controller, "columnButtons", columnButtons);
            SetRef(controller, "gameOverPanel", gameOver);
            SetRef(controller, "resultText", result);
            SetRef(controller, "rematchStatusText", rematchStatus);
            SetRef(controller, "rematchButton", rematch);
            SetRef(controller, "backButton", back);

            SaveScene(scene, "ConnectFour");
        }

        // ---------------------------------------------------------------- settings

        public static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene($"{ScenesDir}/Boot.unity", true),
                new EditorBuildSettingsScene($"{ScenesDir}/Lobby.unity", true),
                new EditorBuildSettingsScene($"{ScenesDir}/ConnectFour.unity", true),
            };
        }

        public static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "TwoUp";
            PlayerSettings.productName = "2UP";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.evermore.twoup");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.Android.forceInternetPermission = true;
            // Dev convenience for ws:// endpoints. TODO: tighten once wss:// is available.
            PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
        }

        [MenuItem("2UP/Build Android APK")]
        public static void BuildApk()
        {
            Directory.CreateDirectory("Builds");
            var options = new BuildPlayerOptions
            {
                scenes = new[]
                {
                    $"{ScenesDir}/Boot.unity",
                    $"{ScenesDir}/Lobby.unity",
                    $"{ScenesDir}/ConnectFour.unity",
                },
                target = BuildTarget.Android,
                locationPathName = "Builds/twoup-client.apk",
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"[SkeletonBuilder] APK build: {report.summary.result}, errors={report.summary.totalErrors}");
            if (Application.isBatchMode)
                EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        // ---------------------------------------------------------------- helpers

        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        private static UnityEngine.SceneManagement.Scene NewScene()
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

        private static void SaveScene(UnityEngine.SceneManagement.Scene scene, string name)
        {
            if (!AssetDatabase.IsValidFolder(ScenesDir))
                AssetDatabase.CreateFolder("Assets", "Scenes");
            string path = $"{ScenesDir}/{name}.unity";
            if (!EditorSceneManager.SaveScene(scene, path))
                Debug.LogError($"[SkeletonBuilder] Failed to save {path}");
            else
                Debug.Log($"[SkeletonBuilder] Saved {path}");
        }

        /// <summary>Canvas (overlay, 1080x1920 reference) + EventSystem + full-screen Screen_* root panel.</summary>
        private static GameObject CreateCanvasWithScreen(string screenName)
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

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return go;
        }

        /// <summary>Flat-color panel (sprite-less Image). raycastTarget stays ON to block clicks behind popups.</summary>
        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            var go = CreateUIObject(name, parent);
            var img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        private static TMP_Text CreateText(Transform parent, string name, string text, float fontSize, Color color)
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

        private static Button CreateButton(Transform parent, string name, string label, Vector2 size, Color bg)
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

        private static TMP_InputField CreateRoomCodeInput(Transform parent)
        {
            var resources = new TMP_DefaultControls.Resources
            {
                inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
            };
            var go = TMP_DefaultControls.CreateInputField(resources);
            go.name = "RoomCodeInput";
            go.layer = LayerMask.NameToLayer("UI");
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            var input = go.GetComponent<TMP_InputField>();
            input.characterLimit = 8;
            input.textComponent.fontSize = 44;
            var placeholder = (TMP_Text)input.placeholder;
            placeholder.text = "Room code";
            placeholder.fontSize = 44;
            return input;
        }

        private static void Place(GameObject go, Vector2 anchor, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }

        private static void Place(Component c, Vector2 anchor, Vector2 pivot, Vector2 anchoredPos, Vector2 size) =>
            Place(c.gameObject, anchor, pivot, anchoredPos, size);

        private static void StretchFull(GameObject go)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetRef(Component target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"[SkeletonBuilder] No serialized field '{fieldName}' on {target.GetType().Name}");
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

        private static void SetArray<T>(Component target, string fieldName, T[] values) where T : Object
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"[SkeletonBuilder] No serialized field '{fieldName}' on {target.GetType().Name}");
                return;
            }
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
