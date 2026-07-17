using TwoUp.UI;
using UnityEditor;
using UnityEngine;

namespace TwoUp.EditorTools
{
    /// <summary>Authors Queue.unity (S4): quick-match matchmaking wait, per AppStateMachine.ToQueue.</summary>
    public static class QueueSceneBuilder
    {
        private static readonly Vector2 Center = UiKit.Center;

        [MenuItem("2UP/Build Scenes/Queue")]
        public static void Build()
        {
            var scene = UiKit.OpenOrCreateScene("Queue");
            var screen = UiKit.CreateCanvasWithScreen("Screen_Queue");

            var status = UiKit.CreateText(screen.transform, "Text_Status", "Finding an opponent...", 52, Color.white);
            UiKit.Place(status.gameObject, Center, Center, new Vector2(0, 120), new Vector2(920, 140));

            var hint = UiKit.CreateText(screen.transform, "Text_Hint", "~10s - a bot joins if nobody is around", 34, new Color(0.8f, 0.84f, 0.92f));
            UiKit.Place(hint.gameObject, Center, Center, new Vector2(0, 0), new Vector2(880, 100));

            var cancel = UiKit.CreateButton(screen.transform, "Btn_Cancel", "Cancel", new Vector2(500, 110), UiKit.ButtonMuted);
            UiKit.Place(cancel.gameObject, Center, Center, new Vector2(0, -180), new Vector2(500, 110));

            var controller = IdempotentBuildUtil.GetOrAddComponent<QueueController>(screen);
            UiKit.SetRef(controller, "statusText", status);
            UiKit.SetRef(controller, "hintText", hint);
            UiKit.SetRef(controller, "cancelButton", cancel);

            UiKit.SaveScene(scene, "Queue");
            UiKit.AddSceneToBuildSettings("Assets/Scenes/Queue.unity");
        }
    }
}
