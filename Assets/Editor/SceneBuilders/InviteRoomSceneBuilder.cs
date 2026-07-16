using TwoUp.UI;
using UnityEditor;
using UnityEngine;

namespace TwoUp.EditorTools
{
    /// <summary>Authors Invite.unity (S3): create-or-join a private room, per AppStateMachine.ToInvite.</summary>
    public static class InviteRoomSceneBuilder
    {
        private static readonly Vector2 Center = UiKit.Center;

        [MenuItem("2UP/Build Scenes/Invite")]
        public static void Build()
        {
            var scene = UiKit.NewScene();
            var screen = UiKit.CreateCanvasWithScreen("Screen_Invite");

            var back = UiKit.CreateButton(screen.transform, "Btn_Back", "Back", new Vector2(180, 90), UiKit.ButtonMuted);
            UiKit.Place(back.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24, -24), new Vector2(180, 90));

            // --- Panel_CreateRoom (top): room code, TTL countdown, copy-link, waiting-for-opponent.
            var createPanel = UiKit.CreatePanel(screen.transform, "Panel_CreateRoom", UiKit.PanelBg);
            UiKit.Place(createPanel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -160), new Vector2(920, 620));

            var roomCode = UiKit.CreateText(createPanel.transform, "Text_RoomCode", "------", 90, Color.white);
            UiKit.Place(roomCode.gameObject, Center, Center, new Vector2(0, 190), new Vector2(760, 140));
            roomCode.fontStyle = TMPro.FontStyles.Bold;

            var ttlCountdown = UiKit.CreateText(createPanel.transform, "Text_TtlCountdown", "00:00", 36, new Color(0.8f, 0.84f, 0.92f));
            UiKit.Place(ttlCountdown.gameObject, Center, Center, new Vector2(0, 70), new Vector2(400, 60));

            var copyLink = UiKit.CreateButton(createPanel.transform, "Btn_CopyLink", "Copy invite link", new Vector2(640, 110), UiKit.ButtonBg);
            UiKit.Place(copyLink.gameObject, Center, Center, new Vector2(0, -60), new Vector2(640, 110));

            var waitingOpponent = UiKit.CreateText(createPanel.transform, "Text_WaitingOpponent", "Waiting for opponent...", 32, new Color(0.8f, 0.84f, 0.92f));
            UiKit.Place(waitingOpponent.gameObject, Center, Center, new Vector2(0, -190), new Vector2(760, 60));

            // --- Panel_JoinRoom (bottom): enter a friend's room code.
            var joinPanel = UiKit.CreatePanel(screen.transform, "Panel_JoinRoom", UiKit.PanelBg);
            UiKit.Place(joinPanel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 160), new Vector2(920, 340));

            var roomCodeInput = UiKit.CreateInputField(joinPanel.transform, "Input_RoomCode", "Room code", 6);
            UiKit.Place(roomCodeInput, Center, Center, new Vector2(0, 70), new Vector2(600, 110));

            var join = UiKit.CreateButton(joinPanel.transform, "Btn_Join", "Join", new Vector2(400, 110), UiKit.ButtonBg);
            UiKit.Place(join.gameObject, Center, Center, new Vector2(0, -70), new Vector2(400, 110));

            // --- Toast (inactive by default).
            var toast = UiKit.CreateText(screen.transform, "Text_Toast", "", 34, Color.white);
            UiKit.Place(toast.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(920, 80));
            toast.gameObject.SetActive(false);

            var controller = screen.AddComponent<InviteRoomController>();
            UiKit.SetRef(controller, "backButton", back);
            UiKit.SetRef(controller, "roomCodeText", roomCode);
            UiKit.SetRef(controller, "ttlCountdownText", ttlCountdown);
            UiKit.SetRef(controller, "copyLinkButton", copyLink);
            UiKit.SetRef(controller, "waitingOpponentText", waitingOpponent);
            UiKit.SetRef(controller, "roomCodeInput", roomCodeInput);
            UiKit.SetRef(controller, "joinButton", join);
            UiKit.SetRef(controller, "toastText", toast);

            UiKit.SaveScene(scene, "Invite");
            UiKit.AddSceneToBuildSettings("Assets/Scenes/Invite.unity");
        }
    }
}
