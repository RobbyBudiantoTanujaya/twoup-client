using System.IO;
using TwoUp.Net;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TwoUp.EditorTools
{
    /// <summary>
    /// Top-level batchmode entry points: TMP essentials import, ServerConfig asset, invoking
    /// every per-scene builder, player settings, and the APK build. Per-scene content lives in
    /// Assets/Editor/SceneBuilders/*SceneBuilder.cs (built on the UiKit helper toolkit).
    /// </summary>
    public static class SkeletonBuilder
    {
        private const string ServerConfigPath = "Assets/Config/ServerConfig.asset";
        private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        [MenuItem("2UP/Build All")]
        public static void BuildAll()
        {
            ImportTmpEssentials();
            CreateServerConfig();
            BootSceneBuilder.BuildBootScene();
            ConnectFourSceneBuilder.BuildConnectFourScene();
            HomeSceneBuilder.Build();
            InviteRoomSceneBuilder.Build();
            QueueSceneBuilder.Build();
            VotingSceneBuilder.Build();
            ResultSceneBuilder.Build();
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
                    "Assets/Scenes/Boot.unity",
                    "Assets/Scenes/ConnectFour.unity",
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
    }
}
