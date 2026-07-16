using TwoUp.Net;
using TwoUp.UI;
using UnityEditor;
using UnityEngine;

namespace TwoUp.EditorTools
{
    /// <summary>Authors Boot.unity: persistent App object + Screen_Boot UI.</summary>
    public static class BootSceneBuilder
    {
        private const string ServerConfigPath = "Assets/Config/ServerConfig.asset";
        private const string GameCatalogPath = "Assets/Config/GameCatalog.asset";
        private static readonly Vector2 Center = UiKit.Center;

        public static void BuildBootScene()
        {
            var scene = UiKit.NewScene();

            // Persistent App object: exists only here; DontDestroyOnLoad at runtime.
            var app = new GameObject("App");
            var net = app.AddComponent<NetworkClient>();
            var stateMachine = app.AddComponent<AppStateMachine>();
            app.AddComponent<DeepLinkRouter>();
            var pushTokenClient = app.AddComponent<PushTokenClient>();
            var config = AssetDatabase.LoadAssetAtPath<ServerConfig>(ServerConfigPath);
            UiKit.SetRef(net, "serverConfig", config);
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(GameCatalogPath);
            UiKit.SetRef(stateMachine, "catalog", catalog);

            var screen = UiKit.CreateCanvasWithScreen("Screen_Boot");

            var title = UiKit.CreateText(screen.transform, "Title", "2UP", 120, Color.white);
            UiKit.Place(title.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -220), new Vector2(600, 160));
            title.fontStyle = TMPro.FontStyles.Bold;

            var status = UiKit.CreateText(screen.transform, "StatusText", "Connecting...", 44, new Color(0.8f, 0.84f, 0.92f));
            UiKit.Place(status.gameObject, Center, Center, new Vector2(0, 40), new Vector2(900, 120));

            var retry = UiKit.CreateButton(screen.transform, "RetryButton", "Retry", new Vector2(500, 120), UiKit.ButtonBg);
            UiKit.Place(retry.gameObject, Center, Center, new Vector2(0, -140), new Vector2(500, 120));
            retry.gameObject.SetActive(false);

            var controller = screen.AddComponent<BootController>();
            UiKit.SetRef(controller, "statusText", status);
            UiKit.SetRef(controller, "retryButton", retry);
            UiKit.SetRef(controller, "pushTokenClient", pushTokenClient);

            UiKit.SaveScene(scene, "Boot");
            UiKit.AddSceneToBuildSettings("Assets/Scenes/Boot.unity");
        }
    }
}
