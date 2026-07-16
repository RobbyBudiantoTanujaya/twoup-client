using TwoUp.UI;
using UnityEngine;

namespace TwoUp.EditorTools
{
    /// <summary>Authors Lobby.unity: quick match / create-join room UI.</summary>
    public static class LobbySceneBuilder
    {
        private static readonly Vector2 Center = UiKit.Center;

        public static void BuildLobbyScene()
        {
            var scene = UiKit.NewScene();
            var screen = UiKit.CreateCanvasWithScreen("Screen_Lobby");

            var title = UiKit.CreateText(screen.transform, "Title", "2UP", 110, Color.white);
            UiKit.Place(title.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -180), new Vector2(600, 150));
            title.fontStyle = TMPro.FontStyles.Bold;

            var quick = UiKit.CreateButton(screen.transform, "QuickMatchButton", "Quick Match", new Vector2(700, 130), UiKit.ButtonBg);
            UiKit.Place(quick.gameObject, Center, Center, new Vector2(0, 430), new Vector2(700, 130));

            var create = UiKit.CreateButton(screen.transform, "CreateRoomButton", "Create Room", new Vector2(700, 130), UiKit.ButtonBg);
            UiKit.Place(create.gameObject, Center, Center, new Vector2(0, 270), new Vector2(700, 130));

            var input = UiKit.CreateInputField(screen.transform, "RoomCodeInput", "Room code", 8);
            UiKit.Place(input.gameObject, Center, Center, new Vector2(-160, 110), new Vector2(380, 110));

            var join = UiKit.CreateButton(screen.transform, "JoinRoomButton", "Join", new Vector2(280, 110), UiKit.ButtonBg);
            UiKit.Place(join.gameObject, Center, Center, new Vector2(190, 110), new Vector2(280, 110));

            var status = UiKit.CreateText(screen.transform, "StatusText", "", 40, new Color(0.8f, 0.84f, 0.92f));
            UiKit.Place(status.gameObject, Center, Center, new Vector2(0, -120), new Vector2(920, 220));

            var cancel = UiKit.CreateButton(screen.transform, "CancelQueueButton", "Cancel", new Vector2(500, 110), UiKit.ButtonMuted);
            UiKit.Place(cancel.gameObject, Center, Center, new Vector2(0, -320), new Vector2(500, 110));
            cancel.gameObject.SetActive(false);

            // Matched-with panel (inactive by default)
            var panel = UiKit.CreatePanel(screen.transform, "Panel_Matched", UiKit.PanelBg);
            UiKit.Place(panel, Center, Center, new Vector2(0, 60), new Vector2(860, 560));

            var matchedTitle = UiKit.CreateText(panel.transform, "MatchedTitle", "Match found!", 60, Color.white);
            UiKit.Place(matchedTitle.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -70), new Vector2(700, 90));

            var opponentName = UiKit.CreateText(panel.transform, "OpponentNameText", "???", 52, Color.white);
            UiKit.Place(opponentName.gameObject, Center, Center, new Vector2(0, 30), new Vector2(700, 80));

            var botBadge = UiKit.CreatePanel(panel.transform, "BotBadge", UiKit.BotBadgeBg);
            UiKit.Place(botBadge, Center, Center, new Vector2(0, -70), new Vector2(150, 64));
            var botLabel = UiKit.CreateText(botBadge.transform, "Label", "BOT", 38, Color.black);
            UiKit.StretchFull(botLabel.gameObject);
            botLabel.fontStyle = TMPro.FontStyles.Bold;
            botBadge.SetActive(false);

            var starting = UiKit.CreateText(panel.transform, "StartingText", "Starting game...", 36, new Color(0.8f, 0.84f, 0.92f));
            UiKit.Place(starting.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 60), new Vector2(700, 60));

            panel.SetActive(false);

            var controller = screen.AddComponent<LobbyController>();
            UiKit.SetRef(controller, "quickMatchButton", quick);
            UiKit.SetRef(controller, "createRoomButton", create);
            UiKit.SetRef(controller, "joinRoomButton", join);
            UiKit.SetRef(controller, "roomCodeInput", input);
            UiKit.SetRef(controller, "cancelQueueButton", cancel);
            UiKit.SetRef(controller, "statusText", status);
            UiKit.SetRef(controller, "matchedPanel", panel);
            UiKit.SetRef(controller, "opponentNameText", opponentName);
            UiKit.SetRef(controller, "botBadge", botBadge);

            UiKit.SaveScene(scene, "Lobby");
            UiKit.AddSceneToBuildSettings("Assets/Scenes/Lobby.unity");
        }
    }
}
