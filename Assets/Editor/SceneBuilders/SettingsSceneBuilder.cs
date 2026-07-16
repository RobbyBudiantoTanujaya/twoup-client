using TMPro;
using TwoUp.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TwoUp.EditorTools
{
    /// <summary>Authors Settings.unity (S10): sound/music/vibration toggles, restore purchase, links, version.</summary>
    public static class SettingsSceneBuilder
    {
        [MenuItem("2UP/Build Scenes/Settings")]
        public static void Build()
        {
            var scene = UiKit.NewScene();
            var screen = UiKit.CreateCanvasWithScreen("Screen_Settings");

            var back = UiKit.CreateButton(screen.transform, "Btn_Back", "Back", new Vector2(220, 90), UiKit.ButtonMuted);
            UiKit.Place(back.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40, -40), new Vector2(220, 90));

            var title = UiKit.CreateText(screen.transform, "Text_Title", "Settings", 52, Color.white);
            UiKit.Place(title.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(700, 80));

            var soundToggle = UiKit.CreateToggle(screen.transform, "Toggle_Sound", "Sound");
            UiKit.Place(soundToggle.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -220), new Vector2(700, 90));

            var musicToggle = UiKit.CreateToggle(screen.transform, "Toggle_Music", "Music");
            UiKit.Place(musicToggle.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -330), new Vector2(700, 90));

            var vibrationToggle = UiKit.CreateToggle(screen.transform, "Toggle_Vibration", "Vibration");
            UiKit.Place(vibrationToggle.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -440), new Vector2(700, 90));

            var restorePurchaseButton = UiKit.CreateButton(screen.transform, "Btn_RestorePurchase", "Restore Purchase", new Vector2(600, 100), UiKit.ButtonBg);
            UiKit.Place(restorePurchaseButton.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -580), new Vector2(600, 100));

            var toastText = UiKit.CreateText(screen.transform, "Text_Toast", "", 34, Color.white);
            UiKit.Place(toastText.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -700), new Vector2(800, 70));
            toastText.gameObject.SetActive(false);

            var linksText = UiKit.CreateText(screen.transform, "Text_Links", "Privacy Policy - Terms", 30, new Color(0.8f, 0.84f, 0.92f));
            UiKit.Place(linksText.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 130), new Vector2(800, 60));

            var versionText = UiKit.CreateText(screen.transform, "Text_Version", "", 26, new Color(0.6f, 0.64f, 0.72f));
            UiKit.Place(versionText.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 60), new Vector2(400, 50));

            var controller = screen.AddComponent<SettingsController>();
            UiKit.SetRef(controller, "backButton", back);
            UiKit.SetRef(controller, "soundToggle", soundToggle);
            UiKit.SetRef(controller, "musicToggle", musicToggle);
            UiKit.SetRef(controller, "vibrationToggle", vibrationToggle);
            UiKit.SetRef(controller, "restorePurchaseButton", restorePurchaseButton);
            UiKit.SetRef(controller, "linksText", linksText);
            UiKit.SetRef(controller, "versionText", versionText);
            UiKit.SetRef(controller, "toastText", toastText);

            UiKit.SaveScene(scene, "Settings");
            UiKit.AddSceneToBuildSettings("Assets/Scenes/Settings.unity");
        }
    }
}
