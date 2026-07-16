using NUnit.Framework;
using TMPro;
using TwoUp.UI;

namespace TwoUp.Tests.EditMode.Scenes
{
    public class InviteSceneTests
    {
        [Test]
        public void Invite_HasCreateAndJoinPanels()
        {
            SceneAsserts.OpenScene("Assets/Scenes/Invite.unity");

            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Invite/Btn_Back");
            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Invite/Panel_CreateRoom/Text_RoomCode");
            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Invite/Panel_CreateRoom/Text_TtlCountdown");
            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Invite/Panel_CreateRoom/Btn_CopyLink");
            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Invite/Panel_CreateRoom/Text_WaitingOpponent");
            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Invite/Panel_JoinRoom/Input_RoomCode");
            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Invite/Panel_JoinRoom/Btn_Join");
            SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Invite/Text_Toast");

            var screen = SceneAsserts.AssertObject("UICanvas/Screen_Invite");
            var controller = screen.GetComponent<InviteRoomController>();
            Assert.IsNotNull(controller, "Screen_Invite is missing InviteRoomController");

            SceneAsserts.AssertRefNotNull(controller, "backButton");
            SceneAsserts.AssertRefNotNull(controller, "roomCodeText");
            SceneAsserts.AssertRefNotNull(controller, "ttlCountdownText");
            SceneAsserts.AssertRefNotNull(controller, "copyLinkButton");
            SceneAsserts.AssertRefNotNull(controller, "waitingOpponentText");
            SceneAsserts.AssertRefNotNull(controller, "roomCodeInput");
            SceneAsserts.AssertRefNotNull(controller, "joinButton");
            SceneAsserts.AssertRefNotNull(controller, "toastText");
        }

        [Test]
        public void Invite_InputCharLimitSix()
        {
            SceneAsserts.OpenScene("Assets/Scenes/Invite.unity");

            var input = SceneAsserts.AssertObjectIncludingInactive("UICanvas/Screen_Invite/Panel_JoinRoom/Input_RoomCode");
            var field = input.GetComponent<TMP_InputField>();
            Assert.IsNotNull(field, "Input_RoomCode is missing TMP_InputField");
            Assert.AreEqual(6, field.characterLimit);
        }
    }
}
